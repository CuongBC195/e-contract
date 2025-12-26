using Domain.Entities;

namespace Infrastructure.Repositories;

public interface ISignatureRepository
{
    Task<Signature?> GetByIdAsync(Guid id);
    Task<Signature> CreateAsync(Signature signature);
    Task<List<Signature>> GetByDocumentIdAsync(string documentId);
}

