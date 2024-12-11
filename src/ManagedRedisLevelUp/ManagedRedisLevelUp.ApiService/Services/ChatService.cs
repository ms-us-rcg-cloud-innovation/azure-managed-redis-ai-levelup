using ManagedRedisLevelUp.ApiService.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace ManagedRedisLevelUp.ApiService.Services;

internal class ChatService(
    IVectorStore vectorStore,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChatCompletionService chatCompletionService)
{
  private IVectorStoreRecordCollection<string, VectorChatHistory> _chatCollection;
  private ChatHistory _chatHistory = [];

  private async Task<VectorChatHistory> GetChatHistoryAsync(string sessionId)
  {
    //TODO: Configure the number of history records to keep/age
    Console.WriteLine($"Looking up Chat History in Vector Store...");
    var _chatCollection = vectorStore.GetCollection<string, VectorChatHistory>("chat");

    var options = new GetRecordOptions() { IncludeVectors = true };
    var vectorChatHistory = await _chatCollection.GetAsync(sessionId, options);
    if (vectorChatHistory == null)
    {
      Console.WriteLine("No chat history found. Creating new chat history.");
      vectorChatHistory = new VectorChatHistory
      {
        Key = Guid.NewGuid().ToString(),
        ChatMessage = "New Chat History",
        UserChatHistory = new ChatHistory(),
        ChatEmbedding = new ReadOnlyMemory<float>(new float[1536])
      };
      vectorChatHistory.UserChatHistory.AddSystemMessage("You are a totally rad and hip dude from California who loves ninja turtles, pizza, and surfing!");
    }
    else
    {
      Console.WriteLine($"Chat history found. Chat Session Key: {vectorChatHistory.Key}");
    }
    return vectorChatHistory;
  }


  public async Task<ChatResponse> SaveChatToMemory(string chatMessage, string session)
  {
    var history = await GetChatHistoryAsync(session);

    history.UserChatHistory.AddUserMessage(chatMessage);
    Console.WriteLine($"Chat History is: {_chatHistory.Count}");

    var resp = await chatCompletionService.GetChatMessageContentAsync(history.UserChatHistory);
    history.UserChatHistory.AddAssistantMessage(resp.Content);
    history.ChatEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(chatMessage);
    history.ChatMessage = chatMessage;
    Console.WriteLine("Vectorized chat message.");

    var _chatCollection = vectorStore.GetCollection<string, VectorChatHistory>("chat");
    await _chatCollection.CreateCollectionIfNotExistsAsync();
    await _chatCollection.UpsertAsync(history);
    Console.WriteLine($"Saved to the {_chatCollection.CollectionName} collection.");

    return new ChatResponse
    {
      SessionId = history.Key,
      AssistantMessage = resp.Content,
      UserMessage = chatMessage
    };
  }


}