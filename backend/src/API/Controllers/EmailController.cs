using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailService emailService,
        IDocumentService documentService,
        ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("send-invitation")]
    public async Task<ActionResult<ApiResponse>> SendInvitation([FromBody] SendInvitationRequestDto request)
    {
        var document = await _documentService.GetDocumentByIdAsync(request.DocumentId);
        if (document == null)
            return NotFound(ApiResponse.NotFound("Không tìm thấy tài liệu"));

        await _emailService.SendInvitationEmailAsync(
            request.CustomerEmail,
            request.CustomerName,
            request.DocumentId,
            document.Title ?? "Tài liệu",
            request.SigningUrl
        );

        return Ok(ApiResponse.Success("Đã gửi email mời ký thành công"));
    }
}

