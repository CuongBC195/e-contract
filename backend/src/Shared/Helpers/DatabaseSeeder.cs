using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;

namespace Shared.Helpers;

public static class DatabaseSeeder
{
    public static async Task SeedAdminUserAsync(ApplicationDbContext context)
    {
        var adminEmail = "admin@econtract.com";
        var adminPassword = "3do_econtract";

        var existingAdmin = await context.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin == null)
        {
            var admin = new User
            {
                Email = adminEmail,
                PasswordHash = PasswordHelper.HashPassword(adminPassword),
                Name = "Administrator",
                EmailVerified = true,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedTemplatesAsync(ApplicationDbContext context)
    {
        var defaultTemplates = TemplateSeeder.GetDefaultTemplates();

        foreach (var template in defaultTemplates)
        {
            var existingTemplate = await context.Templates
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            if (existingTemplate == null)
            {
                context.Templates.Add(template);
            }
            else
            {
                // Update existing template
                existingTemplate.Name = template.Name;
                existingTemplate.Description = template.Description;
                existingTemplate.Category = template.Category;
                existingTemplate.Content = template.Content;
                existingTemplate.Icon = template.Icon;
                existingTemplate.Color = template.Color;
                existingTemplate.Signers = template.Signers;
                existingTemplate.IsActive = template.IsActive;
                existingTemplate.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }
}

