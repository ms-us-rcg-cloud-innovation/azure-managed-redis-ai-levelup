namespace ManagedRedisLevelUp.ApiService.Models;

internal class ChatResponse
{
  public string? UserMessage { get; set; }
  public string? AssistantMessage { get; set; }
  public string? SessionId { get; set; }
}