using ManagedRedisLevelUp.Shared;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace ManagedRedisLevelUp.ApiService.Services;

public class RecipeService(
  IConnectionMultiplexer redisConnection,
  RedisConnectionProvider redisOmConnectionProvider,
  ISemanticCache cache)
{
  private IRedisCollection<Recipe> _collection;

  public async Task InitializeAsync()
  {
    // Create the index in Redis if it doesn't exist.
    // Uses annotations on Recipe class to define the index.
    //if (!redisOmConnectionProvider.Connection.IsIndexCurrent(typeof(Recipe)))
    //{
    //  await redisOmConnectionProvider.Connection.DropIndexAsync(typeof(Recipe));
    //  await redisOmConnectionProvider.Connection.CreateIndexAsync(typeof(Recipe));
    //}
    _collection = redisOmConnectionProvider.RedisCollection<Recipe>();
  }

  public async Task<Recipe?> GetRecipeAsync(string keyId)
  {
    return await _collection.FindByIdAsync(keyId);
  }

  public async Task<IEnumerable<Recipe>> GetRecipesAsync(int numOfRecords = 10)
  {
    var recipes = await _collection.Take(10).ToListAsync();
    return recipes;
  }

  public async Task<IEnumerable<Recipe>> SearchRecipesAsync(string searchString)
  {
    var results = await cache.GetSimilarAsync(searchString);
    if (results.Length > 0)
    {
      Console.WriteLine("found similar result in cache");
      List<Recipe> output = [];
      foreach (var result in results)
      {
        var recipe = await GetRecipeAsync(result.Key);
        if (recipe is not null)
          output.Add(recipe);
      }
      return output;
    }

    var searchResults = _collection.Where(r => r.SearchVector.VectorRange(searchString, 0.25)).Take(3);

    return searchResults;
  }

  public async Task UploadRecipesAsync(List<Recipe> recipes)
  {
    foreach (var recipe in recipes)
    {
      recipe.SetVectors();
    }
    await _collection.InsertAsync(recipes);
  }

  public int GetRecipeCount()
  {
    var endpoints = redisConnection.GetEndPoints();
    var keys = redisConnection.GetServer(endpoints.First()).Keys();
    return keys.Count();
  }
}
