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
                new Plan { Id = Guid.NewGuid(), Name = "Empresarial", Description = "Para grupos de clínicas", MonthlyPrice = 2499, AnnualPrice = 23999, MaxBranches = 20, MaxDoctors = 50, MaxPatients = 10000, MaxAppointmentsPerMonth = 5000, MaxConversationsPerMonth = 10000, HasAI = true, HasWhatsAppIntegration = true, HasAdvancedReports = true, HasPrioritySupport = true, IsActive = true, SortOrder = 3 }
            };
            context.Plans.AddRange(plans);
            await context.SaveChangesAsync();
        }

        if (!await context.Services.AnyAsync())
        {
            var company = await context.Companies.FirstAsync();
            var branch = await context.Branches.FirstAsync();

            var services = new[]
            {
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Limpieza Dental", Description = "Limpieza profesional y removes de sarro", DurationMinutes = 30, Price = 800, Category = "Preventivo", IsActive = true },
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Extracción", Description = "Extracción de pieza dental", DurationMinutes = 45, Price = 1500, Category = "Cirugía", IsActive = true },
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Endodoncia", Description = "Tratamiento de conducto", DurationMinutes = 60, Price = 3500, Category = "Endodoncia", IsActive = true },
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Ortodoncia", Description = "Colocación y control de brackets", DurationMinutes = 30, Price = 500, Category = "Ortodoncia", IsActive = true },
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Blanqueamiento", Description = "Blanqueamiento dental profesional", DurationMinutes = 60, Price = 2500, Category = "Estético", IsActive = true },
                new Service { Id = Guid.NewGuid(), CompanyId = company.Id, Name = "Consulta General", Description = "Evaluación y diagnóstico dental", DurationMinutes = 20, Price = 300, Category = "General", IsActive = true }
            };
            context.Services.AddRange(services);

            var doctorRole = await context.Roles.FirstAsync(r => r.Name == RoleName.Doctor);

            var doctorUser1 = new User
            {
                Id = Guid.NewGuid(), Email = "dr.martinez@dentalbot.com",
                PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes("Doctor123!"))),
                FirstName = "Carlos", LastName = "Martínez", Phone = "8091111111",
                RoleId = doctorRole.Id, CompanyId = company.Id, BranchId = branch.Id, IsActive = true
            };
            var doctorUser2 = new User
            {
                Id = Guid.NewGuid(), Email = "dra.lopez@dentalbot.com",
                PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes("Doctor123!"))),
                FirstName = "María", LastName = "López", Phone = "8092222222",
                RoleId = doctorRole.Id, CompanyId = company.Id, BranchId = branch.Id, IsActive = true
            };
            context.Users.AddRange(doctorUser1, doctorUser2);
            await context.SaveChangesAsync();

            var doctor1 = new Doctor
            {
                Id = Guid.NewGuid(), UserId = doctorUser1.Id, CompanyId = company.Id,
                Specialty = "Odontología General", LicenseNumber = "COL-12345", Bio = "Especialista en odontología general con 10 años de experiencia",
                Color = "#4F46E5"
            };
            var doctor2 = new Doctor
            {
                Id = Guid.NewGuid(), UserId = doctorUser2.Id, CompanyId = company.Id,
                Specialty = "Endodoncia y Cirugía", LicenseNumber = "COL-67890", Bio = "Especialista en endodoncia y cirugía oral",
                Color = "#EC4899"
            };
            context.Doctors.AddRange(doctor1, doctor2);
            await context.SaveChangesAsync();

            var schedule = new BusinessSchedule
            {
                Id = Guid.NewGuid(),
                BranchId = branch.Id
            };
            context.BusinessSchedules.Add(schedule);
            await context.SaveChangesAsync();

            var scheduleDays = Enum.GetValues<Domain.Enums.DayOfWeek>().Select(dow => new ScheduleDay
            {
                Id = Guid.NewGuid(),
                BusinessScheduleId = schedule.Id,
                DayOfWeek = dow,
                IsOpen = dow <= Domain.Enums.DayOfWeek.Viernes,
                OpenTime = new TimeSpan(9, 0, 0),
                CloseTime = new TimeSpan(17, 0, 0)
            }).ToList();
            context.ScheduleDays.AddRange(scheduleDays);

            var lunch = new LunchConfig
            {
                Id = Guid.NewGuid(),
                BusinessScheduleId = schedule.Id,
                DurationMinutes = 45,
                StartTime = new TimeSpan(13, 0, 0)
            };
            context.LunchConfigs.Add(lunch);
            await context.SaveChangesAsync();
        }
    }
}
