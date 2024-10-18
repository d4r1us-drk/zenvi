namespace Zenvi.Server.Entities;

public interface IUpdatableEntity<T> : IBaseEntity<T>
{
    DateTime? UpdatedAt { get; set; }
}
