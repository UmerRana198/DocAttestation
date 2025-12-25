namespace DocAttestation.Models;

public class WorkflowHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public WorkflowLevel Level { get; set; }
    public string ActionByUserId { get; set; } = null!;
    public string Action { get; set; } = null!; // Approved, Rejected, SentBack
    public string? Remarks { get; set; }
    public ApplicationStatus PreviousStatus { get; set; }
    public ApplicationStatus NewStatus { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    
    // Navigation
    public virtual Application Application { get; set; } = null!;
    public virtual ApplicationUser ActionByUser { get; set; } = null!;
}

