using ManagedRedisLevelUp.Shared;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;
using System.Text.Json;

namespace ManagedRedisLevelUp.ApiService.Services;

public class RecipeService
{
  private readonly IConnectionMultiplexer _redisConnection;
  private readonly RedisConnectionProvider _redisOmConnectionProvider;
  private readonly ISemanticCache _cache;
  private IRedisCollection<Recipe> _collection;

  public RecipeService(
      IConnectionMultiplexer redisConnection,
      RedisConnectionProvider redisOmConnectionProvider,
      ISemanticCache cache)
  {
    _redisConnection = redisConnection;
    _redisOmConnectionProvider = redisOmConnectionProvider;
    _cache = cache;
  }

  public void Initialize()
  {
    _collection = _redisOmConnectionProvider.RedisCollection<Recipe>();
  }

  /// <summary>
  /// Get a recipe by its key id.
  /// </summary>
  /// <param name="keyId">The unique identifier of the recipe.</param>
  /// <returns>A <see cref="RecipeSearchResponse"/> if found, otherwise null.</returns>
  public async Task<RecipeSearchResponse?> GetRecipeAsync(string keyId)
  {
    var recipe = await _collection.FindByIdAsync(keyId);
    return recipe is not null ? new RecipeSearchResponse(recipe) : null;
  }

  /// <summary>
  /// Get a list of recipes.
  /// </summary>
  /// <param name="numOfRecords">The number of records to return.</param>
  /// <returns>A list of <see cref="RecipeSearchResponse"/>.</returns>
  public async Task<IEnumerable<RecipeSearchResponse>> GetRecipesAsync(int numOfRecords = 10)
  {
    var recipes = await _collection.Take(numOfRecords).ToListAsync();
    return recipes.Select(r => new RecipeSearchResponse(r));
  }

  /// <summary>
  /// Search for recipes.
  /// </summary>
  /// <param name="searchString">The natural language search string.</param>
  /// <param name="approach">The approach to use for searching. Allowed values are "VectorRange" and "NearestNeighbors".</param>
  /// <returns>A list of <see cref="RecipeSearchResponse"/>.</returns>
  public async Task<IEnumerable<RecipeSearchResponse>> SearchRecipesAsync(string searchString, string? approach = "VectorRange")
  {
    // Confirm that 'approach' is an allowed value (VectorRange or NearestNeighbors)
    var allowedApproaches = new[] { "VectorRange", "NearestNeighbors" };
    if (!allowedApproaches.Contains(approach))
    {
      throw new ArgumentException($"Invalid approach value. Allowed values are: {string.Join(", ", allowedApproaches)}");
    }

    var results = await _cache.GetSimilarAsync(searchString);
    if (results.Length > 0)
    {
      Console.WriteLine("Found similar result in cache");
      return ProcessCachedResults(results);
    }

    Console.WriteLine("Found no similar result in cache");
    return await GetSearchResultsAsync(searchString, approach);
  }

  /// <summary>
  /// Upload a list of recipes to the database.
  /// </summary>
  /// <param name="recipes">A <see cref="List{T}"/> of <see cref="Recipe"/>s to upload.</param>
  public async Task UploadRecipesAsync(List<Recipe> recipes)
  {
    foreach (var recipe in recipes)
    {
      recipe.SetVectors();
    }
    await _collection.InsertAsync(recipes);
  }

  /// <summary>
  /// Get the count of recipes in the database.
  /// </summary>
  /// <returns>The count of recipes.</returns>
  public int GetRecipeCount()
  {
    var endpoints = _redisConnection.GetEndPoints();
    var keys = _redisConnection.GetServer(endpoints.First()).Keys();
    return keys.Count();
  }

  private async Task<IEnumerable<RecipeSearchResponse>> GetSearchResultsAsync(string searchString, string? approach)
  {
    IList<Recipe> searchResultList = [];

    if (approach == "VectorRange")
    {
      var searchResults = _collection.Where(r => r.SearchVector.VectorRange(searchString, 0.25)).Take(3);
      searchResultList = await searchResults.ToListAsync();
    }
    else // approach == "NearestNeighbors"
    {
      var searchResults = _collection.NearestNeighbors(r => r.SearchVector, 3, searchString);
      searchResultList = await searchResults.ToListAsync();
    }

    await UpdateSearchCacheAsync(approach ?? "VectorRange", searchString, searchResultList);

    return searchResultList.Select(r => new RecipeSearchResponse(r));
  }

  private async Task UpdateSearchCacheAsync(string approach, string searchString, IEnumerable<Recipe> searchResults)
  {
    var response = JsonSerializer.Serialize(searchResults);
    await _cache.StoreAsync($"{searchString}|{approach}", response);
  }

  private static List<RecipeSearchResponse> ProcessCachedResults(SemanticCacheResponse[] results)
  {
    var output = new List<RecipeSearchResponse>();
    foreach (var result in results)
    {
      if (result.Response is not null)
      {
        var recipes = JsonSerializer.Deserialize<List<RecipeSearchResponse>>(result.Response);
        recipes?.ForEach(r => r.IsCacheHit = true);
        output.AddRange(recipes!);
      }
    }
    return output;
  }
}
