using Students.Models.DTOS;

namespace Students.Services
{
    public interface IAuthService
    {
        Task<(bool ok, LoginResponseDto? response)> LoginAsync(LoginDto dto);
        Task<string> RegisterAsync(RegisterDto dto);
        Task<(bool ok, LoginResponseDto? response)> RefreshAsync(RefreshRequestDto dto);
    }
}