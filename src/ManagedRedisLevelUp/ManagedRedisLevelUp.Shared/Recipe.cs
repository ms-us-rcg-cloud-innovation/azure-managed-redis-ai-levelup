using Redis.OM;
using Redis.OM.Modeling;
using Redis.OM.Vectorizers;

namespace ManagedRedisLevelUp.Shared;

[Document(StorageType = StorageType.Json, Prefixes = ["Recipes"])]
public class Recipe
{
  [RedisIdField]
  public string Key { get; set; } = Guid.NewGuid().ToString();

  public string Name { get; set; }

  [Indexed(Sortable = true)]
  public DateTime Submitted { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// The total time to prepare the recipe.
  /// </summary>
  [Indexed(Sortable = true)]
  public int TotalTimeInMinutes { get; set; }

  public List<string> Steps { get; set; } = [];

  public string Description { get; set; }

  public List<string> Ingredients { get; set; } = [];

  [Indexed(DistanceMetric = DistanceMetric.COSINE, Algorithm = VectorAlgorithm.HNSW)]
  [AzureOpenAIVectorizer("embedding", "openaiptcltlcx23xmo", 1536)]
  public Vector<string> SearchVector { get; set; }

  public void SetVectors()
  {
    var searchString = $"<Name> {Name} <Description> {Description} <Ingredients> {string.Join(" ", Ingredients)}";
    SearchVector = Vector.Of(searchString);
  }
}
