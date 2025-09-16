using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Profiling;
using System;

public class WorldStreamer : MonoBehaviour
{
    [Header("Streaming Settings")]
    public int viewDistance = 2;
    public int chunkSize = 100;
    public int maxChunksPerFrame = 2;
    public float streamingUpdateInterval = 0.1f;
    public Material defaultChunkMaterial;

    [Header("Performance Settings")]
    public bool useAsyncLoading = true;
    public int preWarmPoolSize = 20;
    public bool enableColliders = true;

    [Header("Performance Monitoring")]
    [SerializeField] private int activeChunkCount;
    [SerializeField] private float memoryUsageMB;
    [SerializeField] private int totalChunksInPool;
    [SerializeField] private int chunksLoading;

    // Simplified chunk data structure
    private class ChunkData
    {
        public GameObject gameObject;
        public Vector2Int coordinate;
        public BoxCollider boxCollider;
        public bool isActive;
    }

    // Object pooling with better memory management
    private Dictionary<Vector2Int, ChunkData> activeChunks = new Dictionary<Vector2Int, ChunkData>(64);
    private Queue<ChunkData> chunkPool = new Queue<ChunkData>();
    private HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunk;
    private Coroutine streamingCoroutine;

    // Shared mesh instance
    private Mesh sharedChunkMesh;
    private bool isInitialized = false;

    // Performance optimization
    private WaitForSeconds streamingWait;
    private int squaredViewDist;
    private List<Vector2Int> chunksToRemove = new List<Vector2Int>(32);

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        // Pre-calculate values
        squaredViewDist = viewDistance * viewDistance;
        streamingWait = new WaitForSeconds(streamingUpdateInterval);

        // Create shared material if none provided
        if (defaultChunkMaterial == null)
        {
            defaultChunkMaterial = new Material(Shader.Find("Standard"));
            defaultChunkMaterial.color = new Color(0.4f, 0.6f, 0.4f);
            defaultChunkMaterial.enableInstancing = true;
        }

        // Create shared mesh only once
        sharedChunkMesh = CreateOptimizedChunkMesh();
        sharedChunkMesh.name = "SharedChunkMesh";

        // Pre-warm object pool
        PreWarmChunkPool(preWarmPoolSize);

        streamingCoroutine = StartCoroutine(StreamingUpdate());
        isInitialized = true;

