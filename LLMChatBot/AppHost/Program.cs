var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddConnectionString("PostgreSQL");
var redis = builder.AddConnectionString("Redis");

builder.AddProject<Projects.API>("api")
    .WithReference(postgres)
    .WithReference(redis);

builder.Build().Run();
