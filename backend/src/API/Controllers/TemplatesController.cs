using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TemplatesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Template>>>> GetTemplates([FromQuery] string? category = null)
    {
        var query = _context.Templates.Where(t => t.IsActive).AsQueryable();
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        var templates = await query.ToListAsync();
        return Ok(ApiResponse<List<Template>>.Success(templates, "Lấy danh sách mẫu hợp đồng thành công"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Template>>> GetTemplate(Guid id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template == null)
            return NotFound(ApiResponse<Template>.NotFound("Không tìm thấy mẫu hợp đồng"));

        return Ok(ApiResponse<Template>.Success(template, "Lấy thông tin mẫu hợp đồng thành công"));
    }
}

