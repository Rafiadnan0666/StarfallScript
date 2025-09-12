using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Terrain))]
public class TerrainCrowdControl : MonoBehaviour
{
    [System.Serializable]
    public class CrowdBiomeData
    {
        [Header("Biome Settings")]
        public BiomeType biomeType;

        [Header("Crowd Distribution")]
        [Range(0, 1)] public float crowdDensity = 0.3f;
        [Range(0, 90)] public float maxSlope = 30f;

        [Header("Element Ratios")]
        [Range(0, 1)] public float vegetationRatio = 0.6f;
        [Range(0, 1)] public float rockRatio = 0.2f;
        [Range(0, 1)] public float crystalRatio = 0.1f;
        [Range(0, 1)] public float tendrilRatio = 0.05f;
        [Range(0, 1)] public float fogRatio = 0.05f;

        [Header("Size Variation")]
        [Range(0.5f, 3f)] public float minElementWidth = 0.8f;
        [Range(0.5f, 3f)] public float maxElementWidth = 1.5f;
        [Range(0.5f, 3f)] public float minElementHeight = 0.8f;
        [Range(0.5f, 3f)] public float maxElementHeight = 1.4f;

        [Header("Color Settings")]
        public Color healthyColor = Color.green;
        public Color dryColor = Color.yellow;
        public Color glowColor = new Color(0.5f, 0.8f, 1f, 0.7f);
        [Range(0, 1)] public float glowProbability = 0.2f;
    }

    [Header("Procedural Generation Settings")]
    public List<CrowdBiomeData> biomeCrowds;
    [Range(16, 1024)] public int detailResolution = 512;
    [Range(8, 32)] public int detailResolutionPerPatch = 16;
    public bool autoGenerateOnStart = true;

    [Header("Advanced Settings")]
    public Material glowMaterial;
    public Shader proceduralShader;
    [Range(0.1f, 5f)] public float noiseScale = 2f;

