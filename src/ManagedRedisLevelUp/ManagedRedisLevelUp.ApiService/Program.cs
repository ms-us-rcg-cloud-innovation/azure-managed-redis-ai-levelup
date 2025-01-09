using Azure.AI.OpenAI;
using ManagedRedisLevelUp.ApiService.Services;
using ManagedRedisLevelUp.Shared;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault secrets from Aspire host to the configuration
builder.Configuration.AddAzureKeyVaultSecrets("secrets");

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add RedisOutputCache from the Aspire client integrations.
builder.AddRedisOutputCache(connectionName: "cache");

// Add OpenAIClient from the Aspire client integrations.
builder.AddAzureOpenAIClient("openAi");

//// Add SearchIndexClient from the Aspire client integrations.
//builder.AddAzureSearchClient("search");

builder.Services.AddScoped((serviceProvider) =>
{
  var azureOpenAIClient = serviceProvider.GetRequiredService<AzureOpenAIClient>();

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
  var kernel = Kernel.CreateBuilder()
    .AddRedisVectorStore(builder.Configuration["REDIS_CONNECTION_STRING"]
      ?? throw new InvalidOperationException("The configuration value for 'REDIS_CONNECTION_STRING' is missing or null"))
    .AddAzureOpenAITextEmbeddingGeneration(
      builder.Configuration["EMBEDDING_DEPLOYMENT_NAME"]
        ?? throw new InvalidOperationException("The configuration value for 'EMBEDDING_DEPLOYMENT_NAME' is missing or null."),
      azureOpenAIClient: azureOpenAIClient
    )
    .AddAzureOpenAIChatCompletion(
      builder.Configuration["CHAT_DEPLOYMENT_NAME"]
        ?? throw new InvalidOperationException("The configuration value for 'AOAI_CHAT_DEPLOYMENT_NAME' is missing or null"),
      azureOpenAIClient: azureOpenAIClient
    );

  kernel.Services.AddSingleton<RecipeService>();
  return kernel.Build();
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

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
  var forecast = Enumerable.Range(1, 5).Select(index =>
      new WeatherForecast
      (
          DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
          Random.Shared.Next(-20, 55),
          summaries[Random.Shared.Next(summaries.Length)]
      ))
      .ToArray();
  return forecast;
})
  .CacheOutput(policy =>
  {
    policy.Expire(TimeSpan.FromSeconds(5));
  })
  .WithName("GetWeatherForecast");

app.MapGet("/recipes", async (Kernel kernel) =>
{
  RecipeService svc = (RecipeService)kernel.Services.GetService(typeof(RecipeService));
  var recipeResponse = await svc.GetRecipeAsync("recipes", "blah");
  return recipeResponse;
})
  .WithName("Get Recipes");

app.MapGet("/recipes/{key}", async (Kernel kernel, string key) =>
{
  RecipeService svc = (RecipeService)kernel.Services.GetService(typeof(RecipeService));
  var recipeResponse = await svc.GetRecipeAsync("recipes", key);
  return recipeResponse;
})
  .WithName("Get Recipe");

app.MapPost("/recipes", async (Kernel kernel, Recipe recipe) =>
{
  RecipeService svc = (RecipeService)kernel.Services.GetService(typeof(RecipeService));
  await svc.GenerateEmbeddingsAndUpload("recipes", [recipe]);
  return Results.Created("/recipes", recipe);
})
  .WithName("Create Recipe");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
