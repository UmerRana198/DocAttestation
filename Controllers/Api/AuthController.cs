using DocAttestation.Models;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocAttestation.Controllers.Api;

/// <summary>
/// API Authentication endpoints for mobile app
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Login for mobile app - returns JWT tokens
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new MobileLoginResponse
            {
                Success = false,
                Message = "Email and password are required"
            });
        }

        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Mobile login failed: User not found or inactive - {Email}", request.Email);
                return Unauthorized(new MobileLoginResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }

            // Check if user is an officer (not just an applicant)
            var roles = await _userManager.GetRolesAsync(user);
            var isOfficer = roles.Any(r => r is "VerificationOfficer" or "Supervisor" or "AttestationOfficer" or "Admin");
            
            if (!isOfficer)
            {
                _logger.LogWarning("Mobile login denied: User is not an officer - {Email}", request.Email);
                return Unauthorized(new MobileLoginResponse
                {
                    Success = false,
                    Message = "Only authorized officers can use the mobile app"
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return Unauthorized(new MobileLoginResponse
                    {
                        Success = false,
                        Message = "Account locked. Please try again later."
                    });
                }

                _logger.LogWarning("Mobile login failed: Invalid password - {Email}", request.Email);
                return Unauthorized(new MobileLoginResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            _logger.LogInformation("Mobile login successful: {Email}", request.Email);

            return Ok(new MobileLoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = accessToken,
                RefreshToken = refreshToken,
                Expiry = DateTime.UtcNow.AddMinutes(15),
                UserName = user.FullName ?? user.Email,
                Roles = roles.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile login error for {Email}", request.Email);
            return StatusCode(500, new MobileLoginResponse
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new MobileLoginResponse
            {
                Success = false,
                Message = "Refresh token is required"
            });
        }

        try
        {
            var storedToken = await _jwtService.GetRefreshTokenAsync(request.RefreshToken);
            
            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new MobileLoginResponse
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                });
            }

            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new MobileLoginResponse
                {
                    Success = false,
                    Message = "User not found or inactive"
                });
            }

            var roles = await _userManager.GetRolesAsync(user);
            
            // Revoke old token and generate new ones
            await _jwtService.RevokeRefreshTokenAsync(request.RefreshToken);
            
            var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            await _jwtService.SaveRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));

            return Ok(new MobileLoginResponse
            {
                Success = true,
                Message = "Token refreshed",
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Expiry = DateTime.UtcNow.AddMinutes(15)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new MobileLoginResponse
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }
}

public class MobileLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MobileLoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? Expiry { get; set; }
    public string? UserName { get; set; }
    public List<string>? Roles { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

