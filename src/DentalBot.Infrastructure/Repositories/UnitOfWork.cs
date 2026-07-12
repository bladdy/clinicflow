using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Infrastructure.Data;

namespace DentalBot.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IGenericRepository<Company>? _companies;
    private IGenericRepository<Branch>? _branches;
    private IGenericRepository<User>? _users;
    private IGenericRepository<Role>? _roles;
    private IGenericRepository<Doctor>? _doctors;
    private IGenericRepository<Patient>? _patients;
    private IGenericRepository<Service>? _services;
    private IGenericRepository<Appointment>? _appointments;
    private IGenericRepository<Conversation>? _conversations;
    private IGenericRepository<Message>? _messages;
    private IGenericRepository<KnowledgeArticle>? _knowledgeArticles;
    private IGenericRepository<BusinessHour>? _businessHours;
    private IGenericRepository<Holiday>? _holidays;
    private IGenericRepository<AISettings>? _aiSettings;
    private IGenericRepository<WhatsAppInstance>? _whatsAppInstances;
    private IGenericRepository<Plan>? _plans;
    private IGenericRepository<CompanySubscription>? _companySubscriptions;
    private IGenericRepository<Invoice>? _invoices;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public IGenericRepository<Company> Companies => _companies ??= new GenericRepository<Company>(_context);
    public IGenericRepository<Branch> Branches => _branches ??= new GenericRepository<Branch>(_context);
    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
    public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
    public IGenericRepository<Doctor> Doctors => _doctors ??= new GenericRepository<Doctor>(_context);
    public IGenericRepository<Patient> Patients => _patients ??= new GenericRepository<Patient>(_context);
    public IGenericRepository<Service> Services => _services ??= new GenericRepository<Service>(_context);
    public IGenericRepository<Appointment> Appointments => _appointments ??= new GenericRepository<Appointment>(_context);
    public IGenericRepository<Conversation> Conversations => _conversations ??= new GenericRepository<Conversation>(_context);
    public IGenericRepository<Message> Messages => _messages ??= new GenericRepository<Message>(_context);
    public IGenericRepository<KnowledgeArticle> KnowledgeArticles => _knowledgeArticles ??= new GenericRepository<KnowledgeArticle>(_context);
    public IGenericRepository<BusinessHour> BusinessHours => _businessHours ??= new GenericRepository<BusinessHour>(_context);
    public IGenericRepository<Holiday> Holidays => _holidays ??= new GenericRepository<Holiday>(_context);
    public IGenericRepository<AISettings> AISettings => _aiSettings ??= new GenericRepository<AISettings>(_context);
    public IGenericRepository<WhatsAppInstance> WhatsAppInstances => _whatsAppInstances ??= new GenericRepository<WhatsAppInstance>(_context);
    public IGenericRepository<Plan> Plans => _plans ??= new GenericRepository<Plan>(_context);
    public IGenericRepository<CompanySubscription> CompanySubscriptions => _companySubscriptions ??= new GenericRepository<CompanySubscription>(_context);
    public IGenericRepository<Invoice> Invoices => _invoices ??= new GenericRepository<Invoice>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
