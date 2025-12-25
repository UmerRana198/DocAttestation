namespace DocAttestation.Models;

public class WorkflowStep
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public WorkflowLevel Level { get; set; }
    public string AssignedToUserId { get; set; } = null!;
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.Pending;
    public string? Remarks { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Navigation
    public virtual Application Application { get; set; } = null!;
    public virtual ApplicationUser AssignedToUser { get; set; } = null!;
}

public enum WorkflowLevel
{
    Verification = 1,
    Supervision = 2,
    Attestation = 3
}

public enum WorkflowStepStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    SentBack = 3
}

