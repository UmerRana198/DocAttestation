using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.EntityFrameworkCore;

namespace DocAttestation.Services;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly IQRCodeService _qrCodeService;
    private readonly IPdfStampingService _pdfStampingService;

    public ApplicationService(
        ApplicationDbContext context,
        IQRCodeService qrCodeService,
        IPdfStampingService pdfStampingService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _pdfStampingService = pdfStampingService;
    }

    public async Task<string> GenerateApplicationNumberAsync()
    {
        var prefix = "APP";
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        var applicationNumber = $"{prefix}-{date}-{random}";

        // Ensure uniqueness
        while (await _context.Applications.AnyAsync(a => a.ApplicationNumber == applicationNumber))
        {
            random = new Random().Next(1000, 9999);
            applicationNumber = $"{prefix}-{date}-{random}";
        }

        return applicationNumber;
    }

    public async Task<Application> CreateApplicationAsync(int applicantProfileId, ApplicationCreateDto dto)
    {
        // Determine number of documents
        int documentCount = 1;
        List<DocumentCreateDto> documentsToSave = new List<DocumentCreateDto>();
        
        if (dto.Documents != null && dto.Documents.Count > 0)
        {
            // Multiple documents mode
            documentCount = dto.Documents.Count;
            documentsToSave = dto.Documents;
        }
        else if (!string.IsNullOrEmpty(dto.DocumentPath))
        {
            // Single document mode (backward compatibility)
            documentsToSave.Add(new DocumentCreateDto
            {
                DocumentName = dto.DocumentType, // Use DocumentType as name for backward compatibility
                DocumentPath = dto.DocumentPath,
                DocumentHash = dto.DocumentHash!
            });
        }
        else
        {
            throw new ArgumentException("At least one document must be provided");
        }
        
        // Calculate fee: base fee per document * number of documents
        decimal baseFeePerDocument = dto.VerificationType == VerificationType.Normal ? 500m : 1500m;
        decimal totalFee = baseFeePerDocument * documentCount;
        
        // Time slot will be assigned only after submission and payment

        // Use first document for backward compatibility fields
        var firstDocument = documentsToSave.First();

        var application = new Application
        {
            ApplicantProfileId = applicantProfileId,
            ApplicationNumber = await GenerateApplicationNumberAsync(),
            DocumentType = dto.DocumentType,
            IssuingAuthority = dto.IssuingAuthority,
            Year = dto.Year,
            RegistrationNumber = dto.RegistrationNumber,
            RollNumber = dto.RollNumber,
            OriginalDocumentPath = firstDocument.DocumentPath, // For backward compatibility
            DocumentHash = firstDocument.DocumentHash, // For backward compatibility
            VerificationType = dto.VerificationType,
            Fee = totalFee,
            TimeSlot = null, // Will be assigned after submission and payment
            Status = ApplicationStatus.Draft,
            SubmittedAt = DateTime.UtcNow
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // Save all documents
        foreach (var docDto in documentsToSave)
        {
            var applicationDocument = new ApplicationDocument
            {
                ApplicationId = application.Id,
                DocumentName = docDto.DocumentName,
                DocumentPath = docDto.DocumentPath,
                DocumentHash = docDto.DocumentHash,
                UploadedAt = DateTime.UtcNow
            };
            
            _context.ApplicationDocuments.Add(applicationDocument);
        }

        await _context.SaveChangesAsync();

        return application;
    }

    public async Task<DateTime> AssignTimeSlotAsync(VerificationType verificationType)
    {
        var now = DateTime.UtcNow;
        DateTime baseDate;
        TimeSpan baseTime;
        
        if (verificationType == VerificationType.Normal)
        {
            // Normal: 20 days from now, starting at 9:30 AM
            baseDate = now.AddDays(20).Date;
            baseTime = new TimeSpan(9, 30, 0); // 9:30 AM
        }
        else
        {
            // Urgent: 24 hours from now, starting at 9:15 AM
            baseDate = now.AddDays(1).Date;
            baseTime = new TimeSpan(9, 15, 0); // 9:15 AM
        }

        // Find the last assigned time slot for this verification type on the same date
        var lastSlot = await _context.Applications
            .Where(a => a.VerificationType == verificationType && 
                       a.TimeSlot.HasValue &&
                       a.TimeSlot.Value.Date == baseDate)
            .OrderByDescending(a => a.TimeSlot)
            .FirstOrDefaultAsync();

        DateTime assignedSlot;
        if (lastSlot?.TimeSlot == null)
        {
            // First application for this date
            assignedSlot = baseDate.Add(baseTime);
        }
        else
        {
            // Add 15 minutes to the last slot
            assignedSlot = lastSlot.TimeSlot.Value.AddMinutes(15);
        }

        return assignedSlot;
    }

    public async Task<Application?> GetApplicationByIdAsync(int id)
    {
        return await _context.Applications
            .Include(a => a.ApplicantProfile)
            .ThenInclude(p => p.User)
            .Include(a => a.WorkflowSteps)
            .ThenInclude(w => w.AssignedToUser)
            .Include(a => a.WorkflowHistory)
            .Include(a => a.Payments)
            .Include(a => a.Documents)
            .ThenInclude(d => d.VerifiedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Application?> GetApplicationByNumberAsync(string applicationNumber)
    {
        return await _context.Applications
            .Include(a => a.ApplicantProfile)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(a => a.ApplicationNumber == applicationNumber);
    }

    public async Task<List<Application>> GetApplicationsByApplicantAsync(int applicantProfileId)
    {
        return await _context.Applications
            .Where(a => a.ApplicantProfileId == applicantProfileId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();
    }

    public async Task<bool> SubmitApplicationAsync(int applicationId)
    {
        var application = await GetApplicationByIdAsync(applicationId);
        if (application == null || application.Status != ApplicationStatus.Draft)
            return false;

        // Check if payment is made
        if (!application.IsPaid)
            return false;

        // Assign time slot only after payment is confirmed and application is being submitted
        if (!application.TimeSlot.HasValue)
        {
            application.TimeSlot = await AssignTimeSlotAsync(application.VerificationType);
        }

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}

