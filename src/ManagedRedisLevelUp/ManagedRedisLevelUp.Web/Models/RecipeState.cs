using ManagedRedisLevelUp.Shared;

namespace ManagedRedisLevelUp.Web.Models
{
  public class RecipeState
  {
    public Recipe CurrentRecipe { get; set; } = new();
    public string RecipeKey { get; set; } = string.Empty;
    public string SearchString { get; set; } = string.Empty;
    public string SearchApproach { get; set; } = "VectorRange";
    public List<RecipeSearchResponse> RecipeList { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;

    public bool IsEditing => RecipeList.Any(r => r.Key == CurrentRecipe.Key);

    public void Reset(bool preserveSearchTerms = false)
    {
      CurrentRecipe = new();

      if (!preserveSearchTerms)
      {
        SearchString = string.Empty;
        // Don't reset SearchApproach to keep the selected option
      }

      ErrorMessage = string.Empty;
    }

  }
}
