using DocAttestation.Configuration;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Options;
using QRCoder;
using System.Security.Cryptography;

namespace DocAttestation.Services;

public class PdfStampingService : IPdfStampingService
{
    private readonly FileUploadSettings _settings;

    public PdfStampingService(IOptions<FileUploadSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> StampPdfAsync(string originalPdfPath, string qrCodeBase64, string outputPath)
    {
        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Read original PDF
        using var reader = new PdfReader(originalPdfPath);
        using var writer = new PdfWriter(outputPath);
        using var pdfDoc = new PdfDocument(reader, writer);
        using var document = new Document(pdfDoc);

        // Get last page
        var numberOfPages = pdfDoc.GetNumberOfPages();
        var lastPage = pdfDoc.GetPage(numberOfPages);

        // Convert QR code base64 to image
        var qrBytes = Convert.FromBase64String(qrCodeBase64);
        var qrImageData = iText.IO.Image.ImageDataFactory.Create(qrBytes);

        // Add QR code to bottom-right corner
        var pageSize = lastPage.GetPageSize();
        var qrWidth = 100f; // Adjust size as needed
        var qrHeight = 100f;
        var margin = 20f;

        var qrImageElement = new iText.Layout.Element.Image(qrImageData)
            .SetWidth(qrWidth)
            .SetHeight(qrHeight)
            .SetFixedPosition(
                pageSize.GetWidth() - qrWidth - margin,
                margin,
                qrWidth);

        document.Add(qrImageElement);

        // Add watermark text
        var watermark = new Paragraph("Digitally Attested")
            .SetFontSize(12)
            .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY)
            .SetOpacity(0.7f)
            .SetFixedPosition(
                pageSize.GetWidth() - qrWidth - margin,
                margin + qrHeight + 5,
                qrWidth)
            .SetTextAlignment(TextAlignment.CENTER);

        document.Add(watermark);

        document.Close();

        return outputPath;
    }

    public string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

