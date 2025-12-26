using System.Net;
using System.Text.Json;
using Shared.Models;

namespace API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. RequestId: {RequestId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if response has already started
        if (context.Response.HasStarted)
        {
            return;
        }

        var errorRef = Guid.NewGuid().ToString("N")[..8].ToUpper();
        
        // Reset response
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        
        ApiResponse<object> response;

        switch (exception)
        {
            case UnauthorizedAccessException:
                response = ApiResponse<object>.Unauthorized(exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case InvalidOperationException invalidOp:
                response = ApiResponse<object>.Error(
                    invalidOp.Message,
                    new List<string> { invalidOp.Message },
                    (int)HttpStatusCode.BadRequest
                );
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case KeyNotFoundException:
                response = ApiResponse<object>.NotFound("Không tìm thấy dữ liệu");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case ArgumentException argEx:
                response = ApiResponse<object>.Error(
                    "Dữ liệu đầu vào không hợp lệ.",
                    new List<string> { argEx.Message },
                    (int)HttpStatusCode.BadRequest
                );
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                // Log full exception for debugging (not sent to client)
                response = ApiResponse<object>.InternalServerError(
                    errorRef,
                    "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ Admin."
                );
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        var responseBytes = System.Text.Encoding.UTF8.GetBytes(jsonResponse);
        context.Response.ContentLength = responseBytes.Length;
        await context.Response.Body.WriteAsync(responseBytes);
    }
}

