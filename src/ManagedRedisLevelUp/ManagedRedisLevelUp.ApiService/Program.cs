using ManagedRedisLevelUp.ApiService.Services;
using ManagedRedisLevelUp.Shared;
using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Vectorizers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault secrets from Aspire host to the configuration
//builder.Configuration.AddAzureKeyVaultSecrets("secrets");

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add RedisOutputCache and IConnectionMultiplexer from the Aspire client integrations.
builder.AddRedisOutputCache(connectionName: "cache");

// Add OpenAIClient from the Aspire client integrations.
builder.AddAzureOpenAIClient("openAi");

Environment.SetEnvironmentVariable(
  "REDIS_OM_AZURE_OAI_TOKEN", 
  builder.Configuration["REDIS_OM_AZURE_OAI_TOKEN"]
);

//// Add SearchIndexClient from the Aspire client integrations.
//builder.AddAzureSearchClient("search");

// Use Aspire-provided Redis IConnectionMultiplexer to get connection string for Redis.OM
builder.Services.AddSingleton<RedisConnectionProvider>(sp =>
{
  var aspireRedis = sp.GetRequiredService<IConnectionMultiplexer>();
  var redisConnectionProvider = new RedisConnectionProvider(aspireRedis);
  return redisConnectionProvider;
});

builder.Services.AddSingleton<ISemanticCache>(sp =>
{
  var config = builder.Configuration.GetSection("AOAI");

  var _provider = sp.GetRequiredService<RedisConnectionProvider>();
  var semanticCache = _provider.AzureOpenAISemanticCache(
    apiKey: config["KEY"],
    resourceName: config["ENDPOINT"],
    deploymentId: config["DEPLOYMENT_NAME"], 
    dim: 1536,
    ttl: 10000); // 10 second TTL
  return semanticCache;
});

builder.Services.AddScoped<RecipeService>(svcProvider =>
{
  var svc = new RecipeService(
    svcProvider.GetRequiredService<IConnectionMultiplexer>(),
    svcProvider.GetRequiredService<RedisConnectionProvider>(),
    svcProvider.GetRequiredService<ISemanticCache>()
  );

  svc.InitializeAsync().GetAwaiter().GetResult();

  return svc;
});

// Rregister a ServiceBusClient for use via the dependency injection container
builder.AddAzureServiceBusClient("messaging");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

// Add OutputCache middleware for Redis provided by Aspire client integrations.
app.UseOutputCache();

app.MapGet("/recipes", async (RecipeService recipeService) =>
{
  var recipeResponse = await recipeService.GetRecipesAsync();
  return Results.Ok(recipeResponse);
})
  .WithName("Get Recipes");

app.MapGet("/recipes/{key}", async ([FromServices] RecipeService recipeService, string key) =>
{
  var recipeResponse = await recipeService.GetRecipeAsync(key);
  return Results.Ok(recipeResponse);
})
  .WithName("Get Recipe");

app.MapGet("/recipes/search/{query}", async (
  [FromServices] RecipeService recipeService, 
  [FromQuery] string? approach, 
  string query) =>
{
  var recipeResponse = await recipeService.SearchRecipesAsync(query, approach);
  var response = recipeResponse.ToList();
  return Results.Ok(recipeResponse);
})
  .WithName("Search Recipes");

app.MapGet("/recipes/count", async ([FromServices] RecipeService recipeService) =>
{
  return Results.Ok(recipeService.GetRecipeCount());
})
  .WithName("Get Recipe Count");

app.MapPost("/recipes", async ([FromServices] RecipeService recipeService, Recipe recipe) =>
{
  List<Recipe> recipes = [recipe];
  await recipeService.UploadRecipesAsync(recipes);
  return Results.Created($"/recipes/{recipe.Key}", recipe);
})
  .WithName("Create Recipe");

app.MapDefaultEndpoints();

app.Run();

static string? GetApiKeyFromConnectionString(string connectionString)
{
  var parts = connectionString.Split(',');
  var keyPart = parts.FirstOrDefault(p => p.StartsWith("password=", StringComparison.OrdinalIgnoreCase));
  return keyPart?.Split('=')[1];
}