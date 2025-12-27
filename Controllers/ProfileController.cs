using DocAttestation.Data;
using DocAttestation.Models;
using DocAttestation.Services;
using DocAttestation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocAttestation.Controllers;

[Authorize(Policy = "ApplicantOnly")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEncryptionService _encryptionService;
    private readonly IPdfStampingService _pdfStampingService;
    private readonly IApplicationService _applicationService;
    private readonly IWorkflowService _workflowService;
    private readonly IEmailService _emailService;
    private readonly IOTPService _otpService;
    private readonly ILogger<ProfileController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEncryptionService encryptionService,
        IPdfStampingService pdfStampingService,
        IApplicationService applicationService,
        IWorkflowService workflowService,
        IEmailService emailService,
        IOTPService otpService,
        ILogger<ProfileController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _encryptionService = encryptionService;
        _pdfStampingService = pdfStampingService;
        _applicationService = applicationService;
        _workflowService = workflowService;
        _emailService = emailService;
        _otpService = otpService;
        _logger = logger;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var profile = await _context.ApplicantProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return RedirectToAction("Step1");

        ViewBag.CurrentStep = profile.CurrentStep;
        ViewBag.IsComplete = profile.IsProfileComplete;

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Step1(bool? editMode = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await GetOrCreateProfileAsync(userId!);

        var model = new ProfileStep1ViewModel
        {
            FullName = profile.FullName,
            FatherName = profile.FatherName,
            DateOfBirth = profile.DateOfBirth != default ? profile.DateOfBirth : DateTime.Now.AddYears(-25),
            Gender = profile.Gender,
            CNIC = _encryptionService.MaskCNIC(_encryptionService.Decrypt(profile.EncryptedCNIC))
        };

        ViewBag.EditMode = editMode ?? false;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step1(ProfileStep1ViewModel model, bool? editMode = false)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.EditMode = editMode ?? false;
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);

            profile.FullName = model.FullName;
            profile.FatherName = model.FatherName;
            profile.DateOfBirth = model.DateOfBirth;
            profile.Gender = model.Gender;
            profile.CurrentStep = Math.Max(profile.CurrentStep, 1);

            // Handle photograph upload
            if (model.Photograph != null && model.Photograph.Length > 0)
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "photos");
                Directory.CreateDirectory(uploadsPath);

                var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(model.Photograph.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photograph.CopyToAsync(stream);
                }

                profile.PhotographPath = $"/uploads/photos/{fileName}";
            }

            profile.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // If edit mode, redirect back to profile index, otherwise continue to Step2
            if (editMode == true)
            {
                TempData["Success"] = "Personal information updated successfully!";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Step2");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Step 1");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            ViewBag.EditMode = editMode ?? false;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Step2()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await GetOrCreateProfileAsync(userId!);

        var model = new ProfileStep2ViewModel
        {
            MobileNumber = profile.MobileNumber,
            Email = profile.Email,
            Address = profile.Address
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step2(ProfileStep2ViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);

            profile.MobileNumber = model.MobileNumber;
            profile.Email = model.Email;
            profile.Address = model.Address;
            profile.CurrentStep = Math.Max(profile.CurrentStep, 2);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction("Step3");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Step 2");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Step3(int? applicationId = null)
    {
        var model = new ProfileStep3ViewModel
        {
            Documents = new List<DocAttestation.ViewModels.DocumentItem>
            {
                new DocAttestation.ViewModels.DocumentItem()
            }
        };

        // If editing an existing application
        if (applicationId.HasValue)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);
            
            var application = await _applicationService.GetApplicationByIdAsync(applicationId.Value);
            
            if (application != null && application.ApplicantProfileId == profile.Id && application.Status == ApplicationStatus.Draft)
            {
                model.VerificationType = application.VerificationType;
                model.ApplicationNumber = application.ApplicationNumber;
                model.City = application.City;
                model.DocumentSubmissionMethod = application.DocumentSubmissionMethod;
                model.SubmissionBy = application.SubmissionBy;
                model.RelationType = application.RelationType;
                model.RelationCNIC = application.RelationCNIC;
                
                // Load existing documents
                if (application.Documents != null && application.Documents.Any())
                {
                    model.Documents = application.Documents.Select(d => new DocAttestation.ViewModels.DocumentItem
                    {
                        DocumentName = d.DocumentName
                        // Note: We can't pre-populate the file input, but we can show the document names
                    }).ToList();
                }
                
                TempData["EditingApplicationId"] = applicationId.Value;
                TempData.Keep("EditingApplicationId");
                ViewBag.IsEditMode = true;
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditApplication(int applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await GetOrCreateProfileAsync(userId!);
        
        var application = await _applicationService.GetApplicationByIdAsync(applicationId);
        
        if (application == null || application.ApplicantProfileId != profile.Id)
            return NotFound();
        
        if (application.Status != ApplicationStatus.Draft)
        {
            TempData["Error"] = "Only draft applications can be edited.";
            return RedirectToAction("Details", "Application", new { id = applicationId });
        }
        
        return RedirectToAction("Step3", new { applicationId = applicationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step3(ProfileStep3ViewModel model)
    {
        // Helper function to preserve edit mode
        void PreserveEditMode()
        {
            if (Request.Form["EditingApplicationId"].Count > 0 && int.TryParse(Request.Form["EditingApplicationId"], out int formAppId))
            {
                TempData["EditingApplicationId"] = formAppId;
                TempData.Keep("EditingApplicationId");
                ViewBag.IsEditMode = true;
            }
        }
        
        if (model.Documents == null || model.Documents.Count == 0)
        {
            ModelState.AddModelError("", "Please upload at least one document");
            PreserveEditMode();
            return View(model);
        }

        // Validate all documents
        var allowedExtensions = new[] { ".pdf" };
        var maxFileSize = 10 * 1024 * 1024; // 10MB
        
        for (int i = 0; i < model.Documents.Count; i++)
        {
            var doc = model.Documents[i];
            
            if (string.IsNullOrWhiteSpace(doc.DocumentName))
            {
                ModelState.AddModelError($"Documents[{i}].DocumentName", "Document name is required");
            }
            
            if (doc.Document == null || doc.Document.Length == 0)
            {
                ModelState.AddModelError($"Documents[{i}].Document", "Please upload a document");
                continue;
            }

            var extension = Path.GetExtension(doc.Document.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError($"Documents[{i}].Document", "Only PDF files are allowed");
            }

            if (doc.Document.Length > maxFileSize)
            {
                ModelState.AddModelError($"Documents[{i}].Document", "File size must be less than 10MB");
            }
        }

        if (!ModelState.IsValid)
        {
            PreserveEditMode();
            return View(model);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);

            // Check if editing existing application - check both TempData and form data
            int? editingApplicationId = null;
            
            // First check form data (more reliable)
            if (Request.Form["EditingApplicationId"].Count > 0 && int.TryParse(Request.Form["EditingApplicationId"], out int formAppId))
            {
                editingApplicationId = formAppId;
            }
            // Fallback to TempData
            else if (TempData["EditingApplicationId"] != null)
            {
                editingApplicationId = Convert.ToInt32(TempData["EditingApplicationId"]);
            }
            
            Application? application = null;
            bool isEditing = false;

            // Check if we're editing an existing application
            if (editingApplicationId.HasValue)
            {
                // Try to get existing application - validate it exists and is editable
                var existingApplication = await _applicationService.GetApplicationByIdAsync(editingApplicationId.Value);
                
                if (existingApplication != null && 
                    existingApplication.ApplicantProfileId == profile.Id && 
                    existingApplication.Status == ApplicationStatus.Draft)
                {
                    // Valid application to edit
                    application = existingApplication;
                    isEditing = true;
                    
                    // Keep TempData for potential return to view
                    TempData["EditingApplicationId"] = editingApplicationId.Value;
                    TempData.Keep("EditingApplicationId");
                }
                else
                {
                    // Invalid application - clear edit mode and create new
                    TempData.Remove("EditingApplicationId");
                    editingApplicationId = null;
                    isEditing = false;
                }
            }

            // Prepare document DTOs
            var documentDtos = new List<DocumentCreateDto>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsPath);

            // Save all documents
            foreach (var doc in model.Documents)
            {
                if (doc.Document == null || doc.Document.Length == 0)
                    continue;

                var fileName = $"{userId}_{Guid.NewGuid()}.pdf";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await doc.Document.CopyToAsync(stream);
                }

                // Compute hash
                var documentHash = _pdfStampingService.ComputeFileHash(filePath);

                documentDtos.Add(new DocumentCreateDto
                {
                    DocumentName = doc.DocumentName,
                    DocumentPath = filePath,
                    DocumentHash = documentHash
                });
            }

            if (documentDtos.Count == 0)
            {
                ModelState.AddModelError("", "Please upload at least one document");
                PreserveEditMode();
                return View(model);
            }

            if (isEditing && application != null)
            {
                // UPDATE EXISTING APPLICATION
                // Delete old documents
                var oldDocuments = await _context.ApplicationDocuments
                    .Where(d => d.ApplicationId == application.Id)
                    .ToListAsync();
                
                foreach (var oldDoc in oldDocuments)
                {
                    // Delete file if exists
                    if (System.IO.File.Exists(oldDoc.DocumentPath))
                    {
                        try { System.IO.File.Delete(oldDoc.DocumentPath); } catch { }
                    }
                    _context.ApplicationDocuments.Remove(oldDoc);
                }

                // Update application
                var oldFee = application.Fee;
                application.VerificationType = model.VerificationType;
                application.City = model.City;
                application.DocumentSubmissionMethod = model.DocumentSubmissionMethod;
                application.SubmissionBy = model.SubmissionBy;
                application.RelationType = model.RelationType;
                application.RelationCNIC = model.RelationCNIC;
                
                // Recalculate fee
                decimal baseFeePerDocument = model.VerificationType == VerificationType.Normal ? 500m : 1500m;
                application.Fee = baseFeePerDocument * documentDtos.Count;
                
                // If fee changed, clear existing payments (user needs to pay new amount)
                if (oldFee != application.Fee && application.Payments.Any())
                {
                    var payments = await _context.Payments
                        .Where(p => p.ApplicationId == application.Id)
                        .ToListAsync();
                    
                    foreach (var payment in payments)
                    {
                        _context.Payments.Remove(payment);
                    }
                }
                
                // Clear time slot - will be reassigned after submission and payment
                application.TimeSlot = null;

                // Use first document for backward compatibility
                var firstDocument = documentDtos.First();
                application.OriginalDocumentPath = firstDocument.DocumentPath;
                application.DocumentHash = firstDocument.DocumentHash;

                await _context.SaveChangesAsync();

                // Add new documents
                foreach (var docDto in documentDtos)
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
            }
            else
            {
                // CREATE NEW APPLICATION (not editing)
                // Create application with default values for removed fields
                var applicationDto = new ApplicationCreateDto
                {
                    DocumentType = "Document", // Default value
                    IssuingAuthority = "N/A", // Default value
                    Year = DateTime.Now.Year, // Current year as default
                    RegistrationNumber = null,
                    RollNumber = null,
                    Documents = documentDtos, // Use already processed documents
                    VerificationType = model.VerificationType
                };

                application = await _applicationService.CreateApplicationAsync(profile.Id, applicationDto);
                
                // Update application with City, DocumentSubmissionMethod, SubmissionBy, RelationType, and RelationCNIC
                application.City = model.City;
                application.DocumentSubmissionMethod = model.DocumentSubmissionMethod;
                application.SubmissionBy = model.SubmissionBy;
                application.RelationType = model.RelationType;
                application.RelationCNIC = model.RelationCNIC;
                await _context.SaveChangesAsync();
            }

            profile.CurrentStep = 4; // Step 3 is now the last step before review
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ApplicationId"] = application.Id;
            if (editingApplicationId.HasValue)
            {
                TempData["Success"] = "Application updated successfully!";
            }
            return RedirectToAction("Step5");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Step 3");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Step4()
    {
        // Step4 is merged into Step3, redirect to Step3
        return RedirectToAction("Step3");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step4(ProfileStep4ViewModel model)
    {
        if (model.Documents == null || model.Documents.Count == 0)
        {
            ModelState.AddModelError("", "Please upload at least one document");
            return View(model);
        }

        // Validate all documents
        var allowedExtensions = new[] { ".pdf" };
        var maxFileSize = 10 * 1024 * 1024; // 10MB
        
        for (int i = 0; i < model.Documents.Count; i++)
        {
            var doc = model.Documents[i];
            
            if (string.IsNullOrWhiteSpace(doc.DocumentName))
            {
                ModelState.AddModelError($"Documents[{i}].DocumentName", "Document name is required");
            }
            
            if (doc.Document == null || doc.Document.Length == 0)
            {
                ModelState.AddModelError($"Documents[{i}].Document", "Please upload a document");
                continue;
            }

            var extension = Path.GetExtension(doc.Document.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError($"Documents[{i}].Document", "Only PDF files are allowed");
            }

            if (doc.Document.Length > maxFileSize)
            {
                ModelState.AddModelError($"Documents[{i}].Document", "File size must be less than 10MB");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);

            // Prepare document DTOs
            var documentDtos = new List<DocumentCreateDto>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadsPath);

            // Save all documents
            foreach (var doc in model.Documents)
            {
                if (doc.Document == null || doc.Document.Length == 0)
                    continue;

                var fileName = $"{userId}_{Guid.NewGuid()}.pdf";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await doc.Document.CopyToAsync(stream);
                }

                // Compute hash
                var documentHash = _pdfStampingService.ComputeFileHash(filePath);

                documentDtos.Add(new DocumentCreateDto
                {
                    DocumentName = doc.DocumentName,
                    DocumentPath = filePath,
                    DocumentHash = documentHash
                });
            }

            if (documentDtos.Count == 0)
            {
                ModelState.AddModelError("", "Please upload at least one document");
                return View(model);
            }

            // Create application
            var verificationType = TempData["VerificationType"] != null 
                ? (VerificationType)Convert.ToInt32(TempData["VerificationType"])
                : VerificationType.Normal;

            var applicationDto = new ApplicationCreateDto
            {
                DocumentType = TempData["DocumentType"]?.ToString()!,
                IssuingAuthority = TempData["IssuingAuthority"]?.ToString()!,
                Year = Convert.ToInt32(TempData["Year"]),
                RegistrationNumber = TempData["RegistrationNumber"]?.ToString(),
                RollNumber = TempData["RollNumber"]?.ToString(),
                Documents = documentDtos,
                VerificationType = verificationType
            };

            var application = await _applicationService.CreateApplicationAsync(profile.Id, applicationDto);

            profile.CurrentStep = 4;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ApplicationId"] = application.Id;
            return RedirectToAction("Step5");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Step 4");
            ModelState.AddModelError("", "An error occurred. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Step5()
    {
        if (TempData["ApplicationId"] == null)
            return RedirectToAction("Index");

        var applicationId = Convert.ToInt32(TempData["ApplicationId"]);
        var application = await _applicationService.GetApplicationByIdAsync(applicationId);

        if (application == null)
            return RedirectToAction("Index");

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitApplication(int applicationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await _context.ApplicantProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
                return Json(new { success = false, message = "Profile not found" });

            var success = await _applicationService.SubmitApplicationAsync(applicationId);

            if (success)
            {
                profile.IsProfileComplete = true;
                profile.CurrentStep = 5;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Application submitted successfully!",
                    redirectUrl = Url.Action("Index", "Application")
                });
            }

            return Json(new { success = false, message = "Failed to submit application" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting application");
            var errorMessage = "An error occurred while submitting the application. Please try again.";
            
            // In development, provide more details
            if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                errorMessage = $"Error: {ex.Message}";
            }
            
            return Json(new { success = false, message = errorMessage });
        }
    }

    private async Task<ApplicantProfile> GetOrCreateProfileAsync(string userId)
    {
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // This should not happen if registration created profile
            throw new InvalidOperationException("Profile not found. Please contact support.");
        }

        return profile;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(int applicationId, string cardNumber, 
        string? city = null, string? documentSubmissionMethod = null, 
        string? submissionBy = null, string? relationType = null, string? relationCNIC = null)
    {
        try
        {
            var application = await _applicationService.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return Json(new { success = false, message = "Application not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var profile = await GetOrCreateProfileAsync(userId!);
            
            if (application.ApplicantProfileId != profile.Id)
                return Json(new { success = false, message = "Unauthorized" });

            // Save city and document submission details
            if (!string.IsNullOrEmpty(city))
            {
                application.City = city;
            }
            
            if (!string.IsNullOrEmpty(documentSubmissionMethod) && int.TryParse(documentSubmissionMethod, out int methodValue))
            {
                application.DocumentSubmissionMethod = (DocumentSubmissionMethod)methodValue;
                
                // If TCS is selected, generate TCS number after payment
                if (application.DocumentSubmissionMethod == DocumentSubmissionMethod.TCS)
                {
                    // Generate unique TCS number: TCS + ApplicationNumber + timestamp
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    application.TCSNumber = $"TCS{application.ApplicationNumber.Replace("-", "")}{timestamp.Substring(timestamp.Length - 6)}";
                }
            }
            
            if (!string.IsNullOrEmpty(submissionBy) && int.TryParse(submissionBy, out int submissionValue))
            {
                application.SubmissionBy = (SubmissionBy)submissionValue;
            }
            
            if (!string.IsNullOrEmpty(relationType))
            {
                application.RelationType = relationType;
            }
            
            if (!string.IsNullOrEmpty(relationCNIC))
            {
                application.RelationCNIC = relationCNIC;
            }

            // Create payment record
            var payment = new Payment
            {
                ApplicationId = applicationId,
                Amount = application.Fee,
                CardNumber = cardNumber, // Last 4 digits only
                Status = PaymentStatus.Paid,
                PaidAt = DateTime.UtcNow,
                PaymentMethod = "Card"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Auto-submit the application after payment
            var submitted = await _applicationService.SubmitApplicationAsync(applicationId);
            if (!submitted)
            {
                _logger.LogWarning("Failed to auto-submit application {ApplicationId} after payment", applicationId);
                return Json(new { success = true, message = "Payment processed successfully, but failed to submit application. Please contact support." });
            }

            // Auto-assign the application to a Verification Officer
            var assigned = await _workflowService.AutoAssignSingleApplicationAsync(applicationId);
            if (!assigned)
            {
                _logger.LogWarning("Failed to auto-assign application {ApplicationId} after payment", applicationId);
            }

            // Reload application with User navigation property for email
            var applicationForEmail = await _context.Applications
                .Include(a => a.ApplicantProfile)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            // Reload payment to get the saved payment with ID
            var savedPayment = await _context.Payments
                .OrderByDescending(p => p.PaidAt)
                .FirstOrDefaultAsync(p => p.ApplicationId == applicationId);

            // Send confirmation email
            if (applicationForEmail != null && savedPayment != null)
            {
                _logger.LogInformation("Preparing to send email for application {ApplicationId}. Application: {AppNumber}, Payment: {PaymentId}", 
                    applicationId, applicationForEmail.ApplicationNumber, savedPayment.Id);
                
                try
                {
                    var emailSent = await _emailService.SendPaymentConfirmationEmailAsync(applicationForEmail, savedPayment);
                    if (emailSent)
                    {
                        _logger.LogInformation("Payment confirmation email sent successfully for application {ApplicationId}", applicationId);
                    }
                    else
                    {
                        _logger.LogWarning("Payment confirmation email failed to send for application {ApplicationId}", applicationId);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Exception occurred while sending payment confirmation email for application {ApplicationId}: {Message}", 
                        applicationId, emailEx.Message);
                    // Don't fail the payment if email fails
                }
            }
            else
            {
                _logger.LogWarning("Cannot send email - Application: {AppNull}, Payment: {PaymentNull}", 
                    applicationForEmail == null ? "null" : "found", savedPayment == null ? "null" : "found");
            }

            return Json(new { success = true, message = "Payment processed successfully. Application has been submitted and assigned. A confirmation email has been sent to your registered email address." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return Json(new { success = false, message = "An error occurred processing payment" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendOTP(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return Json(new { success = false, message = "Please enter a valid email address" });
            }

            _logger.LogInformation("SendOTP request received for email: {Email}", email);
            
            await _otpService.GenerateAndSendOTPAsync(email);
            
            _logger.LogInformation("OTP sent successfully to {Email}", email);
            return Json(new { success = true, message = "OTP has been sent to your email address. Please check your inbox (and spam folder)." });
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid argument when sending OTP to {Email}: {Message}", email, argEx.Message);
            return Json(new { success = false, message = argEx.Message });
        }
        catch (InvalidOperationException opEx)
        {
            _logger.LogError(opEx, "Operation failed when sending OTP to {Email}: {Message}", email, opEx.Message);
            
            // Parse specific error types from EmailService
            var errorMessage = opEx.Message;
            string userMessage;
            
            if (errorMessage.StartsWith("EMAIL_AUTH_ERROR:"))
            {
                userMessage = "Gmail authentication failed. The email account credentials may be incorrect or the app password may have expired. Please contact support to update email settings.";
            }
            else if (errorMessage.StartsWith("EMAIL_CONNECTION_ERROR:"))
            {
                userMessage = "Cannot connect to Gmail SMTP server. This may be due to network issues or firewall blocking. Please check your internet connection or contact support.";
            }
            else if (errorMessage.StartsWith("EMAIL_QUOTA_ERROR:"))
            {
                userMessage = "Gmail sending limit exceeded. Please try again in a few minutes or contact support.";
            }
            else if (errorMessage.StartsWith("EMAIL_SMTP_ERROR:") || errorMessage.StartsWith("EMAIL_ERROR:"))
            {
                // Extract the actual error message after the prefix
                var actualError = errorMessage.Contains(":") ? errorMessage.Substring(errorMessage.IndexOf(":") + 1).Trim() : errorMessage;
                userMessage = $"Email service error: {actualError}. Please contact support if the issue persists.";
            }
            else if (errorMessage.Contains("not configured"))
            {
                userMessage = "Email service is not properly configured. Please contact support.";
            }
            else
            {
                userMessage = "Email service is temporarily unavailable. Please try again later or contact support.";
            }
            
            return Json(new { success = false, message = userMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending OTP to {Email}: {Message}", email, ex.Message);
            
            // In development, provide more details
            var errorMessage = "Failed to send OTP. Please try again.";
            if (HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                errorMessage = $"Failed to send OTP: {ex.Message}";
            }
            
            return Json(new { success = false, message = errorMessage });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOTP(string email, string otp)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                return Json(new { success = false, message = "Email and OTP are required" });
            }

            if (otp.Length != 6 || !otp.All(char.IsDigit))
            {
                return Json(new { success = false, message = "OTP must be a 6-digit number" });
            }

            var isValid = _otpService.VerifyOTP(email, otp);
            if (isValid)
            {
                return Json(new { success = true, message = "OTP verified successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Invalid OTP. Please try again." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {Email}", email);
            return Json(new { success = false, message = "An error occurred while verifying OTP" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return Json(new { success = false, message = "Please enter a valid email address" });
            }

            _logger.LogInformation("Testing email service - Sending test email to {Email}", email);
            
            var testSubject = "Test Email - Document Attestation System";
            var testBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Test Email</title>
</head>
<body style=""font-family: Arial, sans-serif; padding: 20px;"">
    <h2 style=""color: #006633;"">Test Email</h2>
    <p>This is a test email from the Document Attestation System.</p>
    <p>If you received this email, the email service is working correctly.</p>
    <p>Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
</body>
</html>";

            var result = await _emailService.SendEmailAsync(email, testSubject, testBody, true);
            
            if (result)
            {
                return Json(new { success = true, message = "Test email sent successfully! Please check your inbox (and spam folder)." });
            }
            else
            {
                return Json(new { success = false, message = "Failed to send test email. Check server logs for details." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email to {Email}: {Message}", email, ex.Message);
            return Json(new { success = false, message = $"Error: {ex.Message}. Check server logs for more details." });
        }
    }
}

