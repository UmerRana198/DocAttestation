using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IWorkflowService
{
    Task<bool> AssignToLevelAsync(int applicationId, WorkflowLevel level, string assignedToUserId);
    Task<bool> ApproveAsync(int applicationId, string userId, string? remarks = null, List<string>? userRoles = null);
    Task<bool> RejectAsync(int applicationId, string userId, string remarks, List<string>? userRoles = null);
    Task<bool> SendBackAsync(int applicationId, string userId, string remarks, List<string>? userRoles = null);
    Task<List<WorkflowStep>> GetPendingStepsForUserAsync(string userId);
    Task<List<WorkflowStep>> GetRecentlyApprovedAttestationStepsAsync(string userId, int hours = 24);
    Task<WorkflowStep?> GetCurrentStepAsync(int applicationId);
    Task<int> AutoAssignApplicationsAsync();
    Task<bool> AutoAssignSingleApplicationAsync(int applicationId);
}

