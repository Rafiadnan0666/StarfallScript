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
    [Range(1, 20)] public int viewDistance = 8;
    public int chunkSize = 100;
    [Range(1, 15)] public int maxChunksPerFrame = 5;
    [Range(0.01f, 0.5f)] public float streamingUpdateInterval = 0.1f;

    [Header("Memory Management")]
    public int maxMemoryMB = 1024;
    public int preWarmPoolSize = 20;
    public bool enableMemoryCleanup = true;
    [Range(10, 300)] public int memoryCleanupInterval = 60;

    [Header("Performance Settings")]
    public bool useJobSystem = true;
    public bool useGPUInstancing = true;
    public bool useBurstCompilation = true;
    public bool useLODCrossFade = true;
    public bool enableOcclusionCulling = true;
    public LODLevel[] lodLevels;

    [Header("Performance Monitoring")]
    [SerializeField] private int activeChunkCount;
    [SerializeField] private float memoryUsageMB;
    [SerializeField] private int totalChunksInPool;
    [SerializeField] private int chunksLoading;
    [SerializeField] private int currentLODLevel;
    [SerializeField] private float lastUpdateTimeMs;
    [SerializeField] private int drawCallCount;
    [SerializeField] private int triangleCount;
    [SerializeField] private int vertexCount;

    [System.Serializable]
    public struct LODLevel
    {
        [Range(1, 20)] public int distanceThreshold;
        [Range(4, 256)] public int meshResolution;
        public bool generateColliders;
        public float cullRatio;
    }

    private struct ChunkKey : IEquatable<ChunkKey>, IComparable<ChunkKey>
    {
        public int x;
        public int z;

        public ChunkKey(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public bool Equals(ChunkKey other) => x == other.x && z == other.z;
        public override int GetHashCode() => (x << 16) ^ z;
        public int CompareTo(ChunkKey other)
        {
            int xCompare = x.CompareTo(other.x);
            return xCompare != 0 ? xCompare : z.CompareTo(other.z);
        }

        public float SqrDistanceTo(ChunkKey other)
        {
            int dx = x - other.x;
            int dz = z - other.z;
            return dx * dx + dz * dz;
        }
    }

    private class ChunkData : IDisposable, IComparable<ChunkData>
    {
        public GameObject gameObject;
        public ChunkKey coordinate;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public LODGroup lodGroup;
        public int lodLevel;
        public bool isActive;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public Mesh generatedMesh;
        public bool isJobScheduled;
        public float lastUsedTime;
        public float priority;
        public int triangleCount;
        public int vertexCount;

        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
        }

        public int CompareTo(ChunkData other)
        {
            return other.priority.CompareTo(priority);
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
    private List<ChunkData> priorityLoadQueue;
    private Plane[] cameraFrustumPlanes;

    // Memory monitoring
    private float lastMemoryCleanupTime;
    private int frameCounter;
    private const int FRAMES_BETWEEN_MEMORY_CHECK = 60;

    // Static optimization
    private static readonly Vector3 ChunkOffset = new Vector3(0.5f, 0, 0.5f);
    private static readonly Quaternion IdentityRotation = Quaternion.identity;
    private static Camera mainCamera;

    [BurstCompile(FloatPrecision.High, FloatMode.Fast)]
    private struct MeshGenerationJob : IJob
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<int> triangles;
        public int chunkSize;
        public int resolution;
        public ChunkKey coordinate;
        public float heightMultiplier;
        public float noiseScale;
        public Vector2 noiseOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            GenerateTerrainMeshData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateTerrainMeshData()
        {
            int vertexCount = (resolution + 1) * (resolution + 1);
            float step = (float)chunkSize / resolution;

            for (int i = 0; i <= resolution; i++)
            {
                for (int j = 0; j <= resolution; j++)
                {
                    int index = i * (resolution + 1) + j;
                    float x = j * step;
                    float z = i * step;

                    float worldX = (coordinate.x * chunkSize + x) * noiseScale + noiseOffset.x;
                    float worldZ = (coordinate.z * chunkSize + z) * noiseScale + noiseOffset.y;
                    float height = Mathf.PerlinNoise(worldX, worldZ) * heightMultiplier;

                    vertices[index] = new Vector3(x, height, z);
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
        mainCamera = Camera.main;
    }

    void InitializeLODLevels()
    {
        if (lodLevels == null || lodLevels.Length == 0)
        {
            lodLevels = new LODLevel[]
            {
                new LODLevel { distanceThreshold = 2, meshResolution = 128, generateColliders = true, cullRatio = 0.1f },
                new LODLevel { distanceThreshold = 4, meshResolution = 64, generateColliders = false, cullRatio = 0.2f },
                new LODLevel { distanceThreshold = 8, meshResolution = 32, generateColliders = false, cullRatio = 0.3f },
                new LODLevel { distanceThreshold = 12, meshResolution = 16, generateColliders = false, cullRatio = 0.4f }
            };
        }
    }

    void InitializeCollections()
    {
        activeChunks = new Dictionary<ChunkKey, ChunkData>(512);
        neededChunks = new HashSet<ChunkKey>();
        chunkPool = new Queue<ChunkData>();
        chunksToRemove = new List<ChunkKey>(64);
        chunksWithPendingJobs = new List<ChunkData>(16);
        priorityLoadQueue = new List<ChunkData>(32);

        if (mainCamera != null)
        {
            cameraFrustumPlanes = new Plane[6];
        }
    }

    void Start()
    {
        positionProvider = GetComponent<PlayerPositionProvider>();
        if (positionProvider == null)
        {
            Debug.LogError("PlayerPositionProvider component is missing!");
            return;
        }

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

    void OnDisable()
    {
        if (streamingCoroutine != null)
            StopCoroutine(streamingCoroutine);
    }

    void OnEnable()
    {
        if (isInitialized && streamingCoroutine == null)
            streamingCoroutine = StartCoroutine(StreamingUpdate());
    }

    void Cleanup()
    {
        if (streamingCoroutine != null)
        {
            StopCoroutine(streamingCoroutine);
            streamingCoroutine = null;
        }

        if (useJobSystem && !meshGenerationJobHandle.IsCompleted)
        {
            meshGenerationJobHandle.Complete();
            ProcessCompletedJobs();
        }

        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null)
                DestroyChunkObject(chunk);
        }
        activeChunks.Clear();

        foreach (var chunk in chunkPool)
        {
            if (chunk != null)
                DestroyChunkObject(chunk);
        }
        chunkPool.Clear();

        neededChunks.Clear();
        chunksToRemove.Clear();
        chunksWithPendingJobs.Clear();
        priorityLoadQueue.Clear();

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
                if (mat != null)
                    DestroyImmediate(mat);
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

        if (enableOcclusionCulling && mainCamera != null && frameCounter % 10 == 0)
        {
            GeometryUtility.CalculateFrustumPlanes(mainCamera, cameraFrustumPlanes);
        }

        frameCounter++;
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

            if (positionProvider == null)
            {
                Debug.LogError("Position provider is null!");
                yield break;
            }

            Vector3 playerPos = positionProvider.GetPosition();
            ChunkKey playerChunk = WorldToChunk(playerPos);

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
        priorityLoadQueue.Clear();

        int centerX = currentPlayerChunk.x;
        int centerZ = currentPlayerChunk.z;

        for (int x = centerX - viewDistance; x <= centerX + viewDistance; x++)
        {
            for (int z = centerZ - viewDistance; z <= centerZ + viewDistance; z++)
            {
                ChunkKey chunkKey = new ChunkKey(x, z);
                float sqrDist = chunkKey.SqrDistanceTo(currentPlayerChunk);

                if (sqrDist <= squaredViewDist)
                {
                    neededChunks.Add(chunkKey);

                    float priority = 1f / (1f + Mathf.Sqrt(sqrDist));

                    if (!activeChunks.ContainsKey(chunkKey) || !activeChunks[chunkKey].isActive)
                    {
                        var chunk = GetChunkFromPool();
                        if (chunk != null)
                        {
                            chunk.coordinate = chunkKey;
                            chunk.priority = priority;
                            priorityLoadQueue.Add(chunk);
                        }
                    }
                }
            }
        }

        priorityLoadQueue.Sort();
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

        foreach (var chunk in priorityLoadQueue)
        {
            if (chunk != null && !activeChunks.ContainsKey(chunk.coordinate))
            {
                int lodLevel = CalculateLODLevel(chunk.coordinate);
                SetupChunk(chunk, chunk.coordinate, lodLevel);

                if (useJobSystem && chunksWithPendingJobs.Count < 8)
                {
                    StartMeshGenerationJob(chunk, lodLevel);
                }
                else
                {
                    GenerateChunkMeshImmediate(chunk, lodLevel);
                }

                activeChunks.Add(chunk.coordinate, chunk);
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

            if (useJobSystem && chunksWithPendingJobs.Count < 8)
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
        chunk.vertexCount = vertexCount;
        chunk.triangleCount = triangleCount;

        var job = new MeshGenerationJob
        {
            vertices = chunk.vertices,
            triangles = chunk.triangles,
            chunkSize = chunkSize,
            resolution = resolution,
            coordinate = chunk.coordinate,
            heightMultiplier = 10f,
            noiseScale = 0.05f,
            noiseOffset = new Vector2(UnityEngine.Random.value * 1000f, UnityEngine.Random.value * 1000f)
        };

        chunksLoading++;
        chunk.isJobScheduled = true;
        chunksWithPendingJobs.Add(chunk);

        if (useBurstCompilation)
        {
            meshGenerationJobHandle = job.Schedule(meshGenerationJobHandle);
        }
        else
        {
            meshGenerationJobHandle = job.Schedule();
        }
    }

    void GenerateChunkMeshImmediate(ChunkData chunk, int lodLevel)
    {
        int resolution = lodLevels[lodLevel].meshResolution;
        GenerateMeshData(chunk, resolution);
        ApplyMeshToChunk(chunk, lodLevel);
    }

    void ApplyMeshToChunk(ChunkData chunk, int lodLevel)
    {
        if (chunk == null || chunk.meshFilter == null) return;

        if (chunk.generatedMesh == null)
        {
            chunk.generatedMesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32,
                name = $"ChunkMesh_{chunk.coordinate.x}_{chunk.coordinate.z}_LOD{lodLevel}"
            };
        }
        else
        {
            chunk.generatedMesh.Clear();
        }

        chunk.generatedMesh.SetVertices(chunk.vertices);
        chunk.generatedMesh.SetIndices(chunk.triangles, MeshTopology.Triangles, 0);
        chunk.generatedMesh.RecalculateNormals();
        chunk.generatedMesh.RecalculateBounds();
        chunk.generatedMesh.UploadMeshData(false);

        chunk.meshFilter.sharedMesh = chunk.generatedMesh;
        chunk.lodLevel = lodLevel;

        if (chunk.meshCollider != null)
        {
            chunk.meshCollider.enabled = lodLevels[lodLevel].generateColliders;
            if (chunk.meshCollider.enabled)
            {
                chunk.meshCollider.sharedMesh = chunk.generatedMesh;
            }
        }

        if (chunk.lodGroup != null && useLODCrossFade)
        {
            UpdateLODGroup(chunk, lodLevel);
        }

        chunk.lastUsedTime = Time.time;
    }

    void UpdateLODGroup(ChunkData chunk, int lodLevel)
    {
        LOD[] lods = new LOD[lodLevels.Length];
        float relativeHeight = 1f / lods.Length;

        for (int i = 0; i < lods.Length; i++)
        {
            Renderer[] renderers = i == lodLevel ?
                new Renderer[] { chunk.meshRenderer } :
                new Renderer[0];

            lods[i] = new LOD(i == lodLevel ? lodLevels[i].cullRatio : 0.01f, renderers);
        }

        chunk.lodGroup.SetLODs(lods);
        chunk.lodGroup.RecalculateBounds();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int CalculateLODLevel(ChunkKey coord)
    {
        float distance = Mathf.Sqrt(coord.SqrDistanceTo(currentPlayerChunk));

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
            float intensity = 1f - (i / (float)lodLevels.Length) * 0.5f;
            mat.color = new Color(0.4f * intensity, 0.6f * intensity, 0.4f * intensity);
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
        chunk.vertexCount = vertexCount;
        chunk.triangleCount = triangleCount;

        float step = (float)chunkSize / resolution;

        for (int i = 0; i <= resolution; i++)
        {
            for (int j = 0; j <= resolution; j++)
            {
                int index = i * (resolution + 1) + j;
                float x = j * step;
                float z = i * step;

                float worldX = (chunk.coordinate.x * chunkSize + x) * 0.05f;
                float worldZ = (chunk.coordinate.z * chunkSize + z) * 0.05f;
                float height = Mathf.PerlinNoise(worldX, worldZ) * 10f;

                chunk.vertices[index] = new Vector3(x, height, z);
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

        if (chunk.meshFilter != null)
            chunk.meshFilter.sharedMesh = null;

        if (chunk.meshCollider != null)
            chunk.meshCollider.sharedMesh = null;

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

            LODGroup lodGroup = chunkObj.AddComponent<LODGroup>();
            lodGroup.animateCrossFading = useLODCrossFade;
            lodGroup.fadeMode = LODFadeMode.CrossFade;

            return new ChunkData
            {
                gameObject = chunkObj,
                meshFilter = chunkObj.GetComponent<MeshFilter>(),
                meshRenderer = chunkObj.GetComponent<MeshRenderer>(),
                lodGroup = lodGroup,
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

        if (chunk.meshRenderer != null && lodMaterials != null && lodLevel < lodMaterials.Length)
        {
            chunk.meshRenderer.material = lodMaterials[lodLevel];
            chunk.meshRenderer.enabled = IsChunkVisible(coord);
        }

        Vector3 worldPos = new Vector3(coord.x * chunkSize, 0, coord.z * chunkSize);
        chunk.gameObject.transform.SetPositionAndRotation(worldPos, IdentityRotation);
        chunk.gameObject.name = $"Chunk_{coord.x}_{coord.z}_LOD{lodLevel}";

        if (lodLevels[lodLevel].generateColliders)
        {
            if (chunk.meshCollider == null)
            {
                chunk.meshCollider = chunk.gameObject.AddComponent<MeshCollider>();
            }
            chunk.meshCollider.enabled = true;
        }
        else if (chunk.meshCollider != null)
        {
            chunk.meshCollider.enabled = false;
        }
    }

    bool IsChunkVisible(ChunkKey coord)
    {
        if (!enableOcclusionCulling || mainCamera == null)
            return true;

        Vector3 chunkCenter = new Vector3(
            coord.x * chunkSize + chunkSize * 0.5f,
            0,
            coord.z * chunkSize + chunkSize * 0.5f
        );

        Bounds chunkBounds = new Bounds(chunkCenter, new Vector3(chunkSize, 100f, chunkSize));
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, chunkBounds);
    }

    ChunkKey WorldToChunk(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkSize);
        int z = Mathf.FloorToInt(pos.z / chunkSize);
        return new ChunkKey(x, z);
    }

    void UpdatePerformanceMetrics()
    {
        activeChunkCount = activeChunks.Count;
        totalChunksInPool = chunkPool.Count;
        memoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;

        // Calculate total geometry statistics
        triangleCount = 0;
        vertexCount = 0;
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null)
            {
                triangleCount += chunk.triangleCount;
                vertexCount += chunk.vertexCount;
            }
        }

        // Estimate draw calls based on active chunks and LOD levels
        drawCallCount = activeChunkCount;
    }

    void MonitorMemoryUsage()
    {
        if (enableMemoryCleanup && Time.time - lastMemoryCleanupTime > memoryCleanupInterval)
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
        if (useJobSystem && !meshGenerationJobHandle.IsCompleted)
        {
            meshGenerationJobHandle.Complete();
            ProcessCompletedJobs();
        }

        Resources.UnloadUnusedAssets();
        GC.Collect();

        while (chunkPool.Count > preWarmPoolSize && memoryUsageMB > maxMemoryMB * 0.7f)
        {
            var chunk = chunkPool.Dequeue();
            if (chunk != null)
                DestroyChunkObject(chunk);
        }
    }

    void DestroyChunkObject(ChunkData chunk)
    {
        if (chunk != null)
        {
            chunk.Dispose();

            if (chunk.generatedMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(chunk.generatedMesh);
                else
                    DestroyImmediate(chunk.generatedMesh);
                chunk.generatedMesh = null;
            }

            if (chunk.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(chunk.gameObject);
                else
                    DestroyImmediate(chunk.gameObject);
            }
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance * chunkSize);

        Gizmos.color = Color.green;
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Vector3 center = chunk.gameObject.transform.position + new Vector3(chunkSize * 0.5f, 0, chunkSize * 0.5f);
                Gizmos.DrawWireCube(center, new Vector3(chunkSize, 1, chunkSize));
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

    public bool IsChunkLoaded(int x, int z) => activeChunks.ContainsKey(new ChunkKey(x, z));
    public Vector2Int GetCurrentChunk() => new Vector2Int(currentPlayerChunk.x, currentPlayerChunk.z);
    public int GetActiveChunkCount() => activeChunkCount;
    public float GetMemoryUsageMB() => memoryUsageMB;
    public int GetTotalTriangleCount() => triangleCount;
    public int GetTotalVertexCount() => vertexCount;
    public int GetEstimatedDrawCalls() => drawCallCount;
}

public class PlayerPositionProvider : MonoBehaviour
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPosition() => transform.position;
}