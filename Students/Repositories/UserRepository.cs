// Security/Repositories/UserRepository.cs (ejemplo EF)
using Microsoft.EntityFrameworkCore;
using Students.Data;
using Students.Models;

namespace Students.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _ctx;
        public UserRepository(AppDbContext ctx) { _ctx = ctx; }

        public Task<User?> GetByEmailAddress(string email) =>
            _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> GetByRefreshToken(string refreshToken) =>
            _ctx.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        public async Task AddAsync(User user)
        {
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _ctx.Users.Update(user);
            await _ctx.SaveChangesAsync();
        }
    }
}