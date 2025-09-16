using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Profiling;
using System;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Experimental.Rendering;
using System.Runtime.CompilerServices;

[RequireComponent(typeof(PlayerPositionProvider))]
public class AdvancedWorldStreamer : MonoBehaviour
{
    [Header("Streaming Settings")]
    [Range(1, 12)] public int viewDistance = 3;
    public int chunkSize = 100;
    [Range(1, 10)] public int maxChunksPerFrame = 3;
    [Range(0.01f, 0.5f)] public float streamingUpdateInterval = 0.1f;

    [Header("Memory Management")]
    public int maxMemoryMB = 800;
    public int preWarmPoolSize = 15;
    public bool enableMemoryCleanup = true;

    [Header("Performance Settings")]
    public bool useJobSystem = true;
    public bool useGPUInstancing = true;
    public bool useBurstCompilation = true;
    public LODLevel[] lodLevels;

    [Header("Performance Monitoring")]
    [SerializeField] private int activeChunkCount;
    [SerializeField] private float memoryUsageMB;
    [SerializeField] private int totalChunksInPool;
    [SerializeField] private int chunksLoading;
    [SerializeField] private int currentLODLevel;
    [SerializeField] private float lastUpdateTimeMs;

    [System.Serializable]
    public struct LODLevel
    {
        public int distanceThreshold;
        public int meshResolution;
        public bool generateColliders;
    }

    private struct ChunkKey : IEquatable<ChunkKey>
    {
        public int x;
        public int y;

        public ChunkKey(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(ChunkKey other) => x == other.x && y == other.y;
        public override int GetHashCode() => HashCode.Combine(x, y);
    }

    private class ChunkData : IDisposable
    {
        public GameObject gameObject;
        public ChunkKey coordinate;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public int lodLevel;
        public bool isActive;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public Mesh generatedMesh;
        public bool isJobScheduled;

        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();

            // Don't destroy the mesh here as it might be used by renderer/collider
            // Cleanup will happen in DestroyChunkObject
        }
    }

    // Memory-efficient collections
    private Dictionary<ChunkKey, ChunkData> activeChunks;
    private Queue<ChunkData> chunkPool;
    private HashSet<ChunkKey> neededChunks;
    private ChunkKey currentPlayerChunk;
    private Coroutine streamingCoroutine;

    // Shared resources
    private Material[] lodMaterials;
    private bool isInitialized = false;
    private PlayerPositionProvider positionProvider;

    // Performance optimization
    private WaitForSeconds streamingWait;
    private int squaredViewDist;
    private List<ChunkKey> chunksToRemove;
    private JobHandle meshGenerationJobHandle;
    private List<ChunkData> chunksWithPendingJobs;

    // Memory monitoring
    private float lastMemoryCleanupTime;
    private const float MEMORY_CLEANUP_INTERVAL = 30f;
    private const int MAX_CONCURRENT_JOBS = 8;

    // Static optimization
    private static readonly Vector3 ChunkOffset = new Vector3(0.5f, 0, 0.5f);
    private static readonly Quaternion IdentityRotation = Quaternion.identity;

