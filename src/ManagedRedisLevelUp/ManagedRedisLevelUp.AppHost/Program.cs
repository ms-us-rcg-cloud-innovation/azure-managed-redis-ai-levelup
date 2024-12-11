var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var serviceBus = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureServiceBus("messaging")
    : builder.AddConnectionString("messaging");

var apiService = builder.AddProject<Projects.ManagedRedisLevelUp_ApiService>("apiservice")
  .WithReference(serviceBus);

builder.AddProject<Projects.ManagedRedisLevelUp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
