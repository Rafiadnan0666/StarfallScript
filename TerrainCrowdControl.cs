using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain))]
public class TerrainCrowdControl : MonoBehaviour
{
    [System.Serializable]
    public class CrowdBiomeData
    {
        [Header("Biome Settings")]
        public BiomeType biomeType;

        [Header("Tree Settings")]
        public GameObject[] crowdPrefabs;
        [Range(0, 1)] public float crowdDensity = 0.3f;
        [Range(0, 90)] public float maxSlope = 30f;
        [Range(0.5f, 2f)] public float minTreeWidth = 0.8f;
        [Range(0.5f, 2f)] public float maxTreeWidth = 1.2f;
        [Range(0.5f, 2f)] public float minTreeHeight = 0.8f;
        [Range(0.5f, 2f)] public float maxTreeHeight = 1.4f;

        [Header("Grass Settings")]
        public Texture2D[] grassTextures;
        [Range(0, 1)] public float grassDensity = 0.7f;
        [Range(0.1f, 3f)] public float minGrassWidth = 1f;
        [Range(0.1f, 3f)] public float maxGrassWidth = 2f;
        [Range(0.1f, 3f)] public float minGrassHeight = 1f;
        [Range(0.1f, 3f)] public float maxGrassHeight = 2f;
        public Color healthyGrassColor = Color.green;
        public Color dryGrassColor = Color.yellow;
    }

    [Header("Global Settings")]
    public List<CrowdBiomeData> biomeCrowds;
    [Range(16, 1024)] public int grassResolution = 256; // Reduced default
    [Range(8, 32)] public int grassDetailResolutionPerPatch = 16;
    public bool autoGenerateOnStart = true;

    [Header("Optimization Settings")]
    [Range(0.1f, 2f)] public float generationSpeed = 1f;
    public bool useGPUInstancing = true;
    public bool enableLOD = true;
    [Range(10, 500)] public int batchSize = 100;
    [Range(0.1f, 5f)] public float LODDistance = 2f;

    [Header("Natural Distribution")]
    [Range(1f, 20f)] public float noiseScale = 8f;
    [Range(0f, 1f)] public float noiseThreshold = 0.4f;
    [Range(0f, 1f)] public float clusterDensity = 0.6f;

    private Terrain terrain;
    private TerrainData terrainData;
    private BiomeType currentBiome;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Coroutine generationCoroutine;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        TerrainGenerator generator = GetComponent<TerrainGenerator>();
        currentBiome = generator != null ? generator.currentbiome : BiomeType.Grassland;

