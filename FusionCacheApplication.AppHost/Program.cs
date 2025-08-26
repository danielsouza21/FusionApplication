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

// Instance 1
builder.AddProject<Projects.FusionCacheApplication>("fusioncacheapplication-1")
    .WithReference(postgresdb)
    .WithReference(redisCache)
    .WithEnvironment("INSTANCE_ID", "instance-1");
    //.WithEnvironment("ASPNETCORE_URLS", "https://localhost:65001;http://localhost:65002")
    //.WithEnvironment("ASPNETCORE_HTTPS_PORT", "65001")
    //.WithEnvironment("ASPNETCORE_HTTP_PORT", "65002")
    //.WithHttpsEndpoint(port: 65001, targetPort: 8081, name: "https-1")
    //.WithHttpEndpoint(port: 65002, targetPort: 8080, name: "http-1");

// Instance 2
builder.AddProject<Projects.FusionCacheApplication>("fusioncacheapplication-2")
    .WithReference(postgresdb)
    .WithReference(redisCache)
    .WithEnvironment("INSTANCE_ID", "instance-2");
    //.WithEnvironment("ASPNETCORE_URLS", "https://localhost:65006;http://localhost:65007")
    //.WithEnvironment("ASPNETCORE_HTTPS_PORT", "65006")
    //.WithEnvironment("ASPNETCORE_HTTP_PORT", "65007")
    //.WithHttpsEndpoint(port: 65006, targetPort: 8081, name: "https-2")
    //.WithHttpEndpoint(port: 65007, targetPort: 8080, name: "http-2");

builder.Build().Run();
