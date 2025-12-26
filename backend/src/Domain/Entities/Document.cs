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
    
    // PDF-specific: Signature blocks (tọa độ vùng ký)
    public string? PdfSignatureBlocksJson { get; set; }
    
    public SigningMode SigningMode { get; set; } = SigningMode.Public; // Default: Public signing
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
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
    
    // Helper property for PDF Signature Blocks
    public List<PdfSignatureBlock>? PdfSignatureBlocks
    {
        get 
        {
            if (string.IsNullOrEmpty(PdfSignatureBlocksJson))
                return null;
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // Handle case-insensitive property names
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Handle camelCase property names
                };
                var result = JsonSerializer.Deserialize<List<PdfSignatureBlock>>(PdfSignatureBlocksJson, options);
                return result;
            }
            catch (JsonException ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to deserialize PdfSignatureBlocksJson: {ex.Message}");
                // If deserialization fails, return null
                return null;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Unexpected error deserializing PdfSignatureBlocksJson: {ex.Message}");
                return null;
            }
        }
        set 
        {
            if (value == null)
            {
                PdfSignatureBlocksJson = null;
                return;
            }
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Use camelCase for consistency
                };
                PdfSignatureBlocksJson = JsonSerializer.Serialize(value, options);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to serialize PdfSignatureBlocks: {ex.Message}");
                PdfSignatureBlocksJson = null;
            }
        }
    }
}

