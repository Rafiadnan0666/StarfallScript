using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Profiling;
using System.Threading.Tasks;

public class WorldStreamer : MonoBehaviour
{
    [Header("Streaming Settings")]
    public int viewDistance = 2;
    public int chunkSize = 100;
    public int maxChunksPerFrame = 1;
    public float streamingUpdateInterval = 0.5f;
    public Material defaultChunkMaterial;

    [Header("Performance Settings")]
    public bool useAsyncLoading = true;
    public int preWarmPoolSize = 20;
    public float chunkUnloadDelay = 5f;

    [Header("Performance Monitoring")]
    [SerializeField] private int activeChunkCount;
    [SerializeField] private float memoryUsageMB;
    [SerializeField] private int totalChunksInPool;
    [SerializeField] private int chunksLoading;

    // Chunk data structure
    private class ChunkData
    {
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public bool isActive;
        public Vector2Int coordinate;
        public float lastUsedTime;
    }

    // Object pooling
    private Dictionary<Vector2Int, ChunkData> activeChunks = new Dictionary<Vector2Int, ChunkData>();
    private Queue<ChunkData> chunkPool = new Queue<ChunkData>();
    private HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksToUnload = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunk;
    private Coroutine streamingCoroutine;

    // Player reference
    private Transform player;

    // Pre-allocated meshes to avoid GC
    private Mesh cachedFlatMesh;
    private bool isInitialized = false;

    // Loading queues
    private Queue<Vector2Int> chunkLoadQueue = new Queue<Vector2Int>(32);
    private List<Vector2Int> processingChunks = new List<Vector2Int>(32);

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        FindPlayer();

        if (defaultChunkMaterial == null)
        {
            defaultChunkMaterial = new Material(Shader.Find("Standard"));
            defaultChunkMaterial.color = new Color(0.4f, 0.6f, 0.4f);
            defaultChunkMaterial.enableInstancing = true;
        }

        // Pre-generate mesh to avoid GC
        cachedFlatMesh = GenerateFlatChunkMesh();
        cachedFlatMesh.name = "CachedChunkMesh";

        // Pre-warm object pool
        PreWarmChunkPool(preWarmPoolSize);

