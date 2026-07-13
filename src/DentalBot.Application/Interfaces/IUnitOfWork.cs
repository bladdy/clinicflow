using DentalBot.Domain.Entities;

namespace DentalBot.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Company> Companies { get; }
    IGenericRepository<Branch> Branches { get; }
    IGenericRepository<User> Users { get; }
    IGenericRepository<Role> Roles { get; }
    IGenericRepository<Doctor> Doctors { get; }
    IGenericRepository<Patient> Patients { get; }
    IGenericRepository<Service> Services { get; }
    IGenericRepository<Appointment> Appointments { get; }
    IGenericRepository<Conversation> Conversations { get; }
    IGenericRepository<Message> Messages { get; }
    IGenericRepository<KnowledgeArticle> KnowledgeArticles { get; }
    IGenericRepository<BusinessSchedule> BusinessSchedules { get; }
    IGenericRepository<ScheduleDay> ScheduleDays { get; }
    IGenericRepository<BreakPeriod> BreakPeriods { get; }
    IGenericRepository<LunchConfig> LunchConfigs { get; }
    IGenericRepository<Holiday> Holidays { get; }
    IGenericRepository<AISettings> AISettings { get; }
    IGenericRepository<WhatsAppInstance> WhatsAppInstances { get; }
    IGenericRepository<Plan> Plans { get; }
    IGenericRepository<CompanySubscription> CompanySubscriptions { get; }
    IGenericRepository<Invoice> Invoices { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
