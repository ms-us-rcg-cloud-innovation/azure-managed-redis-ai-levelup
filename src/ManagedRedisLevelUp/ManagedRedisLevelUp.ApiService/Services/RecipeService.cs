using ManagedRedisLevelUp.Shared;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using StackExchange.Redis;
using System.Text.Json;

namespace ManagedRedisLevelUp.ApiService.Services;

public class RecipeService(
  IConnectionMultiplexer redisConnection,
  RedisConnectionProvider redisOmConnectionProvider,
  ISemanticCache cache)
{
  private IRedisCollection<Recipe> _collection;

  public async Task InitializeAsync()
  {
    _collection = redisOmConnectionProvider.RedisCollection<Recipe>();
  }

  /// <summary>
  /// Get a recipe by its key id.
  /// </summary>
  /// <param name="keyId">The unique identifier of the recipe.</param>
  public async Task<RecipeSearchResponse?> GetRecipeAsync(string keyId)
  {
    var recipe = await _collection.FindByIdAsync(keyId);
    if (recipe is not null)
    {
      return new RecipeSearchResponse(recipe);
    }
    return null;
  }


  /// <summary>
  /// Get a list of recipes.
  /// </summary>
  /// <param name="numOfRecords">The number of records to return.</param>
  public async Task<IEnumerable<RecipeSearchResponse>> GetRecipesAsync(int numOfRecords = 10)
  {
    var recipes = await _collection.Take(numOfRecords).ToListAsync();
    var recipeList = recipes.Select(r => new RecipeSearchResponse(r));
    return recipeList;
  }

  /// <summary>
  /// Search for recipes.
  /// </summary>
  /// <param name="searchString">The natural language search string.</param>
  /// <param name="approach">The approach to use for searching. Allowed values are "VectorRange" and "NearestNeighbors".</param>
  public async Task<IEnumerable<RecipeSearchResponse>> SearchRecipesAsync(string searchString, string? approach = "VectorRange")
  {
    var results = await cache.GetSimilarAsync(searchString);
    if (results.Length > 0)
    {
      Console.WriteLine("found similar result in cache");
      List<RecipeSearchResponse> output = [];
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

    Console.WriteLine("found no similar result in cache");

    return await GetSearchResultsAsync(searchString, approach);
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

    await UpdateSearchCacheAsync(approach, searchString, searchResultList);

    var recipeList = searchResultList.Select(r => new RecipeSearchResponse(r));
    return recipeList;
  }

  private async Task UpdateSearchCacheAsync(string approach, string searchString, IEnumerable<Recipe> searchResults)
  {
    var response = JsonSerializer.Serialize(searchResults);
    await cache.StoreAsync($"{searchString}|{approach}", response);    
  }

  /// <summary>
  /// Upload a list of recipes to the database.
  /// </summary>
  /// <param name="recipes">A <see cref="List{T}"/> of <see cref="Recipe"/>s to upload</param>
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
  public int GetRecipeCount()
  {
    var endpoints = redisConnection.GetEndPoints();
    var keys = redisConnection.GetServer(endpoints.First()).Keys();
    return keys.Count();
  }
}
