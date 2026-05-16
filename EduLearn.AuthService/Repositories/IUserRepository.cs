using System.Collections.Generic;
using System.Threading.Tasks;
using EduLearn.AuthService.Models;

namespace EduLearn.AuthService.Repositories
{
    // defines repository operations for User data access
    // this abstracts the database layer from the service layer
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUserIdAsync(int userId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<IEnumerable<User>> FindAllByRoleAsync(string role);
        Task<IEnumerable<User>> FindAllAsync();
        Task<IEnumerable<User>> FindAllActiveAsync();
        Task UpdateLastLoginAsync(int userId);
        Task<IEnumerable<User>> SearchUsersAsync(string keyword);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        Task SaveChangesAsync();
    }
}

