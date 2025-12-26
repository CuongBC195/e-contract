using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace API.Services;

public interface IDocumentService
{
    Task<Document> CreateDocumentAsync(DocumentType type, string? title, string? content, Guid? userId, ReceiptInfo? receiptInfo = null, ContractMetadata? metadata = null, SigningMode signingMode = SigningMode.Public);
    Task<Document?> GetDocumentByIdAsync(string id);
    Task<Document> UpdateDocumentAsync(string id, string? title, string? content, ReceiptInfo? receiptInfo = null, ContractMetadata? metadata = null, SigningMode? signingMode = null);
    Task<bool> DeleteDocumentAsync(string id);
    Task<List<Document>> GetDocumentsAsync(Guid? userId, DocumentStatus? status, DocumentType? type, int page, int pageSize);
    Task<int> GetDocumentCountAsync(Guid? userId, DocumentStatus? status, DocumentType? type);
    Task UpdateDocumentStatusAsync(string documentId);
    Task TrackDocumentViewAsync(string documentId);
}

public class ContractMetadata
{
    public string? ContractNumber { get; set; }
    public string? Location { get; set; }
    public DateTime? ContractDate { get; set; }
}

