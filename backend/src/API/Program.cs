using API.Middleware;
using API.Services;
using Shared.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Let .NET auto-select available port or use environment variable
// This avoids port conflicts - .NET will find an available port
// You can override with: dotnet run --urls "http://localhost:5100"

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<ISignatureRepository, SignatureRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ISignatureService, SignatureService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IPdfSignatureService, PdfSignatureService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] 
    ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "https://api.yourdomain.com";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "https://app.yourdomain.com";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
var allowedOrigins = builder.Configuration["CORS:AllowedOrigins"]?.Split(',') 
    ?? new[] { "http://localhost:3000", "https://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for easier testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Contract API V1");
    c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
});

// HTTPS redirection - only enable in production or when HTTPS is configured
// app.UseHttpsRedirection(); // Disabled for development HTTP-only mode

// Enable static file serving from wwwroot folder (for PDF files)
// Must be before routing/authentication to serve static files
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Allow CORS for static files
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
    }
};
app.UseStaticFiles(staticFileOptions);

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<ApiResponseMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Apply migrations and seed admin user
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply pending migrations (Code First approach)
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
        
        // Seed admin user
        await Shared.Helpers.DatabaseSeeder.SeedAdminUserAsync(dbContext);
        logger.LogInformation("Admin user seeded successfully");
        
        // Seed templates
        await Shared.Helpers.DatabaseSeeder.SeedTemplatesAsync(dbContext);
        logger.LogInformation("Templates seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        throw;
    }
}

app.Run();

