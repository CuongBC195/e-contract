using Domain.Entities;

namespace Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
    Task<List<User>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<int> GetTotalCountAsync(string? search = null);
}