        if (autoGenerateOnStart)
        {
            GenerateAllVegetation();
        }
    }

    void OnDestroy()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }
    }

    public void GenerateAllVegetation()
    {
        if (generationCoroutine != null)
            StopCoroutine(generationCoroutine);

        generationCoroutine = StartCoroutine(GenerateVegetationCoroutine());
    }

    private IEnumerator GenerateVegetationCoroutine()
    {
        ClearExistingVegetation();

        // Small delay to spread workload
        if (generationSpeed < 1f)
            yield return new WaitForSeconds(0.1f);

        SetupTreePrototypes();
        yield return StartCoroutine(SpawnTreesCoroutine());

        if (generationSpeed < 1f)
            yield return new WaitForSeconds(0.1f);

        SetupGrassPrototypes();
        SpawnGrassOptimized();

        Debug.Log("✅ Vegetation generation completed");
    }

    public void ClearExistingVegetation()
    {
        // Clear terrain vegetation
        terrainData.treePrototypes = new TreePrototype[0];
        terrainData.treeInstances = new TreeInstance[0];
        terrainData.detailPrototypes = new DetailPrototype[0];

        // Clear spawned objects
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        spawnedObjects.Clear();

        // Clear detail layers
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] emptyLayer = new int[grassResolution, grassResolution];
            terrainData.SetDetailLayer(0, 0, i, emptyLayer);
        }
    }

    void SetupTreePrototypes()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.crowdPrefabs.Length == 0) return;

        List<TreePrototype> prototypes = new List<TreePrototype>();
        foreach (var prefab in data.crowdPrefabs)
        {
            if (prefab != null)
            {
                prototypes.Add(new TreePrototype
                {
                    prefab = prefab,
                    bendFactor = 1.0f
                });
            }
        }

        terrainData.treePrototypes = prototypes.ToArray();
    }

    private IEnumerator SpawnTreesCoroutine()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.crowdPrefabs.Length == 0) yield break;

        List<TreeInstance> trees = new List<TreeInstance>();
        int treeCount = Mathf.RoundToInt(terrainData.size.x * terrainData.size.z * data.crowdDensity * 0.01f);

        // Use multiple noise layers for more natural distribution
        float perlinScale1 = noiseScale;
        float perlinScale2 = noiseScale * 2f;
        float perlinScale3 = noiseScale * 0.5f;

        int processedCount = 0;
        List<Vector3> validPositions = new List<Vector3>();

        // Phase 1: Find all valid positions
        for (int i = 0; i < treeCount; i++)
        {
            float normX = Random.Range(0.05f, 0.95f);
            float normZ = Random.Range(0.05f, 0.95f);
            float worldY = terrainData.GetInterpolatedHeight(normX, normZ);
            float slope = terrainData.GetSteepness(normX, normZ);

            if (slope > data.maxSlope || worldY <= 0.01f)
                continue;

            // Multi-layer noise for more natural distribution
            float perlin1 = Mathf.PerlinNoise(normX * perlinScale1, normZ * perlinScale1);
            float perlin2 = Mathf.PerlinNoise(normX * perlinScale2 + 1000f, normZ * perlinScale2 + 1000f);
            float perlin3 = Mathf.PerlinNoise(normX * perlinScale3 + 2000f, normZ * perlinScale3 + 2000f);

            float combinedNoise = (perlin1 + perlin2 + perlin3) / 3f;

            // Cluster effect - only spawn in areas above threshold
            if (combinedNoise > noiseThreshold && Random.value < clusterDensity)
            {
                Vector3 worldPos = new Vector3(
                    normX * terrainData.size.x,
                    worldY,
                    normZ * terrainData.size.z
                );
                validPositions.Add(worldPos);
            }

            processedCount++;
            if (processedCount % batchSize == 0 && generationSpeed < 1f)
                yield return null;
        }

        // Phase 2: Spawn trees at valid positions
        for (int i = 0; i < validPositions.Count; i++)
        {
            Vector3 worldPos = validPositions[i];
            Vector3 normPos = new Vector3(
                worldPos.x / terrainData.size.x,
                worldPos.y / terrainData.size.y,
                worldPos.z / terrainData.size.z
            );

            int protoIndex = Random.Range(0, data.crowdPrefabs.Length);
            TreeInstance tree = new TreeInstance
            {
                position = normPos,
                prototypeIndex = protoIndex,
                widthScale = Random.Range(data.minTreeWidth, data.maxTreeWidth),
                heightScale = Random.Range(data.minTreeHeight, data.maxTreeHeight),
                color = Color.Lerp(Color.white, data.healthyGrassColor, 0.3f),
                lightmapColor = Color.white
            };
            trees.Add(tree);

            // Instantiate with optimization
            GameObject prefab = data.crowdPrefabs[protoIndex];
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, worldPos, Quaternion.Euler(0, Random.Range(0, 360), 0), this.transform);

                // Optimization components
                if (enableLOD)
                {
                    var lodGroup = instance.GetComponent<LODGroup>();
                    if (lodGroup == null)
                    {
                        lodGroup = instance.AddComponent<LODGroup>();
                        // Simple LOD setup - in practice you'd want proper LOD meshes
                        lodGroup.SetLODs(new LOD[] { new LOD(0.5f, instance.GetComponentsInChildren<Renderer>()) });
                    }
                }

                var renderer = instance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    if (useGPUInstancing)
                        renderer.material.enableInstancing = true;
                }

                // Only add collider if needed for gameplay
                if (instance.GetComponent<Collider>() == null)
                {
                    var collider = instance.AddComponent<BoxCollider>();
                    collider.isTrigger = true; // More performance friendly
                }

                instance.transform.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
                spawnedObjects.Add(instance);
            }

            if (i % batchSize == 0 && generationSpeed < 1f)
                yield return null;
        }

        terrainData.treeInstances = trees.ToArray();
        Debug.Log($"🌲 Planted {trees.Count} trees (Density: {data.crowdDensity})");

        // Force cleanup
        Resources.UnloadUnusedAssets();
    }

    void SetupGrassPrototypes()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.grassTextures.Length == 0) return;

        // Use lower resolution for better performance
        terrainData.SetDetailResolution(grassResolution, grassDetailResolutionPerPatch);

        List<DetailPrototype> detailPrototypes = new List<DetailPrototype>();
        foreach (var texture in data.grassTextures)
        {
            if (texture != null)
            {
                detailPrototypes.Add(new DetailPrototype
                {
                    prototypeTexture = texture,
                    minWidth = data.minGrassWidth,
                    maxWidth = data.maxGrassWidth,
                    minHeight = data.minGrassHeight,
                    maxHeight = data.maxGrassHeight,
                    healthyColor = data.healthyGrassColor,
                    dryColor = data.dryGrassColor,
                    renderMode = DetailRenderMode.GrassBillboard,
                    usePrototypeMesh = false,
                    useInstancing = useGPUInstancing
                });
            }
        }

        terrainData.detailPrototypes = detailPrototypes.ToArray();
    }

    void SpawnGrassOptimized()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.grassTextures.Length == 0) return;

        // Use job system or compute shader in real implementation
        // Here we use optimized coroutine-like approach
        for (int layer = 0; layer < data.grassTextures.Length; layer++)
        {
            int[,] detailLayer = new int[grassResolution, grassResolution];

            // Multi-octave noise for natural distribution
            float perlinScale1 = noiseScale;
            float perlinScale2 = noiseScale * 1.5f;

            for (int y = 0; y < grassResolution; y++)
            {
                for (int x = 0; x < grassResolution; x++)
                {
                    float normX = x / (float)grassResolution;
                    float normZ = y / (float)grassResolution;

                    float slope = terrainData.GetSteepness(normX, normZ);
                    float height = terrainData.GetInterpolatedHeight(normX, normZ);

                    // Multi-layer noise for more natural grass distribution
                    float perlin1 = Mathf.PerlinNoise(normX * perlinScale1, normZ * perlinScale1);
                    float perlin2 = Mathf.PerlinNoise(normX * perlinScale2 + 500f, normZ * perlinScale2 + 500f);
                    float combinedNoise = (perlin1 + perlin2) / 2f;

                    if (slope < data.maxSlope && height > 0.01f && combinedNoise > noiseThreshold)
                    {
                        // Use noise-influenced probability for more natural distribution
                        float spawnProbability = data.grassDensity * combinedNoise;
                        if (Random.value <= spawnProbability)
                        {
                            detailLayer[y, x] = 1;
                        }
                    }
                }
            }

            terrainData.SetDetailLayer(0, 0, layer, detailLayer);
        }

        Debug.Log($"🌿 Grass spawned (Density: {data.grassDensity}, Resolution: {grassResolution})");
    }

    // Async method for better performance in large terrains
    public void RegenerateVegetation()
    {
        GenerateAllVegetation();
    }

    // Optimization: Only generate vegetation in player proximity
    public void GenerateVegetationInArea(Vector3 center, float radius)
    {
        // Implementation for streaming vegetation
        StartCoroutine(GenerateAreaVegetationCoroutine(center, radius));
    }

    private IEnumerator GenerateAreaVegetationCoroutine(Vector3 center, float radius)
    {
        // Area-based generation for open world streaming
        // This is a simplified version - implement based on your needs
        yield return null;
    }

    // Replace the OnApplicationPause method with the following:

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Reduce detail when app is backgrounded
            terrainData.SetDetailResolution(grassResolution / 2, grassDetailResolutionPerPatch);
        }
        else
        {
            // Restore detail when app is foregrounded
            terrainData.SetDetailResolution(grassResolution, grassDetailResolutionPerPatch);
        }
    }
}