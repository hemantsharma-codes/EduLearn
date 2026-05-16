using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using AutoMapper;
using EduLearn.AuthService.DTOs;
using EduLearn.AuthService.Models;
using EduLearn.SharedLib.Exceptions;
using EduLearn.AuthService.Repositories;
using EduLearn.SharedLib.Services;
using MassTransit;
using EduLearn.SharedLib.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace EduLearn.AuthService.Services
{
    // implementation of IAuthService for core authentication and user identity management
    // handles business logic such as JWT token generation, secure password hashing, and user profile management
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IMapper _mapper;
        private readonly IAzureStorageService _storageService;
        private readonly IConfiguration _configuration;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthService(
            IUserRepository userRepository,
            IOptions<JwtSettings> jwtOptions,
            IPasswordHasher<User> passwordHasher,
            IMapper mapper,
            IAzureStorageService storageService,
            IConfiguration configuration,
            IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _jwtSettings = jwtOptions.Value;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _storageService = storageService;
            _configuration = configuration;
            _publishEndpoint = publishEndpoint;
        }

        // register a new user into the platform
        // validates email uniqueness and securely hashes the password before saving
        public async Task<UserProfileDto> RegisterAsync(RegisterDto dto)
        {
            // Verify that the email is not already in use
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                throw new BadRequestException("Email is already registered.");
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Role = "STUDENT", // Default role for standard registrations
                PasswordHash = "" // Placeholder to be hashed below
            };

            // Hash the plain-text password using ASP.NET Core Identity's PasswordHasher
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return _mapper.Map<UserProfileDto>(user);
        }

        // login a user based on their email and password
        // returns an Access Token and a Refresh Token if successful
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Fetch user and ensure they are active
            var user = await _userRepository.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("Invalid login credentials.");
            }

            // Cryptographically verify the provided password against the stored hash
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedException("Invalid login credentials.");
            }

            // Update user metrics
            await _userRepository.UpdateLastLoginAsync(user.UserId);

            // Generate a secure refresh token and store only its hash in the database
            var plainRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashRefreshToken(plainRefreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Generate the short-lived JWT Access Token
            var (token, expiresIn) = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
                RefreshToken = plainRefreshToken,
                ExpiresIn = expiresIn,
                User = _mapper.Map<UserProfileDto>(user)
            };
        }

        // login or register a user via Google Sign-In
        // does not require a password, but generates secure tokens just like standard login
        public async Task<AuthResponseDto> GoogleLoginAsync(string email, string fullName)
        {
            var user = await _userRepository.FindByEmailAsync(email);
            var plainRefreshToken = GenerateRefreshToken();

            if (user == null)
            {
                // Register new user via Google automatically
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    Role = "STUDENT", // Default role for Google sign-in
                    PasswordHash = string.Empty, // No password stored for Google auth users
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    RefreshToken = HashRefreshToken(plainRefreshToken),
                    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
                };
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }
            else if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is inactive.");
            }
            else
            {
                // Update refresh token for existing user
                user.RefreshToken = HashRefreshToken(plainRefreshToken);
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            await _userRepository.UpdateLastLoginAsync(user.UserId);

            var (token, expiresIn) = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                AccessToken = token,
                RefreshToken = plainRefreshToken,
                ExpiresIn = expiresIn,
                User = _mapper.Map<UserProfileDto>(user)
            };
        }

        // get a user profile by their unique ID
        public async Task<UserProfileDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            return user != null ? _mapper.Map<UserProfileDto>(user) : null;
        }

        // get all users assigned to a specific role
        public async Task<IEnumerable<UserProfileDto>> GetAllByRoleAsync(string role)
        {
            var users = await _userRepository.FindAllByRoleAsync(role.ToUpper());
            return _mapper.Map<IEnumerable<UserProfileDto>>(users);
        }

        // searches users by matching a keyword against their name or email
        public async Task<IEnumerable<UserProfileDto>> SearchUsersAsync(string keyword)
        {
            var users = await _userRepository.SearchUsersAsync(keyword);
            return _mapper.Map<IEnumerable<UserProfileDto>>(users);
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.FindAllAsync();
            return _mapper.Map<IEnumerable<UserProfileDto>>(users);
        }

        // update the non-security profile details of a user (e.g. Full Name, Avatar)
        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            user.FullName = dto.FullName;
            // user.AvatarUrl is now managed separately via UploadAvatarAsync

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // changes user password securely by verifying the old password first
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            // Verify old password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new BadRequestException("Incorrect old password.");
            }

            // Hash and save the new password
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // initiates the password reset process by generating a unique, time-limited token
        public async Task<string> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userRepository.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new NotFoundException("User with this email not found.");
            }

            // Generate a simple secure token (Consider a more robust method for production like Identity's GeneratePasswordResetTokenAsync)
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes);
            user.ResetPasswordToken = token;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return token;
        }

        // resets user password after validating their reset token and its expiration
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userRepository.FindByEmailAsync(dto.Email);

            // Validate the user, token match, and token expiry time
            if (user == null || user.ResetPasswordToken != dto.Token || user.ResetPasswordExpiry <= DateTime.UtcNow)
            {
                throw new BadRequestException("Invalid or expired password reset token.");
            }

            // Apply new password
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);

            // Clear the reset tokens from the DB to prevent reuse
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // soft-deletes a user account by setting IsActive to false
        // prevent them from logging in but keeps their data intact
        public async Task<bool> DeactivateAccountAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // restores access to a previously deactivated account
        public async Task<bool> ReactivateAccountAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // hard-deletes a user from the database permanently
        public async Task<bool> DeleteAccountAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            var role = user.Role; // Capture role before deletion

            await _userRepository.DeleteAsync(user);
            await _userRepository.SaveChangesAsync();

            // Notify other services (like CourseService) that a user was deleted
            await _publishEndpoint.Publish<IUserDeletedEvent>(new { UserId = userId, Role = role });

            return true;
        }

        // update user role (e.g. from STUDENT to INSTRUCTOR)
        // restricts allowed roles and protects the system's SuperAdmin
        public async Task<bool> ChangeUserRoleAsync(int userId, string newRole)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            // 1. Prevent changing the role of the SuperAdmin
            var superAdminEmail = _configuration["SuperAdmin:Email"];
            if (user.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The SuperAdmin's role cannot be modified.");
            }

            // 2. Validate that the new role is one of the allowed roles
            var allowedRoles = new[] { "STUDENT", "INSTRUCTOR", "ADMIN" };
            var upperRole = newRole.ToUpper();
            if (!allowedRoles.Contains(upperRole))
            {
                throw new BadRequestException($"Invalid role. Allowed roles are: {string.Join(", ", allowedRoles)}");
            }

            user.Role = upperRole;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // handles uploading a profile picture to a cloud storage service
        public async Task<string?> UploadAvatarAsync(int userId, IFormFile file)
        {
            try 
            {
                var user = await _userRepository.FindByUserIdAsync(userId);
                if (user == null) return null;

                // 1. Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    await _storageService.DeleteAsync(user.AvatarUrl);
                }

                // 2. Upload new avatar - returns "avatars/uniqueid.jpg"
                user.AvatarUrl = await _storageService.UploadAsync(file, "avatars");

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                return user.AvatarUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Avatar upload failed for user {userId}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw; // Rethrow to let global handler handle it
            }
        }

        // logs out a user by invalidating their server-side refresh token
        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null) return false;

            // Clear the refresh token to force re-authentication on next expiration
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        // evaluates basic validity of a JWT token string
        public async Task<bool> ValidateTokenAsync(string token)
        {
            // Placeholder for custom token validation beyond framework middleware
            return await Task.FromResult(!string.IsNullOrEmpty(token));
        }

        // issues a new Access Token and Refresh Token by validating a previously issued (but now expired) Access Token
        // alongside a valid Refresh Token
        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            // Decode the expired access token to get the user claims
            var principal = GetPrincipalFromExpiredToken(dto.Token);
            if (principal == null)
            {
                throw new UnauthorizedException("Invalid access token or refresh token");
            }

            var userIdString = principal.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new UnauthorizedException("Invalid access token or refresh token");
            }

            // Find the user and validate that their provided refresh token perfectly matches the stored hash
            var user = await _userRepository.FindByUserIdAsync(userId);
            if (user == null || user.RefreshToken != HashRefreshToken(dto.RefreshToken) || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Invalid access token or refresh token");
            }

            // Generate brand new token pairs to rotate out the old ones
            var (newAccessToken, expiresIn) = GenerateJwtToken(user);
            var plainNewRefreshToken = GenerateRefreshToken();

            user.RefreshToken = HashRefreshToken(plainNewRefreshToken);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = plainNewRefreshToken,
                ExpiresIn = expiresIn,
                User = _mapper.Map<UserProfileDto>(user)
            };
        }

        // decrypts and validates an expired JWT to retrieve the claims embedded inside it without enforcing expiration
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = _jwtSettings.SecretKey;

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is missing.");
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(secretKey)),
                ValidateLifetime = false // Crucial: We WANT to accept an expired token here
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            // Ensure the token isn't forged and was actually signed via HMAC SHA256
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        // generates a cryptographically strong random string to be used as a Refresh Token
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // hashes a plain-text refresh token using SHA256 so that the raw token is never exposed if the database is breached
        private string HashRefreshToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }

        // generates a short-lived JWT token containing the user's core identity claims
        // returns the token string along with its expiration time (Unix seconds)
        private (string Token, long ExpiresIn) GenerateJwtToken(User user)
        {
            var secretKey = _jwtSettings.SecretKey;

            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey is missing or too short. Check your configuration.");
            }

            var key = new SymmetricSecurityKey(Convert.FromBase64String(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryTime = DateTime.UtcNow.AddMinutes(15);

            var claims = new[]
            {
                new Claim("sub", user.UserId.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.FullName),
                new Claim("role", user.Role),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),     ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiryTime,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var expiresIn = new DateTimeOffset(expiryTime).ToUnixTimeSeconds();

            return (tokenString, expiresIn);
        }
    }
}

