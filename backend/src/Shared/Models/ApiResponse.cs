namespace Shared.Models;

public class ApiResponse<T>
{
    public int StatusCode { get; set; }      // Mã HTTP (200, 400, 500...)
    public string Message { get; set; } = string.Empty;      // Thông báo tóm tắt (Human-readable)
    public T? Data { get; set; }             // Dữ liệu chính (Null nếu lỗi)
    public List<string>? Errors { get; set; } // Chi tiết lỗi (Null nếu thành công)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;  // Thời gian server trả về

    public static ApiResponse<T> Success(T data, string message = "Thành công", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Message = message,
            Data = data,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Tạo mới thành công")
    {
        return new ApiResponse<T>
        {
            StatusCode = 201,
            Message = message,
            Data = data,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Error(string message, List<string>? errors = null, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Message = message,
            Data = default(T),
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> NotFound(string message = "Không tìm thấy dữ liệu")
    {
        return new ApiResponse<T>
        {
            StatusCode = 404,
            Message = message,
            Data = default(T),
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Unauthorized(string message = "Chưa đăng nhập hoặc token đã hết hạn")
    {
        return new ApiResponse<T>
        {
            StatusCode = 401,
            Message = message,
            Data = default(T),
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Forbidden(string message = "Không đủ quyền truy cập")
    {
        return new ApiResponse<T>
        {
            StatusCode = 403,
            Message = message,
            Data = default(T),
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> InternalServerError(string errorRef, string message = "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ Admin.")
    {
        return new ApiResponse<T>
        {
            StatusCode = 500,
            Message = message,
            Data = default(T),
            Errors = new List<string> { $"Error Ref: {errorRef}" },
            Timestamp = DateTime.UtcNow
        };
    }
}

// For void responses
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Success(string message = "Thành công", int statusCode = 200)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Message = message,
            Data = null,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse NoContent(string message = "Xóa thành công")
    {
        return new ApiResponse
        {
            StatusCode = 204,
            Message = message,
            Data = null,
            Errors = null,
            Timestamp = DateTime.UtcNow
        };
    }
}

