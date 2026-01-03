using Domain.ValueObjects;

namespace API.Services;

public interface IPdfSignatureService
{
    /// <summary>
    /// Apply signature image to PDF at specified coordinates
    /// </summary>
    /// <param name="pdfFilePath">Path to the original PDF file</param>
    /// <param name="signatureImageBase64">Base64 encoded signature image</param>
    /// <param name="signatureBlock">Signature block with coordinates</param>
    /// <returns>Path to the signed PDF file</returns>
    Task<string> ApplySignatureToPdfAsync(string pdfFilePath, string signatureImageBase64, PdfSignatureBlock signatureBlock);
    
    /// <summary>
    /// Delete PDF file from storage
    /// </summary>
    Task DeletePdfFileAsync(string pdfFilePath);
}