    [BurstCompile]
    struct MeshGenerationJob : IJob
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public int chunkSize;
        public int resolution;
        public ChunkKey coordinate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            GenerateFlatMeshData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateFlatMeshData()
        {
            int vertexCount = (resolution + 1) * (resolution + 1);
            float step = (float)chunkSize / resolution;

            // Generate vertices
            for (int i = 0; i <= resolution; i++)
            {
                for (int j = 0; j <= resolution; j++)
                {
                    int index = i * (resolution + 1) + j;
                    vertices[index] = new Vector3(j * step, 0, i * step);
                }
            }

            // Generate triangles
            int triIndex = 0;
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    int topLeft = i * (resolution + 1) + j;
                    int topRight = topLeft + 1;
                    int bottomLeft = (i + 1) * (resolution + 1) + j;
                    int bottomRight = bottomLeft + 1;

                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = topRight;

                    triangles[triIndex++] = topRight;
                    triangles[triIndex++] = bottomLeft;
                    triangles[triIndex++] = bottomRight;
                }
            }
        }
    }

    void Awake()
    {
        InitializeLODLevels();
        InitializeCollections();
    }

    void InitializeLODLevels()
    {
        if (lodLevels == null || lodLevels.Length == 0)
        {
            lodLevels = new LODLevel[]
            {
                new LODLevel { distanceThreshold = 1, meshResolution = 64, generateColliders = true },
                new LODLevel { distanceThreshold = 3, meshResolution = 32, generateColliders = false },
                new LODLevel { distanceThreshold = 6, meshResolution = 16, generateColliders = false }
            };
        }
    }

    void InitializeCollections()
    {
        activeChunks = new Dictionary<ChunkKey, ChunkData>(256);
        neededChunks = new HashSet<ChunkKey>();
        chunkPool = new Queue<ChunkData>();
        chunksToRemove = new List<ChunkKey>(32);
        chunksWithPendingJobs = new List<ChunkData>(MAX_CONCURRENT_JOBS);
    }

    void Start()
    {
        positionProvider = GetComponent<PlayerPositionProvider>();
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        squaredViewDist = viewDistance * viewDistance;
        streamingWait = new WaitForSeconds(streamingUpdateInterval);

        CreateLODMaterials();
        PreWarmChunkPool(preWarmPoolSize);

        streamingCoroutine = StartCoroutine(StreamingUpdate());
        isInitialized = true;

        Debug.Log($"AdvancedWorldStreamer initialized with {lodLevels.Length} LOD levels");
    }

    void OnDestroy()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    void Cleanup()
    {
        if (streamingCoroutine != null)
            StopCoroutine(streamingCoroutine);

        // Complete any pending jobs
        if (meshGenerationJobHandle.IsCompleted)
            meshGenerationJobHandle.Complete();

        // Dispose all chunks
        foreach (var chunk in activeChunks.Values)
        {
            DestroyChunkObject(chunk);
        }
        activeChunks.Clear();

        foreach (var chunk in chunkPool)
        {
            DestroyChunkObject(chunk);
        }
        chunkPool.Clear();

        neededChunks.Clear();
        chunksToRemove.Clear();

        // Cleanup shared resources
        CleanupSharedResources();

        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    void CleanupSharedResources()
    {
        if (lodMaterials != null)
        {
            foreach (var mat in lodMaterials)
            {
                if (mat != null) DestroyImmediate(mat);
            }
            lodMaterials = null;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        float startTime = Time.realtimeSinceStartup;

        UpdatePerformanceMetrics();
        MonitorMemoryUsage();

        if (useJobSystem)
        {
            ProcessCompletedJobs();
        }

        lastUpdateTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;
    }

    void ProcessCompletedJobs()
    {
        if (meshGenerationJobHandle.IsCompleted && chunksWithPendingJobs.Count > 0)
        {
            meshGenerationJobHandle.Complete();

            for (int i = chunksWithPendingJobs.Count - 1; i >= 0; i--)
            {
                var chunk = chunksWithPendingJobs[i];
                if (chunk != null && chunk.isJobScheduled)
                {
                    ApplyMeshToChunk(chunk, chunk.lodLevel);
                    chunk.isJobScheduled = false;
                    chunksLoading--;
                    chunksWithPendingJobs.RemoveAt(i);
                }
            }
        }
    }

    IEnumerator StreamingUpdate()
    {
        while (true)
        {
            yield return streamingWait;

            ChunkKey playerChunk = WorldToChunk(positionProvider.GetPosition());
            if (!playerChunk.Equals(currentPlayerChunk) || activeChunks.Count == 0)
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

        for (int x = centerX - viewDistance; x <= centerX + viewDistance; x++)
        {
            for (int y = centerY - viewDistance; y <= centerY + viewDistance; y++)
            {
                int dx = x - centerX;
                int dy = y - centerY;

                if (dx * dx + dy * dy <= squaredViewDist)
                {
                    neededChunks.Add(new ChunkKey(x, y));
                }
            }
        }
    }

    void UnloadUnneededChunks()
    {
        chunksToRemove.Clear();

        foreach (var coord in activeChunks.Keys)
        {
            if (!neededChunks.Contains(coord))
            {
                chunksToRemove.Add(coord);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            if (activeChunks.TryGetValue(coord, out ChunkData chunk))
            {
                ReturnChunkToPool(chunk);
                activeChunks.Remove(coord);
            }
        }
    }

    IEnumerator LoadNeededChunks()
    {
        int chunksProcessed = 0;

        foreach (var coord in neededChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                LoadChunk(coord);
                chunksProcessed++;

                if (chunksProcessed >= maxChunksPerFrame)
                {
                    yield return null;
                    chunksProcessed = 0;
                }
            }
        }
    }

    void LoadChunk(ChunkKey coord)
    {
        ChunkData chunk = GetChunkFromPool();
        if (chunk != null)
        {
            int lodLevel = CalculateLODLevel(coord);
            SetupChunk(chunk, coord, lodLevel);

            if (useJobSystem && chunksWithPendingJobs.Count < MAX_CONCURRENT_JOBS)
            {
                StartMeshGenerationJob(chunk, lodLevel);
            }
            else
            {
                GenerateChunkMeshImmediate(chunk, lodLevel);
            }

            activeChunks.Add(coord, chunk);
        }
    }

    void StartMeshGenerationJob(ChunkData chunk, int lodLevel)
    {
        int resolution = lodLevels[lodLevel].meshResolution;
        int vertexCount = (resolution + 1) * (resolution + 1);
        int triangleCount = resolution * resolution * 6;

        chunk.vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        chunk.triangles = new NativeArray<int>(triangleCount, Allocator.Persistent);

        var job = new MeshGenerationJob
        {
            vertices = chunk.vertices,
            triangles = chunk.triangles,
            chunkSize = chunkSize,
            resolution = resolution,
            coordinate = chunk.coordinate
        };

        chunksLoading++;
        chunk.isJobScheduled = true;
        chunksWithPendingJobs.Add(chunk);

        meshGenerationJobHandle = useBurstCompilation ?
            job.Schedule(meshGenerationJobHandle) :
            job.Schedule();
    }

    void GenerateChunkMeshImmediate(ChunkData chunk, int lodLevel)
    {
        int resolution = lodLevels[lodLevel].meshResolution;
        GenerateMeshData(chunk, resolution);
        ApplyMeshToChunk(chunk, lodLevel);
    }

    void ApplyMeshToChunk(ChunkData chunk, int lodLevel)
    {
        if (chunk.meshFilter == null) return;

        if (chunk.generatedMesh == null)
        {
            chunk.generatedMesh = new Mesh
            {
                indexFormat = (chunk.triangles.Length > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16,
                name = $"ChunkMesh_{chunk.coordinate.x}_{chunk.coordinate.y}_LOD{lodLevel}"
            };
        }
        else
        {
            chunk.generatedMesh.Clear();
        }

        // --- Vertices ---
        chunk.generatedMesh.SetVertices(chunk.vertices);

        // --- Triangles ---
        chunk.generatedMesh.SetIndices(chunk.triangles, MeshTopology.Triangles, 0);

        // --- Finish mesh setup ---
        chunk.generatedMesh.RecalculateNormals();
        chunk.generatedMesh.UploadMeshData(true);

        // --- Assign mesh ---
        chunk.meshFilter.sharedMesh = chunk.generatedMesh;
        chunk.lodLevel = lodLevel;

        // --- Handle collider ---
        if (chunk.meshCollider != null)
        {
            chunk.meshCollider.enabled = lodLevels[lodLevel].generateColliders;
            if (chunk.meshCollider.enabled)
            {
                chunk.meshCollider.sharedMesh = chunk.generatedMesh;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int CalculateLODLevel(ChunkKey coord)
    {
        int distance = Math.Max(Math.Abs(coord.x - currentPlayerChunk.x),
                              Math.Abs(coord.y - currentPlayerChunk.y));

        for (int i = 0; i < lodLevels.Length; i++)
        {
            if (distance <= lodLevels[i].distanceThreshold)
            {
                currentLODLevel = i;
                return i;
            }
        }

        return lodLevels.Length - 1;
    }

    void CreateLODMaterials()
    {
        lodMaterials = new Material[lodLevels.Length];
        for (int i = 0; i < lodLevels.Length; i++)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.4f, 0.6f, 0.4f);
            mat.enableInstancing = useGPUInstancing;
            lodMaterials[i] = mat;
        }
    }

    void GenerateMeshData(ChunkData chunk, int resolution)
    {
        int vertexCount = (resolution + 1) * (resolution + 1);
        int triangleCount = resolution * resolution * 6;

        chunk.vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        chunk.triangles = new NativeArray<int>(triangleCount, Allocator.Persistent);

        float step = (float)chunkSize / resolution;

        for (int i = 0; i <= resolution; i++)
        {
            for (int j = 0; j <= resolution; j++)
            {
                int index = i * (resolution + 1) + j;
                chunk.vertices[index] = new Vector3(j * step, 0, i * step);
            }
        }

        int triIndex = 0;
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                int topLeft = i * (resolution + 1) + j;
                int topRight = topLeft + 1;
                int bottomLeft = (i + 1) * (resolution + 1) + j;
                int bottomRight = bottomLeft + 1;

                chunk.triangles[triIndex++] = topLeft;
                chunk.triangles[triIndex++] = bottomLeft;
                chunk.triangles[triIndex++] = topRight;

                chunk.triangles[triIndex++] = topRight;
                chunk.triangles[triIndex++] = bottomLeft;
                chunk.triangles[triIndex++] = bottomRight;
            }
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
        while (chunkPool.Count > 0)
        {
            ChunkData chunk = chunkPool.Dequeue();
            if (chunk != null && chunk.gameObject != null)
            {
                chunk.gameObject.SetActive(true);
                chunk.isActive = true;
                return chunk;
            }
        }

        return CreateNewChunk();
    }

    void ReturnChunkToPool(ChunkData chunk)
    {
        if (chunk == null) return;

        chunk.gameObject.SetActive(false);
        chunk.isActive = false;

        // Clear mesh references
        if (chunk.meshFilter != null)
            chunk.meshFilter.sharedMesh = null;

        if (chunk.meshCollider != null)
            chunk.meshCollider.sharedMesh = null;

        // Dispose native arrays
        chunk.Dispose();

        if (chunkPool.Count < preWarmPoolSize * 2)
        {
            chunkPool.Enqueue(chunk);
        }
        else
        {
            DestroyChunkObject(chunk);
        }
    }

    ChunkData CreateNewChunk()
    {
        try
        {
            GameObject chunkObj = new GameObject("Chunk", typeof(MeshFilter), typeof(MeshRenderer));
            chunkObj.transform.SetParent(transform);
            chunkObj.layer = gameObject.layer;

            return new ChunkData
            {
                gameObject = chunkObj,
                meshFilter = chunkObj.GetComponent<MeshFilter>(),
                meshRenderer = chunkObj.GetComponent<MeshRenderer>(),
                isActive = false
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create chunk: {e.Message}");
            return null;
        }
    }

    void SetupChunk(ChunkData chunk, ChunkKey coord, int lodLevel)
    {
        if (chunk == null) return;

        chunk.coordinate = coord;
        chunk.lodLevel = lodLevel;
        chunk.meshRenderer.material = lodMaterials[lodLevel];

        Vector3 worldPos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.gameObject.transform.SetPositionAndRotation(worldPos, IdentityRotation);
        chunk.gameObject.name = $"Chunk_{coord.x}_{coord.y}_LOD{lodLevel}";

        // Add collider if needed for this LOD
        if (lodLevels[lodLevel].generateColliders && chunk.meshCollider == null)
        {
            chunk.meshCollider = chunk.gameObject.AddComponent<MeshCollider>();
        }
        else if (chunk.meshCollider != null && !lodLevels[lodLevel].generateColliders)
        {
            chunk.meshCollider.enabled = false;
        }
    }

    ChunkKey WorldToChunk(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int y = Mathf.FloorToInt(pos.z / chunkSize);
        return new ChunkKey(x, y);
    }

    void UpdatePerformanceMetrics()
    {
        activeChunkCount = activeChunks.Count;
        totalChunksInPool = chunkPool.Count;
        memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
    }

    void MonitorMemoryUsage()
    {
        if (enableMemoryCleanup && Time.time - lastMemoryCleanupTime > MEMORY_CLEANUP_INTERVAL)
        {
            if (memoryUsageMB > maxMemoryMB * 0.8f)
            {
                ForceMemoryCleanup();
            }
            lastMemoryCleanupTime = Time.time;
        }
    }

    void ForceMemoryCleanup()
    {
        // Complete any pending jobs first
        if (useJobSystem && !meshGenerationJobHandle.IsCompleted)
        {
            meshGenerationJobHandle.Complete();
            ProcessCompletedJobs();
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();

        // Reduce pool size if memory is high
        while (chunkPool.Count > preWarmPoolSize && memoryUsageMB > maxMemoryMB * 0.7f)
        {
            var chunk = chunkPool.Dequeue();
            DestroyChunkObject(chunk);
        }
    }

    void DestroyChunkObject(ChunkData chunk)
    {
        if (chunk != null)
        {
            // Dispose native arrays
            chunk.Dispose();

            // Destroy the mesh if it exists
            if (chunk.generatedMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(chunk.generatedMesh);
                else
                    DestroyImmediate(chunk.generatedMesh);
                chunk.generatedMesh = null;
            }

            // Destroy the game object
            if (chunk.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(chunk.gameObject);
                else
                    DestroyImmediate(chunk.gameObject);
            }
        }
    }

    // Public API
    public void ForceUpdate()
    {
        if (isInitialized && gameObject.activeInHierarchy)
        {
            if (streamingCoroutine != null)
                StopCoroutine(streamingCoroutine);

            streamingCoroutine = StartCoroutine(ProcessChunkUpdates());
        }
    }

    public bool IsChunkLoaded(int x, int y) => activeChunks.ContainsKey(new ChunkKey(x, y));
    public Vector2Int GetCurrentChunk() => new Vector2Int(currentPlayerChunk.x, currentPlayerChunk.y);
}

public class PlayerPositionProvider : MonoBehaviour
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPosition() => transform.position;
}