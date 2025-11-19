using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Students.Models;
using Students.Models.DTOS;
using Students.Repositories;
using Students.Services;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Students.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository users, IConfiguration configuration)
        {
            _users = users;
            _configuration = configuration;
        }

        public async Task<(bool ok, LoginResponseDto? response)> LoginAsync(LoginDto dto)
        {
            var user = await _users.GetByEmailAddress(dto.Email);
            if (user == null) return (false, null);

            var ok = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!ok) return (false, null);

            // Generar par access/refresh
            var (accessToken, expiresIn, jti) = GenerateJwtToken(user);
            var refreshToken = GenerateSecureRefreshToken();

            var refreshDays = int.Parse(_configuration["Jwt:RefreshDays"] ?? "14");

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
            user.RefreshTokenRevokedAt = null;
            user.CurrentJwtId = jti;
            await _users.UpdateAsync(user);

            var resp = new LoginResponseDto
            {
                User = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email },
                Role = user.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresIn,
                TokenType = "Bearer"
            };

            return (true, resp);
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Username = dto.Username,
                Role = dto.Role
            };
            await _users.AddAsync(user);
            return user.Id.ToString();
        }

        public async Task<(bool ok, LoginResponseDto? response)> RefreshAsync(RefreshRequestDto dto)
        {
            // Buscar usuario que tenga ese refresh token (simple)
            var user = await _users.GetByRefreshToken(dto.RefreshToken);
            if (user == null) return (false, null);

            // Validaciones de refresh
            if (user.RefreshToken != dto.RefreshToken) return (false, null);
            if (user.RefreshTokenRevokedAt.HasValue) return (false, null);
            if (!user.RefreshTokenExpiresAt.HasValue || user.RefreshTokenExpiresAt.Value < DateTime.UtcNow) return (false, null);

            // Rotación: generar nuevo access + refresh y revocar el anterior
            var (accessToken, expiresIn, jti) = GenerateJwtToken(user);
            var newRefresh = GenerateSecureRefreshToken();
            var refreshDays = int.Parse(_configuration["Jwt:RefreshDays"] ?? "14");

            user.RefreshToken = newRefresh;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
            user.RefreshTokenRevokedAt = null; // seguimos activo
            user.CurrentJwtId = jti;
            await _users.UpdateAsync(user);

            var resp = new LoginResponseDto
            {
                User = new UserDto { Id = user.Id, Username = user.Username, Email = user.Email },
                Role = user.Role,
                AccessToken = accessToken,
                RefreshToken = newRefresh,
                ExpiresIn = expiresIn,
                TokenType = "Bearer"
            };

            return (true, resp);
        }

        private (string token, int expiresInSeconds, string jti) GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expireMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "60");

            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim> {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return (jwt, (int)TimeSpan.FromMinutes(expireMinutes).TotalSeconds, jti);
        }

        private static string GenerateSecureRefreshToken()
        {
            // 64 bytes aleatorios en Base64Url
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Base64UrlEncoder.Encode(bytes);
        }
    }
}