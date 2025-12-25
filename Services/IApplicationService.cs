using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IApplicationService
{
    Task<Application> CreateApplicationAsync(int applicantProfileId, ApplicationCreateDto dto);
    Task<Application?> GetApplicationByIdAsync(int id);
    Task<Application?> GetApplicationByNumberAsync(string applicationNumber);
    Task<List<Application>> GetApplicationsByApplicantAsync(int applicantProfileId);
    Task<string> GenerateApplicationNumberAsync();
    Task<bool> SubmitApplicationAsync(int applicationId);
    Task<DateTime> AssignTimeSlotAsync(VerificationType verificationType);
}

public class ApplicationCreateDto
{
    public string DocumentType { get; set; } = null!;
    public string IssuingAuthority { get; set; } = null!;
    public int Year { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RollNumber { get; set; }
    
    // For backward compatibility - will use first document if Documents list is provided
    public string? DocumentPath { get; set; }
    public string? DocumentHash { get; set; }
    
    // Multiple documents support
    public List<DocumentCreateDto>? Documents { get; set; }
    
    public VerificationType VerificationType { get; set; } = VerificationType.Normal;
}

public class DocumentCreateDto
{
    public string DocumentName { get; set; } = null!;
    public string DocumentPath { get; set; } = null!;
    public string DocumentHash { get; set; } = null!;
}

