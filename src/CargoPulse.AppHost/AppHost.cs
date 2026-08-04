var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18.4-alpine")
    .WithDataVolume("cargopulse-postgres-data");

var database = postgres.AddDatabase("cargopulse");

var messaging = builder.AddRabbitMQ("messaging")
    .WithImageTag("4.3.4-management-alpine")
    .WithManagementPlugin()
    .WithDataVolume("cargopulse-rabbitmq-data");

var api = builder.AddProject<Projects.CargoPulse_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/ready")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.CargoPulse_Worker>("worker")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/ready");

builder.AddProject<Projects.CargoPulse_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpHealthCheck("/ready")
    .WithExternalHttpEndpoints();

builder.Build().Run();
