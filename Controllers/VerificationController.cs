using DocAttestation.Configuration;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DocAttestation.Controllers;

[AllowAnonymous]
public class VerificationController : Controller
{
    private readonly IVerificationService _verificationService;
    private readonly MobileAppSettings _mobileAppSettings;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(
        IVerificationService verificationService,
        IOptions<MobileAppSettings> mobileAppSettings,
        ILogger<VerificationController> logger)
    {
        _verificationService = verificationService;
        _mobileAppSettings = mobileAppSettings.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult QR()
    {
        // Show info page about mobile app requirement
        ViewBag.AllowWebVerification = _mobileAppSettings.AllowWebVerification;
        return View();
    }

    /// <summary>
    /// Web-based QR verification - DISABLED by default for security
    /// Only the mobile app can verify QR codes to prevent unauthorized scanning
    /// </summary>
    [HttpGet]
    [Route("verify/qr")]
    public async Task<IActionResult> VerifyQR([FromQuery] string t)
    {
        // Check if web verification is allowed
        if (!_mobileAppSettings.AllowWebVerification)
        {
            _logger.LogWarning("Web verification attempt blocked from IP: {IP}", 
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new
            {
                isValid = false,
                message = "Web verification is disabled for security. Please use the official DocAttestation mobile app to verify documents.",
                requiresApp = true,
                appDownloadUrl = "https://play.google.com/store/apps/details?id=com.mofa.docattestation.scanner"
            });
        }

        if (string.IsNullOrEmpty(t))
        {
            return Json(new
            {
                isValid = false,
                message = "Token is required"
            });
        }

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _verificationService.VerifyQRTokenAsync(t, ipAddress, userAgent);

            return Json(new
            {
                isValid = result.IsValid,
                message = result.Message,
                issuingAuthority = result.IssuingAuthority,
                attestationDate = result.AttestationDate,
                documentType = result.DocumentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying QR token");
            return Json(new
            {
                isValid = false,
                message = "Verification error occurred"
            });
        }
    }
}

