using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EduLearn.AuthService.DTOs;

namespace EduLearn.AuthService.Services
{
    // service interface for User authentication and management
    public interface IAuthService
    {
        Task<UserProfileDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> GoogleLoginAsync(string email, string fullName);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task<UserProfileDto?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserProfileDto>> GetAllByRoleAsync(string role);
        Task<IEnumerable<UserProfileDto>> SearchUsersAsync(string keyword);
        Task<IEnumerable<UserProfileDto>> GetAllUsersAsync();
        Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
        Task<string> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> DeactivateAccountAsync(int userId);
        Task<bool> ReactivateAccountAsync(int userId);
        Task<bool> DeleteAccountAsync(int userId);
        Task<bool> ChangeUserRoleAsync(int userId, string newRole);
        Task<string?> UploadAvatarAsync(int userId, IFormFile file);
        Task<bool> LogoutAsync(int userId);
        Task<bool> ValidateTokenAsync(string token);
    }
}

