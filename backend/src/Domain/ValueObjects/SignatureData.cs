using Domain.Enums;

namespace Domain.ValueObjects;

public class SignatureData
{
    public SignatureType Type { get; set; } // "draw" or "type"
    public string Data { get; set; } = string.Empty; // Vector data or text
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
}

