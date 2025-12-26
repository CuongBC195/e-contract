namespace API.DTOs;

public class SendInvitationRequestDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string SigningUrl { get; set; } = string.Empty;
}

