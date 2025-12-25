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

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWorkflowService workflowService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        var usersWithRoles = new List<dynamic>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new
            {
                User = user,
                Roles = roles
            });
        }

        ViewBag.UsersWithRoles = usersWithRoles;
        return View();
    }

    [HttpGet]
    public IActionResult CreateOfficer()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOfficer(CreateOfficerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "User with this email already exists.");
                return View(model);
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, model.Role);

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating officer");
            ModelState.AddModelError("", "An error occurred while creating the officer account.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Applications()
    {
        var applications = await _context.Applications
            .Include(a => a.ApplicantProfile)
            .Include(a => a.WorkflowSteps)
                .ThenInclude(s => s.AssignedToUser)
            .Include(a => a.Payments)
            .Where(a => a.Status != ApplicationStatus.Draft && 
                   a.Payments.Any(p => p.Status == PaymentStatus.Paid))
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> AssignApplication(int id)
    {
        var application = await _context.Applications
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return NotFound();

        var currentStep = await _workflowService.GetCurrentStepAsync(id);
        ViewBag.CurrentStep = currentStep;

        // Get officers based on what level needs assignment
        WorkflowLevel? levelToAssign = null;
        if (application.Status == ApplicationStatus.Submitted)
        {
            levelToAssign = WorkflowLevel.Verification;
        }
        else if (application.Status == ApplicationStatus.UnderSupervision)
        {
            levelToAssign = WorkflowLevel.Supervision;
        }
        else if (application.Status == ApplicationStatus.UnderAttestation)
        {
            levelToAssign = WorkflowLevel.Attestation;
        }

        ViewBag.LevelToAssign = levelToAssign;

        // Get officers for the required level
        var officers = new List<ApplicationUser>();
        if (levelToAssign == WorkflowLevel.Verification)
        {
            var verificationOfficers = await _userManager.GetUsersInRoleAsync("VerificationOfficer");
            officers = verificationOfficers.Where(u => u.IsActive).ToList();
        }
        else if (levelToAssign == WorkflowLevel.Supervision)
        {
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            officers = supervisors.Where(u => u.IsActive).ToList();
        }
        else if (levelToAssign == WorkflowLevel.Attestation)
        {
            var attestationOfficers = await _userManager.GetUsersInRoleAsync("AttestationOfficer");
            officers = attestationOfficers.Where(u => u.IsActive).ToList();
        }

        ViewBag.Officers = officers;
        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignApplication(int id, string officerId)
    {
        if (string.IsNullOrEmpty(officerId))
        {
            TempData["Error"] = "Please select an officer to assign.";
            return RedirectToAction("AssignApplication", new { id });
        }

        try
        {
            var application = await _context.Applications
                .Include(a => a.WorkflowSteps)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return NotFound();

            // Determine which level to assign
            WorkflowLevel level;
            if (application.Status == ApplicationStatus.Submitted)
            {
                level = WorkflowLevel.Verification;
                application.Status = ApplicationStatus.UnderVerification;
            }
            else if (application.Status == ApplicationStatus.UnderSupervision)
            {
                level = WorkflowLevel.Supervision;
            }
            else if (application.Status == ApplicationStatus.UnderAttestation)
            {
                level = WorkflowLevel.Attestation;
            }
            else
            {
                TempData["Error"] = "Application is not in a state that requires assignment.";
                return RedirectToAction("Applications");
            }

            // Assign to officer
            var success = await _workflowService.AssignToLevelAsync(id, level, officerId);
            
            if (success)
            {
                // Update application status if needed
                if (application.Status == ApplicationStatus.Submitted)
                {
                    application.Status = ApplicationStatus.UnderVerification;
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "Application assigned successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to assign application.";
            }

            return RedirectToAction("Applications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning application");
            TempData["Error"] = "An error occurred while assigning the application.";
            return RedirectToAction("Applications");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ViewApplicationDetails(int id)
    {
        var application = await _context.Applications
            .Include(a => a.ApplicantProfile)
            .Include(a => a.WorkflowSteps)
                .ThenInclude(s => s.AssignedToUser)
            .Include(a => a.WorkflowHistory)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null)
            return NotFound();

        return View("~/Views/Application/Details.cshtml", application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoAssignApplications()
    {
        try
        {
            var assignedCount = await _workflowService.AutoAssignApplicationsAsync();
            if (assignedCount > 0)
            {
                TempData["Success"] = $"{assignedCount} applications auto-assigned successfully";
            }
            else
            {
                TempData["Info"] = "No applications available for auto-assignment";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning applications");
            TempData["Error"] = "An error occurred while auto-assigning applications";
        }
        
        return RedirectToAction(nameof(Applications));
    }
}

