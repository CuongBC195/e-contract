using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Repositories;
using Shared.Helpers;

namespace API.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISignatureRepository _signatureRepository;

    public DocumentService(IDocumentRepository documentRepository, ISignatureRepository signatureRepository)
    {
        _documentRepository = documentRepository;
        _signatureRepository = signatureRepository;
    }

    public async Task<Document> CreateDocumentAsync(DocumentType type, string? title, string? content, Guid? userId, ReceiptInfo? receiptInfo = null, ContractMetadata? metadata = null, SigningMode signingMode = SigningMode.Public)
    {
        var documentId = DocumentIdGenerator.GenerateId(type);
        
        // Ensure unique ID
        while (await _documentRepository.GetByIdAsync(documentId) != null)
        {
            documentId = DocumentIdGenerator.GenerateId(type);
        }

        var document = new Document
        {
            Id = documentId,
            Type = type,
            Title = title,
            Content = content,
            UserId = userId,
            Status = DocumentStatus.Pending,
            SigningMode = signingMode,
            ReceiptInfo = receiptInfo,
            ContractNumber = metadata?.ContractNumber,
            Location = metadata?.Location,
            ContractDate = metadata?.ContractDate
        };

        return await _documentRepository.CreateAsync(document);
    }

    public async Task<Document?> GetDocumentByIdAsync(string id)
    {
        return await _documentRepository.GetByIdWithSignaturesAsync(id);
    }

    public async Task<Document> UpdateDocumentAsync(string id, string? title, string? content, ReceiptInfo? receiptInfo = null, ContractMetadata? metadata = null, SigningMode? signingMode = null)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null)
            throw new InvalidOperationException("Document not found");

        if (document.Status == DocumentStatus.Signed)
            throw new InvalidOperationException("Cannot update fully signed document");

        if (title != null) document.Title = title;
        if (content != null) document.Content = content;
        if (receiptInfo != null) document.ReceiptInfo = receiptInfo;
        if (signingMode.HasValue) document.SigningMode = signingMode.Value;
        if (metadata != null)
        {
            document.ContractNumber = metadata.ContractNumber;
            document.Location = metadata.Location;
            document.ContractDate = metadata.ContractDate;
        }

        document.UpdatedAt = DateTime.UtcNow;
        return await _documentRepository.UpdateAsync(document);
    }

    public async Task<bool> DeleteDocumentAsync(string id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document == null) return false;

        if (document.Status == DocumentStatus.Signed)
            throw new InvalidOperationException("Không thể xóa tài liệu đã được ký đầy đủ");

        return await _documentRepository.DeleteAsync(id);
    }

    public async Task<List<Document>> GetDocumentsAsync(Guid? userId, DocumentStatus? status, DocumentType? type, int page, int pageSize)
    {
        if (userId.HasValue)
            return await _documentRepository.GetByUserIdAsync(userId.Value, status, type, page, pageSize);
        
        return await _documentRepository.GetAllAsync(status, type, page, pageSize);
    }

    public async Task<int> GetDocumentCountAsync(Guid? userId, DocumentStatus? status, DocumentType? type)
    {
        return await _documentRepository.GetTotalCountAsync(userId, status, type);
    }

    public async Task UpdateDocumentStatusAsync(string documentId)
    {
        var document = await _documentRepository.GetByIdWithSignaturesAsync(documentId);
        if (document == null) return;

        var signatureCount = document.Signatures.Count;
        
        if (signatureCount == 0)
        {
            document.Status = DocumentStatus.Pending;
        }
        else if (signatureCount == 1)
        {
            document.Status = DocumentStatus.PartiallySigned;
        }
        else
        {
            document.Status = DocumentStatus.Signed;
            document.SignedAt = DateTime.UtcNow;
        }

        await _documentRepository.UpdateAsync(document);
    }

    public async Task TrackDocumentViewAsync(string documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null) return;

        if (!document.ViewedAt.HasValue)
        {
            document.ViewedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document);
        }
    }
}

