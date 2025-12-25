using DocAttestation.Data;
using DocAttestation.Models;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocAttestation.Controllers;

[Authorize(Policy = "OfficerOnly")]
public class WorkflowController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowService _workflowService;
    private readonly IApplicationService _applicationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WorkflowController> _logger;
    private readonly IQRCodeService _qrCodeService;

    public WorkflowController(
        ApplicationDbContext context,
        IWorkflowService workflowService,
        IApplicationService applicationService,
        UserManager<ApplicationUser> userManager,
        ILogger<WorkflowController> logger,
        IQRCodeService qrCodeService)
    {
        _context = context;
        _workflowService = workflowService;
        _applicationService = applicationService;
        _userManager = userManager;
        _logger = logger;
        _qrCodeService = qrCodeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var pendingSteps = await _workflowService.GetPendingStepsForUserAsync(userId!);
        
        // For Attestation Officers, also get recently approved applications (last 24 hours)
        var user = await _userManager.FindByIdAsync(userId!);
        List<WorkflowStep> recentlyApproved = new();
        if (user != null)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("AttestationOfficer"))
            {
                recentlyApproved = await _workflowService.GetRecentlyApprovedAttestationStepsAsync(userId!, 24);
            }
        }

        ViewBag.PendingSteps = pendingSteps;
        ViewBag.RecentlyApproved = recentlyApproved;
        
        return View(pendingSteps);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var application = await _applicationService.GetApplicationByIdAsync(id);
        if (application == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return Forbid();

        var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
        var currentStep = await _workflowService.GetCurrentStepAsync(id);
        
        // If no pending step, check if this is a recently approved application by Attestation Officer
        if (currentStep == null)
        {
            // Allow access if user is AttestationOfficer and application is approved
            if (userRoles.Contains("AttestationOfficer") && application.Status == ApplicationStatus.Approved)
            {
                // Check if user approved this application recently (last 24 hours)
                var recentlyApproved = await _workflowService.GetRecentlyApprovedAttestationStepsAsync(userId!, 24);
                var userApprovedThis = recentlyApproved.Any(s => s.ApplicationId == id);
                
                if (userApprovedThis)
                {
                    // Get the completed attestation step for this application
                    var completedStep = await _context.WorkflowSteps
                        .Include(s => s.AssignedToUser)
                        .Where(s => s.ApplicationId == id && 
                               s.Level == WorkflowLevel.Attestation && 
                               s.Status == WorkflowStepStatus.Approved &&
                               s.AssignedToUserId == userId)
                        .OrderByDescending(s => s.CompletedAt)
                        .FirstOrDefaultAsync();
                    
                    ViewBag.CurrentStep = completedStep;
                    ViewBag.IsApproved = true;
                    return View(application);
                }
            }
            
            return BadRequest("No pending step for this application");
        }

        // For pending applications, verify assignment
        if (currentStep.AssignedToUserId != userId)
            return Forbid();

        // Verify user's role matches the workflow level
        if (!IsRoleAllowedForLevel(userRoles, currentStep.Level))
        {
            TempData["Error"] = "You are not authorized to review applications at this level.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.CurrentStep = currentStep;
        ViewBag.IsApproved = false;
        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int applicationId, string? remarks = null)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            var success = await _workflowService.ApproveAsync(applicationId, userId!, remarks, userRoles);

            if (success)
            {
                return Json(new { success = true, message = "Application approved successfully" });
            }

            return Json(new { success = false, message = "Failed to approve application. You may not have permission to update this application." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving application");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int applicationId, string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return Json(new { success = false, message = "Remarks are required for rejection" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            var success = await _workflowService.RejectAsync(applicationId, userId!, remarks, userRoles);

            if (success)
            {
                return Json(new { success = true, message = "Application rejected" });
            }

            return Json(new { success = false, message = "Failed to reject application. You may not have permission to update this application." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting application");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ViewDocument(int applicationId)
    {
        var application = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (application == null)
            return NotFound();

        // Verify officer has access to this application
        var currentStep = await _workflowService.GetCurrentStepAsync(applicationId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Allow if assigned to current step or if in workflow history
        if (currentStep != null && currentStep.AssignedToUserId != userId)
        {
            // Check if user has reviewed this application before
            var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
            if (!hasHistory)
                return Forbid();
        }

        if (!string.IsNullOrEmpty(application.OriginalDocumentPath) && System.IO.File.Exists(application.OriginalDocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(application.OriginalDocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Document not found");
    }

    [HttpGet]
    public async Task<IActionResult> ViewStampedDocument(int applicationId)
    {
        var application = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (application == null)
            return NotFound();

        // Verify officer has access to this application
        var currentStep = await _workflowService.GetCurrentStepAsync(applicationId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Allow if assigned to current step or if in workflow history
        if (currentStep != null && currentStep.AssignedToUserId != userId)
        {
            // Check if user has reviewed this application before
            var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
            if (!hasHistory)
                return Forbid();
        }

        if (!string.IsNullOrEmpty(application.StampedDocumentPath) && System.IO.File.Exists(application.StampedDocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(application.StampedDocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Stamped document not found");
    }

    [HttpGet]
    public async Task<IActionResult> ViewApplicationDocument(int documentId)
    {
        var document = await _context.ApplicationDocuments
            .Include(d => d.Application)
            .ThenInclude(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return NotFound();

        var application = document.Application;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Verify officer has access to this application
        var currentStep = await _workflowService.GetCurrentStepAsync(application.Id);
        
        // Allow if assigned to current step or if in workflow history
        if (currentStep != null && currentStep.AssignedToUserId != userId)
        {
            // Check if user has reviewed this application before
            var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
            if (!hasHistory)
                return Forbid();
        }

        if (System.IO.File.Exists(document.DocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.DocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Document not found");
    }

    [HttpGet]
    public async Task<IActionResult> ViewStampedApplicationDocument(int documentId)
    {
        var document = await _context.ApplicationDocuments
            .Include(d => d.Application)
            .ThenInclude(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return NotFound();

        var application = document.Application;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Verify officer has access to this application
        var currentStep = await _workflowService.GetCurrentStepAsync(application.Id);
        
        // Allow if assigned to current step or if in workflow history
        if (currentStep != null && currentStep.AssignedToUserId != userId)
        {
            // Check if user has reviewed this application before
            var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
            if (!hasHistory)
                return Forbid();
        }

        if (!string.IsNullOrEmpty(document.StampedDocumentPath) && System.IO.File.Exists(document.StampedDocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.StampedDocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Stamped document not found");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyDocument(int documentId, string action, string? remarks)
    {
        try
        {
            var document = await _context.ApplicationDocuments
                .Include(d => d.Application)
                .ThenInclude(a => a.WorkflowSteps)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return Json(new { success = false, message = "Document not found" });

            var application = document.Application;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Verify officer has access to this application
            var currentStep = await _workflowService.GetCurrentStepAsync(application.Id);
            
            // Allow if assigned to current step or if in workflow history
            if (currentStep != null && currentStep.AssignedToUserId != userId)
            {
                // Check if user has reviewed this application before
                var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
                if (!hasHistory)
                    return Json(new { success = false, message = "You do not have permission to verify this document" });
            }

            // Validate action
            DocumentStatus newStatus;
            switch (action.ToLower())
            {
                case "approve":
                    newStatus = DocumentStatus.Approved;
                    break;
                case "reject":
                    if (string.IsNullOrWhiteSpace(remarks))
                        return Json(new { success = false, message = "Remarks are required for rejection" });
                    newStatus = DocumentStatus.Rejected;
                    break;
                case "sendback":
                    if (string.IsNullOrWhiteSpace(remarks))
                        return Json(new { success = false, message = "Remarks are required for sending back" });
                    newStatus = DocumentStatus.SentBack;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid action" });
            }

            // Update document status
            document.Status = newStatus;
            document.VerifiedByUserId = userId;
            document.VerifiedAt = DateTime.UtcNow;
            document.VerificationRemarks = remarks;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Document {DocumentId} verified as {Status} by user {UserId}", documentId, newStatus, userId);

            return Json(new { success = true, message = $"Document {newStatus.ToString().ToLower()} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document {DocumentId}", documentId);
            return Json(new { success = false, message = "An error occurred while verifying the document" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PrintQRCode(int applicationId)
    {
        var application = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (application == null)
            return NotFound();

        // Only allow if application is approved and has QR token
        if (application.Status != ApplicationStatus.Approved || string.IsNullOrEmpty(application.QRToken))
        {
            return BadRequest("QR Code is only available for approved applications");
        }

        // Verify officer has access (must be AttestationOfficer who approved it)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return Forbid();

        var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
        if (!userRoles.Contains("AttestationOfficer"))
        {
            // Check if user has reviewed this application before
            var hasHistory = application.WorkflowHistory?.Any(h => h.ActionByUserId == userId) ?? false;
            if (!hasHistory)
                return Forbid();
        }

        // Generate QR code image
        var qrCodeBase64 = _qrCodeService.GenerateQRCodeImage(application.QRToken);
        ViewBag.QRCodeBase64 = qrCodeBase64;

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendBack(int applicationId, string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return Json(new { success = false, message = "Remarks are required when sending back" });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            var userRoles = (await _userManager.GetRolesAsync(user)).ToList();
            var success = await _workflowService.SendBackAsync(applicationId, userId!, remarks, userRoles);

            if (success)
            {
                return Json(new { success = true, message = "Application sent back" });
            }

            return Json(new { success = false, message = "Failed to send back application. You may not have permission to update this application." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending back application");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    /// <summary>
    /// Checks if the user's role is allowed to work on the specified workflow level
    /// </summary>
    private static bool IsRoleAllowedForLevel(List<string> userRoles, WorkflowLevel level)
    {
        return level switch
        {
            WorkflowLevel.Verification => userRoles.Contains("VerificationOfficer"),
            WorkflowLevel.Supervision => userRoles.Contains("Supervisor"),
            WorkflowLevel.Attestation => userRoles.Contains("AttestationOfficer"),
            _ => false
        };
    }
}

