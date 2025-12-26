using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(string id)
    {
        return await _context.Documents
            .Include(d => d.User)
            .Include(d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document?> GetByIdWithSignaturesAsync(string id)
    {
        return await _context.Documents
            .Include(d => d.User)
            .Include(d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document> CreateAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document> UpdateAsync(Document document)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) return false;
        
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Document>> GetByUserIdAsync(Guid userId, DocumentStatus? status = null, DocumentType? type = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Documents
            .Where(d => d.UserId == userId)
            .Include(d => d.Signatures)
            .AsQueryable();
        
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        
        if (type.HasValue)
            query = query.Where(d => d.Type == type.Value);
        
        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Document>> GetAllAsync(DocumentStatus? status = null, DocumentType? type = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Documents
            .Include(d => d.User)
            .Include(d => d.Signatures)
            .AsQueryable();
        
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        
        if (type.HasValue)
            query = query.Where(d => d.Type == type.Value);
        
        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(Guid? userId = null, DocumentStatus? status = null, DocumentType? type = null)
    {
        var query = _context.Documents.AsQueryable();
        
        if (userId.HasValue)
            query = query.Where(d => d.UserId == userId.Value);
        
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        
        if (type.HasValue)
            query = query.Where(d => d.Type == type.Value);
        
        return await query.CountAsync();
    }
}

