using Domain.ValueObjects;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
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

    public async Task<string> SavePdfFileAsync(IFormFile pdfFile, string documentId)
    {
        if (pdfFile == null || pdfFile.Length == 0)
            throw new ArgumentException("PDF file is required");

        if (!pdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File must be a PDF");

        // Validate file size (max 50MB)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (pdfFile.Length > maxFileSize)
            throw new ArgumentException("PDF file size must be less than 50MB");

        var fileName = $"{documentId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(_pdfStoragePath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await pdfFile.CopyToAsync(stream);
        }

        _logger.LogInformation("PDF file saved: {FilePath}", filePath);
        return filePath;
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

    public async Task<string> MergePdfWithFooterAsync(string originalPdfPath, string footerHtml)
    {
        if (!File.Exists(originalPdfPath))
            throw new FileNotFoundException("Original PDF file not found", originalPdfPath);

        var outputFileName = Path.GetFileNameWithoutExtension(originalPdfPath) + "_merged.pdf";
        var outputFilePath = Path.Combine(_pdfStoragePath, outputFileName);

        try
        {
            // Step 1: Generate footer PDF from HTML using PuppeteerSharp
            var footerPdfBytes = await GenerateFooterPdfAsync(footerHtml);
            if (footerPdfBytes == null || footerPdfBytes.Length == 0)
                throw new InvalidOperationException("Failed to generate footer PDF");

            // Step 2: Merge original PDF with footer PDF using iText7
            using (var originalPdfStream = new FileStream(originalPdfPath, FileMode.Open, FileAccess.Read))
            using (var footerPdfStream = new MemoryStream(footerPdfBytes))
            using (var outputPdfStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                using var originalPdfReader = new PdfReader(originalPdfStream);
                using var footerPdfReader = new PdfReader(footerPdfStream);
                using var pdfWriter = new PdfWriter(outputPdfStream);
                using var mergedPdfDoc = new PdfDocument(pdfWriter);

                // Copy all pages from original PDF
                using var originalPdfDoc = new PdfDocument(originalPdfReader);
                originalPdfDoc.CopyPagesTo(1, originalPdfDoc.GetNumberOfPages(), mergedPdfDoc);

                // Copy footer page(s) from footer PDF
                using var footerPdfDoc = new PdfDocument(footerPdfReader);
                footerPdfDoc.CopyPagesTo(1, footerPdfDoc.GetNumberOfPages(), mergedPdfDoc);

                mergedPdfDoc.Close();
            }

            _logger.LogInformation("PDF merged with footer: {OutputFilePath}", outputFilePath);
            return outputFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging PDF with footer: {Message}", ex.Message);
            throw new InvalidOperationException($"Error merging PDF with footer: {ex.Message}", ex);
        }
    }

    private async Task<byte[]?> GenerateFooterPdfAsync(string html)
    {
        try
        {
            var chromeExecutablePath = _configuration["Pdf:ChromeExecutablePath"];
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            };

            if (!string.IsNullOrEmpty(chromeExecutablePath))
            {
                launchOptions.ExecutablePath = chromeExecutablePath;
            }
            else
            {
                try
                {
                    var browserFetcher = new BrowserFetcher();
                    await browserFetcher.DownloadAsync();
                }
                catch (Exception downloadEx)
                {
                    _logger.LogWarning(downloadEx, "Failed to download Chromium, continuing...");
                }
            }

            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();
            
            await page.SetContentAsync(html);
            await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions { Timeout = 5000 });
            
            var pdfOptions = new PdfOptions
            {
                Format = PuppeteerSharp.Media.PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "15mm",
                    Right = "15mm",
                    Bottom = "15mm",
                    Left = "15mm"
                }
            };

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating footer PDF from HTML");
            return null;
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

