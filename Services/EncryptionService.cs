using DocAttestation.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DocAttestation.Services;

public class EncryptionService : IEncryptionService
{
    private readonly EncryptionSettings _settings;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IOptions<EncryptionSettings> settings)
    {
        _settings = settings.Value;
        
        // Validate and prepare encryption key
        if (string.IsNullOrEmpty(_settings.AESKey) || _settings.AESKey.Length != 32)
        {
            throw new InvalidOperationException("AESKey must be exactly 32 characters long");
        }
        
        if (string.IsNullOrEmpty(_settings.AESIV) || _settings.AESIV.Length != 16)
        {
            throw new InvalidOperationException("AESIV must be exactly 16 characters long");
        }
        
        _key = Encoding.UTF8.GetBytes(_settings.AESKey);
        _iv = Encoding.UTF8.GetBytes(_settings.AESIV);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor())
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        catch
        {
            throw new CryptographicException("Failed to decrypt data. Invalid cipher text or key.");
        }
    }

    public string ComputeHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }

    public string MaskCNIC(string cnic)
    {
        if (string.IsNullOrEmpty(cnic) || cnic.Length < 13)
            return cnic;

        // Remove dashes if present
        string cleanCNIC = cnic.Replace("-", "");
        
        if (cleanCNIC.Length != 13)
            return cnic;

        // Format: 35202-*******-7
        return $"{cleanCNIC.Substring(0, 5)}-*******-{cleanCNIC.Substring(12, 1)}";
    }
}

