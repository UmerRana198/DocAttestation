namespace DocAttestation.Models;

public class ApplicationDocument
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    
    // Document Information
    public string DocumentName { get; set; } = null!; // Name selected from dropdown
    public string DocumentPath { get; set; } = null!; // File path
    public string DocumentHash { get; set; } = null!; // SHA256 hash
    public string? StampedDocumentPath { get; set; }
    public string? StampedDocumentHash { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Document Verification Status
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationRemarks { get; set; }
    
    // Navigation
    public virtual Application Application { get; set; } = null!;
    public virtual ApplicationUser? VerifiedByUser { get; set; }
}

public enum DocumentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    SentBack = 3
}

