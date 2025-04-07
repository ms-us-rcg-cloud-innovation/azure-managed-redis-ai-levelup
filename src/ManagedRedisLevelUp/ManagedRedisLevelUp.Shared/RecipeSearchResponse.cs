namespace ManagedRedisLevelUp.Shared;

public class RecipeSearchResponse
{
  public double? Score { get; set; }
  public string Key { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public DateTime Submitted { get; set; }
  public int TotalTimeInMinutes { get; set; }
  public List<string> Steps { get; set; } = [];
  public string Description { get; set; } = string.Empty;
  public List<string> Ingredients { get; set; } = [];
  public bool IsCacheHit { get; set; } = false;

  public Recipe AsRecipe()
  {
    var recipe = new Recipe
    {
      Key = Key,
      Name = Name,
      Ingredients = Ingredients,
      Description = Description,
      Steps = Steps,
      Submitted = Submitted,
      TotalTimeInMinutes = TotalTimeInMinutes
    };
    recipe.SetVectors();
    return recipe;
  }

  public RecipeSearchResponse() { }

  public RecipeSearchResponse(Recipe recipe, bool isFromCache = false) 
  {
    Key = recipe.Key;
    Name = recipe.Name;
    Ingredients = recipe.Ingredients;
    Description = recipe.Description;
    Steps = recipe.Steps;
    Submitted = recipe.Submitted;
    TotalTimeInMinutes = recipe.TotalTimeInMinutes;
    IsCacheHit = isFromCache;
  }
}
