var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("pgUser", "userFusion", secret: false);
var pgPassword = builder.AddParameter("pgPassword", "passwordFusion", secret: false);

var postgres = builder
    .AddPostgres("postgresBackplaneFusion", pgUser, pgPassword)
    .WithPgAdmin(pgAdmin => pgAdmin.WithHostPort(5050))
    .WithDataVolume(isReadOnly: false)
    .WithEnvironment("POSTGRES_USER", "userFusion")
    .WithEnvironment("POSTGRES_PASSWORD", "passwordFusion");

var postgresdb = postgres.AddDatabase("fusionApplicationDb");

var redisPassword = builder.AddParameter("redisPassword", "secretPasswordRedis", secret: false);
var redisCache = builder.AddRedis("redisBackplaneFusion", 6379, redisPassword);

builder.AddProject<Projects.FusionCacheApplication>("fusioncacheapplication")
    .WithReference(postgresdb)
    .WithReference(redisCache)
    .WithReplicas(3);

builder.Build().Run();
