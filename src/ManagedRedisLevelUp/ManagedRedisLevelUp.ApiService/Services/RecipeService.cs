using ManagedRedisLevelUp.Shared;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ManagedRedisLevelUp.ApiService.Services;

internal class RecipeService(
    IVectorStore vectorStore,
    ITextEmbeddingGenerationService textEmbeddingGenerationService)
{
  public async Task<Recipe> GetRecipeAsync(string collectionName, string keyId)
  {
    //return new Recipe() { Ingredients = collectionName, Instructions = keyId, Key = "Test", Name = "Test" };
    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);

    var options = new GetRecordOptions() { IncludeVectors = true };
    var recipe = await collection.GetAsync(keyId, options);
    return recipe;
  }

  public async Task<List<RecipeSearchResponse>> SearchRecipesAsync(string collectionName, string searchString)
  {
    var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
    var vectorSearchOptions = new VectorSearchOptions
    {
      Top = 3,
      IncludeTotalCount = true,
      IncludeVectors = false,
      VectorPropertyName = nameof(Recipe.RecipeEmbedding)
    };

    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);
    var searchResult = await collection.VectorizedSearchAsync(searchVector, vectorSearchOptions, new CancellationToken());

    List<RecipeSearchResponse> recipeResponses = new List<RecipeSearchResponse>();

    await foreach (var result in searchResult.Results)
    {
      Console.WriteLine($"Search score: {result.Score}");
      Console.WriteLine(result.Record.Name);
      Console.WriteLine($"Key: {result.Record.Key}");
      Console.WriteLine("=========");

      recipeResponses.Add(new RecipeSearchResponse
      {
        Name = result.Record.Name,
        Ingredients = result.Record.Ingredients,
        Instructions = result.Record.Instructions,
        Score = result.Score
      });
    }

    return recipeResponses;
  }

  public async Task GenerateEmbeddingsAndUpload(string collectionName, List<Recipe> recipes)
  {
    var collection = vectorStore.GetCollection<string, Recipe>(collectionName);

    await collection.CreateCollectionIfNotExistsAsync();

    foreach (var recipe in recipes)
    {
      // Generate the recipe embedding.
      Console.WriteLine($"Generating embedding for recipe: {recipe.Key}");
      recipe.RecipeEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync($"{recipe.Name} {recipe.Ingredients} {recipe.Instructions}");
      TimeSpan ttl = TimeSpan.FromMinutes(30);

      // Upload the recipe.
      Console.WriteLine($"Upserting recipe: {recipe.Key}");
      await collection.UpsertAsync(recipe);
    }
  }
}
