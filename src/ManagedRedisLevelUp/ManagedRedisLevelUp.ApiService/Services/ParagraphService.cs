using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Redis;
using ManagedRedisLevelUp.ApiService.Models;
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace ManagedRedisLevelUp.ApiService.Services;

internal class ParagraphService(
    IVectorStore vectorStore,
    ITextEmbeddingGenerationService textEmbeddingGenerationService)
{
  public async Task<TextParagraph> GetTextParagraphAsync(string collectionName, string keyId)
  {
    var collection = vectorStore.GetCollection<string, TextParagraph>(collectionName);

    var options = new GetRecordOptions() { IncludeVectors = true };
    var paragraph = await collection.GetAsync(keyId, options);
    return paragraph;
  }

  public async Task<List<ParagraphSearchResponse>> SearchParagraphsAsync(string collectionName, string searchString)
  {
    var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
    var vectorSearchOptions = new VectorSearchOptions
    {
      Top = 3,
      IncludeTotalCount = true,
      IncludeVectors = false,
      VectorPropertyName = nameof(TextParagraph.TextEmbedding)
    };

    var collection = vectorStore.GetCollection<string, TextParagraph>(collectionName);
    var searchResult = await collection.VectorizedSearchAsync(searchVector, vectorSearchOptions, new CancellationToken());

    List<ParagraphSearchResponse> paragraphResponses = new List<ParagraphSearchResponse>();

    var f = searchResult.Results.GetAsyncEnumerator();
    var a = f.Current.Score;

    await foreach (var result in searchResult.Results)
    {
      Console.WriteLine($"Search score: {result.Score}");
      Console.WriteLine(result.Record.Text);
      Console.WriteLine($"Key: {result.Record.Key}");
      Console.WriteLine("=========");

      paragraphResponses.Add(new ParagraphSearchResponse
      {
        Text = result.Record.Text,
        Score = result.Score
      });
    }

    return paragraphResponses;
  }

  public async Task GenerateEmbeddingsAndUpload(string collectionName, List<TextParagraph> textParagraphs)
  {
    var collection = vectorStore.GetCollection<string, TextParagraph>(collectionName);

    await collection.CreateCollectionIfNotExistsAsync();

    foreach (var paragraph in textParagraphs)
    {
      // Generate the text embedding.
      Console.WriteLine($"Generating embedding for paragraph: {paragraph.ParagraphId}");
      paragraph.TextEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(paragraph.Text);
      TimeSpan ttl = TimeSpan.FromMinutes(30);

      // Upload the text paragraph.
      Console.WriteLine($"Upserting paragraph: {paragraph.ParagraphId}");
      await collection.UpsertAsync(paragraph);

      Console.WriteLine();
    }
  }
}