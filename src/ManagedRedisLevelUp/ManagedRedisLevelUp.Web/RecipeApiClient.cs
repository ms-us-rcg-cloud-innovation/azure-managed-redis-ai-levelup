using ManagedRedisLevelUp.Shared;
using System.Text.Json;

namespace ManagedRedisLevelUp.Web;

public class RecipeApiClient(HttpClient httpClient)
{
  public async Task<RecipeSearchResponse[]> GetRecipesAsync(
    int maxItems = 10,
    CancellationToken cancellationToken = default)
  {
    List<RecipeSearchResponse>? recipes = null;

    await foreach (var recipe in httpClient.GetFromJsonAsAsyncEnumerable<RecipeSearchResponse>("/recipes", cancellationToken))
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

  public async Task<RecipeSearchResponse?> GetRecipeAsync(
    string key,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var recipe = await httpClient.GetFromJsonAsync<RecipeSearchResponse>($"/recipes/{key}", cancellationToken);
      return recipe;
    }
    catch (JsonException _)
    {
      Console.WriteLine("Recipe not found");
      return null;
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message, e);
      return null;
    }
  }

  public async Task<string> CreateRecipeAsync(
    Recipe recipe,                                              
    CancellationToken cancellationToken = default)
  {
    var response = await httpClient.PostAsJsonAsync("/recipes", recipe, cancellationToken);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync(cancellationToken);
  }

  public async Task<IEnumerable<RecipeSearchResponse>> SearchRecipesAsync(
    string query,
    string approach,
    CancellationToken cancellationToken = default)
  {
    var response = await httpClient.GetFromJsonAsync<List<RecipeSearchResponse>>($"/recipes/search/{query}?approach={approach}", cancellationToken) ?? [];
    if (response.Count == 0)
    {
      Console.WriteLine("No recipes found");
    }
    return response;
  }
}
