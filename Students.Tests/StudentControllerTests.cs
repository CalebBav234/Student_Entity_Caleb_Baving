using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Students.Data;
using Students.Models;
using Students.Models.DTOS;
using Students.Tests;

namespace Students.Tests;

public class StudentControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public StudentControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetUserToken()
    {
        var registerDto = new RegisterDto { Email = "user@example.com", Password = "password123", Username = "user", Role = "User" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        var loginDto = new LoginDto { Email = "user@example.com", Password = "password123" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return result.AccessToken;
    }

    private async Task<string> GetAdminToken()
    {
        var registerDto = new RegisterDto { Email = "admin@example.com", Password = "password123", Username = "admin", Role = "User" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);

        // Promote to admin
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == "admin@example.com");
        user.Role = "Admin";
        await db.SaveChangesAsync();

        var loginDto = new LoginDto { Email = "admin@example.com", Password = "password123" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return result.AccessToken;
    }

    [Fact]
    public async Task GetAllStudents_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/student");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var students = await response.Content.ReadFromJsonAsync<IEnumerable<Student>>();
        Assert.NotNull(students);
    }

    [Fact]
    public async Task GetOneStudent_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/student/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOneStudent_WithAuth_NotFound_ReturnsNotFound()
    {
        var token = await GetUserToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/student/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateStudent_WithoutAuth_ReturnsUnauthorized()
    {
        var dto = new CreateStudentDto { Name = "Test Student", Email = "test@student.com", Age = 20 };
        var response = await _client.PostAsJsonAsync("/api/student", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStudent_WithUserToken_ReturnsForbidden()
    {
        var token = await GetUserToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateStudentDto { Name = "Test Student", Email = "test@student.com", Age = 20 };
        var response = await _client.PostAsJsonAsync("/api/student", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateStudent_WithAdminToken_ReturnsCreated()
    {
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateStudentDto { Name = "Test Student", Email = "test@student.com", Age = 20 };
        var response = await _client.PostAsJsonAsync("/api/student", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var student = await response.Content.ReadFromJsonAsync<Student>();
        Assert.NotNull(student);
        Assert.Equal("Test Student", student.Name);
        Assert.Equal(20, student.Age);
    }

    [Fact]
    public async Task CreateStudent_InvalidModel_ReturnsBadRequest()
    {
        var token = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateStudentDto { Name = "", Email = "invalid", Age = 20 };
        var response = await _client.PostAsJsonAsync("/api/student", dto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStudent_WithoutAuth_ReturnsUnauthorized()
    {
        var dto = new UpdateStudentDto { Name = "Updated", Email = "updated@test.com" };
        var response = await _client.PutAsJsonAsync("/api/student/00000000-0000-0000-0000-000000000000", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStudent_WithAuth_ReturnsOk()
    {
        // Create student first
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateStudentDto { Name = "Test Student", Email = "test@student.com", Age = 20 };
        var createResponse = await _client.PostAsJsonAsync("/api/student", createDto);
        var createdStudent = await createResponse.Content.ReadFromJsonAsync<Student>();

        // Update with user token
        var userToken = await GetUserToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var updateDto = new UpdateStudentDto { Name = "Updated Student", Email = "updated@student.com", Age = 21 };
        var response = await _client.PutAsJsonAsync($"/api/student/{createdStudent.Id}", updateDto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteStudent_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.DeleteAsync("/api/student/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteStudent_WithUserToken_ReturnsForbidden()
    {
        var token = await GetUserToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync("/api/student/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteStudent_WithAdminToken_ReturnsNoContent()
    {
        // Create student
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateStudentDto { Name = "Test Student", Email = "test@student.com" };
        var createResponse = await _client.PostAsJsonAsync("/api/student", createDto);
        var createdStudent = await createResponse.Content.ReadFromJsonAsync<Student>();

        // Delete
        var response = await _client.DeleteAsync($"/api/student/{createdStudent.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
