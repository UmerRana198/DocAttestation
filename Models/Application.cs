namespace DocAttestation.Models;

public class Application
{
    public int Id { get; set; }
    public int ApplicantProfileId { get; set; }
    public string ApplicationNumber { get; set; } = null!; // Unique identifier
    
    // Document Information
    public string DocumentType { get; set; } = null!; // Degree, Transcript, Certificate
    public string IssuingAuthority { get; set; } = null!;
    public int Year { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RollNumber { get; set; }
    
    // Document File
    public string OriginalDocumentPath { get; set; } = null!;
    public string? StampedDocumentPath { get; set; }
    public string DocumentHash { get; set; } = null!; // SHA256 of original document
    public string? StampedDocumentHash { get; set; } // SHA256 of stamped document
    
    // Application Status
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AttestedAt { get; set; }
    
    // QR Code
    public string? QRToken { get; set; } // Encrypted token
    public DateTime? QRTokenExpiry { get; set; }
    public bool IsQRRevoked { get; set; } = false;
    
    // Verification Type and Fee
    public VerificationType VerificationType { get; set; } = VerificationType.Normal;
    public decimal Fee { get; set; } = 0;
    public DateTime? TimeSlot { get; set; } // Assigned time slot for verification
    
    // City and Document Submission
    public string? City { get; set; }
    public DocumentSubmissionMethod? DocumentSubmissionMethod { get; set; }
    public SubmissionBy? SubmissionBy { get; set; }
    public string? RelationType { get; set; } // Father, Mother, Sister, Brother, etc.
    public string? RelationCNIC { get; set; }
    public string? TCSNumber { get; set; } // Generated after payment if TCS is selected
    
    // Navigation
    public virtual ApplicantProfile ApplicantProfile { get; set; } = null!;
    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
    public virtual ICollection<WorkflowHistory> WorkflowHistory { get; set; } = new List<WorkflowHistory>();
    public virtual ICollection<QRVerificationToken> QRVerificationTokens { get; set; } = new List<QRVerificationToken>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    
    // Helper property to check if payment is completed
    public bool IsPaid => Payments.Any(p => p.Status == PaymentStatus.Paid);
}

public enum ApplicationStatus
{
    Draft = 0,
    Submitted = 1,
    UnderVerification = 2,
    UnderSupervision = 3,
    UnderAttestation = 4,
    Approved = 5,
    Rejected = 6,
    SentBack = 7
}

public enum VerificationType
{
    Normal = 0,
    Urgent = 1
}

public enum DocumentSubmissionMethod
{
    Physical = 0,
    TCS = 1
}

public enum SubmissionBy
{
    ByYourself = 0,
    BloodRelation = 1
}

