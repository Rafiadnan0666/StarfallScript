using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Terrain))]
public class TextureAndSmallController : MonoBehaviour
{
    [System.Serializable]
    public class BiomeDetail
    {
        public BiomeType biomeType;
        public Texture2D[] baseTextures;
        public Texture2D[] steepTextures;
        public Texture2D[] pathTextures;
        public GameObject[] smallDebrisObjects;
        public GameObject[] cliffRockObjects; 

        [Range(0f, 60f)] public float steepnessThreshold = 30f;
        [Range(0f, 1f)] public float placementDensity = 0.05f;
        [Range(0f, 1f)] public float cliffRockDensity = 0.02f; 
        [Range(0.1f, 2f)] public float cliffEmbedDepth = 0.7f; 
    }

    public BiomeDetail[] biomeDetails;

    private Terrain terrain;
    private TerrainData terrainData;
    private TerrainGenerator terrainGenerator;
    private BiomeType currentBiome;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        terrainGenerator = GetComponent<TerrainGenerator>();
        currentBiome = terrainGenerator.currentBiome;

        ApplyBiomeTextures();
        SpawnSmallDebris();
        SpawnCliffRocks();
    }

    void ApplyBiomeTextures()
    {
        BiomeDetail biome = GetBiomeDetail(currentBiome);
        if (biome == null)
        {
            Debug.LogWarning("⚠️ No matching biome detail found.");
            return;
        }

        List<Texture2D> allTextures = new List<Texture2D>();
        allTextures.AddRange(biome.baseTextures);
        allTextures.AddRange(biome.steepTextures);
        allTextures.AddRange(biome.pathTextures);

        int maxLayers = Mathf.Min(allTextures.Count, 8);
        TerrainLayer[] layers = new TerrainLayer[maxLayers];

        for (int i = 0; i < maxLayers; i++)
        {
            TerrainLayer layer = new TerrainLayer
            {
                diffuseTexture = allTextures[i],
                tileSize = new Vector2(10, 10)
            };
            layers[i] = layer;
        }

        terrainData.terrainLayers = layers;

        int mapSize = terrainData.alphamapResolution;
        float[,,] splatmap = new float[mapSize, mapSize, maxLayers];
        float[] weights = new float[maxLayers];

        int baseCount = Mathf.Min(biome.baseTextures.Length, maxLayers);
        int steepCount = Mathf.Min(biome.steepTextures.Length, maxLayers - baseCount);
        int pathCount = Mathf.Min(biome.pathTextures.Length, maxLayers - baseCount - steepCount);

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float normX = x / (float)(mapSize - 1);
                float normY = y / (float)(mapSize - 1);
                float steep = terrainData.GetSteepness(normX, normY);
                float height = terrainData.GetInterpolatedHeight(normX, normY) / terrainData.size.y;

                System.Array.Clear(weights, 0, maxLayers);

                if (steep > biome.steepnessThreshold && steepCount > 0)
                {
                    weights[baseCount + Random.Range(0, steepCount)] = 1f;
                }
                else if (height < 0.25f && pathCount > 0)
                {
                    weights[baseCount + steepCount + Random.Range(0, pathCount)] = 1f;
                }
                else if (baseCount > 0)
                {
                    weights[Random.Range(0, baseCount)] = 1f;
                }

                Normalize(weights);

                for (int i = 0; i < maxLayers; i++)
                {
                    splatmap[x, y, i] = weights[i];
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("✅ Terrain textures applied.");
    }

    void SpawnSmallDebris()
    {
        BiomeDetail biome = GetBiomeDetail(currentBiome);
        if (biome == null || biome.smallDebrisObjects.Length == 0) return;

        int maxDebris = Mathf.Min(150, Mathf.RoundToInt(terrainData.size.x * terrainData.size.z * biome.placementDensity));

        for (int i = 0; i < maxDebris; i++)
        {
            Vector3 pos = GetRandomTerrainPosition();
            float normX = pos.x / terrainData.size.x;
            float normZ = pos.z / terrainData.size.z;
            float slope = terrainData.GetSteepness(normX, normZ);

            if (slope > biome.steepnessThreshold) continue;

            GameObject prefab = biome.smallDebrisObjects[Random.Range(0, biome.smallDebrisObjects.Length)];
            if (!prefab)
            {
                continue;
            }

            Vector3 spawnPos = new Vector3(pos.x, terrain.SampleHeight(pos), pos.z);
            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject obj = Instantiate(prefab, spawnPos, rot, transform);
            obj.isStatic = true;
        }

        Debug.Log("🪨 Debris placed.");
    }

    void SpawnCliffRocks()
    {
        BiomeDetail biome = GetBiomeDetail(currentBiome);
        if (biome == null || biome.cliffRockObjects == null || biome.cliffRockObjects.Length == 0) return;

        int maxCliffRocks = Mathf.Min(100, Mathf.RoundToInt(terrainData.size.x * terrainData.size.z * biome.cliffRockDensity));

        for (int i = 0; i < maxCliffRocks; i++)
        {
            Vector3 pos = GetRandomTerrainPosition();
            float normX = pos.x / terrainData.size.x;
            float normZ = pos.z / terrainData.size.z;
            float slope = terrainData.GetSteepness(normX, normZ);

            if (slope < biome.steepnessThreshold) continue; // Only on steep slopes

            GameObject prefab = biome.cliffRockObjects[Random.Range(0, biome.cliffRockObjects.Length)];
            if (!prefab) continue;

            float y = terrain.SampleHeight(pos);
            float embed = biome.cliffEmbedDepth * prefab.transform.localScale.y;
            Vector3 spawnPos = new Vector3(pos.x, y - embed, pos.z); // Embed the rock
            Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject obj = Instantiate(prefab, spawnPos, rot, transform);
            obj.isStatic = true;
        }

        Debug.Log("🪨 Cliff rocks placed.");
    }

    BiomeDetail GetBiomeDetail(BiomeType type)
    {
        return System.Array.Find(biomeDetails, b => b.biomeType == type);
    }

    Vector3 GetRandomTerrainPosition()
    {
        float x = Random.Range(0f, terrainData.size.x);
        float z = Random.Range(0f, terrainData.size.z);
        return new Vector3(x, 0f, z);
    }

    void Normalize(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++) total += weights[i];
        if (total > 0f)
        {
            for (int i = 0; i < weights.Length; i++) weights[i] /= total;
        }
    }
}
