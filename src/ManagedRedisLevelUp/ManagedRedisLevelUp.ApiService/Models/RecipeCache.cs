using ManagedRedisLevelUp.Shared;
using Redis.OM;
using Redis.OM.Modeling;
using Redis.OM.Vectorizers;

namespace ManagedRedisLevelUp.ApiService.Models;

[Document(StorageType = StorageType.Json, Prefixes = ["RecipesCache"])]
public class RecipeCache
{
  [RedisIdField]
  public string Key { get; set; } = Guid.NewGuid().ToString();

  public List<Recipe> Recipes { get; set; } = [];

  [Indexed(DistanceMetric = DistanceMetric.COSINE, Algorithm = VectorAlgorithm.HNSW)]
  [AzureOpenAIVectorizer("embedding", "openAiptcltlcx23xmo", 1536)]
  public Vector<string> SearchVector { get; set; }

  public void SetVectors()
  {
    var searchString = string.Join("||", Recipes.Select(r => r.SearchVector));
    SearchVector = Vector.Of(searchString);
  }
}