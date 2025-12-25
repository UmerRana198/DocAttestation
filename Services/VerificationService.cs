using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.EntityFrameworkCore;

namespace DocAttestation.Services;

public class VerificationService : IVerificationService
{
    private readonly IQRCodeService _qrCodeService;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public VerificationService(
        IQRCodeService qrCodeService,
        ApplicationDbContext context,
        IAuditService auditService)
    {
        _qrCodeService = qrCodeService;
        _context = context;
        _auditService = auditService;
    }

    public async Task<VerificationResult> VerifyQRTokenAsync(string encryptedToken, string? ipAddress = null, string? userAgent = null)
    {
        var result = new VerificationResult { IsValid = false };

        try
        {
            // Decrypt token
            var tokenData = await _qrCodeService.DecryptQRTokenAsync(encryptedToken);
            if (tokenData == null)
            {
                result.Message = "Invalid or expired token";
                await LogScanAttemptAsync(encryptedToken, false, "Token decryption failed", ipAddress, userAgent);
                return result;
            }

            // Get application
            var application = await _context.Applications
                .Include(a => a.ApplicantProfile)
                .FirstOrDefaultAsync(a => a.Id == tokenData.ApplicationId);

            if (application == null)
            {
                result.Message = "Application not found";
                await LogScanAttemptAsync(encryptedToken, false, "Application not found", ipAddress, userAgent);
                return result;
            }

            // Check if application is approved
            if (application.Status != ApplicationStatus.Approved)
            {
                result.Message = "Document not attested";
                await LogScanAttemptAsync(encryptedToken, false, "Application not approved", ipAddress, userAgent);
                return result;
            }

            // Check if QR is revoked
            if (application.IsQRRevoked)
            {
                result.Message = "Token has been revoked";
                await LogScanAttemptAsync(encryptedToken, false, "Token revoked", ipAddress, userAgent);
                return result;
            }

            // Verify document hash matches
            var expectedHash = application.StampedDocumentHash ?? application.DocumentHash;
            if (tokenData.DocumentHash != expectedHash)
            {
                result.Message = "Document hash mismatch - document may have been tampered";
                await LogScanAttemptAsync(encryptedToken, false, "Hash mismatch", ipAddress, userAgent);
                return result;
            }

            // Verify token expiry
            if (tokenData.ExpiryDate < DateTime.UtcNow)
            {
                result.Message = "Token has expired";
                await LogScanAttemptAsync(encryptedToken, false, "Token expired", ipAddress, userAgent);
                return result;
            }

            // All validations passed
            result.IsValid = true;
            result.Message = "Document verified successfully";
            result.IssuingAuthority = application.IssuingAuthority;
            result.AttestationDate = application.AttestedAt;
            result.DocumentType = application.DocumentType;

            await LogScanAttemptAsync(encryptedToken, true, "Verification successful", ipAddress, userAgent);
        }
        catch (Exception ex)
        {
            result.Message = "Verification error occurred";
            await LogScanAttemptAsync(encryptedToken, false, $"Exception: {ex.Message}", ipAddress, userAgent);
        }

        return result;
    }

    private async Task LogScanAttemptAsync(string encryptedToken, bool isValid, string result, string? ipAddress, string? userAgent)
    {
        var qrToken = await _context.QRVerificationTokens
            .FirstOrDefaultAsync(q => q.EncryptedToken == encryptedToken);

        if (qrToken != null)
        {
            var scanLog = new QRScanLog
            {
                QRVerificationTokenId = qrToken.Id,
                ScanTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsValid = isValid,
                ValidationResult = result
            };

            _context.QRScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();
        }
    }
}

