using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Profiling;

public class WorldStreamer : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject chunkPrefab;

    [Header("Streaming Settings")]
    public int viewDistance = 2;
    public int chunkSize = 100;
    public int maxChunksPerFrame = 2;
    public float streamingUpdateInterval = 0.1f;

    [Header("Performance Monitoring")]
    [SerializeField] private int activeChunkCount;
    [SerializeField] private float memoryUsageMB;

    private Dictionary<Vector2Int, ChunkData> chunkPool = new Dictionary<Vector2Int, ChunkData>();
    private Queue<Vector2Int> chunkLoadQueue = new Queue<Vector2Int>();
    private HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunk;
    private Coroutine streamingCoroutine;

    private class ChunkData
    {
        public GameObject gameObject;
        public bool isActive;
        public Vector2Int coordinate;
    }

    void Start()
    {
        if (player == null)
            player = Camera.main?.transform;

        streamingCoroutine = StartCoroutine(StreamingUpdate());
    }

    void OnDestroy()
    {
        if (streamingCoroutine != null)
            StopCoroutine(streamingCoroutine);
    }

    void Update()
    {
        UpdatePerformanceMetrics();
    }

    IEnumerator StreamingUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(streamingUpdateInterval);

            if (player == null || chunkPrefab == null) continue;

            Vector2Int playerChunk = WorldToChunk(player.position);
            if (playerChunk != currentPlayerChunk)
            {
                currentPlayerChunk = playerChunk;
                yield return StartCoroutine(ProcessChunkUpdates());
            }
        }
    }

    IEnumerator ProcessChunkUpdates()
    {
        Profiler.BeginSample("CalculateNeededChunks");
        CalculateNeededChunks();
        Profiler.EndSample();

        Profiler.BeginSample("UnloadDistantChunks");
        UnloadDistantChunks();
        Profiler.EndSample();

        Profiler.BeginSample("LoadNewChunks");
        yield return StartCoroutine(LoadQueuedChunks());
        Profiler.EndSample();
    }

    void CalculateNeededChunks()
    {
        neededChunks.Clear();
        chunkLoadQueue.Clear();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                Vector2Int coord = currentPlayerChunk + new Vector2Int(x, y);
                neededChunks.Add(coord);

                if (!chunkPool.ContainsKey(coord) || !chunkPool[coord].isActive)
                {
                    chunkLoadQueue.Enqueue(coord);
                }
            }
        }
    }

    void UnloadDistantChunks()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (var kvp in chunkPool)
        {
            if (!neededChunks.Contains(kvp.Key) && kvp.Value.isActive)
            {
                kvp.Value.gameObject.SetActive(false);
                kvp.Value.isActive = false;

                // Only destroy if we want to completely remove from memory
                // For object pooling, we keep them disabled instead
            }

            // Optional: Clean up chunks that are too far away completely
            if (Vector2Int.Distance(kvp.Key, currentPlayerChunk) > viewDistance * 2)
            {
                if (chunkPool.TryGetValue(kvp.Key, out ChunkData chunk))
                {
                    Destroy(chunk.gameObject);
                    chunksToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var coord in chunksToRemove)
        {
            chunkPool.Remove(coord);
        }
    }

    IEnumerator LoadQueuedChunks()
    {
        int chunksProcessed = 0;

        while (chunkLoadQueue.Count > 0 && chunksProcessed < maxChunksPerFrame)
        {
            Vector2Int coord = chunkLoadQueue.Dequeue();

            if (chunkPool.TryGetValue(coord, out ChunkData existingChunk))
            {
                // Reactivate existing chunk
                existingChunk.gameObject.SetActive(true);
                existingChunk.isActive = true;
            }
            else
            {
                // Create new chunk
                Vector3 worldPos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
                GameObject chunkObj = Instantiate(chunkPrefab, worldPos, Quaternion.identity);
                chunkObj.name = $"Chunk_{coord.x}_{coord.y}";

                ChunkData newChunk = new ChunkData
                {
                    gameObject = chunkObj,
                    isActive = true,
                    coordinate = coord
                };

                chunkPool.Add(coord, newChunk);
            }

            chunksProcessed++;

            if (chunksProcessed >= maxChunksPerFrame)
                yield return null;
        }
    }

    Vector2Int WorldToChunk(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int y = Mathf.FloorToInt(pos.z / chunkSize);
        return new Vector2Int(x, y);
    }

    void UpdatePerformanceMetrics()
    {
        activeChunkCount = 0;
        foreach (var chunk in chunkPool.Values)
        {
            if (chunk.isActive) activeChunkCount++;
        }

        // Calculate approximate memory usage
        memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;
    }

    // Public methods for external access
    public bool IsChunkActive(Vector2Int coord)
    {
        return chunkPool.ContainsKey(coord) && chunkPool[coord].isActive;
    }

    public int GetActiveChunkCount()
    {
        return activeChunkCount;
    }

    public IEnumerable<Vector2Int> GetActiveChunkCoordinates()
    {
        foreach (var kvp in chunkPool)
        {
            if (kvp.Value.isActive)
                yield return kvp.Key;
        }
    }

    // Editor visualization
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        foreach (var coord in neededChunks)
        {
            Vector3 center = new Vector3(coord.x * chunkSize + chunkSize / 2f, 0, coord.y * chunkSize + chunkSize / 2f);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 0.1f, chunkSize));
        }

        Gizmos.color = Color.red;
        foreach (var kvp in chunkPool)
        {
            if (!kvp.Value.isActive) continue;
            Vector3 center = new Vector3(kvp.Key.x * chunkSize + chunkSize / 2f, 0, kvp.Key.y * chunkSize + chunkSize / 2f);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 0.1f, chunkSize));
        }
    }
}