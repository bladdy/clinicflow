using System.Linq.Expressions;
using DentalBot.Domain.Common;

namespace DentalBot.Application.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SoftDelete(T entity);
}
