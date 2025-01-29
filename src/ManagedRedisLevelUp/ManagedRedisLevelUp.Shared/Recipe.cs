using Microsoft.Extensions.VectorData;
namespace ManagedRedisLevelUp.Shared;

public class Recipe
{
  [VectorStoreRecordKey]
  public string Key { get; set; } = Guid.NewGuid().ToString();

  [VectorStoreRecordData]
  public string Name { get; set; }

  [VectorStoreRecordData]
  public DateTime Submitted { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// The total time to prepare the recipe.
  /// </summary>
  [VectorStoreRecordData]
  public int TotalTimeInMinutes { get; set; }

  [VectorStoreRecordData]
  public List<string> Steps { get; set; } = [];

  [VectorStoreRecordData]
  public string Description { get; set; }

  [VectorStoreRecordData]
  public List<string> Ingredients { get; set; } = [];

  [VectorStoreRecordVector(Dimensions: 1536)]
  public ReadOnlyMemory<float> RecipeEmbedding { get; set; } = new float[1536];

  public string GetEmbeddingString() => 
    $"Steps: {string.Join(',', Steps)}\nDescription: {Description}\nIngredients: {string.Join(',', Ingredients)}";
}
