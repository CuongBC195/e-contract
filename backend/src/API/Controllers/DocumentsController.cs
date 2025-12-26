using API.DTOs;
using API.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;
using Shared.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ISignatureService _signatureService;
    private readonly IPdfService _pdfService;
    private readonly IPdfSignatureService _pdfSignatureService;
    private readonly IEmailService _emailService;
    private readonly IAuthService _authService;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ISignatureService signatureService,
        IPdfService pdfService,
        IPdfSignatureService pdfSignatureService,
        IEmailService emailService,
        IAuthService authService,
        IDocumentRepository documentRepository,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _signatureService = signatureService;
        _pdfService = pdfService;
        _pdfSignatureService = pdfSignatureService;
        _emailService = emailService;
        _authService = authService;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> CreateDocument([FromBody] CreateDocumentRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<DocumentResponseDto>.Unauthorized());

        ReceiptInfo? receiptInfo = null;
        if (request.ReceiptInfo != null)
        {
            receiptInfo = new ReceiptInfo
            {
                SenderName = request.ReceiptInfo.SenderName,
                SenderAddress = request.ReceiptInfo.SenderAddress,
                ReceiverName = request.ReceiptInfo.ReceiverName,
                ReceiverAddress = request.ReceiptInfo.ReceiverAddress,
                Amount = request.ReceiptInfo.Amount,
                AmountInWords = request.ReceiptInfo.AmountInWords ?? NumberToWordsConverter.Convert(request.ReceiptInfo.Amount),
                Reason = request.ReceiptInfo.Reason,
                Location = request.ReceiptInfo.Location,
                Date = request.ReceiptInfo.Date,
                CustomFields = request.ReceiptInfo.CustomFields
            };
        }

        ContractMetadata? metadata = null;
        if (request.Metadata != null)
        {
            metadata = new ContractMetadata
            {
                ContractNumber = request.Metadata.ContractNumber,
                Location = request.Metadata.Location,
                ContractDate = request.Metadata.ContractDate
            };
        }

        var document = await _documentService.CreateDocumentAsync(
            request.Type,
            request.Title,
            request.Content,
            userId,
            receiptInfo,
            metadata,
            request.SigningMode
        );

        var documentDto = MapToDto(document);
        return StatusCode(201, ApiResponse<DocumentResponseDto>.Created(documentDto, "Tạo tài liệu thành công"));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PaginatedResponseDto<DocumentResponseDto>>>> GetDocuments(
        [FromQuery] DocumentStatus? status,
        [FromQuery] DocumentType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 4)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        
        Guid? userId = null;
        if (roleClaim != "Admin" && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var documents = await _documentService.GetDocumentsAsync(userId, status, type, page, pageSize);
        var totalCount = await _documentService.GetDocumentCountAsync(userId, status, type);

        var paginatedResponse = new PaginatedResponseDto<DocumentResponseDto>
        {
            Items = documents.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResponseDto<DocumentResponseDto>>.Success(paginatedResponse, "Lấy danh sách tài liệu thành công"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> GetDocument(string id)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

        var documentDto = MapToDto(document);
        return Ok(ApiResponse<DocumentResponseDto>.Success(documentDto, "Lấy thông tin tài liệu thành công"));
    }

    [HttpGet("pdf/{fileName}")]
    public async Task<ActionResult> GetPdfFile(string fileName)
    {
        try
        {
            var pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", fileName);
            
            if (!System.IO.File.Exists(pdfFilePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfFilePath);
            
            // Set Content-Disposition to inline (display in browser) instead of attachment (download)
            // Use FileContentResult to ensure header is set correctly before returning
            var result = new FileContentResult(fileBytes, "application/pdf")
            {
                FileDownloadName = null // Don't set FileDownloadName to prevent download
            };
            
            // Set Content-Disposition header explicitly
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving PDF file: {FileName}", fileName);
            return StatusCode(500, "Error serving PDF file");
        }
    }

    [HttpGet("{id}/export-pdf")]
    public async Task<ActionResult> ExportPdfWithSignatures(string id)
    {
        try
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound("Không tìm thấy tài liệu");

            if (document.Type != DocumentType.Pdf || string.IsNullOrEmpty(document.PdfUrl))
                return BadRequest("Chỉ hỗ trợ cho tài liệu PDF");

            // Get original PDF file path
            var pdfFileName = Path.GetFileName(document.PdfUrl.Replace("/api/documents/pdf/", ""));
            var originalPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", pdfFileName);
            
            if (!System.IO.File.Exists(originalPdfPath))
                return NotFound("Không tìm thấy file PDF gốc");

            // Generate footer HTML with signatures
            var footerHtml = GeneratePdfFooterHtml(document);

            // Merge PDF with footer
            var mergedPdfPath = await _pdfSignatureService.MergePdfWithFooterAsync(originalPdfPath, footerHtml);

            // Read merged PDF and return
            var mergedPdfBytes = await System.IO.File.ReadAllBytesAsync(mergedPdfPath);
            var outputFileName = $"{document.Id}_signed_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            
            // Set Content-Disposition to attachment to force download (this is the export endpoint)
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{outputFileName}\"");
            
            return File(mergedPdfBytes, "application/pdf", outputFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting PDF with signatures: {Message}", ex.Message);
            return StatusCode(500, $"Lỗi khi xuất PDF: {ex.Message}");
        }
    }

    private string GeneratePdfFooterHtml(Domain.Entities.Document document)
    {
        // Generate signature HTML (similar to GenerateDocumentHtml but only footer part)
        var signaturesHtml = string.Join("", document.Signatures.OrderBy(s => s.SignedAt).Select(s =>
        {
            var signatureHtml = "";
            var signatureData = s.SignatureData;
            
            if (signatureData.Type == Domain.Enums.SignatureType.Draw && !string.IsNullOrEmpty(signatureData.Data))
            {
                signatureHtml = ConvertSignaturePointsToSvg(signatureData.Data, signatureData.Color ?? "#000000");
            }
            else if (signatureData.Type == Domain.Enums.SignatureType.Type && !string.IsNullOrEmpty(signatureData.Data))
            {
                var fontFamily = signatureData.FontFamily ?? "Times New Roman, serif";
                var color = signatureData.Color ?? "#000000";
                signatureHtml = $@"<div style=""font-family: {fontFamily}; color: {color}; font-size: 24px; font-weight: bold; padding: 10px 0; border-bottom: 2px solid {color};"">{EscapeHtml(signatureData.Data)}</div>";
            }
            else
            {
                signatureHtml = $@"<div style=""font-size: 18px; font-weight: bold; padding: 10px 0; border-bottom: 2px solid #ccc;"">{EscapeHtml(s.SignerName ?? "")}</div>";
            }
            
            return $@"
            <div style=""margin: 20px 0; padding: 15px 0; border-bottom: 1px solid #eee;"">
                <div style=""margin-bottom: 10px;"">{signatureHtml}</div>
                <div style=""margin-top: 10px; font-size: 12px; color: #666;"">
                    <div><strong>{EscapeHtml(s.SignerRole)}:</strong> {EscapeHtml(s.SignerName ?? "")}</div>
                    {(string.IsNullOrEmpty(s.SignerEmail) ? "" : $"<div><strong>Email:</strong> {EscapeHtml(s.SignerEmail)}</div>")}
                    <div><strong>Ngày ký:</strong> {s.SignedAt:dd/MM/yyyy HH:mm}</div>
                </div>
            </div>";
        }));

        var location = document.Location ?? "TP. Cần Thơ";
        var dateString = document.ContractDate.HasValue 
            ? FormatDateToVietnamese(document.ContractDate.Value)
            : FormatDateToVietnamese(document.CreatedAt);

        var html = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Chữ ký - {EscapeHtml(document.Title ?? "Tài liệu")}</title>
    <style>
        @page {{
            margin: 15mm;
        }}
        body {{
            font-family: 'Times New Roman', 'Tinos', serif;
            font-size: 12pt;
            line-height: 1.6;
            color: #000;
            max-width: 210mm;
            margin: 0 auto;
            padding: 20px;
        }}
        .date-location {{
            text-align: right;
            margin-bottom: 30px;
            font-size: 11pt;
            color: #333;
        }}
        .signatures {{
            margin-top: 30px;
        }}
        .signature-item {{
            margin: 20px 0;
            padding: 15px 0;
            border-bottom: 1px solid #eee;
        }}
        .signature-svg {{
            max-width: 250px;
            max-height: 100px;
            margin-bottom: 10px;
        }}
        .signature-info {{
            margin-top: 10px;
            font-size: 11pt;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class=""date-location"">
        {EscapeHtml(location)}, {dateString}
    </div>
    <div class=""signatures"">
        <h2 style=""font-size: 14pt; margin-bottom: 20px; border-bottom: 1px solid #ccc; padding-bottom: 10px;"">Chữ ký các bên</h2>
        {signaturesHtml}
    </div>
</body>
</html>";

        return html;
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> UpdateDocument(string id, [FromBody] UpdateDocumentRequestDto request)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (roleClaim != "Admin" && (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || document.UserId != userId))
            return StatusCode(403, ApiResponse<DocumentResponseDto>.Forbidden());

            ReceiptInfo? receiptInfo = null;
            if (request.ReceiptInfo != null)
            {
                receiptInfo = new ReceiptInfo
                {
                    SenderName = request.ReceiptInfo.SenderName,
                    SenderAddress = request.ReceiptInfo.SenderAddress,
                    ReceiverName = request.ReceiptInfo.ReceiverName,
                    ReceiverAddress = request.ReceiptInfo.ReceiverAddress,
                    Amount = request.ReceiptInfo.Amount,
                    AmountInWords = request.ReceiptInfo.AmountInWords,
                    Reason = request.ReceiptInfo.Reason,
                    Location = request.ReceiptInfo.Location,
                    Date = request.ReceiptInfo.Date,
                    CustomFields = request.ReceiptInfo.CustomFields
                };
            }

            ContractMetadata? metadata = null;
            if (request.Metadata != null)
            {
                // Parse CreatedDate string to DateTime if provided
                DateTime? contractDate = request.Metadata.ContractDate;
                if (!contractDate.HasValue && !string.IsNullOrEmpty(request.Metadata.CreatedDate))
                {
                    // Try to parse the date string (format: "ngày DD tháng MM năm YYYY" or standard formats)
                    if (DateTime.TryParse(request.Metadata.CreatedDate, out var parsedDate))
                    {
                        contractDate = parsedDate;
                    }
                }
                
                metadata = new ContractMetadata
                {
                    ContractNumber = request.Metadata.ContractNumber,
                    Location = request.Metadata.Location,
                    ContractDate = contractDate
                };
            }

            var updated = await _documentService.UpdateDocumentAsync(id, request.Title, request.Content, receiptInfo, metadata, request.SigningMode);
            var updatedDto = MapToDto(updated);
            return Ok(ApiResponse<DocumentResponseDto>.Success(updatedDto, "Cập nhật tài liệu thành công"));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteDocument(string id)
    {
        // Get document without IsDeleted filter for deletion check
        var document = await _documentRepository.GetByIdForDeleteAsync(id);
        if (document == null)
            return NotFound(ApiResponse.NotFound("Không tìm thấy tài liệu"));

        // Check if already deleted
        if (document.IsDeleted)
            return NotFound(ApiResponse.NotFound("Tài liệu đã bị xóa"));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (roleClaim != "Admin" && (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || document.UserId != userId))
            return StatusCode(403, ApiResponse.Forbidden());

        try
        {
            // If it's a PDF document, delete the PDF file first
            if (document.Type == DocumentType.Pdf && !string.IsNullOrEmpty(document.PdfUrl))
            {
                try
                {
                    // Extract filename from PdfUrl (e.g., "/api/documents/pdf/PDF-123_file.pdf")
                    var pdfFileName = Path.GetFileName(document.PdfUrl.Replace("/api/documents/pdf/", ""));
                    var pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", pdfFileName);
                    
                    if (System.IO.File.Exists(pdfFilePath))
                    {
                        await _pdfSignatureService.DeletePdfFileAsync(pdfFilePath);
                        _logger.LogInformation("PDF file deleted: {FilePath}", pdfFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete PDF file for document {DocumentId}, continuing with document deletion", id);
                    // Continue with document deletion even if PDF file deletion fails
                }
            }

            var deleted = await _documentService.DeleteDocumentAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse.NotFound("Không tìm thấy tài liệu"));
            }
            
            return Ok(ApiResponse<object>.Success(new object(), "Xóa tài liệu thành công"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Error(ex.Message, new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}: {Message}", id, ex.Message);
            return StatusCode(500, ApiResponse<object>.Error("Lỗi khi xóa tài liệu", new List<string> { ex.Message }));
        }
    }

    [HttpPut("{id}/pdf-signature-blocks")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> UpdatePdfSignatureBlocks(string id, [FromBody] List<PdfSignatureBlockDto> signatureBlocks)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

        if (document.Type != DocumentType.Pdf)
            return BadRequest(ApiResponse<DocumentResponseDto>.Error("Chỉ hỗ trợ cho tài liệu PDF", new List<string> { "Chỉ hỗ trợ cho tài liệu PDF" }));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (roleClaim != "Admin" && (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) || document.UserId != userId))
            return StatusCode(403, ApiResponse<DocumentResponseDto>.Forbidden());

        try
        {
            // Validate signature blocks
            if (signatureBlocks == null)
            {
                return BadRequest(ApiResponse<DocumentResponseDto>.Error("Danh sách vị trí ký không được để trống", new List<string> { "Danh sách vị trí ký không được để trống" }));
            }

            // Convert DTOs to ValueObjects - preserve IDs from frontend
            var pdfSignatureBlocks = signatureBlocks.Select(block => 
            {
                var vo = new Domain.ValueObjects.PdfSignatureBlock
                {
                    Id = string.IsNullOrEmpty(block.Id) ? Guid.NewGuid().ToString() : block.Id, // Preserve ID from frontend, generate new if empty
                    PageNumber = block.PageNumber,
                    XPercent = block.XPercent,
                    YPercent = block.YPercent,
                    WidthPercent = block.WidthPercent,
                    HeightPercent = block.HeightPercent,
                    SignerRole = block.SignerRole
                };
                _logger.LogInformation("Converting block DTO to VO. DTO ID: {DtoId}, VO ID: {VoId}, Role: {Role}", block.Id, vo.Id, vo.SignerRole);
                return vo;
            }).ToList();

            _logger.LogInformation("Saving {Count} signature blocks for document {DocumentId}. Block IDs: {BlockIds}", 
                pdfSignatureBlocks.Count, id, string.Join(", ", pdfSignatureBlocks.Select(b => $"{b.Id}")));

            document.PdfSignatureBlocks = pdfSignatureBlocks;
            document.UpdatedAt = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document);

            // Reload document to get the latest data and verify blocks were saved correctly
            document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

            // Verify blocks were saved correctly
            var savedBlocks = document.PdfSignatureBlocks ?? new List<Domain.ValueObjects.PdfSignatureBlock>();
            _logger.LogInformation("Document reloaded. Saved blocks count: {Count}. Block IDs: {BlockIds}", 
                savedBlocks.Count, string.Join(", ", savedBlocks.Select(b => $"{b.Id}")));

            var documentDto = MapToDto(document);
            return Ok(ApiResponse<DocumentResponseDto>.Success(documentDto, "Cập nhật vị trí ký thành công"));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating PDF signature blocks: {Message}", ex.Message);
            var innerEx = ex.InnerException;
            var errorMsg = "Lỗi cơ sở dữ liệu";
            if (innerEx != null)
            {
                errorMsg = innerEx.Message.Contains("value too long") || innerEx.Message.Contains("character varying")
                    ? "Dữ liệu quá lớn. Vui lòng giảm số lượng vị trí ký."
                    : innerEx.Message;
            }
            return StatusCode(500, ApiResponse<DocumentResponseDto>.Error("Lỗi khi cập nhật vị trí ký", new List<string> { errorMsg }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PDF signature blocks: {Message}\nStackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, ApiResponse<DocumentResponseDto>.Error("Lỗi khi cập nhật vị trí ký", new List<string> { ex.Message ?? "Lỗi không xác định" }));
        }
    }

    [HttpPost("{id}/apply-pdf-signature")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> ApplyPdfSignature(string id, [FromBody] ApplyPdfSignatureRequestDto request)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

        if (document.Type != DocumentType.Pdf || string.IsNullOrEmpty(document.PdfUrl))
            return BadRequest(ApiResponse<DocumentResponseDto>.Error("Chỉ hỗ trợ cho tài liệu PDF", new List<string> { "Chỉ hỗ trợ cho tài liệu PDF" }));

        try
        {
            // Reload document to ensure we have the latest PdfSignatureBlocks
            document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

            // Find signature block
            var signatureBlocks = document.PdfSignatureBlocks ?? new List<Domain.ValueObjects.PdfSignatureBlock>();
            _logger.LogInformation("Looking for signature block. DocumentId: {DocumentId}, RequestBlockId: {RequestBlockId}, Total blocks: {BlockCount}", 
                id, request.SignatureBlockId, signatureBlocks.Count);
            
            if (signatureBlocks.Count > 0)
            {
                _logger.LogInformation("Available block IDs: {BlockIds}", string.Join(", ", signatureBlocks.Select(b => $"{b.Id} (Role: {b.SignerRole}, Signed: {b.IsSigned})")));
            }
            
            var signatureBlock = signatureBlocks.FirstOrDefault(b => b.Id == request.SignatureBlockId);
            
            if (signatureBlock == null)
            {
                _logger.LogWarning("Signature block not found. DocumentId: {DocumentId}, BlockId: {BlockId}, Available blocks: {BlockIds}", 
                    id, request.SignatureBlockId, string.Join(", ", signatureBlocks.Select(b => b.Id)));
                return BadRequest(ApiResponse<DocumentResponseDto>.Error("Không tìm thấy vị trí ký", new List<string> { "Không tìm thấy vị trí ký. Vui lòng lưu vị trí ký trước." }));
            }

            if (signatureBlock.IsSigned)
                return BadRequest(ApiResponse<DocumentResponseDto>.Error("Vị trí ký đã được ký", new List<string> { "Vị trí ký đã được ký" }));

            // Get full PDF file path
            var pdfFileName = Path.GetFileName(document.PdfUrl);
            if (string.IsNullOrEmpty(pdfFileName))
                return BadRequest(ApiResponse<DocumentResponseDto>.Error("Invalid PDF URL", new List<string> { "URL PDF không hợp lệ" }));
            
            var pdfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdfs", pdfFileName);

            if (!System.IO.File.Exists(pdfFilePath))
                return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy file PDF"));

            // Apply signature to PDF
            var signedPdfPath = await _pdfSignatureService.ApplySignatureToPdfAsync(
                pdfFilePath,
                request.SignatureImageBase64,
                signatureBlock
            );

            // Update PDF URL to signed version
            var signedPdfFileName = Path.GetFileName(signedPdfPath);
            document.PdfUrl = $"/api/documents/pdf/{signedPdfFileName}";
            document.UpdatedAt = DateTime.UtcNow;

            // Mark signature block as signed
            signatureBlock.IsSigned = true;
            document.PdfSignatureBlocks = signatureBlocks;
            await _documentRepository.UpdateAsync(document);

            // Create signature record
            var signatureData = new Domain.ValueObjects.SignatureData
            {
                Type = Domain.Enums.SignatureType.Draw,
                Data = request.SignatureImageBase64,
                Color = "#000000"
            };

            try
            {
                await _signatureService.AddSignatureAsync(
                    document.Id,
                    signatureBlock.Id, // Use signature block ID as signer ID
                    signatureBlock.SignerRole,
                    request.SignerName,
                    request.SignerEmail,
                    signatureData,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot add signature record: {Message}. Document: {DocumentId}, Block: {BlockId}", 
                    ex.Message, document.Id, signatureBlock.Id);
                // If signature record already exists, continue - the PDF is already signed
                // This can happen if the user clicks "Sign" multiple times
                if (!ex.Message.Contains("already signed", StringComparison.OrdinalIgnoreCase))
                {
                    throw; // Re-throw if it's a different error
                }
                _logger.LogInformation("Signature record already exists, continuing with PDF update");
            }

            // Update document status
            await _documentService.UpdateDocumentStatusAsync(id);

            var documentDto = MapToDto(document);
            return Ok(ApiResponse<DocumentResponseDto>.Success(documentDto, "Ký PDF thành công"));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "PDF file not found when applying signature: {FilePath}", ex.FileName);
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy file PDF"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when applying signature: {Message}", ex.Message);
            return BadRequest(ApiResponse<DocumentResponseDto>.Error("Dữ liệu không hợp lệ", new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when applying signature: {Message}\nInnerException: {InnerException}", ex.Message, ex.InnerException?.Message);
            var errorMsg = ex.InnerException != null && ex.InnerException.Message != ex.Message 
                ? $"{ex.Message}: {ex.InnerException.Message}" 
                : ex.Message;
            return BadRequest(ApiResponse<DocumentResponseDto>.Error("Không thể ký PDF", new List<string> { errorMsg }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying PDF signature. Message: {Message}\nInnerException: {InnerException}\nStackTrace: {StackTrace}", 
                ex.Message, ex.InnerException?.Message, ex.StackTrace);
            var errorMsg = ex.InnerException != null && ex.InnerException.Message != ex.Message 
                ? $"{ex.Message}: {ex.InnerException.Message}" 
                : (ex.Message ?? "Lỗi không xác định");
            return StatusCode(500, ApiResponse<DocumentResponseDto>.Error("Lỗi khi ký PDF", new List<string> { errorMsg }));
        }
    }

    [HttpPost("{id}/sign")]
    public async Task<ActionResult<ApiResponse<DocumentResponseDto>>> SignDocument(string id, [FromBody] SignDocumentRequestDto request)
    {
        var document = await _documentService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));

        // 🔒 SECURITY: Check signing mode authorization
        // If RequiredLogin, user must be authenticated
        if (document.SigningMode == SigningMode.RequiredLogin)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized(ApiResponse<DocumentResponseDto>.Unauthorized("Yêu cầu đăng nhập để ký tài liệu này"));
        }
        // If Public mode, anyone can sign (no auth required)

        var signatureData = new SignatureData
        {
            Type = Enum.Parse<SignatureType>(request.SignatureData.Type, true),
            Data = request.SignatureData.Data,
            FontFamily = request.SignatureData.FontFamily,
            Color = request.SignatureData.Color
        };

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        // Get signer info from request
        var signerRole = request.SignerRole ?? "Signer"; // Default if not provided
        var signerName = request.SignerName ?? ""; // Use from request
        var signerEmail = request.SignerEmail ?? ""; // Use from request

        try
        {
            await _signatureService.AddSignatureAsync(
                id,
                request.SignerId,
                signerRole,
                signerName,
                signerEmail,
                signatureData,
                ipAddress,
                userAgent
            );
        }
        catch (InvalidOperationException ex) when (ex.Message != null && ex.Message.Contains("already signed", StringComparison.OrdinalIgnoreCase))
        {
            // If signer already signed, return the document as-is
            // This is not an error - the document is already signed by this signer
            _logger.LogInformation("Signer {SignerId} has already signed document {DocumentId}", request.SignerId, id);
            var existingDocument = await _documentService.GetDocumentByIdAsync(id);
            if (existingDocument == null)
                return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu"));
            
            var existingDocumentDto = MapToDto(existingDocument);
            return Ok(ApiResponse<DocumentResponseDto>.Success(existingDocumentDto, "Tài liệu đã được ký bởi người ký này"));
        }

        await _documentService.UpdateDocumentStatusAsync(id);

        // Get updated document
        var updatedDocument = await _documentService.GetDocumentByIdAsync(id);
        if (updatedDocument == null)
            return NotFound(ApiResponse<DocumentResponseDto>.NotFound("Không tìm thấy tài liệu sau khi ký"));

        // Send email notifications if fully signed
        // Note: We don't attach PDF in email. Instead, we provide a link to view the document online,
        // where users can download the PDF directly from the web view (ensures PDF matches web version exactly)
        if (updatedDocument.Status == DocumentStatus.Signed)
        {
            var emailTasks = new List<Task>();

            // Send to document creator
            if (updatedDocument.UserId.HasValue)
            {
                var creator = await _authService.GetUserByIdAsync(updatedDocument.UserId.Value);
                if (creator != null && !string.IsNullOrEmpty(creator.Email))
                {
                    _logger.LogInformation("Sending confirmation email to creator {Email} with view link", creator.Email);
                    emailTasks.Add(_emailService.SendSigningConfirmationEmailAsync(
                        creator.Email,
                        creator.Name,
                        updatedDocument.Id,
                        updatedDocument.Title ?? "Tài liệu",
                        null // No PDF attachment - use online view link instead
                    ));
                }
            }

            // Send to customer (last signer)
            var lastSignature = updatedDocument.Signatures.OrderByDescending(s => s.SignedAt).FirstOrDefault();
            if (lastSignature != null && !string.IsNullOrEmpty(lastSignature.SignerEmail))
            {
                _logger.LogInformation("Sending confirmation email to customer {Email} with view link", lastSignature.SignerEmail);
                emailTasks.Add(_emailService.SendSigningConfirmationEmailAsync(
                    lastSignature.SignerEmail,
                    lastSignature.SignerName ?? "Khách hàng",
                    updatedDocument.Id,
                    updatedDocument.Title ?? "Tài liệu",
                    null // No PDF attachment - use online view link instead
                ));
            }

            await Task.WhenAll(emailTasks);
        }

        var documentDto = MapToDto(updatedDocument);
        return Ok(ApiResponse<DocumentResponseDto>.Success(documentDto, "Ký tài liệu thành công"));
    }

    [HttpPost("{id}/track-view")]
    public async Task<ActionResult<ApiResponse>> TrackView(string id)
    {
        // Only track if not authenticated (customer view)
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            await _documentService.TrackDocumentViewAsync(id);
        }

        return Ok(ApiResponse.Success("Đã ghi nhận lượt xem"));
    }

    private DocumentResponseDto MapToDto(Document document)
    {
        return new DocumentResponseDto
        {
            Id = document.Id,
            Type = document.Type,
            Title = document.Title,
            Content = document.Content,
            Status = document.Status,
            SigningMode = document.SigningMode,
            ReceiptInfo = document.ReceiptInfo == null ? null : new ReceiptInfoDto
            {
                SenderName = document.ReceiptInfo.SenderName,
                SenderAddress = document.ReceiptInfo.SenderAddress,
                ReceiverName = document.ReceiptInfo.ReceiverName,
                ReceiverAddress = document.ReceiptInfo.ReceiverAddress,
                Amount = document.ReceiptInfo.Amount,
                AmountInWords = document.ReceiptInfo.AmountInWords,
                Reason = document.ReceiptInfo.Reason,
                Location = document.ReceiptInfo.Location,
                Date = document.ReceiptInfo.Date,
                CustomFields = document.ReceiptInfo.CustomFields
            },
            Metadata = new ContractMetadataDto
            {
                ContractNumber = document.ContractNumber,
                Location = document.Location,
                ContractDate = document.ContractDate,
                // For PDF documents, format CreatedAt as Vietnamese date string
                CreatedDate = document.Type == Domain.Enums.DocumentType.Pdf 
                    ? FormatDateToVietnamese(document.ContractDate ?? document.CreatedAt)
                    : null
            },
            Creator = document.User == null ? null : new UserDto
            {
                Id = document.User.Id,
                Email = document.User.Email,
                Name = document.User.Name,
                Role = document.User.Role.ToString(),
                EmailVerified = document.User.EmailVerified
            },
            Signatures = document.Signatures.Select(s => new SignatureResponseDto
            {
                Id = s.Id,
                SignerId = s.SignerId,
                SignerRole = s.SignerRole,
                SignerName = s.SignerName,
                SignerEmail = s.SignerEmail,
                SignatureData = new SignatureDataDto
                {
                    Type = s.SignatureData.Type.ToString(),
                    Data = s.SignatureData.Data,
                    FontFamily = s.SignatureData.FontFamily,
                    Color = s.SignatureData.Color
                },
                SignedAt = s.SignedAt
            }).ToList(),
            CreatedAt = document.CreatedAt,
            SignedAt = document.SignedAt,
            ViewedAt = document.ViewedAt,
            PdfUrl = document.PdfUrl,
            PdfSignatureBlocks = document.PdfSignatureBlocks?.Select(block => new PdfSignatureBlockDto
            {
                Id = block.Id,
                PageNumber = block.PageNumber,
                XPercent = block.XPercent,
                YPercent = block.YPercent,
                WidthPercent = block.WidthPercent,
                HeightPercent = block.HeightPercent,
                SignerRole = block.SignerRole,
                IsSigned = block.IsSigned,
                SignatureId = block.SignatureId
            }).ToList()
        };
    }

    private string GenerateDocumentHtml(Document document)
    {
        // Generate signatures HTML
        var signaturesHtml = string.Join("", document.Signatures.OrderBy(s => s.SignedAt).Select(s =>
        {
            var signatureHtml = "";
            var signatureData = s.SignatureData;
            
            if (signatureData.Type == Domain.Enums.SignatureType.Draw && !string.IsNullOrEmpty(signatureData.Data))
            {
                // Render drawn signature as SVG
                signatureHtml = ConvertSignaturePointsToSvg(signatureData.Data, signatureData.Color ?? "#000000");
            }
            else if (signatureData.Type == Domain.Enums.SignatureType.Type && !string.IsNullOrEmpty(signatureData.Data))
            {
                // Render typed signature as text
                var fontFamily = signatureData.FontFamily ?? "Times New Roman, serif";
                var color = signatureData.Color ?? "#000000";
                signatureHtml = $@"<div style=""font-family: {fontFamily}; color: {color}; font-size: 24px; font-weight: bold; padding: 10px 0; border-bottom: 2px solid {color};"">{EscapeHtml(signatureData.Data)}</div>";
            }
            else
            {
                // Fallback: just show name
                signatureHtml = $@"<div style=""font-size: 18px; font-weight: bold; padding: 10px 0; border-bottom: 2px solid #ccc;"">{EscapeHtml(s.SignerName ?? "")}</div>";
            }
            
            return $@"
            <div style=""margin: 30px 0; padding: 20px 0; border-bottom: 1px solid #eee;"">
                <div style=""margin-bottom: 10px;"">{signatureHtml}</div>
                <div style=""margin-top: 10px; font-size: 14px; color: #666;"">
                    <div><strong>{EscapeHtml(s.SignerRole)}:</strong> {EscapeHtml(s.SignerName ?? "")}</div>
                    {(string.IsNullOrEmpty(s.SignerEmail) ? "" : $"<div><strong>Email:</strong> {EscapeHtml(s.SignerEmail)}</div>")}
                    <div><strong>Ngày ký:</strong> {s.SignedAt:dd/MM/yyyy HH:mm}</div>
                </div>
            </div>";
        }));

        // Generate metadata HTML
        var metadataHtml = "";
        if (!string.IsNullOrEmpty(document.ContractNumber) || document.ContractDate.HasValue || !string.IsNullOrEmpty(document.Location))
        {
            metadataHtml = @"
            <div style=""margin: 20px 0; padding: 15px; background: #f5f5f5; border-radius: 5px; font-size: 14px;"">
                <h3 style=""margin: 0 0 10px 0; font-size: 16px; font-weight: bold;"">Thông tin hợp đồng</h3>
                <table style=""width: 100%; border-collapse: collapse;"">";
            
            if (!string.IsNullOrEmpty(document.ContractNumber))
            {
                metadataHtml += $@"<tr><td style=""padding: 5px 0; width: 150px;""><strong>Số hợp đồng:</strong></td><td>{EscapeHtml(document.ContractNumber)}</td></tr>";
            }
            if (document.ContractDate.HasValue)
            {
                metadataHtml += $@"<tr><td style=""padding: 5px 0;""><strong>Ngày hợp đồng:</strong></td><td>{document.ContractDate.Value:dd/MM/yyyy}</td></tr>";
            }
            if (!string.IsNullOrEmpty(document.Location))
            {
                metadataHtml += $@"<tr><td style=""padding: 5px 0;""><strong>Địa điểm:</strong></td><td>{EscapeHtml(document.Location)}</td></tr>";
            }
            
            metadataHtml += @"
                </table>
            </div>";
        }

        var html = $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{EscapeHtml(document.Title ?? "Tài liệu")}</title>
    <style>
        @page {{
            margin: 15mm;
        }}
        body {{
            font-family: 'Times New Roman', 'Tinos', serif;
            font-size: 12pt;
            line-height: 1.6;
            color: #000;
            max-width: 210mm;
            margin: 0 auto;
            padding: 20px;
        }}
        h1 {{
            font-size: 20pt;
            font-weight: bold;
            margin-bottom: 20px;
            text-align: center;
            border-bottom: 2px solid #000;
            padding-bottom: 10px;
        }}
        .content {{
            line-height: 1.8;
            margin: 30px 0;
            text-align: justify;
        }}
        .signatures {{
            margin-top: 50px;
            page-break-inside: avoid;
        }}
        .signature-item {{
            margin: 30px 0;
            padding: 20px 0;
            border-bottom: 1px solid #eee;
        }}
        .signature-svg {{
            max-width: 250px;
            max-height: 100px;
            margin-bottom: 10px;
        }}
        .signature-info {{
            margin-top: 10px;
            font-size: 11pt;
            color: #666;
        }}
        .metadata {{
            margin: 20px 0;
            padding: 15px;
            background: #f5f5f5;
            border-radius: 5px;
            font-size: 11pt;
        }}
    </style>
</head>
<body>
    <h1>{EscapeHtml(document.Title ?? "Tài liệu")}</h1>
    {metadataHtml}
    <div class=""content"">{document.Content ?? ""}</div>
    <div class=""signatures"">
        <h2 style=""font-size: 16pt; margin-bottom: 20px; border-bottom: 1px solid #ccc; padding-bottom: 10px;"">Chữ ký các bên</h2>
        {signaturesHtml}
    </div>
</body>
</html>";
        return html;
    }

    private string ConvertSignaturePointsToSvg(string jsonData, string strokeColor)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            var root = doc.RootElement;
            
            if (root.ValueKind != JsonValueKind.Array)
                return "";

            var allPoints = new List<(double x, double y)>();
            var strokes = new List<List<(double x, double y)>>();

            foreach (var strokeElement in root.EnumerateArray())
            {
                var stroke = new List<(double x, double y)>();
                foreach (var pointElement in strokeElement.EnumerateArray())
                {
                    if (pointElement.TryGetProperty("x", out var xProp) && 
                        pointElement.TryGetProperty("y", out var yProp) &&
                        xProp.TryGetDouble(out var x) &&
                        yProp.TryGetDouble(out var y))
                    {
                        stroke.Add((x, y));
                        allPoints.Add((x, y));
                    }
                }
                if (stroke.Count > 0)
                    strokes.Add(stroke);
            }

            if (allPoints.Count == 0)
                return "";

            // Calculate bounding box
            var minX = allPoints.Min(p => p.x);
            var minY = allPoints.Min(p => p.y);
            var maxX = allPoints.Max(p => p.x);
            var maxY = allPoints.Max(p => p.y);

            var width = maxX - minX;
            var height = maxY - minY;

            if (width <= 0 || height <= 0)
                return "";

            // Calculate scale and offset to fit in 250x100 viewBox
            var targetWidth = 250.0;
            var targetHeight = 100.0;
            var scale = Math.Min(Math.Min(targetWidth / width * 0.9, targetHeight / height * 0.9), 1.0);
            var offsetX = (targetWidth - width * scale) / 2 - minX * scale;
            var offsetY = (targetHeight - height * scale) / 2 - minY * scale;

            // Generate SVG paths
            var paths = strokes.Select(stroke =>
            {
                var pathData = string.Join(" ", stroke.Select((point, index) =>
                {
                    var x = point.x * scale + offsetX;
                    var y = point.y * scale + offsetY;
                    return index == 0 ? $"M {x:F2} {y:F2}" : $"L {x:F2} {y:F2}";
                }));
                return $"<path d=\"{pathData}\" stroke=\"{EscapeHtml(strokeColor)}\" stroke-width=\"2\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>";
            });

            return $@"<svg viewBox=""0 0 {targetWidth} {targetHeight}"" class=""signature-svg"" style=""width: 250px; height: 100px;"">{string.Join("", paths)}</svg>";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert signature points to SVG");
            return "";
        }
    }

    private string EscapeHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string FormatDateToVietnamese(DateTime date)
    {
        return $"ngày {date.Day} tháng {date.Month} năm {date.Year}";
    }
}

