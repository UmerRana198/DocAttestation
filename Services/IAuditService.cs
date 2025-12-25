using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IAuditService
{
    Task LogWorkflowActionAsync(
        int applicationId,
        WorkflowLevel level,
        string userId,
        string action,
        string? remarks,
        ApplicationStatus previousStatus,
        ApplicationStatus newStatus);
}

