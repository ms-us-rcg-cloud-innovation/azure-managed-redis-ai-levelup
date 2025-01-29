using ManagedRedisLevelUp.Shared;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using StackExchange.Redis;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ManagedRedisLevelUp.ApiService.Services;

internal class RecipeService(
  IVectorStore vectorStore,
  ITextEmbeddingGenerationService textEmbeddingGenerationService,
  IConnectionMultiplexer redisConnection)
{
  public async Task<Recipe> GetRecipeAsync(string collectionName, string keyId)
  {
    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);

    var options = new GetRecordOptions() { IncludeVectors = true };
    var recipe = await collection.GetAsync(keyId, options);
    return recipe;
  }

  public async IAsyncEnumerable<Recipe> GetRecipesAsync(string collectionName, int numOfRecords = 10)
  {
    var endpoints = redisConnection.GetEndPoints();
    var keys = redisConnection.GetServer(endpoints.First()).KeysAsync(pageSize: numOfRecords);

    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);

    int count = 0;
    await foreach (var key in keys)
    {
      if (count >= numOfRecords)
      {
        break;
      }
      var keyString = key.ToString();
      var keyGuid = keyString.Substring(keyString.IndexOf(':') + 1);
      var recipe = await collection.GetAsync(keyGuid);
      if (recipe != null)
      {
        yield return recipe;
      }
    }
  }

  public async IAsyncEnumerable<Recipe> SearchRecipesAsync(string collectionName, string searchString)
  {
    var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
    var vectorSearchOptions = new VectorSearchOptions
    {
      Top = 3,
      IncludeTotalCount = true,
      IncludeVectors = false,
      VectorPropertyName = nameof(Recipe.RecipeEmbedding)
    };

    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);
    var searchResult = await collection.VectorizedSearchAsync(searchVector, vectorSearchOptions, new CancellationToken());

    await foreach (var result in searchResult.Results)
    {
      Console.WriteLine($"Search score: {result.Score}");
      Console.WriteLine(result.Record.Name);
      Console.WriteLine($"Key: {result.Record.Key}");
      Console.WriteLine("=========");

      yield return result.Record;
    }
  }

  public async Task UploadRecipesAsync(string collectionName, List<Recipe> recipes)
  {
    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);

    await collection.CreateCollectionIfNotExistsAsync();

    foreach (var recipe in recipes)
    {
      // Upload the recipe.
      Console.WriteLine($"Upserting recipe: {recipe.Key}");
      await collection.UpsertAsync(recipe);
    }
  }

  public async Task<IEnumerable<Recipe>> GenerateEmbeddingsAsync(IEnumerable<Recipe> recipes)
  {
    foreach (var recipe in recipes)
    {
      // Generate the recipe embedding.
      Console.WriteLine($"Generating embedding for recipe: {recipe.Key}");
      recipe.RecipeEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(recipe.GetEmbeddingString());
    }
    return recipes;
  }

  public int GetRecipeCount()
  {
    var endpoints = redisConnection.GetEndPoints();
    var keys = redisConnection.GetServer(endpoints.First()).Keys();
    return keys.Count();
  }
}
