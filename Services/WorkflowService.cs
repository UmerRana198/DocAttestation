using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocAttestation.Services;

public class WorkflowService : IWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IPdfStampingService _pdfStampingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        ApplicationDbContext context, 
        IAuditService auditService,
        IQRCodeService qrCodeService,
        IPdfStampingService pdfStampingService,
        UserManager<ApplicationUser> userManager,
        ILogger<WorkflowService> logger)
    {
        _context = context;
        _auditService = auditService;
        _qrCodeService = qrCodeService;
        _pdfStampingService = pdfStampingService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> AssignToLevelAsync(int applicationId, WorkflowLevel level, string assignedToUserId)
    {
        var application = await _context.Applications
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            return false;

        // Check if step already exists
        var existingStep = application.WorkflowSteps
            .FirstOrDefault(s => s.Level == level);

        if (existingStep == null)
        {
            var step = new WorkflowStep
            {
                ApplicationId = applicationId,
                Level = level,
                AssignedToUserId = assignedToUserId,
                Status = WorkflowStepStatus.Pending,
                AssignedAt = DateTime.UtcNow
            };

            _context.WorkflowSteps.Add(step);
        }
        else
        {
            existingStep.AssignedToUserId = assignedToUserId;
            existingStep.Status = WorkflowStepStatus.Pending;
            existingStep.AssignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveAsync(int applicationId, string userId, string? remarks = null, List<string>? userRoles = null)
    {
        var application = await _context.Applications
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            return false;

        var currentStep = await GetCurrentStepAsync(applicationId);
        if (currentStep == null || currentStep.AssignedToUserId != userId)
            return false;

        // Verify user's role matches the workflow level
        if (userRoles != null && !IsRoleAllowedForLevel(userRoles, currentStep.Level))
            return false;

        var previousStatus = application.Status;

        // Update step status
        currentStep.Status = WorkflowStepStatus.Approved;
        currentStep.CompletedAt = DateTime.UtcNow;
        currentStep.Remarks = remarks;

        // Move to next level or complete
        switch (currentStep.Level)
        {
            case WorkflowLevel.Verification:
                application.Status = ApplicationStatus.UnderSupervision;
                // Auto-assign to an available Supervisor
                var supervisor = await GetAvailableOfficerAsync("Supervisor");
                if (supervisor != null)
                {
                    await AssignToLevelAsync(applicationId, WorkflowLevel.Supervision, supervisor.Id);
                    _logger.LogInformation("Application {AppId} auto-assigned to Supervisor {SupervisorId}", applicationId, supervisor.Id);
                }
                else
                {
                    _logger.LogWarning("No available Supervisor found for application {AppId}", applicationId);
                }
                break;
            case WorkflowLevel.Supervision:
                application.Status = ApplicationStatus.UnderAttestation;
                // Auto-assign to an available Attestation Officer
                var attestationOfficer = await GetAvailableOfficerAsync("AttestationOfficer");
                if (attestationOfficer != null)
                {
                    await AssignToLevelAsync(applicationId, WorkflowLevel.Attestation, attestationOfficer.Id);
                    _logger.LogInformation("Application {AppId} auto-assigned to AttestationOfficer {OfficerId}", applicationId, attestationOfficer.Id);
                }
                else
                {
                    _logger.LogWarning("No available AttestationOfficer found for application {AppId}", applicationId);
                }
                break;
            case WorkflowLevel.Attestation:
                application.Status = ApplicationStatus.Approved;
                application.AttestedAt = DateTime.UtcNow;
                
                // Generate QR token and stamp PDF only on final attestation
                var qrToken = await _qrCodeService.GenerateQRTokenAsync(applicationId, application.DocumentHash);
                var qrCodeBase64 = _qrCodeService.GenerateQRCodeImage(qrToken);
                
                // Stamp PDF with QR code
                var stampedPath = application.OriginalDocumentPath.Replace(".pdf", "_stamped.pdf");
                await _pdfStampingService.StampPdfAsync(
                    application.OriginalDocumentPath,
                    qrCodeBase64,
                    stampedPath);
                
                // Compute hash of stamped document
                var stampedHash = _pdfStampingService.ComputeFileHash(stampedPath);
                
                application.StampedDocumentPath = stampedPath;
                application.StampedDocumentHash = stampedHash;
                application.QRToken = qrToken;
                break;
        }

        // Create audit trail
        await _auditService.LogWorkflowActionAsync(
            applicationId,
            currentStep.Level,
            userId,
            "Approved",
            remarks,
            previousStatus,
            application.Status);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int applicationId, string userId, string remarks, List<string>? userRoles = null)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ArgumentException("Remarks are required for rejection");

        var application = await _context.Applications
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            return false;

        var currentStep = await GetCurrentStepAsync(applicationId);
        if (currentStep == null || currentStep.AssignedToUserId != userId)
            return false;

        // Verify user's role matches the workflow level
        if (userRoles != null && !IsRoleAllowedForLevel(userRoles, currentStep.Level))
            return false;

        var previousStatus = application.Status;

        currentStep.Status = WorkflowStepStatus.Rejected;
        currentStep.CompletedAt = DateTime.UtcNow;
        currentStep.Remarks = remarks;

        application.Status = ApplicationStatus.Rejected;

        await _auditService.LogWorkflowActionAsync(
            applicationId,
            currentStep.Level,
            userId,
            "Rejected",
            remarks,
            previousStatus,
            application.Status);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendBackAsync(int applicationId, string userId, string remarks, List<string>? userRoles = null)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ArgumentException("Remarks are required when sending back");

        var application = await _context.Applications
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            return false;

        var currentStep = await GetCurrentStepAsync(applicationId);
        if (currentStep == null || currentStep.AssignedToUserId != userId)
            return false;

        // Verify user's role matches the workflow level
        if (userRoles != null && !IsRoleAllowedForLevel(userRoles, currentStep.Level))
            return false;

        var previousStatus = application.Status;

        currentStep.Status = WorkflowStepStatus.SentBack;
        currentStep.CompletedAt = DateTime.UtcNow;
        currentStep.Remarks = remarks;

        application.Status = ApplicationStatus.SentBack;

        await _auditService.LogWorkflowActionAsync(
            applicationId,
            currentStep.Level,
            userId,
            "SentBack",
            remarks,
            previousStatus,
            application.Status);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WorkflowStep>> GetPendingStepsForUserAsync(string userId)
    {
        return await _context.WorkflowSteps
            .Include(s => s.Application)
            .ThenInclude(a => a.ApplicantProfile)
            .Where(s => s.AssignedToUserId == userId && s.Status == WorkflowStepStatus.Pending)
            .OrderBy(s => s.AssignedAt)
            .ToListAsync();
    }

    public async Task<List<WorkflowStep>> GetRecentlyApprovedAttestationStepsAsync(string userId, int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        
        return await _context.WorkflowSteps
            .Include(s => s.Application)
            .ThenInclude(a => a.ApplicantProfile)
            .Where(s => s.AssignedToUserId == userId && 
                   s.Level == WorkflowLevel.Attestation &&
                   s.Status == WorkflowStepStatus.Approved &&
                   s.CompletedAt.HasValue &&
                   s.CompletedAt.Value >= cutoffTime &&
                   s.Application.Status == ApplicationStatus.Approved)
            .OrderByDescending(s => s.CompletedAt)
            .ToListAsync();
    }

    public async Task<WorkflowStep?> GetCurrentStepAsync(int applicationId)
    {
        return await _context.WorkflowSteps
            .Include(s => s.AssignedToUser)
            .Where(s => s.ApplicationId == applicationId && s.Status == WorkflowStepStatus.Pending)
            .OrderBy(s => s.Level)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Auto-assigns applications to Verification Officers (200 per officer per day)
    /// </summary>
    public async Task<int> AutoAssignApplicationsAsync()
    {
        var today = DateTime.UtcNow.Date;
        const int maxAssignmentsPerOfficer = 200;
        int assignedCount = 0;

        // Get unassigned submitted applications that are paid
        var unassignedApplications = await _context.Applications
            .Include(a => a.Payments)
            .Include(a => a.WorkflowSteps)
            .Where(a => a.Status == ApplicationStatus.Submitted &&
                   a.Payments.Any(p => p.Status == PaymentStatus.Paid) &&
                   !a.WorkflowSteps.Any(s => s.Level == WorkflowLevel.Verification && s.Status == WorkflowStepStatus.Pending))
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        if (!unassignedApplications.Any())
        {
            _logger.LogInformation("No unassigned applications found for auto-assignment");
            return 0;
        }

        // Get all active VerificationOfficers
        var verificationOfficers = await _userManager.GetUsersInRoleAsync("VerificationOfficer");
        var activeOfficers = verificationOfficers.Where(u => u.IsActive).ToList();

        if (!activeOfficers.Any())
        {
            _logger.LogWarning("No active VerificationOfficers found for auto-assignment");
            return 0;
        }

        // Get today's assignment count per officer
        var todayAssignments = await _context.WorkflowSteps
            .Where(s => s.AssignedAt.Date == today && 
                   s.Level == WorkflowLevel.Verification)
            .GroupBy(s => s.AssignedToUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        foreach (var application in unassignedApplications)
        {
            // Find officer with least assignments today
            var availableOfficer = activeOfficers
                .OrderBy(o => todayAssignments.GetValueOrDefault(o.Id, 0))
                .FirstOrDefault(o => todayAssignments.GetValueOrDefault(o.Id, 0) < maxAssignmentsPerOfficer);

            if (availableOfficer == null)
            {
                _logger.LogInformation("All officers have reached daily limit");
                break;
            }

            // Assign application
            await AssignToLevelAsync(application.Id, WorkflowLevel.Verification, availableOfficer.Id);
            application.Status = ApplicationStatus.UnderVerification;
            assignedCount++;
            
            // Update assignment count
            if (todayAssignments.ContainsKey(availableOfficer.Id))
                todayAssignments[availableOfficer.Id]++;
            else
                todayAssignments[availableOfficer.Id] = 1;

            _logger.LogInformation("Application {AppId} auto-assigned to VerificationOfficer {OfficerId}", 
                application.Id, availableOfficer.Id);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("{AssignedCount} applications auto-assigned to verification officers", assignedCount);
        return assignedCount;
    }

    /// <summary>
    /// Auto-assigns a single submitted application to a Verification Officer
    /// </summary>
    public async Task<bool> AutoAssignSingleApplicationAsync(int applicationId)
    {
        var application = await _context.Applications
            .Include(a => a.Payments)
            .Include(a => a.WorkflowSteps)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
        {
            _logger.LogWarning("Application {ApplicationId} not found for auto-assignment", applicationId);
            return false;
        }

        // Check if application is submitted and paid
        if (application.Status != ApplicationStatus.Submitted)
        {
            _logger.LogWarning("Application {ApplicationId} is not in Submitted status (current: {Status})", applicationId, application.Status);
            return false;
        }

        if (!application.Payments.Any(p => p.Status == PaymentStatus.Paid))
        {
            _logger.LogWarning("Application {ApplicationId} does not have a paid payment", applicationId);
            return false;
        }

        // Check if already assigned
        if (application.WorkflowSteps.Any(s => s.Level == WorkflowLevel.Verification && s.Status == WorkflowStepStatus.Pending))
        {
            _logger.LogInformation("Application {ApplicationId} already has a pending verification assignment", applicationId);
            return true; // Already assigned, consider it success
        }

        // Get available Verification Officer
        var verificationOfficer = await GetAvailableOfficerAsync("VerificationOfficer");
        if (verificationOfficer == null)
        {
            _logger.LogWarning("No available VerificationOfficer found for application {ApplicationId}", applicationId);
            return false;
        }

        // Assign application
        await AssignToLevelAsync(applicationId, WorkflowLevel.Verification, verificationOfficer.Id);
        application.Status = ApplicationStatus.UnderVerification;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Application {ApplicationId} auto-assigned to VerificationOfficer {OfficerId}", 
            applicationId, verificationOfficer.Id);
        
        return true;
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

    /// <summary>
    /// Gets an available officer for the specified role with least workload
    /// </summary>
    public async Task<ApplicationUser?> GetAvailableOfficerAsync(string roleName)
    {
        var today = DateTime.UtcNow.Date;
        const int maxAssignmentsPerOfficer = 200;

        // Get all active officers for the role
        var officers = await _userManager.GetUsersInRoleAsync(roleName);
        var activeOfficers = officers.Where(u => u.IsActive).ToList();

        if (!activeOfficers.Any())
        {
            _logger.LogWarning("No active officers found for role {RoleName}", roleName);
            return null;
        }

        // Get workflow level for this role
        var level = roleName switch
        {
            "VerificationOfficer" => WorkflowLevel.Verification,
            "Supervisor" => WorkflowLevel.Supervision,
            "AttestationOfficer" => WorkflowLevel.Attestation,
            _ => WorkflowLevel.Verification
        };

        // Get today's pending assignment count per officer
        var pendingAssignments = await _context.WorkflowSteps
            .Where(s => s.Level == level && s.Status == WorkflowStepStatus.Pending)
            .GroupBy(s => s.AssignedToUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        // Find officer with least pending assignments
        var availableOfficer = activeOfficers
            .OrderBy(o => pendingAssignments.GetValueOrDefault(o.Id, 0))
            .FirstOrDefault(o => pendingAssignments.GetValueOrDefault(o.Id, 0) < maxAssignmentsPerOfficer);

        return availableOfficer;
    }
}

