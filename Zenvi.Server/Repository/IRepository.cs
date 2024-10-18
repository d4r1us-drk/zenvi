using Zenvi.Server.Entities;

namespace Zenvi.Server.Repository;

public interface IRepository<T, in TId> where T : IBaseEntity<TId>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(TId id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(TId id);
}