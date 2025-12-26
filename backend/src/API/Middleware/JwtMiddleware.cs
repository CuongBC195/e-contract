using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            AttachUserToContext(context, token);
        }

        await _next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        // Try to get token from cookie first
        if (context.Request.Cookies.TryGetValue("jwt_token", out var cookieToken))
        {
            return cookieToken;
        }

        // Try to get token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var jwtSecret = _configuration["JWT:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret not configured");
            var jwtIssuer = _configuration["JWT:Issuer"] ?? "https://api.yourdomain.com";
            var jwtAudience = _configuration["JWT:Audience"] ?? "https://app.yourdomain.com";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            context.User = new ClaimsPrincipal(identity);
        }
        catch
        {
            // Token validation failed, do nothing
            // User will remain unauthenticated
        }
    }
}

