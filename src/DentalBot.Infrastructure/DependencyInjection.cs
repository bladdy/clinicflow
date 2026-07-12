using DentalBot.Application.Interfaces;
using DentalBot.Infrastructure.Data;
using DentalBot.Infrastructure.Repositories;
using DentalBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DentalBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHostedService<AppointmentReminderBackgroundService>();

        return services;
    }
}
