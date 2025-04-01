/*
 * How to use this service
 * 
 * 1. Set Up User Secrets:
 *    - Add your Azure OpenAI API key and endpoint to the user secrets. You can do this by running the following commands in your terminal:
 *      dotnet user-secrets set "AOAI:KEY" "<your-azure-openai-key>"
 *      dotnet user-secrets set "AOAI:ENDPOINT" "<your-azure-openai-endpoint>"
 *      dotnet user-secrets set "AOAI:DEPLOYMENT_NAME" "<your-deployment-name>"
 *      dotnet user-secrets set "ConnectionStrings:REDIS" "<your-redis-connection-string>"
 * 
 * 2. Prepare the JSON File:
 *    - Ensure you have a `recipes.json` file in the same directory as the executable. This file should contain the recipes data in JSON format.
 * 
 * 3. Run the Program:
 *    - Execute the program by running the following command in your terminal:
 *      dotnet run
 * 
 * 4. Program Execution:
 *    - The program will read the `recipes.json` file, parse the recipes, and upload them to the Redis database.
 *    - It will also set up the necessary indices and vectors for the recipes.
 * 
 * 5. Output:
 *    - After successful execution, the program will output the total number of records upserted.
 * 
 * Example `recipes.json` File:
 * 
 * [
 *   {
 *     "Key": "1",
 *     "Name": "Spaghetti Bolognese",
 *     "Submitted": "2023-01-01T00:00:00Z",
 *     "TotalTimeInMinutes": 45,
 *     "Steps": ["Boil water", "Cook pasta", "Prepare sauce"],
 *     "Description": "A classic Italian pasta dish.",
 *     "Ingredients": ["Pasta", "Tomato sauce", "Ground beef"]
 *   },
 *   {
 *     "Key": "2",
 *     "Name": "Chicken Curry",
 *     "Submitted": "2023-01-02T00:00:00Z",
 *     "TotalTimeInMinutes": 60,
 *     "Steps": ["Cook chicken", "Prepare curry sauce", "Serve with rice"],
 *     "Description": "A spicy and flavorful chicken curry.",
 *     "Ingredients": ["Chicken", "Curry powder", "Coconut milk"]
 *   }
 * ]
 * 
 * Values to Set:
 * - AOAI:KEY: Your Azure OpenAI API key.
 * - AOAI:ENDPOINT: Your Azure OpenAI endpoint.
 * - AOAI:DEPLOYMENT_NAME: Your Azure OpenAI deployment name.
 * - ConnectionStrings:REDIS: Your Redis connection string.
 */


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

    if (!await connectionProvider.Connection.IsIndexCurrentAsync(typeof(Recipe)))
    {
      connectionProvider.Connection.DropIndex(typeof(Recipe));
      connectionProvider.Connection.CreateIndex(typeof(Recipe));
    }

    await recipeService.UploadRecipesAsync(recipes);

    Console.WriteLine($"Total records upserted: {recipes.Count}.");
  }

  /// <summary>
  /// Read from local JSON file and parse into List<Recipe>
  /// </summary>
  /// <returns></returns>
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