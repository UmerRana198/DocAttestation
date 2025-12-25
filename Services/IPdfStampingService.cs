namespace DocAttestation.Services;

public interface IPdfStampingService
{
    Task<string> StampPdfAsync(string originalPdfPath, string qrCodeBase64, string outputPath);
    string ComputeFileHash(string filePath);
}

