namespace EduLearn.AuthService.DTOs
{
    public class RefreshTokenRequestDto
    {
        public required string Token { get; set; }
        public required string RefreshToken { get; set; }
    }
}
