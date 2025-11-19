using Students.Models;

namespace Students.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAddress(string email);
        Task<User?> GetByRefreshToken(string refreshToken); 
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}