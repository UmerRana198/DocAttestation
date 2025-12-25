namespace DocAttestation.Models;

public class ApplicantProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    
    // CNIC - Encrypted in DB
    public string EncryptedCNIC { get; set; } = null!;
    public string CNICHash { get; set; } = null!; // For lookup/indexing
    
    // Personal Information
    public string FullName { get; set; } = null!;
    public string FatherName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = null!;
    public string? PhotographPath { get; set; }
    
    // Contact Information
    public string MobileNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Address { get; set; } = null!;
    
    // Profile completion status
    public int CurrentStep { get; set; } = 0; // 0 = not started, 1-5 = step number
    public bool IsProfileComplete { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}

