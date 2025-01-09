using ManagedRedisLevelUp.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
    : builder.AddConnectionString("secrets");

var cache = builder.ExecutionContext.IsPublishMode
  ? builder.AddRedis("cache")
  : builder.AddConnectionString("cache");

var serviceBus = builder.ExecutionContext.IsPublishMode
  ? builder.AddAzureServiceBus("messaging")
  : builder.AddConnectionString("messaging");

var openai = builder.ExecutionContext.IsPublishMode
  ? builder.AddAzureOpenAIWithKeyBasedAuth("openAi")
      .AddDeployment(new AzureOpenAIDeployment("embedding", "text-embedding-ada-002", "2"))
      .AddDeployment(new AzureOpenAIDeployment("chat", "gpt-4o", "2024-08-06"))
  : builder.AddConnectionString("openAi");

var apiService = builder.AddProject<Projects.ManagedRedisLevelUp_ApiService>("apiservice")
  .WithReference(serviceBus)
  .WithReference(openai)
  .WithReference(cache);

builder.AddProject<Projects.ManagedRedisLevelUp_Web>("webfrontend")
  .WithExternalHttpEndpoints()
  .WithReference(cache)
  //.WaitFor(cache)
  .WithReference(apiService)
  .WaitFor(apiService);

builder.Build().Run();
