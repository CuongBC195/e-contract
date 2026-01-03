using Domain.ValueObjects;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using PuppeteerSharp;

namespace API.Services;

public class PdfSignatureService : IPdfSignatureService
{
    private readonly string _pdfStoragePath;
    private readonly ILogger<PdfSignatureService> _logger;
    private readonly IConfiguration _configuration;

    public PdfSignatureService(IConfiguration configuration, ILogger<PdfSignatureService> logger)
    {
        _configuration = configuration;
        // Get PDF storage path from configuration or use default
        _pdfStoragePath = configuration["FileStorage:PdfPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs");
        _logger = logger;
        
        // Ensure directory exists
        if (!Directory.Exists(_pdfStoragePath))
        {
            Directory.CreateDirectory(_pdfStoragePath);
        }
    }


    public async Task<string> ApplySignatureToPdfAsync(string pdfFilePath, string signatureImageBase64, PdfSignatureBlock signatureBlock)
    {
        if (!File.Exists(pdfFilePath))
            throw new FileNotFoundException("PDF file not found", pdfFilePath);

        // Decode base64 signature image
        byte[] signatureImageBytes;
        try
        {
            var base64String = signatureImageBase64;
            // Remove data URL prefix if present
            if (base64String.Contains(","))
            {
                base64String = base64String.Substring(base64String.IndexOf(",") + 1);
            }
            signatureImageBytes = Convert.FromBase64String(base64String);
            
            if (signatureImageBytes.Length == 0)
            {
                throw new ArgumentException("Signature image data is empty");
            }
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid base64 signature image data");
            throw new ArgumentException("Invalid signature image format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding signature image");
            throw new ArgumentException("Error processing signature image", ex);
        }
        
        // Create output file path
        var outputFileName = Path.GetFileNameWithoutExtension(pdfFilePath) + "_signed.pdf";
        var outputFilePath = Path.Combine(_pdfStoragePath, outputFileName);

        try
        {
            // Read PDF
            using var pdfReader = new PdfReader(pdfFilePath);
            using var pdfWriter = new PdfWriter(outputFilePath);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);
            
            // Validate page number
            var totalPages = pdfDoc.GetNumberOfPages();
            var targetPage = signatureBlock.PageNumber + 1; // iText uses 1-based page numbers
            if (targetPage < 1 || targetPage > totalPages)
            {
                throw new ArgumentException($"Invalid page number: {signatureBlock.PageNumber}. PDF has {totalPages} pages.");
            }
            
            // Get the page to sign (0-based index)
            var page = pdfDoc.GetPage(targetPage);
            var pageSize = page.GetPageSize();
            
            // Calculate absolute coordinates from percentages
            var x = pageSize.GetWidth() * signatureBlock.XPercent / 100.0;
            var y = pageSize.GetHeight() * (100 - signatureBlock.YPercent - signatureBlock.HeightPercent) / 100.0; // Y is from bottom in PDF
            var width = pageSize.GetWidth() * signatureBlock.WidthPercent / 100.0;
            var height = pageSize.GetHeight() * signatureBlock.HeightPercent / 100.0;
            
            // Validate coordinates
            if (x < 0 || y < 0 || width <= 0 || height <= 0)
            {
                throw new ArgumentException($"Invalid signature block coordinates: x={x}, y={y}, width={width}, height={height}");
            }
            
            // Create image from bytes
            iText.IO.Image.ImageData imageData;
            try
            {
                imageData = ImageDataFactory.Create(signatureImageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating image from bytes");
                throw new ArgumentException("Invalid image format. Supported formats: PNG, JPEG", ex);
            }
            
            // Use Canvas to add image directly to the page (recommended approach for adding to existing PDF)
            try
            {
                using var canvas = new Canvas(new PdfCanvas(page, true), pageSize);
                var image = new iText.Layout.Element.Image(imageData);
                
                // Set image position and size
                image.SetFixedPosition((float)x, (float)y);
                image.SetWidth((float)width);
                image.SetHeight((float)height);
                
                // Add image to canvas
                canvas.Add(image);
                canvas.Close();
                
                _logger.LogInformation("Image added to page {PageNumber} at position ({X}, {Y}) with size ({Width}, {Height})", 
                    targetPage, x, y, width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding image to PDF canvas. Page: {Page}, Position: ({X}, {Y}), Size: ({Width}, {Height})", 
                    targetPage, x, y, width, height);
                throw;
            }
            
            pdfDoc.Close();
            
            _logger.LogInformation("Signature applied to PDF: {OutputFilePath}", outputFilePath);
            return outputFilePath;
        }
        catch (iText.Kernel.Exceptions.PdfException ex)
        {
            _logger.LogError(ex, "PDF processing error: {Message}", ex.Message);
            throw new InvalidOperationException($"PDF processing error: {ex.Message}", ex);
        }
        catch (ArgumentException ex)
        {
            // Re-throw ArgumentException as-is (already logged)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying signature to PDF: {Message}\nStackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            throw new InvalidOperationException($"Error applying signature to PDF: {ex.Message}", ex);
        }
    }

    public async Task DeletePdfFileAsync(string pdfFilePath)
    {
        if (File.Exists(pdfFilePath))
        {
            await Task.Run(() => File.Delete(pdfFilePath));
            _logger.LogInformation("PDF file deleted: {FilePath}", pdfFilePath);
        }
    }

}

// Extension method to read all bytes from stream
public static class StreamExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }
        
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}

