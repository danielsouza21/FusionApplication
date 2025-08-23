using FusionCacheApplication.Configuration;
using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using FusionCacheApplication.Dtos;
using FusionCacheApplication.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace FusionCacheApplication;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // Add these lines for complete Swagger configuration
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "FusionCache Application API",
                Version = "v1",
                Description = $"A sample API demonstrating FusionCache with Redis and PostgreSQL - Instance {Guid.NewGuid()}"
            });
        });

        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        builder.AddRedisWithAspire();
        builder.AddDatabaseWithAspire();
        builder.Services.AddDatabaseRepositories();
        builder.Services.AddFusionCacheService(builder.Configuration);
        builder.Services.AddApplicationServices();

        var app = builder.Build();

        // Apply database migrations using our custom service
        using (var scope = app.Services.CreateScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
            
            try
            {
                await migrationService.MigrateAsync();
                Console.WriteLine("Database migration completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying database migration: {ex.Message}");
                // You might want to throw here depending on your requirements
            }
        }


        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            // Add this line to enable Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FusionCache Application API V1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.MapGet("/health", () => Results.Ok("ok"));

        ConfigureUserController(app);
        ConfigureAdminController(app);

        app.Run();
    }

    private static void ConfigureUserController(WebApplication app)
    {
        app.MapGet("/users/{id:guid}", async (Guid id, IUserRepository repo, AppDbContext db, CancellationToken ct) =>
        {
            var user = await repo.GetByIdAsync(id, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        app.MapPost("/users", async ([FromBody] UpsertUserDto dto, IUserRepository repo, CancellationToken ct) =>
        {
            var user = new User(dto.Username, dto.Email, dto.Id);
            await repo.UpsertAsync(user, ct);
            return Results.Created($"/users/{dto.Id}", dto);
        });

        app.MapDelete("/users/{id:guid}", async (Guid id, IUserRepository repo, CancellationToken ct) =>
        {
            await repo.RemoveAsync(id, ct);
            return Results.NoContent();
        });
    }

    private static void ConfigureAdminController(WebApplication app)
    {
        app.MapGet("/admin/db/chaos", (ChaosSettings chaos) => chaos);

        app.MapPost("/admin/db/fail", (bool on, ChaosSettings chaos) => { chaos.Fail = on; return Results.Ok(new { chaos.Fail }); });
        app.MapPost("/admin/db/slow", (int ms, ChaosSettings chaos) => { chaos.SlowMs = ms; return Results.Ok(new { chaos.SlowMs }); });
    }
}
