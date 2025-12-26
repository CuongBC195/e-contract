namespace Domain.ValueObjects;

/// <summary>
/// Represents a signature block position on a PDF page
/// Coordinates are stored as percentages to maintain accuracy across different screen sizes
/// </summary>
public class PdfSignatureBlock
{
    /// <summary>
    /// Unique identifier for this signature block
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Page number (0-based index)
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// X coordinate as percentage of page width (0-100)
    /// </summary>
    public double XPercent { get; set; }
    
    /// <summary>
    /// Y coordinate as percentage of page height (0-100)
    /// </summary>
    public double YPercent { get; set; }
    
    /// <summary>
    /// Width as percentage of page width (0-100)
    /// </summary>
    public double WidthPercent { get; set; }
    
    /// <summary>
    /// Height as percentage of page height (0-100)
    /// </summary>
    public double HeightPercent { get; set; }
    
    /// <summary>
    /// Signer role (e.g., "Bên A", "Bên B")
    /// </summary>
    public string SignerRole { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this signature block has been signed
    /// </summary>
    public bool IsSigned { get; set; } = false;
    
    /// <summary>
    /// Signature ID if already signed
    /// </summary>
    public Guid? SignatureId { get; set; }
}

