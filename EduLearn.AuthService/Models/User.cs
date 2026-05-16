using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduLearn.AuthService.Models
{
    // represents a user in the EduLearn platform (Student, Instructor, or Admin)
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [Required]
        [MaxLength(150)]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Role { get; set; } // Roles: STUDENT, INSTRUCTOR, ADMIN

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(200)]
        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        [MaxLength(200)]
        public string? ResetPasswordToken { get; set; }

        public DateTime? ResetPasswordExpiry { get; set; }
    }
}

