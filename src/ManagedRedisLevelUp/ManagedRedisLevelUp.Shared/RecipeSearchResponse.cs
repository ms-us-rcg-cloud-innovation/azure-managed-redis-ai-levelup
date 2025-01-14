namespace ManagedRedisLevelUp.Shared;

public class RecipeSearchResponse
{
  public double? Score { get; set; }
  public string Key { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Ingredients { get; set; } = string.Empty;
  public string Instructions { get; set; } = string.Empty;

  public Recipe AsRecipe()
  {
    return new Recipe
    {
      Key = Key,
      Name = Name,
      Ingredients = Ingredients,
      Instructions = Instructions
    };
  }
}
