using Domain.Enums;

namespace API.DTOs;

public class CreateDocumentRequestDto
{
    public DocumentType Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public ReceiptInfoDto? ReceiptInfo { get; set; }
    public ContractMetadataDto? Metadata { get; set; }
    public List<SignerDto> Signers { get; set; } = new();
    public SigningMode SigningMode { get; set; } = SigningMode.Public; // Default: Public
}

public class UpdateDocumentRequestDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public ReceiptInfoDto? ReceiptInfo { get; set; }
    public ContractMetadataDto? Metadata { get; set; }
    public SigningMode? SigningMode { get; set; }
}

public class DocumentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DocumentStatus Status { get; set; }
    public SigningMode SigningMode { get; set; }
    public ReceiptInfoDto? ReceiptInfo { get; set; }
    public ContractMetadataDto? Metadata { get; set; }
    public UserDto? Creator { get; set; }
    public List<SignatureResponseDto> Signatures { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? ViewedAt { get; set; }
}

public class ReceiptInfoDto
{
    public string? SenderName { get; set; }
    public string? SenderAddress { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAddress { get; set; }
    public decimal Amount { get; set; }
    public string? AmountInWords { get; set; }
    public string? Reason { get; set; }
    public string? Location { get; set; }
    public DateTime? Date { get; set; }
    public Dictionary<string, string>? CustomFields { get; set; }
}

public class ContractMetadataDto
{
    public string? ContractNumber { get; set; }
    public string? Location { get; set; }
    public DateTime? ContractDate { get; set; }
}

public class SignerDto
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class SignDocumentRequestDto
{
    public string SignerId { get; set; } = string.Empty;
    public string? SignerRole { get; set; }
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    public SignatureDataDto SignatureData { get; set; } = null!;
}

public class SignatureDataDto
{
    public string Type { get; set; } = string.Empty; // "draw" or "type"
    public string Data { get; set; } = string.Empty;
    public string? FontFamily { get; set; }
    public string? Color { get; set; }
}

public class SignatureResponseDto
{
    public Guid Id { get; set; }
    public string SignerId { get; set; } = string.Empty;
    public string SignerRole { get; set; } = string.Empty;
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    public SignatureDataDto SignatureData { get; set; } = null!;
    public DateTime SignedAt { get; set; }
}

public class PaginatedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

