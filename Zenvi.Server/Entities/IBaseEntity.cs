namespace Zenvi.Server.Entities;

public interface IBaseEntity<T>
{
    T Id { get; set; }
    DateTime CreatedAt { get; set; }
}
