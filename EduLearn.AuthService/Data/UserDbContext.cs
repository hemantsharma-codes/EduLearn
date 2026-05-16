using EduLearn.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.AuthService.Data
{
    // entity Framework Core DbContext for the User/Auth Service
    // manages database access for users
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Ensure Email is unique at the database level
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}

