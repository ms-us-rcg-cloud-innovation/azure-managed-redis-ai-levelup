using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ManagedRedisLevelUp.ApiService.Models;

public sealed class VectorChatHistory
{
  [VectorStoreRecordKey]
  public string Key { get; set; }

  [VectorStoreRecordData]
  public required string ChatMessage { get; set; }

  [VectorStoreRecordData]
  public required ChatHistory UserChatHistory { get; set; }

  [VectorStoreRecordVector(Dimensions: 1536)]
  public ReadOnlyMemory<float> ChatEmbedding { get; set; }
}