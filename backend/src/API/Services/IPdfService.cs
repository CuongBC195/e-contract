namespace API.Services;

public interface IPdfService
{
    Task<byte[]?> GeneratePdfFromHtmlAsync(string html, string? title = null);
}

