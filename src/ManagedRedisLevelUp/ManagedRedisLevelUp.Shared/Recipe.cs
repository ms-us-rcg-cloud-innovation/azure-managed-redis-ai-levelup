using Microsoft.Extensions.VectorData;
namespace ManagedRedisLevelUp.Shared;

public class Recipe
{
  /// <summary>A unique key for the recipe.</summary>
  [VectorStoreRecordKey]
  public required string Key { get; set; } = Guid.NewGuid().ToString();

  /// <summary>The name of the recipe.</summary>
  [VectorStoreRecordData]
  public required string Name { get; set; } = string.Empty;

  /// <summary>The ingredients of the recipe.</summary>
  [VectorStoreRecordData]
  public required string Ingredients { get; set; } = string.Empty;

  /// <summary>The instructions for the recipe.</summary>
  [VectorStoreRecordData]
  public required string Instructions { get; set; } = string.Empty;

  /// <summary>The embedding generated from the recipe text.</summary>
  [VectorStoreRecordVector(Dimensions: 1536)]
  public ReadOnlyMemory<float> RecipeEmbedding { get; set; } = new float[1536];
}

