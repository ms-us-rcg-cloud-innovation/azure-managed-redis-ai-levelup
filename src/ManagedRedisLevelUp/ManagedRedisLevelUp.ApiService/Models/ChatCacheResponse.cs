namespace ManagedRedisLevelUp.ApiService.Models;

internal class ChatCacheResponse
{
  public string? UserMessage { get; set; }
  public string? AssistantMessage { get; set; }
  public bool IsCacheHit { get; set; } = false;
}