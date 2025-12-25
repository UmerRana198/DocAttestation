using DocAttestation.Configuration;
using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;
using System.Text;
using System.Text.Json;

namespace DocAttestation.Services;

public class QRCodeService : IQRCodeService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ApplicationDbContext _context;
    private readonly QRCodeSettings _settings;

    public QRCodeService(
        IEncryptionService encryptionService,
        ApplicationDbContext context,
        IOptions<QRCodeSettings> settings)
    {
        _encryptionService = encryptionService;
        _context = context;
        _settings = settings.Value;
    }

    public async Task<string> GenerateQRTokenAsync(int applicationId, string documentHash)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application == null)
            throw new ArgumentException("Application not found");

        // Create token data
        var tokenData = new QRTokenData
        {
            ApplicationId = applicationId,
            DocumentHash = documentHash,
            IssuedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(_settings.TokenExpirationDays),
            Nonce = Guid.NewGuid().ToString()
        };

        // Serialize to JSON
        var json = JsonSerializer.Serialize(tokenData);
        
        // Encrypt the token
        var encryptedToken = _encryptionService.Encrypt(json);

        // Save to database
        var qrToken = new QRVerificationToken
        {
            ApplicationId = applicationId,
            EncryptedToken = encryptedToken,
            IssuedAt = tokenData.IssuedAt,
            ExpiryDate = tokenData.ExpiryDate,
            IsRevoked = false
        };

        _context.QRVerificationTokens.Add(qrToken);
        application.QRToken = encryptedToken;
        application.QRTokenExpiry = tokenData.ExpiryDate;
        await _context.SaveChangesAsync();

        return encryptedToken;
    }

    public string GenerateQRCodeImage(string encryptedToken)
    {
        // Generate QR URL
        var qrUrl = $"{_settings.VerificationBaseUrl}?t={Uri.EscapeDataString(encryptedToken)}";

        // Generate QR Code
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        // Return as base64 string
        return Convert.ToBase64String(qrCodeBytes);
    }

    public async Task<QRTokenData?> DecryptQRTokenAsync(string encryptedToken)
    {
        try
        {
            // Decrypt token
            var decryptedJson = _encryptionService.Decrypt(encryptedToken);
            
            // Deserialize
            var tokenData = JsonSerializer.Deserialize<QRTokenData>(decryptedJson);
            
            if (tokenData == null)
                return null;

            // Verify token exists in database and is not revoked
            var qrToken = await _context.QRVerificationTokens
                .FirstOrDefaultAsync(q => q.EncryptedToken == encryptedToken);

            if (qrToken == null || qrToken.IsRevoked || qrToken.ExpiryDate < DateTime.UtcNow)
                return null;

            return tokenData;
        }
        catch
        {
            return null;
        }
    }
}

