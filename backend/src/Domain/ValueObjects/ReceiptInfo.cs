namespace Domain.ValueObjects;

public class ReceiptInfo
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

