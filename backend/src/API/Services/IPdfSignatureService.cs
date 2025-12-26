using Domain.ValueObjects;

namespace API.Services;

public interface IPdfSignatureService
{
    /// <summary>
    /// Save uploaded PDF file to storage and return file path
    /// </summary>
    Task<string> SavePdfFileAsync(IFormFile pdfFile, string documentId);
    
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
    
    /// <summary>
    /// Merge PDF with footer containing signatures
    /// </summary>
    /// <param name="originalPdfPath">Path to the original PDF file</param>
    /// <param name="footerHtml">HTML content for the footer page</param>
    /// <returns>Path to the merged PDF file</returns>
    Task<string> MergePdfWithFooterAsync(string originalPdfPath, string footerHtml);
}

