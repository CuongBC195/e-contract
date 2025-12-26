using System.Text.Json;

namespace Domain.Entities;

public class Template
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty; // 'Lao động', 'Bất động sản', etc.
    public string Content { get; set; } = string.Empty; // HTML template content
    public string Icon { get; set; } = "FileText";
    public string Color { get; set; } = "gray";
    public string SignersJson { get; set; } = "[]"; // Array of signer roles
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Helper property for Signers
    public List<string> Signers
    {
        get => JsonSerializer.Deserialize<List<string>>(SignersJson) ?? new List<string>();
        set => SignersJson = JsonSerializer.Serialize(value);
    }
}

