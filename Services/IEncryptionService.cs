namespace DocAttestation.Services;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string ComputeHash(string input);
    string MaskCNIC(string cnic);
}

