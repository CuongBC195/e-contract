using PuppeteerSharp;

namespace API.Services;

public class PdfService : IPdfService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IConfiguration configuration, ILogger<PdfService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]?> GeneratePdfFromHtmlAsync(string html, string? title = null)
    {
        try
        {
            var chromeExecutablePath = _configuration["Pdf:ChromeExecutablePath"];
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            };

            // If Chrome executable path is provided, use it
            if (!string.IsNullOrEmpty(chromeExecutablePath))
            {
                launchOptions.ExecutablePath = chromeExecutablePath;
                _logger.LogInformation("Using Chrome executable at: {Path}", chromeExecutablePath);
            }
            else
            {
                // Otherwise, ensure Chromium is downloaded
                // PuppeteerSharp will automatically use the downloaded browser
                _logger.LogInformation("Chrome executable path not configured, ensuring Chromium is available...");
                try
                {
                    var browserFetcher = new BrowserFetcher();
                    await browserFetcher.DownloadAsync();
                    _logger.LogInformation("Chromium is available");
                }
                catch (Exception downloadEx)
                {
                    _logger.LogError(downloadEx, "Failed to download Chromium, but continuing - Puppeteer may find system browser");
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
                    Top = "20mm",
                    Right = "20mm",
                    Bottom = "20mm",
                    Left = "20mm"
                }
            };

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("PDF generation returned empty bytes");
                return null;
            }

            _logger.LogInformation("PDF generated successfully, size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF: {Message}", ex.Message);
            return null; // Return null instead of throwing to allow email to be sent without PDF
        }
    }
}

