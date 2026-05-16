using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EduLearn.AuthService.Data;
using EduLearn.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AuthService.Repositories
{
    // implementation of the IUserRepository using Entity Framework Core
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _context;

        public UserRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> FindByUserIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> FindAllByRoleAsync(string role)
        {
            return await _context.Users.AsNoTracking().Where(u => u.Role == role).ToListAsync();
        }

        public async Task<IEnumerable<User>> FindAllAsync()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<User>> FindAllActiveAsync()
        {
            return await _context.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync();
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            // Atomically update LastLoginAt using EF Core ExecuteUpdateAsync
            await _context.Users
                .Where(u => u.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string keyword)
        {
            // Search by full name or email
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.FullName.Contains(keyword) || u.Email.Contains(keyword))
                .ToListAsync();
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

