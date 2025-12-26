using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(string id);
    Task<Document?> GetByIdWithSignaturesAsync(string id);
    Task<Document> CreateAsync(Document document);
    Task<Document> UpdateAsync(Document document);
    Task<bool> DeleteAsync(string id);
    Task<List<Document>> GetByUserIdAsync(Guid userId, DocumentStatus? status = null, DocumentType? type = null, int page = 1, int pageSize = 20);
    Task<List<Document>> GetAllAsync(DocumentStatus? status = null, DocumentType? type = null, int page = 1, int pageSize = 20);
    Task<int> GetTotalCountAsync(Guid? userId = null, DocumentStatus? status = null, DocumentType? type = null);
}

