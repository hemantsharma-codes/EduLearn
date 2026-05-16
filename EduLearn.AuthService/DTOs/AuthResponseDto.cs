namespace EduLearn.AuthService.DTOs
{
    public class AuthResponseDto
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public long ExpiresIn { get; set; } // Unix timestamp or seconds
        public required UserProfileDto User { get; set; }
    }
}
