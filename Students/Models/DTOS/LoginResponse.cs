namespace Students.Models.DTOS
{
    public class LoginResponseDto
    {
        public required UserDto User { get; set; }
        public required string Role { get; set; }
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; } // en segundos

        public object Should()
        {
            throw new NotImplementedException();
        }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
    }

    public class RefreshRequestDto
    {
        public required string RefreshToken { get; set; }
    }
}
