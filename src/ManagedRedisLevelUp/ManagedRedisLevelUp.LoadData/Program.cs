using ManagedRedisLevelUp.ApiService.Services;
using ManagedRedisLevelUp.Shared;
using Microsoft.Extensions.Configuration;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Vectorizers;
using StackExchange.Redis;

namespace ManagedRedisLevelUp.LoadData;

class Program
{
  static async Task Main(string[] args)
  {
    var builder = new ConfigurationBuilder()
        .AddUserSecrets<Program>();

    var config = builder.Build();

    Environment.SetEnvironmentVariable("REDIS_OM_AZURE_OAI_TOKEN", config["AOAI:KEY"]);

    var jsonFilePath = "recipes.json";
    List<Recipe> recipes = GetRecipesFromFile(jsonFilePath);

    var redisConfig = new ConfigurationOptions();

    var redisConnectionString = config.GetConnectionString("REDIS");

    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
    ConnectionMultiplexer _newConnection = await ConnectionMultiplexer.ConnectAsync(configurationOptions);

    var connectionProvider = new RedisConnectionProvider(_newConnection);

    var openAiConfig = config.GetSection("AOAI");
    var semanticCache = GetSemanticCache(openAiConfig, connectionProvider);

    var recipeService = new RecipeService(
      _newConnection,
      connectionProvider,
      semanticCache);

    for (int i = 0; i < recipes.Count; i++)
    {
      recipes[i].SetVectors();
    }

    await recipeService.InitializeAsync();
    await recipeService.UploadRecipesAsync(recipes);

    Console.WriteLine($"Total records upserted: {recipes.Count}.");
  }

  /// <summary>
  /// Read from local JSON file and parse into List<Recipe>
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  private static List<Recipe> GetRecipesFromFile(string jsonFilePath)
  {
    using StreamReader reader = new(jsonFilePath);
    return System.Text.Json.JsonSerializer.Deserialize<List<Recipe>>(reader.ReadToEnd())
      ?? throw new InvalidOperationException("The recipes list is null.");
  }

  private static ISemanticCache GetSemanticCache(IConfigurationSection config, RedisConnectionProvider connectionProvider)
  {        
    var semanticCache = connectionProvider.AzureOpenAISemanticCache(
      apiKey: config["KEY"],
      resourceName: config["ENDPOINT"],
      deploymentId: config["DEPLOYMENT_NAME"],
      dim: 1536);
    return semanticCache;
  }
}