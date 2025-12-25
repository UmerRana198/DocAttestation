namespace DocAttestation.Configuration;

public class FileUploadSettings
{
    public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string UploadPath { get; set; } = string.Empty;
}

