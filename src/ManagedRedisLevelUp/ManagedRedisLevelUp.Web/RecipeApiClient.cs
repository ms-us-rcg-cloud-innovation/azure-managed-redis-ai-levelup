using ManagedRedisLevelUp.Shared;

namespace ManagedRedisLevelUp.Web;

public class RecipeApiClient(HttpClient httpClient)
{
  public async Task<Recipe[]> GetRecipesAsync(int maxItems = 10, CancellationToken cancellationToken = default)
  {
    List<Recipe>? recipes = null;

    await foreach (var recipe in httpClient.GetFromJsonAsAsyncEnumerable<Recipe>("/recipes", cancellationToken))
    {
      if (recipes?.Count >= maxItems)
      {
        break;
      }
      if (recipe is not null)
      {
        recipes ??= [];
        recipes.Add(recipe);
      }
    }

    return recipes?.ToArray() ?? [];
  }

  public async Task<Recipe> GetRecipeAsync(string key, CancellationToken cancellationToken = default)
  {
    var recipe = await httpClient.GetFromJsonAsync<Recipe>($"/recipes/{key}", cancellationToken);
    return recipe;
  }

  public async Task<string> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
  {
    var response = await httpClient.PostAsJsonAsync("/recipes", recipe, cancellationToken);
    response.EnsureSuccessStatusCode();
    
    return await response.Content.ReadAsStringAsync();
  }

  public async Task<IEnumerable<Recipe>> SearchRecipesAsync(string query, CancellationToken cancellationToken = default)
  {
    var response = await httpClient.GetFromJsonAsync<List<RecipeSearchResponse>>($"/recipes/search/{query}", cancellationToken) ?? [];
    if (response.Count == 0)
    {
      Console.WriteLine("No recipes found");
    }
    var recipeList = response.Select(r => r.AsRecipe());
    return recipeList;
  }
}
