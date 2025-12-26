using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SignatureRepository : ISignatureRepository
{
    private readonly ApplicationDbContext _context;

    public SignatureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Signature?> GetByIdAsync(Guid id)
    {
        return await _context.Signatures
            .Include(s => s.Document)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Signature> CreateAsync(Signature signature)
    {
        _context.Signatures.Add(signature);
        await _context.SaveChangesAsync();
        return signature;
    }

    public async Task<List<Signature>> GetByDocumentIdAsync(string documentId)
    {
        return await _context.Signatures
            .Where(s => s.DocumentId == documentId)
            .OrderBy(s => s.SignedAt)
            .ToListAsync();
    }
}

