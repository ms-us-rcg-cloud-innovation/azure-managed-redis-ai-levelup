using ManagedRedisLevelUp.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.ExecutionContext.IsPublishMode
  ? builder.AddRedis("cache")
  : builder.AddConnectionString("cache");

var openai = builder.ExecutionContext.IsPublishMode
  ? builder.AddAzureOpenAIWithKeyBasedAuth("openAi")
      .AddDeployment(new AzureOpenAIDeployment("embedding", "text-embedding-ada-002", "2"))
      .AddDeployment(new AzureOpenAIDeployment("chat", "gpt-4o", "2024-08-06"))
  : builder.AddConnectionString("openAi");

var apiService = builder.AddProject<Projects.ManagedRedisLevelUp_ApiService>("apiservice")
  .WithReference(openai)
  .WithReference(cache);

builder.AddProject<Projects.ManagedRedisLevelUp_Web>("webfrontend")
  .WithExternalHttpEndpoints()
  .WithReference(cache)
  .WithReference(apiService)
  .WaitFor(apiService);

builder.Build().Run();
