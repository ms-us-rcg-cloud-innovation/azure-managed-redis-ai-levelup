using ManagedRedisLevelUp.ApiService.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped((serviceProvider) =>
{
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
  var kernel = Kernel.CreateBuilder()
    //.AddInMemoryVectorStore()
    .AddRedisVectorStore(builder.Configuration["REDIS_CONNECTION_STRING"])
    .AddAzureOpenAITextEmbeddingGeneration(
      builder.Configuration["EMBEDDING_DEPLOYMENT_NAME"],
      builder.Configuration["AOAI_ENDPOINT"],
      builder.Configuration["AOAI_API_KEY"]
    )
    .AddAzureOpenAIChatCompletion(
      builder.Configuration["AOAI_CHAT_DEPLOYMENT_NAME"],
      builder.Configuration["AOAI_ENDPOINT"],
      builder.Configuration["AOAI_API_KEY"]);

  kernel.Services.AddSingleton<ParagraphService>();
  kernel.Services.AddSingleton<ChatService>();
  kernel.Services.AddSingleton<ChatCacheService>();
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
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
