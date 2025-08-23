using FusionCacheApplication.Configuration;
using FusionCacheApplication.Domain.Interfaces;
using FusionCacheApplication.Domain.Models;
using FusionCacheApplication.Dtos;
using FusionCacheApplication.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace FusionCacheApplication;

//TODO:
//  - Create DistributedErrorSettings and service for chaos
//  - Create CacheService to centralize all configs and chaos behaviour

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddControllers();

        builder.Services.AddOpenApi();

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

        await app.ApplyDatabaseMigrationsAsync();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
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
        app.MapGet("/users/{id:guid}", async (Guid id, IUserService service, AppDbContext db, CancellationToken ct) =>
        {
            var user = await service.GetByIdAsync(id, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        app.MapPost("/users", async ([FromBody] UpsertUserDto dto, IUserService service, CancellationToken ct) =>
        {
            var user = new User(dto.Username, dto.Email, dto.Id);
            await service.UpsertAsync(user, ct);
            return Results.Created($"/users/{dto.Id}", dto);
        });

        app.MapDelete("/users/{id:guid}", async (Guid id, IUserService service, CancellationToken ct) =>
        {
            await service.RemoveAsync(id, ct);
            return Results.NoContent();
        });

        app.MapGet("/users/invalidate-all-cache", async (IUserService service, CancellationToken ct) =>
        {
            await service.InvalidateCacheForUsersAsync(ct);
            return Results.Accepted();
        });
    }

    private static void ConfigureAdminController(WebApplication app)
    {
        app.MapGet("/admin/db/chaos", (ChaosSettings chaos) => chaos);

        app.MapPost("/admin/db/fail", (bool on, ChaosSettings chaos) => { chaos.Fail = on; return Results.Ok(new { chaos.Fail }); });
        app.MapPost("/admin/db/slow", (int ms, ChaosSettings chaos) => { chaos.SlowMs = ms; return Results.Ok(new { chaos.SlowMs }); });
    }
}
