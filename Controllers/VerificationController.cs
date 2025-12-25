using DocAttestation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocAttestation.Controllers;

[AllowAnonymous]
public class VerificationController : Controller
{
    private readonly IVerificationService _verificationService;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(
        IVerificationService verificationService,
        ILogger<VerificationController> logger)
    {
        _verificationService = verificationService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult QR()
    {
        return View();
    }

    [HttpGet]
    [Route("verify/qr")]
    public async Task<IActionResult> VerifyQR([FromQuery] string t)
    {
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

