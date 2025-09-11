using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Range(16, 1024)] public int grassResolution = 512;
    [Range(8, 32)] public int grassDetailResolutionPerPatch = 16;
    public bool autoGenerateOnStart = true;

    private Terrain terrain;
    private TerrainData terrainData;
    private BiomeType currentBiome;

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

    public void GenerateAllVegetation()
    {
        ClearExistingVegetation();
        SetupTreePrototypes();
        SpawnTrees();
        SetupGrassPrototypes();
        SpawnGrass();
    }

    public void ClearExistingVegetation()
    {
        terrainData.treePrototypes = new TreePrototype[0];
        terrainData.treeInstances = new TreeInstance[0];
        terrainData.detailPrototypes = new DetailPrototype[0];
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            terrainData.SetDetailLayer(0, 0, i, new int[grassResolution, grassResolution]);
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

    void SpawnTrees()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.crowdPrefabs.Length == 0) return;

        List<TreeInstance> trees = new List<TreeInstance>();
        int treeCount = Mathf.RoundToInt(terrainData.size.x * terrainData.size.z * data.crowdDensity * 0.01f);

        // Use Perlin noise to create natural dense/sparse patterns
        float perlinScale = 8f; // Lower = larger patches, Higher = more noise
        float perlinThreshold = 0.45f; // Controls overall sparseness

        for (int i = 0; i < treeCount; i++)
        {
            float normX = Random.Range(0.05f, 0.95f);
            float normZ = Random.Range(0.05f, 0.95f);
            float worldY = terrainData.GetInterpolatedHeight(normX, normZ);
            float slope = terrainData.GetSteepness(normX, normZ);
            if (slope > data.maxSlope || worldY <= 0.01f)
                continue;

            // Perlin noise for density pattern
            float perlin = Mathf.PerlinNoise(normX * perlinScale, normZ * perlinScale);
            if (perlin < perlinThreshold) continue; // Skip to make sparse areas

            int protoIndex = Random.Range(0, data.crowdPrefabs.Length);
            TreeInstance tree = new TreeInstance
            {
                position = new Vector3(normX, worldY / terrainData.size.y, normZ),
                prototypeIndex = protoIndex,
                widthScale = Random.Range(data.minTreeWidth, data.maxTreeWidth),
                heightScale = Random.Range(data.minTreeHeight, data.maxTreeHeight),
                color = Color.Lerp(Color.white, data.healthyGrassColor, 0.3f),
                lightmapColor = Color.white
            };

            trees.Add(tree);

            // Instantiate a GameObject with a collider at the tree's world position
            Vector3 worldPos = new Vector3(
                normX * terrainData.size.x,
                worldY,
                normZ * terrainData.size.z
            );
            GameObject prefab = data.crowdPrefabs[protoIndex];
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);
                if (instance.GetComponent<Collider>() == null)
                {
                    instance.AddComponent<BoxCollider>();
                }
                instance.transform.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            }
        }

        terrainData.treeInstances = trees.ToArray();
        Debug.Log($"🌲 Planted {trees.Count} trees (Density: {data.crowdDensity})");
    }

    void SetupGrassPrototypes()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.grassTextures.Length == 0) return;

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
                    usePrototypeMesh = false
                });
            }
        }

        terrainData.detailPrototypes = detailPrototypes.ToArray();
    }

    void SpawnGrass()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null || data.grassTextures.Length == 0) return;

        for (int layer = 0; layer < data.grassTextures.Length; layer++)
        {
            int[,] detailLayer = new int[grassResolution, grassResolution];

            float perlinScale = 8f;
            float perlinThreshold = 0.45f;

            for (int y = 0; y < grassResolution; y++)
            {
                for (int x = 0; x < grassResolution; x++)
                {
                    float normX = x / (float)grassResolution;
                    float normZ = y / (float)grassResolution;

                    float slope = terrainData.GetSteepness(normX, normZ);
                    float height = terrainData.GetInterpolatedHeight(normX, normZ);

                    // Perlin noise for grass density
                    float perlin = Mathf.PerlinNoise(normX * perlinScale, normZ * perlinScale);
                    if (slope < data.maxSlope && height > 0.01f && perlin > perlinThreshold)
                    {
                        if (Random.value <= data.grassDensity)
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

    public void RegenerateVegetation()
    {
        GenerateAllVegetation();
    }
}
