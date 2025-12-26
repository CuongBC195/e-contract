using Domain.Enums;
using Domain.ValueObjects;
using System.Text.Json;

namespace Domain.Entities;

public class Document
{
    public string Id { get; set; } = string.Empty; // Format: "3DO-XXXXXX" or "REC-XXXXXX"
    public DocumentType Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; } // HTML for contracts
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    
    // Receipt-specific (JSON)
    public string? ReceiptInfoJson { get; set; }
    
    // Contract-specific
    public string? ContractNumber { get; set; }
    public string? Location { get; set; }
    public DateTime? ContractDate { get; set; }
    
    // Metadata
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? PdfUrl { get; set; }
    
    public SigningMode SigningMode { get; set; } = SigningMode.Public; // Default: Public signing
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    
    // Helper property for ReceiptInfo
    public ReceiptInfo? ReceiptInfo
    {
        get => string.IsNullOrEmpty(ReceiptInfoJson) 
            ? null 
            : JsonSerializer.Deserialize<ReceiptInfo>(ReceiptInfoJson);
        set => ReceiptInfoJson = value == null 
            ? null 
            : JsonSerializer.Serialize(value);
    }
}