        streamingCoroutine = StartCoroutine(StreamingUpdate());
        isInitialized = true;
    }

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                player = mainCamera.transform;
            }
        }
    }

    void OnDestroy()
    {
        if (streamingCoroutine != null)
            StopCoroutine(streamingCoroutine);

        Cleanup();
    }

    void Cleanup()
    {
        foreach (var chunk in activeChunks.Values)
        {
            SafeDestroy(chunk.gameObject);
        }
        activeChunks.Clear();

        while (chunkPool.Count > 0)
        {
            var chunk = chunkPool.Dequeue();
            SafeDestroy(chunk.gameObject);
        }

        if (cachedFlatMesh != null)
        {
            DestroyImmediate(cachedFlatMesh);
        }
    }

    void SafeDestroy(Object obj)
    {
        if (obj != null)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        UpdatePerformanceMetrics();
        ProcessDelayedUnloads();
    }

    IEnumerator StreamingUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(streamingUpdateInterval);

            if (player == null)
            {
                FindPlayer();
                continue;
            }

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

        Profiler.BeginSample("MarkChunksForUnload");
        MarkChunksForUnload();
        Profiler.EndSample();

        Profiler.BeginSample("LoadNewChunks");
        yield return StartCoroutine(LoadQueuedChunks());
        Profiler.EndSample();
    }

    void CalculateNeededChunks()
    {
        neededChunks.Clear();
        chunkLoadQueue.Clear();

        int squaredViewDist = viewDistance * viewDistance;

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                // Circular view distance check
                if (x * x + y * y > squaredViewDist)
                    continue;

                Vector2Int coord = currentPlayerChunk + new Vector2Int(x, y);
                neededChunks.Add(coord);

                if (!activeChunks.ContainsKey(coord) && !processingChunks.Contains(coord))
                {
                    chunkLoadQueue.Enqueue(coord);
                }
            }
        }
    }

    void MarkChunksForUnload()
    {
        chunksToUnload.Clear();

        foreach (var kvp in activeChunks)
        {
            if (!neededChunks.Contains(kvp.Key))
            {
                chunksToUnload.Add(kvp.Key);
            }
        }
    }

    void ProcessDelayedUnloads()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (var coord in chunksToUnload)
        {
            if (activeChunks.TryGetValue(coord, out ChunkData chunk))
            {
                chunk.lastUsedTime += Time.deltaTime;

                if (chunk.lastUsedTime >= chunkUnloadDelay)
                {
                    ReturnChunkToPool(chunk);
                    chunksToRemove.Add(coord);
                }
            }
        }

        foreach (var coord in chunksToRemove)
        {
            activeChunks.Remove(coord);
            chunksToUnload.Remove(coord);
        }
    }

    IEnumerator LoadQueuedChunks()
    {
        int chunksProcessed = 0;

        while (chunkLoadQueue.Count > 0 && chunksProcessed < maxChunksPerFrame)
        {
            Vector2Int coord = chunkLoadQueue.Dequeue();

            if (!activeChunks.ContainsKey(coord) && !processingChunks.Contains(coord))
            {
                processingChunks.Add(coord);

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
                    yield return null;
            }
        }
    }

    IEnumerator LoadChunkAsync(Vector2Int coord)
    {
        yield return null; // Wait one frame to spread load

        ChunkData chunk = GetChunkFromPool();
        SetupChunk(chunk, coord);
        activeChunks.Add(coord, chunk);

        processingChunks.Remove(coord);
        chunksLoading--;
    }

    void LoadChunkImmediate(Vector2Int coord)
    {
        ChunkData chunk = GetChunkFromPool();
        SetupChunk(chunk, coord);
        activeChunks.Add(coord, chunk);

        processingChunks.Remove(coord);
        chunksLoading--;
    }

    void PreWarmChunkPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ChunkData chunk = CreateNewChunk();
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
        }
    }

    ChunkData GetChunkFromPool()
    {
        if (chunkPool.Count > 0)
        {
            ChunkData chunk = chunkPool.Dequeue();
            chunk.gameObject.SetActive(true);
            chunk.lastUsedTime = 0f;
            return chunk;
        }

        return CreateNewChunk();
    }

    void ReturnChunkToPool(ChunkData chunk)
    {
        chunk.gameObject.SetActive(false);
        chunk.lastUsedTime = 0f;
        chunkPool.Enqueue(chunk);
    }

    ChunkData CreateNewChunk()
    {
        GameObject chunkObj = new GameObject("Chunk");
        chunkObj.transform.SetParent(transform);
        chunkObj.layer = gameObject.layer;

        MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = chunkObj.AddComponent<MeshCollider>();

        meshRenderer.material = defaultChunkMaterial;
        meshFilter.mesh = cachedFlatMesh;
        meshCollider.sharedMesh = cachedFlatMesh;

        return new ChunkData
        {
            gameObject = chunkObj,
            meshFilter = meshFilter,
            meshRenderer = meshRenderer,
            meshCollider = meshCollider,
            isActive = false,
            coordinate = Vector2Int.zero,
            lastUsedTime = 0f
        };
    }

    void SetupChunk(ChunkData chunk, Vector2Int coord)
    {
        chunk.coordinate = coord;
        Vector3 worldPos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.gameObject.transform.position = worldPos;
        chunk.gameObject.name = $"Chunk_{coord.x}_{coord.y}";
        chunk.isActive = true;
    }

    Mesh GenerateFlatChunkMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ChunkMesh";

        // Simple optimized flat plane
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
        foreach (var kvp in activeChunks)
        {
            Vector3 center = new Vector3(kvp.Key.x * chunkSize + chunkSize / 2f, 0, kvp.Key.y * chunkSize + chunkSize / 2f);
            Gizmos.DrawWireCube(center, new Vector3(chunkSize, 0.1f, chunkSize));
        }

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(player.position, 2f);
        }
    }

    // Public methods for external control
    public void ForceUpdate()
    {
        if (isInitialized)
        {
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
}