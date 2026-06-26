using Ambev.DeveloperEvaluation.Application.Common.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Events;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<DefaultContext>());
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();

        // MongoDB Client and Database Registration
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetValue<string>("MongoDb:ConnectionString") 
                ?? "mongodb://developer:ev%40luAt10n@localhost:27017";
            return new MongoClient(connectionString);
        });

        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var databaseName = configuration.GetValue<string>("MongoDb:DatabaseName") 
                ?? "developer_evaluation_audit";
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        // Event Publisher
        builder.Services.AddScoped<IEventPublisher, LoggingEventPublisher>();
    }
}