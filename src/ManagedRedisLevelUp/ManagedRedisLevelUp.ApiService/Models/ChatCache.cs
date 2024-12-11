using Microsoft.Extensions.VectorData;

namespace ManagedRedisLevelUp.ApiService.Models;

public class ChatCache
{
  [VectorStoreRecordKey]
  public string Key { get; set; }

  [VectorStoreRecordData]
  public required string ChatMessage { get; set; }

  [VectorStoreRecordData]
  public required string AssistantMessage { get; set; }

  [VectorStoreRecordVector(Dimensions: 1536, DistanceFunction: DistanceFunction.CosineDistance, IndexKind: IndexKind.Hnsw)]
  public ReadOnlyMemory<float> ChatEmbedding { get; set; }
}