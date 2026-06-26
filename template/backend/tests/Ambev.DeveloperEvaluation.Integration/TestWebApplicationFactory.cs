using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ambev.DeveloperEvaluation.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:SecretKey", "YourSuperSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32BytesLong" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext options
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DefaultContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContext));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add DefaultContext using InMemory database for testing
            services.AddDbContext<DefaultContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());

            // Replace Event Publisher to avoid MongoDB connection attempts during tests
            var eventPublisherDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEventPublisher));

            if (eventPublisherDescriptor != null)
            {
                services.Remove(eventPublisherDescriptor);
            }

            services.AddScoped<IEventPublisher, DummyEventPublisher>();
        });
    }
}

public class DummyEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }
}
