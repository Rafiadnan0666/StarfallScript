using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(TerrainCollider))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("Core Terrain Settings")]
    public int width = 1000;
    public int height = 1000;
    public int depth = 300;
    public float scale = 70f;
    [Range(0, 1)] public float waterLevel = 0.15f;
    public bool generateCollider = true;
    public Material waterMaterial;
    public int neighborTerrainCount = 2;

    [Header("Grass Placement Settings")]
    public float grassSpacing = 2f;
    public float maxSlope = 50f;
    public float minHeight = 0f;


    public BiomeType currentbiome;

    [Header("Mountain Settings")]
    public float mountainHeight = 1.2f;
    public float mountainScale = 0.008f;
    public int mountainCount = 7;
    public float mountainFalloff = 1.8f;
    public float mountainRoughness = 0.5f;
    public Gradient mountainColorGradient;

    [Header("Hill Settings")]
    public float hillHeight = 0.5f;
    public float hillScale = 0.03f;
    public float hillFrequency = 4f;
    public float hillSharpness = 2f;

    [Header("Valley Settings")]
    public float valleyDepth = 0.4f;
    public float valleyWidth = 0.2f;
    public int valleyCount = 3;

    [Header("River Settings")]
    public float riverWidth = 8f;
    public float riverDepth = 0.3f;
    public int riverCount = 2;
    public float riverCurviness = 0.7f;

    [Header("Path Settings")]
    public float pathWidth = 15f;
    public float pathDepth = 0.08f;
    public int pathCount = 4;
    public float pathSmoothness = 0.6f;

    [Header("Biome Settings")]
    public BiomeObjects[] biomeSpecificObjects;
    [Range(0, 1)] public float objectDensity = 0.15f;
    public float minObjectSpacing = 8f;
    public float crowdDensityMultiplier = 1.5f;
    public float crowdRadius = 20f;

    [Header("Environmental Effects")]
    public Gradient fogColorByBiome;
    public float fogDensity = 0.015f;
    public Color waterColor;
    public GameObject[] environmentParticles;
    public float windStrength = 0.5f;
    public Vector2 windDirection = Vector2.right;

    [Header("UI Elements")]
    public TextMeshProUGUI biomeInfoText;
    public GameObject[] objectivePrefabs;

    public Terrain terrain;
    private TerrainCollider terrainCollider;
    private PlanetSituation situation;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<Vector3> pathPoints = new List<Vector3>();
    private List<Terrain> neighborTerrains = new List<Terrain>();
    private GameObject waterPlane;
    private int seed;

    void Start()
    {
        seed = Random.Range(0, int.MaxValue);
        Random.InitState(seed);

        InitializeComponents();
        LoadPlanetData();
        GenerateTerrain();
        CreateWaterPlane();
        SpawnEnvironmentObjects();
        ApplyEnvironmentalEffects();
        UpdateBiomeInfo();
    }

    void InitializeComponents()
    {
        terrain = GetComponent<Terrain>();
        terrainCollider = GetComponent<TerrainCollider>();
        terrainCollider.enabled = generateCollider;
        terrain.materialTemplate = new Material(Shader.Find("Nature/Terrain/Standard"));
    }

    void LoadPlanetData()
    {
        if (PlanetManager.Instance != null)
        {
            situation = PlanetManager.Instance.currentSituation;
        }
        else
        {
            Debug.LogWarning("No PlanetManager found. Using default settings.");
            situation = new PlanetSituation
            {
                biome = (BiomeType)Random.Range(0, 4),
                gravity = Random.Range(7f, 12f),
                waveHeight = Random.Range(0.1f, 0.5f),
                grassDensity = Random.Range(0.7f, 1.3f),
                temperature = Random.Range(-20f, 40f),
                primaryObjective = (ObjectiveType)Random.Range(0, 1),
                minObjectives = Random.Range(1, 4),
                maxObjectives = Random.Range(2, 5),
                hasEarthquakes = Random.value > 0.7f,
                earthquakeIntensity = Random.Range(0.05f, 0.2f),
                waterColor = new Color(Random.Range(0.1f, 0.3f), Random.Range(0.3f, 0.6f), Random.Range(0.7f, 0.9f), Random.Range(0.6f, 0.8f))
            };
        }

        // Procedurally set parameters based on biome
        ProceduralParameterSetup();

        Debug.Log($"Loaded Planet Situation: {situation.biome}, Gravity: {situation.gravity}, Temperature: {situation.temperature}");
        currentbiome = situation.biome;
    }

    void ProceduralParameterSetup()
    {
        // Set parameters based on biome type for natural variation
        switch (situation.biome)
        {
            case BiomeType.Forest:
                waterLevel = Random.Range(0.12f, 0.18f);
                mountainHeight = Random.Range(0.8f, 1.4f);
                mountainCount = Random.Range(5, 9);
                hillHeight = Random.Range(0.4f, 0.7f);
                valleyDepth = Random.Range(0.3f, 0.5f);
                riverCount = Random.Range(1, 3);
                pathCount = Random.Range(3, 6);
                objectDensity = Random.Range(0.2f, 0.3f);
                break;

            case BiomeType.Desert:
                waterLevel = Random.Range(0.05f, 0.1f);
                mountainHeight = Random.Range(1.0f, 1.8f);
                mountainCount = Random.Range(3, 6);
                hillHeight = Random.Range(0.3f, 0.6f);
                valleyDepth = Random.Range(0.2f, 0.4f);
                riverCount = Random.Range(0, 1);
                pathCount = Random.Range(2, 4);
                objectDensity = Random.Range(0.1f, 0.2f);
                break;

            case BiomeType.Snowy:
                waterLevel = Random.Range(0.1f, 0.15f);
                mountainHeight = Random.Range(1.5f, 2.2f);
                mountainCount = Random.Range(6, 10);
                hillHeight = Random.Range(0.5f, 0.8f);
                valleyDepth = Random.Range(0.4f, 0.6f);
                riverCount = Random.Range(1, 2);
                pathCount = Random.Range(2, 4);
                objectDensity = Random.Range(0.08f, 0.15f);
                break;

            case BiomeType.Grassland:
                waterLevel = Random.Range(0.15f, 0.22f);
                mountainHeight = Random.Range(0.6f, 1.0f);
                mountainCount = Random.Range(2, 5);
                hillHeight = Random.Range(0.6f, 0.9f);
                valleyDepth = Random.Range(0.3f, 0.5f);
                riverCount = Random.Range(2, 4);
                pathCount = Random.Range(4, 7);
                objectDensity = Random.Range(0.25f, 0.4f);
                break;
        }

        // Add some random variation to all parameters
        scale = Random.Range(10f, 200f);
        mountainScale = Random.Range(0.005f, 0.12f);
        mountainFalloff = Random.Range(1.5f, 2.2f);
        mountainRoughness = Random.Range(0.03f, 0.07f);
        hillScale = Random.Range(0.02f, 200f);
        hillFrequency = Random.Range(3f, 5f);
        hillSharpness = Random.Range(1.5f, 2.5f);
        valleyWidth = Random.Range(0.15f, 0.25f);
        valleyCount = Random.Range(2, 5);
        riverWidth = Random.Range(6f, 12f);
        riverDepth = Random.Range(0.2f, 0.4f);
        riverCurviness = Random.Range(0.5f, 0.9f);
        pathWidth = Random.Range(12f, 18f);
        pathDepth = Random.Range(0.06f, 0.1f);
        pathSmoothness = Random.Range(0.5f, 0.7f);
        minObjectSpacing = Random.Range(6f, 10f);
        crowdDensityMultiplier = Random.Range(1.2f, 1.8f);
        crowdRadius = Random.Range(18f, 25f);
        fogDensity = Random.Range(0.0001f, 0.0002f);
        windStrength = Random.Range(0.3f, 0.7f);
        windDirection = Random.insideUnitCircle.normalized;
    }

    void GenerateTerrain()
    {
        BiomeObjects biome = GetCurrentBiome();

        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(width, depth, height);
        terrainData.heightmapResolution = Mathf.NextPowerOfTwo(width) + 1;

        float[,] heights = GenerateHeightmap(terrainData.heightmapResolution);
        terrainData.SetHeights(0, 0, heights);

        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;

        ApplyBiomeTextures(terrainData, biome);
        ApplyBiomeDetails(terrainData, biome);
    }

  
    void SpawnBiomeGrass(Terrain terrain, BiomeObjects biome)
    {
        if (biome.Grass == null)
        {
            Debug.LogWarning("Grass prefab is missing!");
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int xCount = Mathf.FloorToInt(data.size.x / grassSpacing);
        int zCount = Mathf.FloorToInt(data.size.z / grassSpacing);

        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                float normX = (x * grassSpacing) / data.size.x;
                float normZ = (z * grassSpacing) / data.size.z;

                float height = data.GetInterpolatedHeight(normX, normZ);
                float slope = data.GetSteepness(normX, normZ);

                if (slope > maxSlope || height < minHeight)
                    continue;

                Vector3 worldPos = new Vector3(
                    x * grassSpacing + terrainPos.x,
                    height + terrainPos.y,
                    z * grassSpacing + terrainPos.z
                );

                Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                GameObject grass = Instantiate(biome.Grass, worldPos, rot, terrain.transform);
                grass.transform.localScale *= Random.Range(0.9f, 1.1f);
            }
        }

        Debug.Log("🌿 Grass spawned across terrain.");
    }

    void GenerateNeighborTerrains()
    {
        BiomeObjects currentBiome = GetCurrentBiome();

        for (int i = 0; i < neighborTerrainCount; i++)
        {
            GameObject neighborObj = new GameObject($"NeighborTerrain_{i}");

            float xOffset = (i == 0) ? -width : width;
            Vector3 position = transform.position + new Vector3(xOffset, 0, 0);
            neighborObj.transform.position = position;

            Terrain neighborTerrain = neighborObj.AddComponent<Terrain>();
            TerrainCollider neighborCollider = neighborObj.AddComponent<TerrainCollider>();

            TerrainData neighborData = new TerrainData();
            neighborData.heightmapResolution = Mathf.NextPowerOfTwo(width) + 1;
            neighborData.size = new Vector3(width, depth, height);

            float[,] neighborHeights = GenerateHeightmap(neighborData.heightmapResolution);
            neighborData.SetHeights(0, 0, neighborHeights);

            ApplyBiomeTextures(neighborData, currentBiome);
            ApplyBiomeDetails(neighborData, currentBiome);

            neighborTerrain.terrainData = neighborData;
            neighborCollider.terrainData = neighborData;

            neighborTerrains.Add(neighborTerrain);
        }

        UpdateTerrainConnections();
    }

    void UpdateTerrainConnections()
    {
        if (neighborTerrains.Count == 2)
        {
            Terrain left = neighborTerrains[0];
            Terrain right = neighborTerrains[1];

            terrain.SetNeighbors(left, null, right, null);
            left.SetNeighbors(null, null, terrain, null);
            right.SetNeighbors(terrain, null, null, null);
        }
    }

    BiomeObjects GetCurrentBiome()
    {
        foreach (var biome in biomeSpecificObjects)
        {
            if (biome.biomeType == situation.biome)
                return biome;
        }
        return biomeSpecificObjects[0];
    }

    void CreateWaterPlane()
    {
        if (waterMaterial == null) return;

        Vector3 terrainSize = terrain.terrainData.size;

        waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "WaterPlane";
        waterPlane.transform.position = new Vector3(
            terrainSize.x / 2f,
            waterLevel * terrainSize.y,
            terrainSize.z / 2f
        );

        waterPlane.transform.localScale = new Vector3(terrainSize.x / 10f, 1, terrainSize.z / 10f);

        Renderer waterRenderer = waterPlane.GetComponent<Renderer>();
        waterRenderer.material = waterMaterial;
        waterRenderer.material.color = situation.waterColor;

        WaterController waterController = waterPlane.AddComponent<WaterController>();
        waterController.waveHeight = situation.waveHeight;
        waterController.waveSpeed = situation.waveSpeed;
    }

    float[,] GenerateHeightmap(int resolution)
    {
        float[,] heights = new float[resolution, resolution];
        pathPoints.Clear();

        // Base noise with multiple octaves for natural terrain
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float nx = x / (float)resolution * scale;
                float ny = y / (float)resolution * scale;

                // Multi-layered noise for natural terrain
                float baseNoise = FractalNoise(nx, ny, 4) * 0.4f;
                float detailNoise = Mathf.PerlinNoise(nx * 3f, ny * 3f) * 0.15f;
                float fineNoise = Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.08f;
                float ridgeNoise = GenerateRidgedNoise(nx, ny) * 0.1f;

                heights[x, y] = waterLevel + baseNoise + detailNoise + fineNoise + ridgeNoise;
            }
        }

        AddMountains(heights, resolution);
        AddHills(heights, resolution);
        AddValleysAndRivers(heights, resolution);

        for (int i = 0; i < pathCount; i++)
        {
            GeneratePath(heights, resolution);
        }

        heights = ApplyErosion(heights, resolution);
        heights = ApplyThermalErosion(heights, resolution, 5);

        NormalizeTerrain(heights, resolution);
        AddCliffBorder(heights, resolution);

        return heights;
    }

    float FractalNoise(float x, float y, int octaves)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            frequency *= 2f;
            amplitude *= 0.5f;
        }

        return value;
    }

    float GenerateRidgedNoise(float x, float y)
    {
        return 1f - Mathf.Abs(Mathf.PerlinNoise(x, y) * 2f - 1f);
    }

    void AddMountains(float[,] heights, int resolution)
    {
        for (int i = 0; i < mountainCount; i++)
        {
            Vector2 mountainCenter = new Vector2(
                Random.Range(0.1f, 0.9f) * resolution,
                Random.Range(0.1f, 0.9f) * resolution);

            float mountainHeightVariation = mountainHeight * Random.Range(0.8f, 1.2f);
            float mountainFalloffVariation = mountainFalloff * Random.Range(0.7f, 1.3f);

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), mountainCenter) / resolution;
                    float mountainEffect = mountainHeightVariation * Mathf.Exp(-Mathf.Pow(distance * 2f, mountainFalloffVariation));

                    // Multi-layered mountain noise for realistic shapes
                    float baseShape = Mathf.PerlinNoise(x * mountainScale, y * mountainScale);
                    float ridgeNoise = 1f - Mathf.Abs(Mathf.PerlinNoise(x * mountainScale * 0.7f, y * mountainScale * 0.7f) * 2f - 1f);
                    float detailNoise = Mathf.PerlinNoise(x * mountainScale * 3f, y * mountainScale * 3f) * mountainRoughness;
                    float crackNoise = GenerateCrackleNoise(x * mountainScale * 5f, y * mountainScale * 5f) * mountainRoughness * 0.3f;

                    heights[x, y] += mountainEffect * (baseShape * 0.5f + ridgeNoise * 0.3f + detailNoise * 0.15f + crackNoise * 0.05f);
                }
            }
        }
    }

    float GenerateCrackleNoise(float x, float y)
    {
        float noise = Mathf.PerlinNoise(x, y);
        return Mathf.Pow(noise, 3f);
    }

    void AddHills(float[,] heights, int resolution)
    {
        float scaleVariation = Random.Range(0.8f, 1.2f);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float nx = x / (float)resolution * scale * hillScale * scaleVariation;
                float ny = y / (float)resolution * scale * hillScale * scaleVariation;

                // Multi-frequency noise for natural hills
                float noise1 = Mathf.PerlinNoise(nx, ny);
                float noise2 = Mathf.PerlinNoise(nx * hillFrequency, ny * hillFrequency) * 0.5f;
                float noise3 = Mathf.PerlinNoise(nx * hillFrequency * 2f, ny * hillFrequency * 2f) * 0.25f;
                float noise4 = Mathf.PerlinNoise(nx * hillFrequency * 4f, ny * hillFrequency * 4f) * 0.125f;

                float ridgedNoise = 1f - Mathf.Abs(noise1 * 2f - 1f);

                float hillEffect = Mathf.Pow(noise1 * 0.3f + noise2 * 0.25f + noise3 * 0.2f + noise4 * 0.15f + ridgedNoise * 0.1f, hillSharpness);

                heights[x, y] += hillHeight * hillEffect;
            }
        }
    }

    void AddValleysAndRivers(float[,] heights, int resolution)
    {
        for (int i = 0; i < valleyCount; i++)
        {
            Vector2 valleyDirection = Random.insideUnitCircle.normalized;
            float valleyOffset = Random.Range(0f, 100f);

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float valleyPos = Vector2.Dot(pos, valleyDirection) * valleyWidth + valleyOffset;

                    // More natural valley shape with sine and cosine components
                    float valleyEffect = (Mathf.Sin(valleyPos) + 0.3f * Mathf.Cos(valleyPos * 0.5f)) * valleyDepth * 0.4f;

                    heights[x, y] -= Mathf.Abs(valleyEffect);
                }
            }
        }

        for (int i = 0; i < riverCount; i++)
        {
            GenerateRiver(heights, resolution);
        }
    }

    void GenerateRiver(float[,] heights, int resolution)
    {
        Vector2 start = new Vector2(
            Random.Range(0.1f, 0.9f) * resolution,
            Random.Range(0.1f, 0.9f) * resolution);

        Vector2 end = new Vector2(
            Random.Range(0.1f, 0.9f) * resolution,
            Random.Range(0.1f, 0.9f) * resolution);

        List<Vector2> riverPoints = new List<Vector2>();
        int segments = 30 + Mathf.RoundToInt(riverCurviness * 20f);

        // Generate more natural river path with multiple control points
        Vector2 control1 = Vector2.Lerp(start, end, 0.3f) + Random.insideUnitCircle * resolution * 0.15f;
        Vector2 control2 = Vector2.Lerp(start, end, 0.7f) + Random.insideUnitCircle * resolution * 0.15f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = CalculateCubicBezierPoint(t, start, control1, control2, end);

            // Add natural meandering
            float meander = Mathf.Sin(t * Mathf.PI * 2f) * riverCurviness * resolution * 0.08f;
            Vector2 perp = new Vector2(-(end - start).y, (end - start).x).normalized;
            point += perp * meander;

            riverPoints.Add(point);
        }

        // Carve river into terrain
        for (int i = 0; i < riverPoints.Count - 1; i++)
        {
            Vector2 current = riverPoints[i];
            Vector2 next = riverPoints[i + 1];

            Vector2 direction = (next - current).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            int steps = Mathf.CeilToInt(Vector2.Distance(current, next));
            for (int j = 0; j < steps; j++)
            {
                Vector2 pos = Vector2.Lerp(current, next, j / (float)steps);

                int widthSteps = Mathf.CeilToInt(riverWidth);
                for (int k = -widthSteps; k <= widthSteps; k++)
                {
                    Vector2 offset = perpendicular * k;
                    Vector2 samplePos = pos + offset;

                    if (samplePos.x >= 0 && samplePos.x < resolution && samplePos.y >= 0 && samplePos.y < resolution)
                    {
                        float distanceFromCenter = Mathf.Abs(k) / riverWidth;
                        // Natural river cross-section (parabolic)
                        float crossSection = 1f - Mathf.Pow(distanceFromCenter, 2f);
                        float smoothing = Mathf.SmoothStep(0, 1, crossSection);

                        int x = Mathf.RoundToInt(samplePos.x);
                        int y = Mathf.RoundToInt(samplePos.y);

                        float targetHeight = waterLevel - riverDepth * smoothing;
                        heights[x, y] = Mathf.Min(heights[x, y], targetHeight);

                        // Add river banks
                        if (distanceFromCenter > 0.8f && distanceFromCenter < 1f)
                        {
                            float bankHeight = waterLevel + riverDepth * 0.2f * (distanceFromCenter - 0.8f) / 0.2f;
                            heights[x, y] = Mathf.Max(heights[x, y], bankHeight);
                        }
                    }
                }
            }
        }
    }

    Vector2 CalculateCubicBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return uuu * p0 +
               3 * uu * t * p1 +
               3 * u * tt * p2 +
               ttt * p3;
    }

    void ApplyBiomeTextures(TerrainData terrainData, BiomeObjects biome)
    {
        List<TerrainLayer> layers = new List<TerrainLayer>();

        // Base layer
        TerrainLayer baseLayer = new TerrainLayer();
        baseLayer.diffuseTexture = biome.groundTexture;
        baseLayer.tileSize = new Vector2(15, 15);
        baseLayer.tileOffset = new Vector2(Random.Range(0f, 5f), Random.Range(0f, 5f));
        layers.Add(baseLayer);

        // Mountain layer
        TerrainLayer mountainLayer = new TerrainLayer();
        mountainLayer.diffuseTexture = biome.mountainTexture;
        mountainLayer.tileSize = new Vector2(20, 20);
        mountainLayer.tileOffset = new Vector2(Random.Range(0f, 5f), Random.Range(0f, 5f));
        layers.Add(mountainLayer);

        terrainData.terrainLayers = layers.ToArray();
        SetupTextureSplatmap(terrainData, biome);
    }

    void SetupTextureSplatmap(TerrainData terrainData, BiomeObjects biome)
    {
        float[,,] splatmapData = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, 2];

        for (int y = 0; y < terrainData.alphamapResolution; y++)
        {
            for (int x = 0; x < terrainData.alphamapResolution; x++)
            {
                float y_01 = (float)y / (terrainData.alphamapResolution - 1);
                float x_01 = (float)x / (terrainData.alphamapResolution - 1);

                float steepness = terrainData.GetSteepness(y_01, x_01);
                float height = terrainData.GetHeight(y, x) / terrainData.size.y;

                // Base texture everywhere
                splatmapData[x, y, 0] = 1f;

                // Mountain texture on steep areas
                float mountainBlend = Mathf.Clamp01((steepness - 30f) / 15f);
                // Also blend based on height
                float heightBlend = Mathf.Clamp01((height - 0.6f) / 0.3f);

                float finalMountainBlend = Mathf.Max(mountainBlend, heightBlend);

                splatmapData[x, y, 1] = finalMountainBlend;
                splatmapData[x, y, 0] = 1f - finalMountainBlend;
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    void ApplyBiomeDetails(TerrainData terrainData, BiomeObjects biome)
    {
        int resolution = 1024;
        int resolutionPerPatch = 16;
        terrainData.SetDetailResolution(resolution, resolutionPerPatch);

        // Setup grass prototype
        DetailPrototype detailPrototype = new DetailPrototype
        {
            prototypeTexture = biome.grassTexture,
            healthyColor = biome.grassHealthyColor,
            dryColor = biome.grassDryColor,
            minWidth = 1.0f + Random.Range(-0.2f, 0.2f),
            maxWidth = 2.0f + Random.Range(-0.3f, 0.3f),
            minHeight = 1.0f + Random.Range(-0.2f, 0.2f),
            maxHeight = 2.0f + Random.Range(-0.3f, 0.3f),
            noiseSpread = 0.15f + Random.Range(-0.05f, 0.05f),
            renderMode = DetailRenderMode.GrassBillboard
        };

        terrainData.detailPrototypes = new DetailPrototype[] { detailPrototype };

        int[,] detailLayer = new int[resolution, resolution];

        float spacingFactor = 0.1f;
        float steepnessThreshold = 40f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normX = x / (float)resolution;
                float normY = y / (float)resolution;

                float steepness = terrainData.GetSteepness(normX, normY);
                float height = terrainData.GetInterpolatedHeight(normX, normY) / terrainData.size.y;

                bool valid = steepness < steepnessThreshold && height > waterLevel + 0.02f;

                if (valid)
                {
                    // Add some randomness to grass placement
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    if (noise > 0.3f)
                    {
                        detailLayer[y, x] = Mathf.RoundToInt(situation.grassDensity * spacingFactor * 16f * noise);
                    }
                }
                else
                {
                    detailLayer[y, x] = 0;
                }
            }
        }

        terrainData.SetDetailLayer(0, 0, 0, detailLayer);
        Debug.Log("🌱 Grass detail layer applied.");
    }

    void SpawnEnvironmentObjects()
    {
        ClearExistingObjects();

        foreach (var biomeObj in biomeSpecificObjects)
        {
            if (biomeObj.biomeType == situation.biome)
            {
                SpawnObjectGroup(biomeObj.objects, GetBiomeDensity());

                if (biomeObj.supportsCrowds)
                {
                    SpawnCrowdClusters(biomeObj.crowdObjects, GetBiomeDensity() * crowdDensityMultiplier * 0.3f, biomeObj.Grass);
                }
            }
        }

        SpawnObjectivesAlongPaths();
    }

    void SpawnCrowdGrass(float density, GameObject grassPrefab, List<Vector3> clusterCenters)
    {
        if (grassPrefab == null || clusterCenters == null || clusterCenters.Count == 0)
            return;

        List<Vector3> usedPositions = new List<Vector3>();

        foreach (Vector3 clusterCenter in clusterCenters)
        {
            int crowdSize = Random.Range(3, 8);
            for (int j = 0; j < crowdSize; j++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.2f;
                offset.y = 0;

                Vector3 spawnPos = clusterCenter + offset;
                spawnPos.y = terrain.SampleHeight(spawnPos);

                if (!IsPositionValid(spawnPos, clusterCenters) || !IsPositionValid(spawnPos, usedPositions))
                    continue;

                usedPositions.Add(spawnPos);

                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                Instantiate(grassPrefab, spawnPos, rotation);
            }
        }
    }

    void SpawnCrowdClusters(GameObject[] crowdPrefabs, float density, GameObject grassPrefab)
    {
        if (crowdPrefabs == null || crowdPrefabs.Length == 0 || grassPrefab == null)
            return;

        int clusterCount = Mathf.RoundToInt(density * 5f);
        List<Vector3> clusterCenters = new List<Vector3>();
        List<Vector3> usedPositions = new List<Vector3>();

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 clusterCenter = GetValidSpawnPosition(clusterCenters);
            if (clusterCenter == Vector3.zero) continue;

            clusterCenters.Add(clusterCenter);

            int crowdSize = Random.Range(3, 8);
            for (int j = 0; j < crowdSize; j++)
            {
                Vector3 offset = Random.insideUnitSphere * crowdRadius;
                offset.y = 0;

                Vector3 spawnPos = clusterCenter + offset;
                spawnPos.y = terrain.SampleHeight(spawnPos);

                if (!IsPositionValid(spawnPos, clusterCenters) || !IsPositionValid(spawnPos, usedPositions))
                    continue;

                usedPositions.Add(spawnPos);

                Quaternion rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Instantiate Grass
                Instantiate(grassPrefab, spawnPos, rotation);

                // Instantiate Crowd Object
                GameObject selectedPrefab = crowdPrefabs[Random.Range(0, crowdPrefabs.Length)];
                GameObject entity = Instantiate(selectedPrefab, spawnPos, rotation);
                entity.transform.localScale = Vector3.one * Random.Range(0.8f, 1.0f);

                spawnedObjects.Add(entity);
            }
        }

        SpawnCrowdGrass(density * 0.6f, grassPrefab, clusterCenters);
    }

    void ClearExistingObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    void ApplyEnvironmentalEffects()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColorByBiome.Evaluate((float)situation.biome / 3f);
        RenderSettings.fogDensity = fogDensity * Random.Range(0.8f, 1.2f);

        if (waterPlane != null)
        {
            waterPlane.GetComponent<Renderer>().material.color = situation.waterColor;
        }

        if (environmentParticles.Length > 0)
        {
            GameObject particles = Instantiate(
                environmentParticles[Random.Range(0, environmentParticles.Length)],
                Vector3.zero,
                Quaternion.identity
            );
            spawnedObjects.Add(particles);

            if (situation.biome == BiomeType.Snowy)
            {
                var particleSystem = particles.GetComponent<ParticleSystem>();
                var main = particleSystem.main;
                main.simulationSpeed = 0.7f;
            }
        }

        // Add wind zone
        GameObject windZoneObj = new GameObject("WindZone");
        WindZone windZone = windZoneObj.AddComponent<WindZone>();
        windZone.mode = WindZoneMode.Directional;
        windZone.windMain = windStrength;
        windZone.windTurbulence = windStrength * 0.5f;
        windZone.windPulseMagnitude = windStrength * 0.3f;
        windZone.windPulseFrequency = 0.1f;
        windZone.transform.rotation = Quaternion.LookRotation(new Vector3(windDirection.x, 0, windDirection.y));
        spawnedObjects.Add(windZoneObj);
    }

    float[,] ApplyErosion(float[,] heights, int resolution)
    {
        float[,] eroded = new float[resolution, resolution];
        System.Array.Copy(heights, eroded, heights.Length);

        // Biome-specific erosion
        float erosionStrength = 0.35f;
        float erosionDetail = 0.12f;
        float erosionVariation = 0.2f;

        switch (situation.biome)
        {
            case BiomeType.Desert:
                erosionStrength = 0.6f;
                erosionDetail = 0.25f;
                erosionVariation = 0.3f;
                break;
            case BiomeType.Snowy:
                erosionStrength = 0.25f;
                erosionDetail = 0.08f;
                erosionVariation = 0.15f;
                break;
            case BiomeType.Forest:
                erosionStrength = 0.45f;
                erosionDetail = 0.18f;
                erosionVariation = 0.25f;
                break;
        }

        for (int x = 1; x < resolution - 1; x++)
        {
            for (int y = 1; y < resolution - 1; y++)
            {
                float avg = (heights[x, y] + heights[x + 1, y] + heights[x - 1, y] +
                           heights[x, y + 1] + heights[x, y - 1]) / 5f;

                float detailX = x * erosionDetail + Random.Range(-100f, 100f);
                float detailY = y * erosionDetail + Random.Range(-100f, 100f);
                float detail = Mathf.PerlinNoise(detailX, detailY) * erosionVariation;

                eroded[x, y] = Mathf.Lerp(heights[x, y], avg, erosionStrength) + detail * 0.1f;
            }
        }

        return eroded;
    }

    float[,] ApplyThermalErosion(float[,] heights, int resolution, int iterations)
    {
        float[,] result = new float[resolution, resolution];
        System.Array.Copy(heights, result, heights.Length);

        float talusAngle = 0.1f;
        float materialTransfer = 0.2f;

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int x = 1; x < resolution - 1; x++)
            {
                for (int y = 1; y < resolution - 1; y++)
                {
                    float h = result[x, y];
                    float maxDiff = 0;
                    int maxX = x, maxY = y;

                    // Find the steepest descent
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = x + dx;
                            int ny = y + dy;
                            float diff = h - result[nx, ny];

                            if (diff > maxDiff)
                            {
                                maxDiff = diff;
                                maxX = nx;
                                maxY = ny;
                            }
                        }
                    }

                    // Transfer material if slope is too steep
                    if (maxDiff > talusAngle)
                    {
                        float transfer = maxDiff * materialTransfer * 0.5f;
                        result[x, y] -= transfer;
                        result[maxX, maxY] += transfer;
                    }
                }
            }
        }

        return result;
    }

    void SpawnObjectGroup(GameObject[] prefabs, float density)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int objectCount = Mathf.RoundToInt(width * height * 0.0001f * density * Random.Range(0.8f, 1.2f));
        List<Vector3> spawnedPositions = new List<Vector3>();

        for (int i = 0; i < objectCount; i++)
        {
            Vector3 position = GetRandomTerrainPosition();
            position.y = terrain.SampleHeight(position);

            if (!IsPositionValid(position, spawnedPositions)) continue;

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float scaleVariation = Random.Range(0.8f, 20f);
            Vector3 scale = Vector3.one * scaleVariation;

            GameObject obj = Instantiate(prefab, position, rotation);
            obj.transform.localScale = scale;

            obj.transform.position += Vector3.up * 0.1f;

            if (obj.GetComponent<Collider>() == null)
                obj.AddComponent<BoxCollider>();

            spawnedObjects.Add(obj);
            spawnedPositions.Add(position);
        }
    }

    void SpawnObjectivesAlongPaths()
    {
        if (pathPoints.Count < 2 || objectivePrefabs.Length == 0) return;

        int objectiveCount = Random.Range(situation.minObjectives, situation.maxObjectives + 1);
        float pathLength = CalculatePathLength();

        for (int i = 0; i < objectiveCount; i++)
        {
            float distanceAlongPath = (i + 1) * (pathLength / (objectiveCount + 1));
            Vector3 position = GetPointAlongPath(distanceAlongPath);

            if (position != Vector3.zero)
            {
                GameObject prefab = objectivePrefabs[Random.Range(0, objectivePrefabs.Length)];
                Objective objective = Instantiate(prefab, position, Quaternion.identity).GetComponent<Objective>();

                if (objective != null)
                {
                    //objective.objectiveType = (i == 0) ?
                    //    (Objective.ObjectiveType)(int)situation.primaryObjective :
                    //    (Objective.ObjectiveType)Random.Range(0, System.Enum.GetValues(typeof(Objective.ObjectiveType)).Length);

                    objective.transform.localScale = Vector3.one * 1.3f;
                    spawnedObjects.Add(objective.gameObject);
                }
            }
        }
    }

    float CalculatePathLength()
    {
        float length = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            length += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
        }
        return length;
    }

    Vector3 GetPointAlongPath(float targetDistance)
    {
        float accumulatedDistance = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            if (accumulatedDistance + segmentLength >= targetDistance)
            {
                float t = (targetDistance - accumulatedDistance) / segmentLength;
                Vector3 position = Vector3.Lerp(pathPoints[i], pathPoints[i + 1], t);
                position.y = terrain.SampleHeight(position) + 0.5f;
                return position;
            }
            accumulatedDistance += segmentLength;
        }
        return Vector3.zero;
    }

    Vector3 GetValidSpawnPosition(List<Vector3> existingPositions)
    {
        int attempts = 0;
        while (attempts < 100)
        {
            Vector3 position = GetRandomTerrainPosition();

            if (IsPositionValid(position, existingPositions))
                return position;

            attempts++;
        }
        return Vector3.zero;
    }

    bool IsPositionValid(Vector3 position, List<Vector3> existingPositions)
    {
        // Check water level
        if (position.y < waterLevel * depth + 0.1f) return false;

        // Check slope
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
            position.x / terrain.terrainData.size.x,
            position.z / terrain.terrainData.size.z);
        if (Vector3.Angle(normal, Vector3.up) > 35f) return false;

        // Check spacing
        foreach (var existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minObjectSpacing)
                return false;
        }

        return true;
    }

    float GetBiomeDensity()
    {
        float density = objectDensity;

        switch (situation.biome)
        {
            case BiomeType.Forest:
                density *= situation.treeDensity * Random.Range(0.9f, 1.1f);
                break;
            case BiomeType.Desert:
                density *= situation.rockDensity * Random.Range(0.8f, 1.2f);
                break;
            case BiomeType.Snowy:
                density *= situation.rockDensity * 0.6f * Random.Range(0.7f, 1.3f);
                break;
            case BiomeType.Grassland:
                density *= situation.grassDensity * Random.Range(0.9f, 1.1f);
                break;
        }

        return Mathf.Clamp(density, 0.05f, 2f);
    }

    Vector3 GetRandomTerrainPosition()
    {
        Vector3 size = terrain.terrainData.size;
        float x = Random.Range(0, size.x);
        float z = Random.Range(0, size.z);
        float y = terrain.SampleHeight(new Vector3(x, 0, z));
        return new Vector3(x, y, z);
    }

    void UpdateBiomeInfo()
    {
        biomeInfoText = FindAnyObjectByType<Player>().info;
        if (biomeInfoText != null)
        {
            biomeInfoText.text = $"<b>Biome:</b> {situation.biome}\n" +
                               $"<b>Temperature:</b> {situation.temperature}°C\n" +
                               $"<b>Gravity:</b> {situation.gravity}g\n" +
                               $"<b>Features:</b> {GetTerrainFeatures()}\n" +
                               $"<b>Objectives:</b> {situation.primaryObjective}";
        }
    }

    string GetTerrainFeatures()
    {
        List<string> features = new List<string>();

        if (situation.hasEarthquakes) features.Add("Earthquakes");
        if (mountainCount > 0) features.Add($"Mountains x{mountainCount}");
        if (pathCount > 0) features.Add($"Paths x{pathCount}");
        if (riverCount > 0) features.Add($"Rivers x{riverCount}");
        if (waterLevel > 0) features.Add("Water Bodies");
        features.Add("Natural Erosion");

        return string.Join(", ", features);
    }

    void OnDestroy()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
    }

    void NormalizeTerrain(float[,] heights, int resolution)
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        // Find min and max heights
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                if (heights[x, y] < minHeight) minHeight = heights[x, y];
                if (heights[x, y] > maxHeight) maxHeight = heights[x, y];
            }
        }

        // Normalize while keeping water level constant
        float waterHeight = waterLevel * depth;
        float terrainRange = maxHeight - minHeight;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float normalized = (heights[x, y] - minHeight) / terrainRange;

                // Preserve water areas
                if (heights[x, y] <= waterLevel)
                {
                    heights[x, y] = waterLevel;
                }
                else
                {
                    heights[x, y] = waterLevel + (normalized * (1 - waterLevel));
                }
            }
        }
    }

    void GeneratePath(float[,] heights, int resolution)
    {
        Vector3 size = terrain.terrainData.size;

        Vector2 start = new Vector2(
            Random.Range(0.1f, 0.9f) * size.x,
            Random.Range(0.1f, 0.9f) * size.z
        );
        Vector2 end = new Vector2(
            Random.Range(0.1f, 0.9f) * size.x,
            Random.Range(0.1f, 0.9f) * size.z
        );

        Vector2 control1 = Vector2.Lerp(start, end, 0.33f) + Random.insideUnitCircle * size.x * 0.2f;
        Vector2 control2 = Vector2.Lerp(start, end, 0.66f) + Random.insideUnitCircle * size.x * 0.2f;

        int pathSegments = 50;

        for (int i = 0; i < pathSegments - 1; i++)
        {
            float t1 = i / (float)pathSegments;
            float t2 = (i + 1) / (float)pathSegments;

            Vector2 current = CalculateCubicBezierPoint(t1, start, control1, control2, end);
            Vector2 next = CalculateCubicBezierPoint(t2, start, control1, control2, end);
            Vector2 dir = (next - current).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            int steps = Mathf.CeilToInt(Vector2.Distance(current, next));
            for (int j = 0; j <= steps; j++)
            {
                Vector2 pos = Vector2.Lerp(current, next, j / (float)steps);

                int widthSteps = Mathf.CeilToInt(pathWidth * 0.5f);
                for (int k = -widthSteps; k <= widthSteps; k++)
                {
                    Vector2 offset = perp * k;
                    Vector2 samplePos = pos + offset;

                    int x = Mathf.RoundToInt(samplePos.x / size.x * resolution);
                    int y = Mathf.RoundToInt(samplePos.y / size.z * resolution);

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        float distFromCenter = Mathf.Abs(k) / (float)widthSteps;
                        float smoothing = Mathf.SmoothStep(0f, 1f, 1f - distFromCenter);
                        float targetHeight = waterLevel + pathDepth * smoothing;

                        heights[x, y] = Mathf.Lerp(heights[x, y], targetHeight, pathSmoothness);
                    }
                }
            }

            // Store pathPoint every 5 steps for objectives only
            if (i % 5 == 0)
            {
                Vector3 mid = new Vector3(current.x, 0, current.y);
                pathPoints.Add(mid);
            }
        }
    }

    void AddCliffBorder(float[,] heights, int resolution, float cliffHeight = 0.95f, float borderWidth = 0.04f, float noiseScale = 0.15f)
    {
        int border = Mathf.RoundToInt(resolution * borderWidth);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                // Distance to closest edge
                int minDist = Mathf.Min(x, y, resolution - 1 - x, resolution - 1 - y);
                if (minDist < border)
                {
                    float t = 1f - (minDist / (float)border);
                    // Add Perlin noise for natural cliff shape
                    float noise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale) * 0.15f;
                    float targetHeight = Mathf.Lerp(heights[x, y], cliffHeight + noise, t * t);
                    heights[x, y] = Mathf.Max(heights[x, y], targetHeight);
                }
            }
        }
    }
}

[System.Serializable]
public class BiomeObjects
{
    public BiomeType biomeType;
    public Texture2D groundTexture;
    public Texture2D mountainTexture;
    public Texture2D grassTexture;
    public Color grassHealthyColor = Color.green;
    public Color grassDryColor = Color.yellow;
    public GameObject[] objects;
    public GameObject[] crowdObjects;
    public bool supportsCrowds = true;
    public GameObject Grass;
}

public enum BiomeType
{
    Forest,
    Desert,
    Snowy,
    Grassland
}

public class WaterController : MonoBehaviour
{
    public float waveHeight = 0.3f;
    public float waveSpeed = 0.5f;

    private Material waterMaterial;
    private float waveOffset;

    void Start()
    {
        waterMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        waveOffset += Time.deltaTime * waveSpeed;
        waterMaterial.SetFloat("_WaveOffset", waveOffset);
        waterMaterial.SetFloat("_WaveHeight", waveHeight);
    }
}


