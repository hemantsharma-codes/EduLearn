using System.Security.Claims;
using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Services;
using EduLearn.SharedLib.Common;
using EduLearn.SharedLib.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLearn.AuthService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageService _storageService;

        public AuthController(IAuthService AuthService, IConfiguration configuration, IAzureStorageService storageService)
        {
            _authService = AuthService;
            _configuration = configuration;
            _storageService = storageService;
        }

        // register a new user with the specified details
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userProfile = await _authService.RegisterAsync(dto);
            if (!string.IsNullOrEmpty(userProfile.AvatarUrl))
                userProfile.AvatarUrl = _storageService.GenerateSasUrl(userProfile.AvatarUrl);

            return CreatedAtAction(nameof(GetProfileById), new { id = userProfile.UserId },
                ApiResponse<UserProfileDto>.SuccessResult(userProfile, "Registration successful. Please login to continue."));
        }

        // login a user and returns a JWT access token and refresh token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _authService.LoginAsync(dto);
            if (response?.User != null && !string.IsNullOrEmpty(response.User.AvatarUrl))
            {
                response.User.AvatarUrl = _storageService.GenerateSasUrl(response.User.AvatarUrl);
            }
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(response!, "Login successful."));
        }

        // refreshes an expired access token using a valid refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var response = await _authService.RefreshTokenAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.SuccessResult(response, "Token refreshed successfully."));
        }

        // generates a password reset token for the specified email
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var token = await _authService.ForgotPasswordAsync(dto);
            return Ok(ApiResponse<object>.SuccessResult(new { ResetToken = token },
                "Password reset token generated. Please check your email."));
        }

        // resets the user's password using a valid reset token
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _authService.ResetPasswordAsync(dto);
            if (!success)
                return BadRequest(ApiResponse<object>.FailureResult("Failed to reset password."));

            return Ok(ApiResponse<object>.SuccessResult(null, "Password has been reset successfully."));
        }

        // initiates the Google OAuth login process
        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // internal callback for Google OAuth
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest(new { message = "Google authentication failed." });

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Email could not be retrieved from Google." });

            var response = await _authService.GoogleLoginAsync(email, name ?? "Google User");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/auth/callback?token={response.AccessToken}");
        }

        // get the profile of the currently authenticated user
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetCurrentProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var profile = await _authService.GetUserByIdAsync(userId);
            if (profile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            if (!string.IsNullOrEmpty(profile.AvatarUrl))
                profile.AvatarUrl = _storageService.GenerateSasUrl(profile.AvatarUrl);

            return Ok(ApiResponse<UserProfileDto>.SuccessResult(profile));
        }

        // get a user profile by ID (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id}/profile")]
        public async Task<IActionResult> GetProfileById(int id)
        {
            var profile = await _authService.GetUserByIdAsync(id);
            if (profile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            if (!string.IsNullOrEmpty(profile.AvatarUrl))
                profile.AvatarUrl = _storageService.GenerateSasUrl(profile.AvatarUrl);

            return Ok(ApiResponse<UserProfileDto>.SuccessResult(profile));
        }

        // update the current user's profile information
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var success = await _authService.UpdateProfileAsync(userId, dto);
            if (!success)
                return BadRequest(ApiResponse<object>.FailureResult("Failed to update profile."));

            return Ok(ApiResponse<object>.SuccessResult(null, "Profile updated successfully."));
        }

        // changes the current user's password
        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var success = await _authService.ChangePasswordAsync(userId, dto);
            if (!success)
                return BadRequest(ApiResponse<object>.FailureResult("Failed to change password."));

            return Ok(ApiResponse<object>.SuccessResult(null, "Password changed successfully."));
        }

        // lists all users with a specific role (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpGet("role/{role}")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var users = await _authService.GetAllByRoleAsync(role);
            return Ok(ApiResponse<IEnumerable<UserProfileDto>>.SuccessResult(users));
        }

        // searches users by keyword (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return BadRequest(ApiResponse<object>.FailureResult("Keyword is required."));

            var users = await _authService.SearchUsersAsync(keyword);
            return Ok(ApiResponse<IEnumerable<UserProfileDto>>.SuccessResult(users));
        }

        // lists all users in the system (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserProfileDto>>.SuccessResult(users));
        }

        // deactivates a user account (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(currentUserIdStr, out int currentUserId);

            if (await IsSuperAdmin(id))
                return StatusCode(403, ApiResponse<object>.FailureResult("SuperAdmin account cannot be deactivated."));

            var targetProfile = await _authService.GetUserByIdAsync(id);
            if (targetProfile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            if (targetProfile.Role == "ADMIN" && !(await IsSuperAdmin(currentUserId)))
                return StatusCode(403, ApiResponse<object>.FailureResult("Only SuperAdmin can deactivate another Admin."));

            var success = await _authService.DeactivateAccountAsync(id);
            if (!success)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            return Ok(ApiResponse<object>.SuccessResult(null, "User deactivated successfully."));
        }

        // reactivates a deactivated user account (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(currentUserIdStr, out int currentUserId);

            var targetProfile = await _authService.GetUserByIdAsync(id);
            if (targetProfile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            if (targetProfile.Role == "ADMIN" && !(await IsSuperAdmin(currentUserId)))
                return StatusCode(403, ApiResponse<object>.FailureResult("Only SuperAdmin can reactivate another Admin."));

            var success = await _authService.ReactivateAccountAsync(id);
            if (!success)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            return Ok(ApiResponse<object>.SuccessResult(null, "User reactivated successfully."));
        }

        // delete a user account (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(currentUserIdStr, out int currentUserId);

            if (await IsSuperAdmin(id))
                return StatusCode(403, ApiResponse<object>.FailureResult("SuperAdmin account cannot be deleted."));

            var targetProfile = await _authService.GetUserByIdAsync(id);
            if (targetProfile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            if (targetProfile.Role == "ADMIN" && !(await IsSuperAdmin(currentUserId)))
                return StatusCode(403, ApiResponse<object>.FailureResult("Only SuperAdmin can delete another Admin."));

            var success = await _authService.DeleteAccountAsync(id);
            if (!success)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            return Ok(ApiResponse<object>.SuccessResult(null, "User permanently deleted successfully."));
        }

        public class RoleUpdateDto
        {
            public required string Role { get; set; }
        }

        // update user role (Admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] RoleUpdateDto dto)
        {
            var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(currentUserIdStr, out int currentUserId);

            if (await IsSuperAdmin(id))
                return StatusCode(403, ApiResponse<object>.FailureResult("SuperAdmin role cannot be changed."));

            if (string.IsNullOrEmpty(dto?.Role))
                return BadRequest(ApiResponse<object>.FailureResult("Role is required."));

            var upperRole = dto.Role.ToUpper();
            if (upperRole != "ADMIN" && upperRole != "INSTRUCTOR" && upperRole != "STUDENT")
                return BadRequest(ApiResponse<object>.FailureResult("Invalid role. Allowed roles are ADMIN, INSTRUCTOR, STUDENT."));

            var targetProfile = await _authService.GetUserByIdAsync(id);
            if (targetProfile == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            bool isCurrentSuperAdmin = await IsSuperAdmin(currentUserId);

            if (targetProfile.Role == "ADMIN" && !isCurrentSuperAdmin)
                return StatusCode(403, ApiResponse<object>.FailureResult("Only SuperAdmin can change the role of an Admin."));

            if (upperRole == "ADMIN" && !isCurrentSuperAdmin)
                return StatusCode(403, ApiResponse<object>.FailureResult("Only SuperAdmin can promote a user to Admin."));

            var success = await _authService.ChangeUserRoleAsync(id, upperRole);
            if (!success)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            return Ok(ApiResponse<object>.SuccessResult(null, "User role updated successfully."));
        }

        // uploads an avatar image for the current user
        [Authorize]
        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<object>.FailureResult("No file provided."));

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var avatarUrl = await _authService.UploadAvatarAsync(userId, file);
            if (avatarUrl == null)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            // Generate SAS URL so frontend can display it immediately
            var sasUrl = _storageService.GenerateSasUrl(avatarUrl);

            return Ok(ApiResponse<object>.SuccessResult(new { url = sasUrl }, "Avatar uploaded successfully."));
        }

        // remove the user's refresh token and logs them out
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized();

            var success = await _authService.LogoutAsync(userId);
            if (!success)
                return NotFound(ApiResponse<object>.FailureResult("User not found."));

            return Ok(ApiResponse<object>.SuccessResult(null, "Logged out successfully."));
        }

        private async Task<bool> IsSuperAdmin(int userId)
        {
            var profile = await _authService.GetUserByIdAsync(userId);
            var superAdminEmail = _configuration["SuperAdmin:Email"];
            return profile != null && profile.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase);
        }
    }
}
