using DocAttestation.Data;
using DocAttestation.Models;
using DocAttestation.Services;
using DocAttestation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocAttestation.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly IJwtService _jwtService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IEncryptionService encryptionService,
        IJwtService jwtService,
        ICaptchaService captchaService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _encryptionService = encryptionService;
        _jwtService = jwtService;
        _captchaService = captchaService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        var challenge = _captchaService.GenerateSliderChallenge();
        var model = new RegisterViewModel
        {
            CaptchaChallengeId = challenge.ChallengeId,
            CaptchaBackgroundImage = challenge.BackgroundImage,
            CaptchaPuzzleImage = challenge.PuzzlePieceImage,
            CaptchaPuzzleY = challenge.PuzzleY
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Validate Slider CAPTCHA
        if (!int.TryParse(model.CaptchaAnswer, out var sliderPosition) || 
            !_captchaService.ValidateSliderChallenge(model.CaptchaChallengeId, sliderPosition))
        {
            return Json(new
            {
                success = false,
                message = "Slider verification failed. Please try again.",
                title = "Verification Error"
            });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            return Json(new
            {
                success = false,
                message = string.Join(" ", errors),
                title = "Validation Error"
            });
        }

        try
        {
            // Clean CNIC (remove dashes)
            var cleanCNIC = model.CNIC.Replace("-", "").Trim();
            
            if (cleanCNIC.Length != 13)
            {
                return Json(new
                {
                    success = false,
                    message = "CNIC must be exactly 13 digits",
                    title = "Validation Error"
                });
            }

            // Check if CNIC already exists (using hash)
            var cnicHash = _encryptionService.ComputeHash(cleanCNIC);
            var existingProfile = await _context.ApplicantProfiles
                .FirstOrDefaultAsync(p => p.CNICHash == cnicHash);

            if (existingProfile != null)
            {
                // CNIC already registered - return JSON for SweetAlert
                return Json(new
                {
                    success = false,
                    message = "CNIC already registered. Please login.",
                    title = "Registration Failed"
                });
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.MobileNumber,
                FullName = "",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description).ToList();
                return Json(new
                {
                    success = false,
                    message = string.Join(" ", errorMessages),
                    title = "Registration Failed"
                });
            }

            // Encrypt and store CNIC
            var encryptedCNIC = _encryptionService.Encrypt(cleanCNIC);

            // Create applicant profile
            // Note: Personal info fields are set to empty/default values as profile will be completed in steps
            var profile = new ApplicantProfile
            {
                UserId = user.Id,
                EncryptedCNIC = encryptedCNIC,
                CNICHash = cnicHash,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                FullName = "", // Will be filled in Step 1
                FatherName = "", // Will be filled in Step 1
                DateOfBirth = DateTime.UtcNow, // Will be updated in Step 1
                Gender = "", // Will be filled in Step 1
                Address = "", // Will be filled in Step 2
                CurrentStep = 0,
                IsProfileComplete = false
            };

            _context.ApplicantProfiles.Add(profile);
            await _context.SaveChangesAsync();

            // Assign Applicant role
            await _userManager.AddToRoleAsync(user, "Applicant");

            // Refresh user's security stamp to ensure claims are updated
            await _userManager.UpdateSecurityStampAsync(user);

            // Generate JWT
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            // Sign in user (this will include the updated role claims)
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Store tokens in cookies (secure, httpOnly)
            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Json(new
            {
                success = true,
                message = "Registration successful! Please complete your profile.",
                redirectUrl = Url.Action("Step1", "Profile")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return Json(new
            {
                success = false,
                message = "An error occurred during registration. Please try again.",
                title = "Registration Failed"
            });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var challenge = _captchaService.GenerateSliderChallenge();
        var model = new LoginViewModel
        {
            CaptchaChallengeId = challenge.ChallengeId,
            CaptchaBackgroundImage = challenge.BackgroundImage,
            CaptchaPuzzleImage = challenge.PuzzlePieceImage,
            CaptchaPuzzleY = challenge.PuzzleY
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        // Helper to regenerate captcha
        void RegenerateCaptcha()
        {
            var challenge = _captchaService.GenerateSliderChallenge();
            model.CaptchaChallengeId = challenge.ChallengeId;
            model.CaptchaBackgroundImage = challenge.BackgroundImage;
            model.CaptchaPuzzleImage = challenge.PuzzlePieceImage;
            model.CaptchaPuzzleY = challenge.PuzzleY;
        }

        // Validate Slider CAPTCHA
        if (!int.TryParse(model.CaptchaAnswer, out var sliderPosition) || 
            !_captchaService.ValidateSliderChallenge(model.CaptchaChallengeId, sliderPosition))
        {
            ModelState.AddModelError("", "Slider verification failed. Please try again.");
            RegenerateCaptcha();
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            RegenerateCaptcha();
            return View(model);
        }

        try
        {
            // Try to find user by email or CNIC
            ApplicationUser? user = null;

            // First try by email
            user = await _userManager.FindByEmailAsync(model.Login);

            // If not found, try by CNIC (search in profiles)
            if (user == null)
            {
                var cnicHash = _encryptionService.ComputeHash(model.Login.Replace("-", "").Trim());
                var profile = await _context.ApplicantProfiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.CNICHash == cnicHash);

                if (profile != null)
                {
                    user = profile.User;
                }
            }

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                RegenerateCaptcha();
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate JWT
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _jwtService.GenerateAccessToken(user, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();
                await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

                // Store tokens
                Response.Cookies.Append("accessToken", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(15)
                });

                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Redirect based on role
                if (await _userManager.IsInRoleAsync(user, "Applicant"))
                {
                    return RedirectToAction("Index", "Profile");
                }
                else if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Workflow");
                }
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked out. Please try again later.");
                RegenerateCaptcha();
                return View(model);
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            RegenerateCaptcha();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ModelState.AddModelError("", "An error occurred during login. Please try again.");
            RegenerateCaptcha();
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetCaptcha()
    {
        var challenge = _captchaService.GenerateSliderChallenge();
        return Json(new
        {
            challengeId = challenge.ChallengeId,
            backgroundImage = challenge.BackgroundImage,
            puzzleImage = challenge.PuzzlePieceImage,
            puzzleY = challenge.PuzzleY
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _jwtService.RevokeRefreshTokenAsync(refreshToken);
            }
        }

        await _signInManager.SignOutAsync();
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

        return RedirectToAction("Index", "Home");
    }
}

