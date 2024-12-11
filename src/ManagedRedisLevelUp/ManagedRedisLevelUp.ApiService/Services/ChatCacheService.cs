using ManagedRedisLevelUp.ApiService.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 
namespace ManagedRedisLevelUp.ApiService.Services;

internal class ChatCacheService(
    IVectorStore vectorStore,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChatCompletionService chatCompletionService)
{
  private readonly IVectorStoreRecordCollection<string, ChatCache> _chatCollection;
  internal async Task<ChatCacheResponse> CheckCache(string chatMessage)
  {
    //1. Vectorize the chat message
    var vectorChatMessage = await textEmbeddingGenerationService.GenerateEmbeddingAsync(chatMessage);


    //2. Search the vector store for the closest match (semantic cachce)
    string collectionName = "chatcache";
    var _chatCollection = vectorStore.GetCollection<string, ChatCache>(collectionName);
    await _chatCollection.CreateCollectionIfNotExistsAsync();

    var vectorSearchOptions = new VectorSearchOptions
    {
      Top = 1,
      IncludeVectors = false,
      VectorPropertyName = nameof(ChatCache.ChatEmbedding),
      IncludeTotalCount = true
    };

    var searchResult = await _chatCollection.VectorizedSearchAsync(vectorChatMessage, vectorSearchOptions);

    await foreach (var result in searchResult.Results)
    {
      var score = result.Score;
      return new ChatCacheResponse
      {
        IsCacheHit = true,
        AssistantMessage = result.Record.AssistantMessage,
        UserMessage = chatMessage
      };
    }


    //3. If a match is found, return the response
    //if (searchResult.TotalCount != null && searchResult.TotalCount == 1)
    //{
    //    var record = searchResult.Results.GetAsyncEnumerator().Current.Record;


    //    //return the response
    //    return new ChatResponse
    //    {
    //        SessionId = "null",
    //        AssistantMessage = record.ChatMessage,
    //        UserMessage = chatMessage
    //    };
    //}


    //4. If no match is found, call LLM 
    var resp = await chatCompletionService.GetChatMessageContentAsync(chatMessage);


    //5. Save the chat message to the vector store async no wait
    await _chatCollection.UpsertAsync(
        new ChatCache()
        {
          Key = Guid.NewGuid().ToString(),
          ChatMessage = chatMessage,
          AssistantMessage = resp.Content,
          ChatEmbedding = vectorChatMessage
        });

    //6. Return the response
    return new ChatCacheResponse
    {
      IsCacheHit = false,
      AssistantMessage = resp.Content,
      UserMessage = chatMessage
    };

  }
}