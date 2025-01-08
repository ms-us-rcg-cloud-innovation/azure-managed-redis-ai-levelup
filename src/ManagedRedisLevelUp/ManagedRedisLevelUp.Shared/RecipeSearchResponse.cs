namespace ManagedRedisLevelUp.Shared;

public class RecipeSearchResponse
{
  public double? Score { get; set; }
  public string Name { get; set; }
  public string Ingredients { get; set; }
  public string Instructions { get; set; }
}
