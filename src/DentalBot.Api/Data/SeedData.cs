using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

        if (await context.Roles.AnyAsync())
            return;

        var roles = new[]
        {
            new Role { Id = Guid.NewGuid(), Name = RoleName.Administrador, Description = "Administrador del sistema" },
            new Role { Id = Guid.NewGuid(), Name = RoleName.Recepcion, Description = "Personal de recepción" },
            new Role { Id = Guid.NewGuid(), Name = RoleName.Doctor, Description = "Doctor odontólogo" },
            new Role { Id = Guid.NewGuid(), Name = RoleName.SoloLectura, Description = "Solo lectura" }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        var defaultCompany = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Clínica DentalBot",
            Phone = "+52 55 1234 5678",
            Email = "contacto@dentalbot.com",
            Address = "Av. Principal 123, Ciudad de México"
        };
        context.Companies.Add(defaultCompany);
        await context.SaveChangesAsync();

        var mainBranch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = defaultCompany.Id,
            Name = "Sucursal Principal",
            Phone = "+52 55 1234 5678",
            Email = "sucursal@dentalbot.com",
            Address = "Av. Principal 123, Ciudad de México",
            IsMain = true
        };
        context.Branches.Add(mainBranch);
        await context.SaveChangesAsync();

        var adminRole = roles.First(r => r.Name == RoleName.Administrador);
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@dentalbot.com",
            PasswordHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create().
                ComputeHash(System.Text.Encoding.UTF8
                .GetBytes("Admin123!"))
                ),
            FirstName = "Administrador",
            LastName = "Sistema",
            RoleId = adminRole.Id,
            CompanyId = defaultCompany.Id,
            BranchId = mainBranch.Id,
            IsActive = true
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        if (!await context.Plans.AnyAsync())
        {
            var plans = new[]
            {
                new Plan { Id = Guid.NewGuid(), Name = "Trial", Description = "Prueba gratuita por 14 días", MonthlyPrice = 0, AnnualPrice = 0, MaxBranches = 1, MaxDoctors = 2, MaxPatients = 50, MaxAppointmentsPerMonth = 30, MaxConversationsPerMonth = 50, HasAI = false, HasWhatsAppIntegration = false, HasAdvancedReports = false, HasPrioritySupport = false, IsActive = true, SortOrder = 0 },
                new Plan { Id = Guid.NewGuid(), Name = "Básico", Description = "Para clínicas pequeñas", MonthlyPrice = 499, AnnualPrice = 4790, MaxBranches = 1, MaxDoctors = 3, MaxPatients = 200, MaxAppointmentsPerMonth = 100, MaxConversationsPerMonth = 200, HasAI = false, HasWhatsAppIntegration = true, HasAdvancedReports = false, HasPrioritySupport = false, IsActive = true, SortOrder = 1 },
                new Plan { Id = Guid.NewGuid(), Name = "Profesional", Description = "Para clínicas en crecimiento", MonthlyPrice = 999, AnnualPrice = 9590, MaxBranches = 3, MaxDoctors = 10, MaxPatients = 1000, MaxAppointmentsPerMonth = 500, MaxConversationsPerMonth = 1000, HasAI = true, HasWhatsAppIntegration = true, HasAdvancedReports = true, HasPrioritySupport = false, IsActive = true, SortOrder = 2 },
                new Plan { Id = Guid.NewGuid(), Name = "Empresarial", Description = "Para grupos de clínicas", MonthlyPrice = 2499, AnnualPrice = 23990, MaxBranches = 20, MaxDoctors = 50, MaxPatients = 10000, MaxAppointmentsPerMonth = 5000, MaxConversationsPerMonth = 10000, HasAI = true, HasWhatsAppIntegration = true, HasAdvancedReports = true, HasPrioritySupport = true, IsActive = true, SortOrder = 3 }
            };
            context.Plans.AddRange(plans);
            await context.SaveChangesAsync();
        }
    }
}
