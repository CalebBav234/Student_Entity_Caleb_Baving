using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Students.Models.DTOS;
using Students.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Students.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidInput_ReturnsCreated()
    {
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "password123",
            Username = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsBadRequest()
    {
        var dto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "",
            Username = "",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Register first
        var registerDto = new RegisterDto
        {
            Email = "login@example.com",
            Password = "password123",
            Username = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = "login@example.com",
            Password = "password123"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidRefreshToken_ReturnsOkWithNewTokens()
    {
        // Register and login
        var registerDto = new RegisterDto
        {
            Email = "refresh@example.com",
            Password = "password123",
            Username = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = "refresh@example.com",
            Password = "password123"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        var refreshDto = new RefreshRequestDto
        {
            RefreshToken = loginResult.RefreshToken
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.AccessToken);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidRefreshToken_ReturnsUnauthorized()
    {
        var refreshDto = new RefreshRequestDto
        {
            RefreshToken = "invalidtoken"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
