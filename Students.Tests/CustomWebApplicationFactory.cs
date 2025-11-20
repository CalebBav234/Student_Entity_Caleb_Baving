using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Students.Data;

namespace Students.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "JWT_KEY", "L51eEHXSFpC540MuPARe443GzoZlZYm6OjUvTiX1i6G" },
                { "JWT_ISSUER", "StudentAPi" },
                { "JWT_AUDIENCE", "StudentClient" }
            });
        });

        Environment.SetEnvironmentVariable("Jwt__Key", "L51eEHXSFpC540MuPARe443GzoZlZYm6OjUvTiX1i6G");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "StudentAPi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "StudentClient");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var contextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppDbContext));

            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
        });
    }
}
