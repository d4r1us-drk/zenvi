using Zenvi.Server.Entities;

namespace Zenvi.Server.Repository;

public class UserRepository(IRepository<User, int> userRepository)
{
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userRepository.GetAllAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await userRepository.GetByIdAsync(id);
    }

    public async Task AddUserAsync(User user)
    {
        await userRepository.AddAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        await userRepository.UpdateAsync(user);
    }

    public async Task DeleteUserAsync(int id)
    {
        await userRepository.DeleteAsync(id);
    }
}