    private Terrain terrain;
    private TerrainData terrainData;
    private BiomeType currentBiome;
    private Dictionary<Vector3, GameObject> spawnedElements = new Dictionary<Vector3, GameObject>();

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
        SpawnCrowdElements();
    }

    public void ClearExistingVegetation()
    {
        // Clear existing tree instances
        terrainData.treeInstances = new TreeInstance[0];

        // Clear spawned game objects
        foreach (var element in spawnedElements.Values)
        {
            if (element != null)
                DestroyImmediate(element);
        }
        spawnedElements.Clear();

        Debug.Log("♻️ Cleared all existing vegetation");
    }

    void SpawnCrowdElements()
    {
        CrowdBiomeData data = biomeCrowds.Find(b => b.biomeType == currentBiome);
        if (data == null) return;

        int elementCount = Mathf.RoundToInt(terrainData.size.x * terrainData.size.z * data.crowdDensity * 0.01f);

        // Calculate element type counts based on ratios
        int vegetationCount = Mathf.RoundToInt(elementCount * data.vegetationRatio);
        int rockCount = Mathf.RoundToInt(elementCount * data.rockRatio);
        int crystalCount = Mathf.RoundToInt(elementCount * data.crystalRatio);
        int tendrilCount = Mathf.RoundToInt(elementCount * data.tendrilRatio);
        int fogCount = Mathf.RoundToInt(elementCount * data.fogRatio);

        // Use different noise patterns for each element type
        float vegetationNoiseScale = noiseScale * 0.8f;
        float rockNoiseScale = noiseScale * 1.2f;
        float crystalNoiseScale = noiseScale * 1.5f;
        float tendrilNoiseScale = noiseScale * 0.5f;
        float fogNoiseScale = noiseScale * 2f;

        // Spawn different element types
        SpawnElementType(vegetationCount, CrowdElementType.Vegetation, vegetationNoiseScale, data);
        SpawnElementType(rockCount, CrowdElementType.Rock, rockNoiseScale, data);
        SpawnElementType(crystalCount, CrowdElementType.Crystal, crystalNoiseScale, data);
        SpawnElementType(tendrilCount, CrowdElementType.Tendril, tendrilNoiseScale, data);
        SpawnElementType(fogCount, CrowdElementType.Fog, fogNoiseScale, data);

        Debug.Log($"🌿 Spawned {vegetationCount} vegetation, {rockCount} rocks, {crystalCount} crystals, {tendrilCount} tendrils, {fogCount} fog patches");
    }

    void SpawnElementType(int count, CrowdElementType elementType, float noiseScale, CrowdBiomeData data)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 normalizedPos = FindSuitableLocation(noiseScale, data.maxSlope);
            if (normalizedPos.x < 0 || normalizedPos.y < 0) continue; // No suitable location found

            float normX = normalizedPos.x;
            float normZ = normalizedPos.y;
            float worldY = terrainData.GetInterpolatedHeight(normX, normZ);

            Vector3 worldPos = new Vector3(
                normX * terrainData.size.x,
                worldY,
                normZ * terrainData.size.z
            );

            // Check if this position is already occupied
            if (IsPositionOccupied(worldPos)) continue;

            // Create the element
            GameObject element = CreateProceduralElement(elementType, data);
            if (element != null)
            {
                element.transform.position = worldPos;
                element.transform.parent = this.transform;

                // Random rotation
                element.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                // Random scale
                float widthScale = Random.Range(data.minElementWidth, data.maxElementWidth);
                float heightScale = Random.Range(data.minElementHeight, data.maxElementHeight);
                element.transform.localScale = new Vector3(widthScale, heightScale, widthScale);

                // Add to dictionary to track spawned elements
                spawnedElements[worldPos] = element;
            }
        }
    }

    Vector2 FindSuitableLocation(float noiseScale, float maxSlope)
    {
        // Try to find a suitable location with limited attempts
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float normX = Random.Range(0.05f, 0.95f);
            float normZ = Random.Range(0.05f, 0.95f);

            // Check slope
            float slope = terrainData.GetSteepness(normX, normZ);
            if (slope > maxSlope) continue;

            // Check height (avoid underwater if not appropriate)
            float height = terrainData.GetInterpolatedHeight(normX, normZ);
            if (height <= 0.01f) continue;

            // Use noise to create natural distribution patterns
            float perlin = Mathf.PerlinNoise(normX * noiseScale, normZ * noiseScale);
            if (perlin < 0.4f) continue; // Skip sparse areas

            return new Vector2(normX, normZ);
        }

        return Vector2.negativeInfinity; // No suitable location found
    }

    bool IsPositionOccupied(Vector3 position)
    {
        float checkRadius = 1.5f; // Minimum distance between elements
        return spawnedElements.Keys.Any(pos => Vector3.Distance(pos, position) < checkRadius);
    }

    GameObject CreateProceduralElement(CrowdElementType elementType, CrowdBiomeData data)
    {
        GameObject element = new GameObject("Procedural_" + elementType.ToString());

        // Add appropriate components based on element type
        switch (elementType)
        {
            case CrowdElementType.Vegetation:
                return CreateVegetation(element, data);
            case CrowdElementType.Rock:
                return CreateRock(element, data);
            case CrowdElementType.Crystal:
                return CreateCrystal(element, data);
            case CrowdElementType.Tendril:
                return CreateTendril(element, data);
            case CrowdElementType.Fog:
                return CreateFog(element, data);
            default:
                return CreateVegetation(element, data);
        }
    }

    GameObject CreateVegetation(GameObject element, CrowdBiomeData data)
    {
        // Create a simple plant-like structure
        int branchCount = Random.Range(3, 8);
        float height = Random.Range(1f, 3f);

        // Main stem
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.transform.parent = element.transform;
        stem.transform.localPosition = Vector3.zero;
        stem.transform.localScale = new Vector3(0.2f, height, 0.2f);

        // Apply material
        Renderer stemRenderer = stem.GetComponent<Renderer>();
        stemRenderer.material = CreateProceduralMaterial(data.healthyColor, false);

        // Add branches
        for (int i = 0; i < branchCount; i++)
        {
            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.transform.parent = element.transform;

            float branchHeight = height * Random.Range(0.3f, 0.8f);
            float yPos = Random.Range(0.2f, height - 0.3f);
            float angle = Random.Range(0, 360);

            branch.transform.localPosition = new Vector3(0, yPos, 0);
            branch.transform.localRotation = Quaternion.Euler(0, 0, angle);
            branch.transform.localScale = new Vector3(0.1f, branchHeight, 0.1f);

            // Apply material
            Renderer branchRenderer = branch.GetComponent<Renderer>();
            branchRenderer.material = CreateProceduralMaterial(data.healthyColor, false);
        }

        // Add foliage
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.parent = element.transform;
        foliage.transform.localPosition = new Vector3(0, height, 0);
        foliage.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        // Apply material
        Renderer foliageRenderer = foliage.GetComponent<Renderer>();
        foliageRenderer.material = CreateProceduralMaterial(data.healthyColor, Random.value < data.glowProbability);

        // Add collider
        element.AddComponent<CapsuleCollider>().height = height * 2;

        return element;
    }

    GameObject CreateRock(GameObject element, CrowdBiomeData data)
    {
        // Create a rock with irregular shape
        int rockParts = Random.Range(2, 5);
        Color rockColor = Color.Lerp(Color.gray, data.dryColor, 0.3f);

        for (int i = 0; i < rockParts; i++)
        {
            GameObject rockPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rockPart.transform.parent = element.transform;

            // Randomize position, rotation, and scale
            rockPart.transform.localPosition = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.4f),
                Random.Range(-0.3f, 0.3f)
            );

            rockPart.transform.localRotation = Quaternion.Euler(
                Random.Range(0, 360),
                Random.Range(0, 360),
                Random.Range(0, 360)
            );

            rockPart.transform.localScale = new Vector3(
                Random.Range(0.4f, 1f),
                Random.Range(0.3f, 0.8f),
                Random.Range(0.4f, 1f)
            );

            // Apply material
            Renderer partRenderer = rockPart.GetComponent<Renderer>();
            partRenderer.material = CreateProceduralMaterial(rockColor, false);
        }

        // Add collider
        element.AddComponent<BoxCollider>();

        return element;
    }

    GameObject CreateCrystal(GameObject element, CrowdBiomeData data)
    {
        // Create a crystal cluster
        int crystalCount = Random.Range(3, 8);
        bool hasGlow = Random.value < data.glowProbability;

        for (int i = 0; i < crystalCount; i++)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            crystal.transform.parent = element.transform;

            // Position crystals in a cluster
            crystal.transform.localPosition = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(0f, 0.8f),
                Random.Range(-0.4f, 0.4f)
            );

            // Point crystals upward with some variation
            crystal.transform.localRotation = Quaternion.Euler(
                Random.Range(-20, 20),
                Random.Range(0, 360),
                Random.Range(-20, 20)
            );

            // Make crystals tall and thin
            crystal.transform.localScale = new Vector3(
                Random.Range(0.1f, 0.3f),
                Random.Range(0.5f, 1.5f),
                Random.Range(0.1f, 0.3f)
            );

            // Apply material - crystals often have emissive properties
            Renderer crystalRenderer = crystal.GetComponent<Renderer>();
            Color crystalColor = Color.Lerp(data.healthyColor, data.glowColor, 0.7f);
            crystalRenderer.material = CreateProceduralMaterial(crystalColor, hasGlow);
        }

        // Add collider
        element.AddComponent<BoxCollider>();

        return element;
    }

    GameObject CreateTendril(GameObject element, CrowdBiomeData data)
    {
        // Create twisting tendrils/vines
        int segmentCount = Random.Range(4, 10);
        float segmentLength = 0.5f;
        bool hasGlow = Random.value < data.glowProbability;

        GameObject previousSegment = null;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            segment.transform.parent = element.transform;

            if (previousSegment == null)
            {
                // First segment at base
                segment.transform.localPosition = new Vector3(0, 0, 0);
            }
            else
            {
                // Subsequent segments with offset and rotation
                float yOffset = segmentLength;
                float xOffset = Random.Range(-0.2f, 0.2f);
                float zOffset = Random.Range(-0.2f, 0.2f);

                segment.transform.localPosition = previousSegment.transform.localPosition +
                                                new Vector3(xOffset, yOffset, zOffset);

                // Add some twist
                segment.transform.localRotation = Quaternion.Euler(
                    Random.Range(-15, 15),
                    Random.Range(-15, 15),
                    Random.Range(-15, 15)
                );
            }

            segment.transform.localScale = new Vector3(
                0.1f * (1 - i * 0.1f), // Taper tendril
                segmentLength,
                0.1f * (1 - i * 0.1f)
            );

            // Apply material
            Renderer segmentRenderer = segment.GetComponent<Renderer>();
            Color tendrilColor = Color.Lerp(data.healthyColor, data.dryColor, 0.5f);
            segmentRenderer.material = CreateProceduralMaterial(tendrilColor, hasGlow && i % 2 == 0);

            previousSegment = segment;
        }

        // Add collider
        element.AddComponent<CapsuleCollider>().height = segmentCount * segmentLength;

        return element;
    }

    GameObject CreateFog(GameObject element, CrowdBiomeData data)
    {
        // Create a fog volume (using particle system)
        ParticleSystem fog = element.AddComponent<ParticleSystem>();

        // Configure particle system
        var main = fog.main;
        main.loop = true;
        main.startLifetime = 5f;
        main.startSpeed = 0.1f;
        main.startSize = 2f;
        main.startColor = Color.Lerp(data.glowColor, Color.white, 0.3f);

        var emission = fog.emission;
        emission.rateOverTime = 10f;

        var shape = fog.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;

        var renderer = fog.GetComponent<Renderer>();
        renderer.material = CreateProceduralMaterial(data.glowColor, true);

        // Fog doesn't need a collider
        return element;
    }

    Material CreateProceduralMaterial(Color baseColor, bool shouldGlow)
    {
        if (proceduralShader == null)
        {
            proceduralShader = Shader.Find("Standard");
        }

        Material mat = new Material(proceduralShader);
        mat.color = baseColor;

        // Add some variation to the material
        mat.SetFloat("_Metallic", Random.Range(0f, 0.3f));
        mat.SetFloat("_Glossiness", Random.Range(0.1f, 0.5f));

        // Add emission if needed
        if (shouldGlow && glowMaterial != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * 0.7f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return mat;
    }

    public void RegenerateVegetation()
    {
        GenerateAllVegetation();
    }
}

public enum CrowdElementType
{
    Vegetation,
    Rock,
    Crystal,
    Tendril,
    Fog
}

//public enum BiomeType
//{
//    Grassland,
//    Forest,
//    Desert,
//    Tundra,
//    Volcanic,
//    CrystalCavern,
//    Swamp
//}