        Debug.Log($"WorldStreamer initialized with pool size: {preWarmPoolSize}");
    }

    void OnDestroy()
    {
        if (streamingCoroutine != null)
            StopCoroutine(streamingCoroutine);

        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    void Cleanup()
    {
        // Clear active chunks
        foreach (var chunk in activeChunks.Values)
        {
            ReturnChunkToPool(chunk);
        }
        activeChunks.Clear();

        // Clear pool
        while (chunkPool.Count > 0)
        {
            var chunk = chunkPool.Dequeue();
            DestroyChunkObject(chunk);
        }

        // Clean up shared mesh
        if (sharedChunkMesh != null)
        {
            DestroyImmediate(sharedChunkMesh);
            sharedChunkMesh = null;
        }

        chunksToRemove.Clear();
        neededChunks.Clear();

        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    void DestroyChunkObject(ChunkData chunk)
    {
        if (chunk != null && chunk.gameObject != null)
        {
            if (Application.isPlaying)
                Destroy(chunk.gameObject);
            else
                DestroyImmediate(chunk.gameObject);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        UpdatePerformanceMetrics();
    }

    IEnumerator StreamingUpdate()
    {
        while (true)
        {
            yield return streamingWait;

            Vector2Int playerChunk = WorldToChunk(transform.position);
            if (playerChunk != currentPlayerChunk || activeChunks.Count == 0)
            {
                currentPlayerChunk = playerChunk;
                yield return StartCoroutine(ProcessChunkUpdates());
            }
        }
    }

    IEnumerator ProcessChunkUpdates()
    {
        CalculateNeededChunks();
        UnloadUnneededChunks();
        yield return StartCoroutine(LoadNeededChunks());
    }

    void CalculateNeededChunks()
    {
        neededChunks.Clear();

        int centerX = currentPlayerChunk.x;
        int centerY = currentPlayerChunk.y;

        // Calculate needed chunks with bounds checking
        for (int x = centerX - viewDistance; x <= centerX + viewDistance; x++)
        {
            for (int y = centerY - viewDistance; y <= centerY + viewDistance; y++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= squaredViewDist)
                {
                    neededChunks.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    void UnloadUnneededChunks()
    {
        chunksToRemove.Clear();

        foreach (var kvp in activeChunks)
        {
            if (!neededChunks.Contains(kvp.Key))
            {
                chunksToRemove.Add(kvp.Key);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            ReturnChunkToPool(activeChunks[coord]);
            activeChunks.Remove(coord);
        }
    }

    IEnumerator LoadNeededChunks()
    {
        int chunksProcessed = 0;

        foreach (var coord in neededChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                if (useAsyncLoading)
                {
                    StartCoroutine(LoadChunkAsync(coord));
                }
                else
                {
                    LoadChunkImmediate(coord);
                }

                chunksProcessed++;
                chunksLoading++;

                if (chunksProcessed >= maxChunksPerFrame)
                {
                    yield return null;
                    chunksProcessed = 0;
                }
            }
        }
    }

    IEnumerator LoadChunkAsync(Vector2Int coord)
    {
        yield return null;

        ChunkData chunk = GetChunkFromPool();
        if (chunk != null)
        {
            SetupChunk(chunk, coord);
            activeChunks.Add(coord, chunk);
            chunksLoading--;
        }
    }

    void LoadChunkImmediate(Vector2Int coord)
    {
        ChunkData chunk = GetChunkFromPool();
        if (chunk != null)
        {
            SetupChunk(chunk, coord);
            activeChunks.Add(coord, chunk);
            chunksLoading--;
        }
    }

    void PreWarmChunkPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ChunkData chunk = CreateNewChunk();
            if (chunk != null)
            {
                chunk.gameObject.SetActive(false);
                chunkPool.Enqueue(chunk);
            }
        }
    }

    ChunkData GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            ChunkData chunk = chunkPool.Dequeue();
            if (chunk != null && chunk.gameObject != null)
            {
                chunk.gameObject.SetActive(true);
                chunk.isActive = true;
                return chunk;
            }
        }

        // Create new chunk if pool is empty
        return CreateNewChunk();
    }

    void ReturnChunkToPool(ChunkData chunk)
    {
        if (chunk == null || chunk.gameObject == null) return;

        chunk.gameObject.SetActive(false);
        chunk.isActive = false;
        chunkPool.Enqueue(chunk);
    }

    ChunkData CreateNewChunk()
    {
        try
        {
            GameObject chunkObj = new GameObject("Chunk");
            chunkObj.transform.SetParent(transform);
            chunkObj.layer = gameObject.layer;

            MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkObj.AddComponent<MeshRenderer>();

            meshRenderer.material = defaultChunkMaterial;
            meshFilter.sharedMesh = sharedChunkMesh; // Use shared mesh

            BoxCollider boxCollider = null;
            if (enableColliders)
            {
                boxCollider = chunkObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(chunkSize, 0.1f, chunkSize);
                boxCollider.center = new Vector3(chunkSize / 2f, 0, chunkSize / 2f);
            }

            return new ChunkData
            {
                gameObject = chunkObj,
                coordinate = Vector2Int.zero,
                boxCollider = boxCollider,
                isActive = false
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create chunk: {e.Message}");
            return null;
        }
    }

    void SetupChunk(ChunkData chunk, Vector2Int coord)
    {
        if (chunk == null) return;

        chunk.coordinate = coord;
        Vector3 worldPos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.gameObject.transform.position = worldPos;
        chunk.gameObject.name = $"Chunk_{coord.x}_{coord.y}";
    }

    Mesh CreateOptimizedChunkMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "OptimizedChunkMesh";

        // Simple quad mesh - much more memory efficient
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(chunkSize, 0, 0),
            new Vector3(0, 0, chunkSize),
            new Vector3(chunkSize, 0, chunkSize)
        };

        int[] triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Mark as not readable for better performance
        mesh.UploadMeshData(true);

        return mesh;
    }

    Vector2Int WorldToChunk(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int y = Mathf.FloorToInt(pos.z / chunkSize);
        return new Vector2Int(x, y);
    }

    void UpdatePerformanceMetrics()
    {
        activeChunkCount = activeChunks.Count;
        totalChunksInPool = chunkPool.Count;
        memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
    }

    // Public methods for external control
    public void ForceUpdate()
    {
        if (isInitialized && gameObject.activeInHierarchy)
        {
            if (streamingCoroutine != null)
                StopCoroutine(streamingCoroutine);

            streamingCoroutine = StartCoroutine(ProcessChunkUpdates());
        }
    }

    public bool IsChunkLoaded(Vector2Int coord)
    {
        return activeChunks.ContainsKey(coord);
    }

    public Vector2Int GetCurrentChunk()
    {
        return currentPlayerChunk;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !isInitialized) return;

        Gizmos.color = Color.green;
        foreach (var coord in neededChunks)
        {
            Vector3 center = new Vector3(coord.x * chunkSize + chunkSize / 2f, 0, coord.y * chunkSize + chunkSize / 2f);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 0.1f, chunkSize));
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }

    // Memory management
    public void ClearUnusedChunks()
    {
        int chunksToKeep = Mathf.Min(preWarmPoolSize, chunkPool.Count);
        while (chunkPool.Count > chunksToKeep)
        {
            var chunk = chunkPool.Dequeue();
            DestroyChunkObject(chunk);
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
}