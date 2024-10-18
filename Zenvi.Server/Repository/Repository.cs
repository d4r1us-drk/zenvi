using System.Data;
using Dapper;
using Zenvi.Server.Entities;

namespace Zenvi.Server.Repository;

public class Repository<T, TId>(IDbConnection dbConnection) : IRepository<T, TId>
    where T : class, IBaseEntity<TId>
{
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        string sql = $"SELECT * FROM {typeof(T).Name}s";
        return await dbConnection.QueryAsync<T>(sql);
    }

    public async Task<T?> GetByIdAsync(TId id)
    {
        string sql = $"SELECT * FROM {typeof(T).Name}s WHERE Id = @Id";
        return await dbConnection.QuerySingleOrDefaultAsync<T>(sql, new { Id = id });
    }

    public async Task AddAsync(T entity)
    {
        string sql = $"INSERT INTO {typeof(T).Name}s ({GetColumns()}) VALUES ({GetColumnParameters()})";
        await dbConnection.ExecuteAsync(sql, entity);
    }

    public async Task UpdateAsync(T entity)
    {
        // Check if the entity supports updates (i.e., if it implements IUpdatableEntity<TId>)
        if (entity is IUpdatableEntity<TId> updatableEntity)
        {
            // Automatically update the UpdatedAt field to the current time
            updatableEntity.UpdatedAt = DateTime.UtcNow;

            string sql = $"UPDATE {typeof(T).Name}s SET {GetUpdateColumns()} WHERE Id = @Id";
            await dbConnection.ExecuteAsync(sql, entity);
        }
        else
        {
            throw new InvalidOperationException($"Entity {typeof(T).Name} cannot be updated.");
        }
    }

    public async Task DeleteAsync(TId id)
    {
        string sql = $"DELETE FROM {typeof(T).Name}s WHERE Id = @Id";
        await dbConnection.ExecuteAsync(sql, new { Id = id });
    }

    // Helper methods for dynamic column generation
    private string GetColumns()
    {
        // Generates a comma-separated list of column names based on the properties of the entity
        var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
        return string.Join(", ", properties.Select(p => p.Name));
    }

    private string GetColumnParameters()
    {
        // Generates a comma-separated list of parameter names (e.g., @PropertyName)
        var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
        return string.Join(", ", properties.Select(p => "@" + p.Name));
    }

    private string GetUpdateColumns()
    {
        // Generates a comma-separated list of "PropertyName = @PropertyName" for the UPDATE statement
        var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
        return string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));
    }
}