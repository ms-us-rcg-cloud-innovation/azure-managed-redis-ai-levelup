using ManagedRedisLevelUp.ApiService.Services;
using ManagedRedisLevelUp.Shared;
using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Vectorizers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Configure Redis and OpenAI services
ConfigureRedisServices(builder);
ConfigureOpenAIServices(builder);

// Register application services
builder.Services.AddScoped<RecipeService>(ConfigureRecipeService);

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigureRequestPipeline(app);

// Map API endpoints
MapRecipeEndpoints(app);

app.MapDefaultEndpoints();
app.Run();

// Service configuration methods
static void ConfigureRedisServices(WebApplicationBuilder builder)
{
  // Add Redis cache and connection multiplexer
  builder.AddRedisOutputCache(connectionName: "cache");

  // Register Redis.OM services
  builder.Services.AddSingleton<RedisConnectionProvider>(sp =>
  {
    var aspireRedis = sp.GetRequiredService<IConnectionMultiplexer>();
    return new RedisConnectionProvider(aspireRedis);
  });
}

static void ConfigureOpenAIServices(WebApplicationBuilder builder)
{
  // Add OpenAI client from Aspire
  builder.AddAzureOpenAIClient("openAi");

  // Set required environment variables for Redis.OM
  var aoaiKey = builder.Configuration["AOAI:KEY"];
  if (!string.IsNullOrEmpty(aoaiKey))
  {
    Environment.SetEnvironmentVariable("REDIS_OM_AZURE_OAI_TOKEN", aoaiKey);
  }

  // Configure semantic cache
  builder.Services.AddSingleton<ISemanticCache>(sp =>
  {
    var config = builder.Configuration.GetSection("AOAI");
    var provider = sp.GetRequiredService<RedisConnectionProvider>();

    return provider.AzureOpenAISemanticCache(
            apiKey: config["KEY"],
            resourceName: config["ENDPOINT"],
            deploymentId: config["DEPLOYMENT_NAME"],
            dim: 1536,
            ttl: 10000);  // 10 second TTL
  });
}

static RecipeService ConfigureRecipeService(IServiceProvider svcProvider)
{
  var svc = new RecipeService(
      svcProvider.GetRequiredService<IConnectionMultiplexer>(),
      svcProvider.GetRequiredService<RedisConnectionProvider>(),
      svcProvider.GetRequiredService<ISemanticCache>()
  );

  svc.Initialize();
  return svc;
}

static void ConfigureRequestPipeline(WebApplication app)
{
  app.UseExceptionHandler();

  if (app.Environment.IsDevelopment())
  {
    app.MapOpenApi();
  }

  // Add OutputCache middleware
  app.UseOutputCache();
}

static void MapRecipeEndpoints(WebApplication app)
{
  var recipes = app.MapGroup("/recipes");

  recipes.MapGet("", GetRecipes)
      .WithName("Get Recipes");

  recipes.MapGet("{key}", GetRecipeById)
      .WithName("Get Recipe");

  recipes.MapGet("search/{query}", SearchRecipes)
      .WithName("Search Recipes");

  recipes.MapGet("count", GetRecipeCount)
      .WithName("Get Recipe Count");

  recipes.MapPost("", CreateRecipe)
      .WithName("Create Recipe");
}

// Request handlers
static async Task<IResult> GetRecipes(RecipeService recipeService)
{
  var recipeResponse = await recipeService.GetRecipesAsync();
  return Results.Ok(recipeResponse);
}

static async Task<IResult> GetRecipeById([FromServices] RecipeService recipeService, string key)
{
  var recipeResponse = await recipeService.GetRecipeAsync(key);
  return Results.Ok(recipeResponse);
}

static async Task<IResult> SearchRecipes(
    [FromServices] RecipeService recipeService,
    [FromQuery] string? approach,
    string query)
{
  var recipeResponse = await recipeService.SearchRecipesAsync(query, approach);
  return Results.Ok(recipeResponse);
}

static IResult GetRecipeCount([FromServices] RecipeService recipeService)
{
  return Results.Ok(recipeService.GetRecipeCount());
}

static async Task<IResult> CreateRecipe([FromServices] RecipeService recipeService, Recipe recipe)
{
  List<Recipe> recipes = [recipe];
  await recipeService.UploadRecipesAsync(recipes);
  return Results.Created($"/recipes/{recipe.Key}", recipe);
}