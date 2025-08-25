var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("pgUser", "userFusion", secret: false);
var pgPassword = builder.AddParameter("pgPassword", "passwordFusion", secret: false);

var postgres = builder
    .AddPostgres("postgresBackplaneFusion", pgUser, pgPassword)
    .WithContainerName("postgresBackplaneFusion")
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050))
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("POSTGRES_USER", "userFusion")
    .WithEnvironment("POSTGRES_PASSWORD", "passwordFusion");

var postgresdb = postgres.AddDatabase("fusionApplicationDb");

var redisPassword = builder.AddParameter("redisPassword", "secretPasswordRedis", secret: false);
var redisCache = builder
    .AddRedis("redisBackplaneFusion", 6379, redisPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("redisBackplaneFusion");

builder.AddProject<Projects.FusionCacheApplication>("fusioncacheapplication-1")
    .WithReference(postgresdb)
    .WithReference(redisCache)
    .WithEnvironment("INSTANCE_ID", "instance-1")
    .WithEnvironment("ASPNETCORE_URLS", "https://localhost:63322");

builder.AddProject<Projects.FusionCacheApplication>("fusioncacheapplication-2")
    .WithReference(postgresdb)
    .WithReference(redisCache)
    .WithEnvironment("INSTANCE_ID", "instance-2")
    .WithEnvironment("ASPNETCORE_URLS", "https://localhost:63327");

builder.Build().Run();
