using DocAttestation.Data;
using DocAttestation.Models;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocAttestation.Controllers;

[Authorize(Policy = "ApplicantOnly")]
public class ApplicationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IApplicationService _applicationService;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(
        ApplicationDbContext context,
        IApplicationService applicationService,
        ILogger<ApplicationController> logger)
    {
        _context = context;
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return RedirectToAction("Index", "Profile");

        var applications = await _applicationService.GetApplicationsByApplicantAsync(profile.Id);

        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        var application = await _applicationService.GetApplicationByIdAsync(id);

        if (application == null || application.ApplicantProfileId != profile.Id)
            return NotFound();

        return View(application);
    }

    [HttpGet]
    public async Task<IActionResult> ViewDocument(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        var application = await _applicationService.GetApplicationByIdAsync(id);

        if (application == null || application.ApplicantProfileId != profile.Id)
            return NotFound();

        // View original document if available
        if (!string.IsNullOrEmpty(application.OriginalDocumentPath) && System.IO.File.Exists(application.OriginalDocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(application.OriginalDocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Document not found");
    }

    [HttpGet]
    public async Task<IActionResult> ViewApplicationDocument(int documentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        var document = await _context.ApplicationDocuments
            .Include(d => d.Application)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Application.ApplicantProfileId != profile.Id)
            return NotFound();

        if (System.IO.File.Exists(document.DocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.DocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Document not found");
    }

    [HttpGet]
    public async Task<IActionResult> ViewStampedDocument(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        var application = await _applicationService.GetApplicationByIdAsync(id);

        if (application == null || application.ApplicantProfileId != profile.Id)
            return NotFound();

        // View stamped document (with QR code) if available
        if (!string.IsNullOrEmpty(application.StampedDocumentPath) && System.IO.File.Exists(application.StampedDocumentPath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(application.StampedDocumentPath);
            return File(fileBytes, "application/pdf");
        }

        return NotFound("Stamped document not found");
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await _context.ApplicantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return NotFound();

        var application = await _applicationService.GetApplicationByIdAsync(id);

        if (application == null || application.ApplicantProfileId != profile.Id)
            return NotFound();

        if (application.Status != ApplicationStatus.Approved || string.IsNullOrEmpty(application.StampedDocumentPath))
            return BadRequest("Document not available for download");

        if (!System.IO.File.Exists(application.StampedDocumentPath))
            return NotFound("File not found");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(application.StampedDocumentPath);
        var fileName = $"{application.ApplicationNumber}_attested.pdf";

        return File(fileBytes, "application/pdf", fileName);
    }
}

