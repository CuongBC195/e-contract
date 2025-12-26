using Domain.ValueObjects;
using System.Text.Json;

namespace Domain.Entities;

public class Signature
{
    public Guid Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public Document Document { get; set; } = null!;
    
    public string SignerId { get; set; } = string.Empty; // Role-based identifier
    public string SignerRole { get; set; } = string.Empty; // "Bên A", "Bên B", etc.
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    
    public string SignatureDataJson { get; set; } = string.Empty; // JSON
    
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Helper property for SignatureData
    public SignatureData SignatureData
    {
        get => JsonSerializer.Deserialize<SignatureData>(SignatureDataJson) 
            ?? new SignatureData();
        set => SignatureDataJson = JsonSerializer.Serialize(value);
    }
}

