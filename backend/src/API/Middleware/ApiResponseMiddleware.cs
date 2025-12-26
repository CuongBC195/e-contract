using System.Text.Json;
using Shared.Models;

namespace API.Middleware;

public class ApiResponseMiddleware
{
    private readonly RequestDelegate _next;

    public ApiResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip wrapping for certain paths (like Swagger, static files, and PDF endpoint)
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api/docs") ||
            context.Request.Path.StartsWithSegments("/pdfs") ||
            context.Request.Path.StartsWithSegments("/api/documents/pdf"))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;

        MemoryStream? responseBody = null;
        try
        {
            responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Skip wrapping for 204 No Content (no body allowed)
            if (context.Response.StatusCode == 204)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBodyStream;
                responseBody.Dispose();
                return;
            }

            // Only wrap if response is JSON and not already wrapped
            if (context.Response.ContentType?.Contains("application/json") == true &&
                context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();

                // Check if already wrapped
                if (!responseBodyText.Contains("\"statusCode\""))
                {
                    try
                    {
                        // Try to parse as JSON
                        var jsonOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };

                        object? data = null;
                        try
                        {
                            data = JsonSerializer.Deserialize<object>(responseBodyText, jsonOptions);
                        }
                        catch
                        {
                            // If not JSON, wrap as string
                            data = responseBodyText;
                        }

                        var wrappedResponse = ApiResponse<object>.Success(
                            data ?? new object(),
                            GetSuccessMessage(context),
                            context.Response.StatusCode
                        );

                        var wrappedJson = JsonSerializer.Serialize(wrappedResponse, jsonOptions);
                        var wrappedBytes = System.Text.Encoding.UTF8.GetBytes(wrappedJson);

                        context.Response.ContentLength = wrappedBytes.Length;
                        context.Response.Body = originalBodyStream;
                        await context.Response.Body.WriteAsync(wrappedBytes);
                        responseBody.Dispose();
                        return;
                    }
                    catch
                    {
                        // If wrapping fails, return original response
                    }
                }
            }

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBody.CopyToAsync(originalBodyStream);
            responseBody.Dispose();
        }
        catch
        {
            // If exception occurs, restore original body stream and rethrow
            // GlobalExceptionHandlerMiddleware will handle the exception
            if (responseBody != null)
            {
                context.Response.Body = originalBodyStream;
                responseBody.Dispose();
            }
            throw;
        }
    }

    private static string GetSuccessMessage(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (method == "GET")
        {
            if (path.Contains("/auth/me")) return "Lấy thông tin người dùng thành công.";
            if (path.Contains("/documents")) return "Lấy danh sách tài liệu thành công.";
            if (path.Contains("/users")) return "Lấy danh sách người dùng thành công.";
            if (path.Contains("/templates")) return "Lấy danh sách mẫu hợp đồng thành công.";
            return "Lấy dữ liệu thành công.";
        }
        if (method == "POST")
        {
            if (path.Contains("/auth/login")) return "Đăng nhập thành công.";
            if (path.Contains("/auth/register")) return "Đăng ký thành công.";
            if (path.Contains("/documents")) return "Tạo tài liệu thành công.";
            if (path.Contains("/sign")) return "Ký tài liệu thành công.";
            if (path.Contains("/email")) return "Gửi email thành công.";
            return "Thao tác thành công.";
        }
        if (method == "PUT")
        {
            return "Cập nhật thành công.";
        }
        if (method == "DELETE")
        {
            return "Xóa thành công.";
        }

        return "Thành công.";
    }
}

