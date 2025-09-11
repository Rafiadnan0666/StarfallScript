//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
//public class AdvancedProceduralAlienStructure : MonoBehaviour
//{
//    private enum StructureType
//    {
//        QuantumFractal,
//        BioluminescentTangle,
//        ChronoDisplacementCrystal,
//        GravitationalAnomaly,
//        PlasmaGeode,
//        NeuralNetworkFungus,
//        VoidTendrilNexus,
//        HyperdimensionalPolyhedron,
//        SentientMistFormation,
//        RealityGlitchCluster
//    }

//    [Header("Generation Parameters")]
//    [SerializeField] private StructureType structureType;
//    [SerializeField] private int generationSeed = -1;
//    [SerializeField] private float structureComplexity = 1f;

//    [Header("References")]
//    private MeshFilter structureMeshFilter;
//    private MeshRenderer structureMeshRenderer;
//    private MeshCollider structureMeshCollider;
//    private List<Light> structureLights;
//    private List<ParticleSystem> structureParticleSystems;

//    // Procedural parameters
//    private float structureBaseScale;
//    private float structureAnimationSpeed;
//    private float structureDistortionAmount;
//    private float structureEmissionIntensity;
//    private float structureMorphFrequency;

//    // Materials
//    private Material[] structureMaterials;
//    private Color[] structureBaseColors;
//    private Color[] structureEmissionColors;

//    // Animation state
//    private float currentAnimationTime;
//    private Vector3 initialPosition;
//    private Quaternion initialRotation;
//    private float randomTimeOffset;

//    // Advanced components
//    private bool hasPhysicsComponents;
//    private bool hasParticleSystems;
//    private bool usesMultipleMaterials;

//    // Optimization
//    private Mesh currentMesh;
//    private Coroutine physicsCoroutine;
//    private Dictionary<long, int> middlePointCache;

//    void Awake()
//    {
//        InitializeComponentsq();
//        SetupGenerationParameters();
//        GenerateCompleteStructure();
//    }

//    private void InitializeComponentsq()
//    {
//        structureMeshFilter = GetComponent<MeshFilter>();
//        structureMeshRenderer = GetComponent<MeshRenderer>();
//        structureMeshCollider = GetComponent<MeshCollider>();

//        structureLights = new List<Light>();
//        structureParticleSystems = new List<ParticleSystem>();
//        middlePointCache = new Dictionary<long, int>();
//    }

//    private void SetupGenerationParameters()
//    {
//        // Set random seed if not specified
//        if (generationSeed == -1) generationSeed = Random.Range(0, int.MaxValue);
//        Random.InitState(generationSeed);

//        randomTimeOffset = Random.Range(0f, 1000f);
//        initialPosition = transform.position;
//        initialRotation = transform.rotation;

//        // Randomize type if not set in inspector
//        if (structureType == default(StructureType))
//        {
//            structureType = (StructureType)Random.Range(0, System.Enum.GetValues(typeof(StructureType)).Length);
//        }

//        // Clamp complexity
//        structureComplexity = Mathf.Clamp(structureComplexity, 0.5f, 5f);
//    }

//    private void GenerateCompleteStructure()
//    {
//        DetermineStructurePropertiesq();
//        GenerateProceduralMeshq();
//        CreateProceduralMaterials();
//        AddSpecialEffects();
//    }

//    private void DetermineStructurePropertiesq()
//    {
//        switch (structureType)
//        {
//            case StructureType.QuantumFractal:
//                structureBaseScale = Random.Range(0.5f, 3f);
//                structureAnimationSpeed = Random.Range(0.2f, 1.5f);
//                structureDistortionAmount = Random.Range(0.1f, 0.8f);
//                structureEmissionIntensity = Random.Range(1f, 5f);
//                structureMorphFrequency = Random.Range(0.5f, 3f);
//                usesMultipleMaterials = true;
//                break;

//            case StructureType.BioluminescentTangle:
//                structureBaseScale = Random.Range(0.8f, 2.5f);
//                structureAnimationSpeed = Random.Range(0.5f, 2f);
//                structureDistortionAmount = Random.Range(0.3f, 1f);
//                structureEmissionIntensity = Random.Range(2f, 8f);
//                structureMorphFrequency = Random.Range(1f, 4f);
//                hasParticleSystems = true;
//                break;

//            case StructureType.ChronoDisplacementCrystal:
//                structureBaseScale = Random.Range(1f, 4f);
//                structureAnimationSpeed = Random.Range(0.1f, 0.8f);
//                structureDistortionAmount = Random.Range(0.2f, 0.6f);
//                structureEmissionIntensity = Random.Range(3f, 7f);
//                structureMorphFrequency = Random.Range(0.8f, 2.5f);
//                usesMultipleMaterials = true;
//                break;

//            case StructureType.GravitationalAnomaly:
//                structureBaseScale = Random.Range(1.2f, 3.5f);
//                structureAnimationSpeed = Random.Range(0.3f, 1.2f);
//                structureDistortionAmount = Random.Range(0.4f, 1.2f);
//                structureEmissionIntensity = Random.Range(2f, 6f);
//                structureMorphFrequency = Random.Range(0.7f, 2f);
//                hasPhysicsComponents = true;
//                break;

//            case StructureType.PlasmaGeode:
//                structureBaseScale = Random.Range(0.7f, 2.8f);
//                structureAnimationSpeed = Random.Range(0.4f, 1.8f);
//                structureDistortionAmount = Random.Range(0.2f, 0.9f);
//                structureEmissionIntensity = Random.Range(4f, 9f);
//                structureMorphFrequency = Random.Range(1.2f, 3f);
//                usesMultipleMaterials = true;
//                break;

//            default:
//                structureBaseScale = Random.Range(0.5f, 3f);
//                structureAnimationSpeed = Random.Range(0.3f, 2f);
//                structureDistortionAmount = Random.Range(0.1f, 0.9f);
//                structureEmissionIntensity = Random.Range(1f, 6f);
//                structureMorphFrequency = Random.Range(0.5f, 3.5f);
//                break;
//        }

//        // Apply complexity modifier
//        structureBaseScale *= structureComplexity;
//        structureAnimationSpeed *= structureComplexity;
//        structureDistortionAmount *= structureComplexity;
//        structureEmissionIntensity *= structureComplexity;
//        structureMorphFrequency *= structureComplexity;

//        transform.localScale = Vector3.one * structureBaseScale;
//    }

//    private void GenerateProceduralMeshq()
//    {
//        Mesh generatedMesh = null;

//        switch (structureType)
//        {
//            case StructureType.QuantumFractal:
//                generatedMesh = GenerateQuantumFractalMesh();
//                break;
//            case StructureType.BioluminescentTangle:
//                generatedMesh = GenerateBioluminescentTangleMesh();
//                break;
//            case StructureType.ChronoDisplacementCrystal:
//                generatedMesh = GenerateChronoDisplacementCrystalMesh();
//                break;
//            case StructureType.GravitationalAnomaly:
//                generatedMesh = GenerateGravitationalAnomalyMesh();
//                break;
//            case StructureType.PlasmaGeode:
//                generatedMesh = GeneratePlasmaGeodeMesh();
//                break;
//            case StructureType.NeuralNetworkFungus:
//                generatedMesh = GenerateNeuralNetworkFungusMesh();
//                break;
//            case StructureType.VoidTendrilNexus:
//                generatedMesh = GenerateVoidTendrilNexusMesh();
//                break;
//            case StructureType.HyperdimensionalPolyhedron:
//                generatedMesh = GenerateHyperdimensionalPolyhedronMesh();
//                break;
//            case StructureType.SentientMistFormation:
//                generatedMesh = GenerateSentientMistFormationMesh();
//                break;
//            case StructureType.RealityGlitchCluster:
//                generatedMesh = GenerateRealityGlitchClusterMesh();
//                break;
//        }

//        if (generatedMesh != null)
//        {
//            generatedMesh.RecalculateNormals();
//            generatedMesh.RecalculateBounds();
//            structureMeshFilter.mesh = generatedMesh;
//            structureMeshCollider.sharedMesh = generatedMesh;
//            currentMesh = generatedMesh;
//        }
//    }

//    private Mesh GenerateQuantumFractalMesh()
//    {
//        int iterations = Mathf.FloorToInt(5 * structureComplexity);
//        List<Vector3> vertices = new List<Vector3>();
//        List<int> triangles = new List<int>();

//        // Start with an icosahedron
//        float t = (1f + Mathf.Sqrt(5f)) / 2f;
//        vertices.AddRange(new Vector3[] {
//            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
//            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
//            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
//        });

//        // Initial triangles
//        triangles.AddRange(new int[] {
//            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
//            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
//            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
//            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
//        });

//        // Subdivide for fractal effect
//        middlePointCache.Clear();
//        for (int i = 0; i < iterations; i++)
//        {
//            List<int> newTriangles = new List<int>();
//            Dictionary<long, int> currentMiddlePointCache = new Dictionary<long, int>();

//            for (int j = 0; j < triangles.Count; j += 3)
//            {
//                int a = triangles[j];
//                int b = triangles[j + 1];
//                int c = triangles[j + 2];

//                int ab = GetMiddlePointIndex(a, b, vertices, currentMiddlePointCache);
//                int bc = GetMiddlePointIndex(b, c, vertices, currentMiddlePointCache);
//                int ca = GetMiddlePointIndex(c, a, vertices, currentMiddlePointCache);

//                newTriangles.AddRange(new int[] { a, ab, ca });
//                newTriangles.AddRange(new int[] { b, bc, ab });
//                newTriangles.AddRange(new int[] { c, ca, bc });
//                newTriangles.AddRange(new int[] { ab, bc, ca });
//            }

//            triangles = newTriangles;
//        }

//        // Apply quantum distortion
//        for (int i = 0; i < vertices.Count; i++)
//        {
//            Vector3 vertex = vertices[i];
//            float noise = Mathf.PerlinNoise(vertex.x * 2f, vertex.z * 2f);
//            vertex += Random.insideUnitSphere * structureDistortionAmount * noise;
//            vertices[i] = vertex;
//        }

//        Mesh mesh = new Mesh();
//        mesh.vertices = vertices.ToArray();
//        mesh.triangles = triangles.ToArray();
//        return mesh;
//    }

//    private int GetMiddlePointIndex(int p1, int p2, List<Vector3> vertices, Dictionary<long, int> cache)
//    {
//        long key = (long)Mathf.Min(p1, p2) << 32 | Mathf.Max(p1, p2);

//        if (cache.TryGetValue(key, out int ret))
//            return ret;

//        Vector3 point1 = vertices[p1];
//        Vector3 point2 = vertices[p2];
//        Vector3 middle = Vector3.Lerp(point1, point2, 0.5f).normalized;

//        int i = vertices.Count;
//        vertices.Add(middle);
//        cache.Add(key, i);
//        return i;
//    }

//    private Mesh GenerateBioluminescentTangleMesh()
//    {
//        int tendrils = Mathf.FloorToInt(8 * structureComplexity);
//        int segments = Mathf.FloorToInt(10 * structureComplexity);
//        List<Vector3> vertices = new List<Vector3>();
//        List<int> triangles = new List<int>();
//        List<Vector2> uv = new List<Vector2>();
//        List<Color> colors = new List<Color>();

//        // Create central core
//        int coreVertices = 16;
//        for (int i = 0; i < coreVertices; i++)
//        {
//            float angle = i * Mathf.PI * 2 / coreVertices;
//            vertices.Add(new Vector3(Mathf.Cos(angle) * 0.3f, 0, Mathf.Sin(angle) * 0.3f));
//            uv.Add(new Vector2((float)i / coreVertices, 0));
//            colors.Add(Color.white);
//        }

//        // Create tendrils
//        for (int t = 0; t < tendrils; t++)
//        {
//            float angle = t * Mathf.PI * 2 / tendrils;
//            Vector3 direction = new Vector3(Mathf.Cos(angle), Random.Range(-0.3f, 0.3f), Mathf.Sin(angle)).normalized;

//            int prevRingStartIndex = -1;

//            for (int s = 0; s <= segments; s++)
//            {
//                float progress = (float)s / segments;
//                float radius = Mathf.Lerp(0.2f, 0.05f, progress);
//                float height = progress * Random.Range(1.5f, 3f);

//                int ringStartIndex = vertices.Count;

//                // Create a ring of vertices
//                int ringVertices = Mathf.FloorToInt(8 * (1 - progress * 0.7f));
//                for (int r = 0; r < ringVertices; r++)
//                {
//                    float ringAngle = r * Mathf.PI * 2 / ringVertices;
//                    Vector3 offset = new Vector3(
//                        Mathf.Cos(ringAngle) * radius,
//                        Mathf.Sin(ringAngle) * radius,
//                        0
//                    );

//                    // Rotate offset to align with tendril direction
//                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
//                    offset = rotation * offset;

//                    Vector3 position = direction * height + offset;

//                    // Add some organic curvature
//                    position += new Vector3(
//                        Mathf.Sin(progress * 10f + t) * 0.1f,
//                        Mathf.Cos(progress * 8f + t) * 0.1f,
//                        Mathf.Sin(progress * 12f + t) * 0.1f
//                    );

//                    vertices.Add(position);
//                    uv.Add(new Vector2((float)r / ringVertices, progress));

//                    // Color based on progress
//                    Color color = Color.Lerp(
//                        new Color(0.2f, 0.8f, 0.3f, 1f),
//                        new Color(0.8f, 0.2f, 0.9f, 1f),
//                        progress
//                    );
//                    colors.Add(color);
//                }

//                // Create triangles between rings
//                if (s > 0 && prevRingStartIndex >= 0)
//                {
//                    for (int r = 0; r < ringVertices; r++)
//                    {
//                        int current = ringStartIndex + r;
//                        int next = ringStartIndex + (r + 1) % ringVertices;
//                        int prev = prevRingStartIndex + r;
//                        int prevNext = prevRingStartIndex + (r + 1) % ringVertices;

//                        triangles.Add(current);
//                        triangles.Add(prev);
//                        triangles.Add(prevNext);

//                        triangles.Add(current);
//                        triangles.Add(prevNext);
//                        triangles.Add(next);
//                    }
//                }

//                prevRingStartIndex = ringStartIndex;
//            }
//        }

//        Mesh mesh = new Mesh();
//        mesh.vertices = vertices.ToArray();
//        mesh.triangles = triangles.ToArray();
//        mesh.uv = uv.ToArray();
//        mesh.colors = colors.ToArray();
//        return mesh;
//    }

//    // Additional mesh generation methods
//    private Mesh GenerateChronoDisplacementCrystalMesh()
//    {
//        Mesh mesh = GenerateBasicIcosphere(3);

//        // Add crystal-like sharp edges
//        Vector3[] vertices = mesh.vertices;
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            if (Random.value > 0.7f)
//            {
//                vertices[i] = vertices[i].normalized * Random.Range(0.8f, 1.2f);
//            }
//        }
//        mesh.vertices = vertices;

//        return mesh;
//    }

//    private Mesh GenerateGravitationalAnomalyMesh()
//    {
//        Mesh mesh = GenerateBasicIcosphere(2);

//        // Create a warped, distorted appearance
//        Vector3[] vertices = mesh.vertices;
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            float distortion = Mathf.PerlinNoise(
//                vertices[i].x * 3f,
//                vertices[i].z * 3f) * structureDistortionAmount;

//            vertices[i] += vertices[i].normalized * distortion;
//        }
//        mesh.vertices = vertices;

//        return mesh;
//    }

//    private Mesh GeneratePlasmaGeodeMesh()
//    {
//        Mesh mesh = GenerateBasicIcosphere(4);

//        // Create geode-like interior
//        Vector3[] vertices = mesh.vertices;
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            if (Vector3.Dot(vertices[i], Vector3.up) > 0.3f)
//            {
//                vertices[i] *= Random.Range(0.3f, 0.7f);
//            }
//        }
//        mesh.vertices = vertices;

//        return mesh;
//    }

//    private Mesh GenerateNeuralNetworkFungusMesh()
//    {
//        return GenerateBioluminescentTangleMesh(); // Similar structure
//    }

//    private Mesh GenerateVoidTendrilNexusMesh()
//    {
//        return GenerateBioluminescentTangleMesh(); // Similar structure
//    }

//    private Mesh GenerateHyperdimensionalPolyhedronMesh()
//    {
//        return GenerateQuantumFractalMesh(); // Similar structure
//    }

//    private Mesh GenerateSentientMistFormationMesh()
//    {
//        Mesh mesh = GenerateBasicIcosphere(2);

//        // Create a wispy, ethereal appearance
//        Vector3[] vertices = mesh.vertices;
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            float noise = Mathf.PerlinNoise(
//                vertices[i].x * 5f + randomTimeOffset,
//                vertices[i].z * 5f + randomTimeOffset);

//            vertices[i] += Random.insideUnitSphere * noise * 0.3f;
//        }
//        mesh.vertices = vertices;

//        return mesh;
//    }

//    private Mesh GenerateRealityGlitchClusterMesh()
//    {
//        Mesh mesh = GenerateBasicIcosphere(4);

//        // Create glitchy, fragmented appearance
//        Vector3[] vertices = mesh.vertices;
//        for (int i = 0; i < vertices.Length; i++)
//        {
//            if (Random.value > 0.8f)
//            {
//                vertices[i] += Random.insideUnitSphere * 0.5f;
//            }
//        }
//        mesh.vertices = vertices;

//        return mesh;
//    }

//    private Mesh GenerateBasicIcosphere(int subdivisions)
//    {
//        List<Vector3> vertices = new List<Vector3>();
//        List<int> triangles = new List<int>();

//        float t = (1f + Mathf.Sqrt(5f)) / 2f;
//        vertices.AddRange(new Vector3[] {
//            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
//            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
//            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
//        });

//        triangles.AddRange(new int[] {
//            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
//            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
//            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
//            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
//        });

//        for (int i = 0; i < subdivisions; i++)
//        {
//            List<int> newTriangles = new List<int>();
//            Dictionary<long, int> cache = new Dictionary<long, int>();

//            for (int j = 0; j < triangles.Count; j += 3)
//            {
//                int a = triangles[j];
//                int b = triangles[j + 1];
//                int c = triangles[j + 2];

//                int ab = GetMiddlePointIndex(a, b, vertices, cache);
//                int bc = GetMiddlePointIndex(b, c, vertices, cache);
//                int ca = GetMiddlePointIndex(c, a, vertices, cache);

//                newTriangles.AddRange(new int[] { a, ab, ca });
//                newTriangles.AddRange(new int[] { b, bc, ab });
//                newTriangles.AddRange(new int[] { c, ca, bc });
//                newTriangles.AddRange(new int[] { ab, bc, ca });
//            }

//            triangles = newTriangles;
//        }

//        Mesh mesh = new Mesh();
//        mesh.vertices = vertices.ToArray();
//        mesh.triangles = triangles.ToArray();
//        return mesh;
//    }

//    private void CreateProceduralMaterialsq()
//    {
//        int materialCount = usesMultipleMaterials ? 3 : 1;
//        structureMaterials = new Material[materialCount];
//        structureBaseColors = new Color[materialCount];
//        structureEmissionColors = new Color[materialCount];

//        for (int i = 0; i < materialCount; i++)
//        {
//            structureMaterials[i] = new Material(Shader.Find("Standard"));

//            // Generate alien colors
//            float hue = Random.value;
//            structureBaseColors[i] = Color.HSVToRGB(hue, Random.Range(0.7f, 1f), Random.Range(0.3f, 0.8f));
//            structureEmissionColors[i] = Color.HSVToRGB((hue + 0.5f) % 1f, Random.Range(0.8f, 1f), Random.Range(0.8f, 1f));

//            structureMaterials[i].color = structureBaseColors[i];
//            structureMaterials[i].SetColor("_EmissionColor", structureEmissionColors[i] * structureEmissionIntensity);
//            structureMaterials[i].SetFloat("_Metallic", Random.Range(0.1f, 0.9f));
//            structureMaterials[i].SetFloat("_Glossiness", Random.Range(0.2f, 0.8f));
//            structureMaterials[i].EnableKeyword("_EMISSION");

//            // Apply special properties based on type
//            switch (structureType)
//            {
//                case StructureType.QuantumFractal:
//                    structureMaterials[i].SetFloat("_Mode", 3); // Transparent
//                    structureMaterials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//                    structureMaterials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//                    structureMaterials[i].renderQueue = 3000;
//                    break;

//                case StructureType.BioluminescentTangle:
//                    structureMaterials[i].EnableKeyword("_NORMALMAP");
//                    structureMaterials[i].SetTexture("_BumpMap", CreateProceduralNormalMapq());
//                    break;

//                case StructureType.ChronoDisplacementCrystal:
//                    structureMaterials[i].SetFloat("_Metallic", 0.9f);
//                    structureMaterials[i].SetFloat("_Glossiness", 0.95f);
//                    break;
//            }
//        }

//        structureMeshRenderer.materials = structureMaterials;
//    }

//    private Texture2D CreateProceduralNormalMapq()
//    {
//        int size = 256;
//        Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGB24, false);

//        for (int y = 0; y < size; y++)
//        {
//            for (int x = 0; x < size; x++)
//            {
//                float xf = (float)x / size;
//                float yf = (float)y / size;

//                // Generate organic-looking normal patterns
//                float noise1 = Mathf.PerlinNoise(xf * 10f, yf * 10f);
//                float noise2 = Mathf.PerlinNoise(xf * 20f + 5f, yf * 20f + 5f);

//                Vector3 normal = new Vector3(
//                    noise1 * 2f - 1f,
//                    noise2 * 2f - 1f,
//                    1f
//                ).normalized;

//                Color color = new Color(
//                    normal.x * 0.5f + 0.5f,
//                    normal.y * 0.5f + 0.5f,
//                    normal.z * 0.5f + 0.5f
//                );

//                normalMap.SetPixel(x, y, color);
//            }
//        }

//        normalMap.Apply();
//        return normalMap;
//    }

//    private void AddSpecialEffectsq()
//    {
//        // Add lights
//        switch (structureType)
//        {
//            case StructureType.QuantumFractal:
//                AddPointLightsq(3, 5f, structureEmissionColors[0] * 2f);
//                break;

//            case StructureType.BioluminescentTangle:
//                AddPointLightsq(8, 3f, structureEmissionColors[0] * 1.5f);
//                AddParticleSystems(10, structureEmissionColors[0]);
//                break;

//            case StructureType.ChronoDisplacementCrystal:
//                AddPointLightsq(6, 4f, structureEmissionColors[0] * 2f);
//                break;

//            case StructureType.GravitationalAnomaly:
//                AddPointLightsq(4, 6f, Color.cyan);
//                AddPhysicsComponents();
//                break;

//            case StructureType.PlasmaGeode:
//                AddPointLightsq(5, 4f, structureEmissionColors[0] * 2f);
//                AddParticleSystems(8, structureEmissionColors[0]);
//                break;

//            default:
//                AddPointLightsq(4, 3f, structureEmissionColors[0] * 1.5f);
//                break;
//        }

//        // Add physics for some types
//        if ((structureType == StructureType.GravitationalAnomaly && !hasPhysicsComponents) || Random.value > 0.7f)
//        {
//            AddPhysicsComponents();
//        }
//    }

//    private void AddPointLightsq(int count, float range, Color color)
//    {
//        for (int i = 0; i < count; i++)
//        {
//            GameObject lightObj = new GameObject("StructureLight");
//            lightObj.transform.SetParent(transform);
//            lightObj.transform.localPosition = Random.insideUnitSphere * 1.5f;

//            Light light = lightObj.AddComponent<Light>();
//            light.type = LightType.Point;
//            light.range = range;
//            light.intensity = Random.Range(0.5f, 2f);
//            light.color = color;
//            light.renderMode = LightRenderMode.ForcePixel;

//            structureLights.Add(light);
//        }
//    }

//    private void AddParticleSystems(int count, Color color)
//    {
//        for (int i = 0; i < count; i++)
//        {
//            GameObject particleObj = new GameObject("StructureParticles");
//            particleObj.transform.SetParent(transform);
//            particleObj.transform.localPosition = Random.insideUnitSphere;

//            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
//            var main = ps.main;
//            main.startColor = color;
//            main.startSize = Random.Range(0.05f, 0.2f);
//            main.startLifetime = Random.Range(1f, 3f);
//            main.simulationSpace = ParticleSystemSimulationSpace.World;

//            var emission = ps.emission;
//            emission.rateOverTime = Random.Range(5f, 20f);

//            var shape = ps.shape;
//            shape.shapeType = ParticleSystemShapeType.Sphere;
//            shape.radius = 0.1f;

//            var velocity = ps.velocityOverLifetime;
//            velocity.enabled = true;
//            velocity.space = ParticleSystemSimulationSpace.World;

//            structureParticleSystems.Add(ps);
//        }
//    }

//    private void AddPhysicsComponentsq()
//    {
//        hasPhysicsComponents = true;

//        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
//        if (rb == null)
//        {
//            rb = gameObject.AddComponent<Rigidbody>();
//        }

//        rb.isKinematic = true;
//        rb.useGravity = false;

//        // Add some force to make it float
//        if (physicsCoroutine != null)
//        {
//            StopCoroutine(physicsCoroutine);
//        }
//        physicsCoroutine = StartCoroutine(ApplyRandomForcesCoroutine());
//    }

//    private IEnumerator ApplyRandomForcesCoroutine()
//    {
//        Rigidbody rb = GetComponent<Rigidbody>();
//        if (rb == null) yield break;

//        while (hasPhysicsComponents)
//        {
//            Vector3 force = Random.insideUnitSphere * Random.Range(0.1f, 0.5f);
//            rb.AddForce(force, ForceMode.Impulse);

//            yield return new WaitForSeconds(Random.Range(1f, 3f));
//        }
//    }

//    void Update()
//    {
//        currentAnimationTime += Time.deltaTime * structureAnimationSpeed;

//        // Apply continuous transformation based on type
//        switch (structureType)
//        {
//            case StructureType.QuantumFractal:
//                ApplyQuantumFractalAnimation();
//                break;

//            case StructureType.BioluminescentTangle:
//                ApplyBioluminescentTangleAnimation();
//                break;

//            case StructureType.ChronoDisplacementCrystal:
//                ApplyChronoDisplacementAnimation();
//                break;

//            case StructureType.GravitationalAnomaly:
//                ApplyGravitationalAnomalyAnimation();
//                break;

//            case StructureType.PlasmaGeode:
//                ApplyPlasmaGeodeAnimation();
//                break;

//            default:
//                ApplyDefaultAnimation();
//                break;
//        }

//        UpdateLights();
//        UpdateParticles();
//    }

//    private void ApplyQuantumFractalAnimationq()
//    {
//        if (currentMesh == null) return;

//        // Morph the mesh vertices
//        Vector3[] vertices = currentMesh.vertices;
//        Vector3[] normals = currentMesh.normals;

//        for (int i = 0; i < vertices.Length; i++)
//        {
//            float timeOffset = randomTimeOffset + i * 0.1f;
//            float distortion =
//                Mathf.PerlinNoise(vertices[i].x * structureMorphFrequency + currentAnimationTime,
//                                 vertices[i].z * structureMorphFrequency) *
//                Mathf.PerlinNoise(vertices[i].y * structureMorphFrequency, currentAnimationTime * 0.5f) *
//                structureDistortionAmount;

//            vertices[i] += normals[i] * Mathf.Sin(currentAnimationTime + timeOffset) * distortion;
//        }

//        currentMesh.vertices = vertices;
//        currentMesh.RecalculateNormals();

//        // Pulse the scale
//        transform.localScale = Vector3.one * structureBaseScale * (1f + Mathf.Sin(currentAnimationTime) * 0.1f);

//        // Rotate in multiple dimensions
//        transform.rotation = initialRotation *
//            Quaternion.AngleAxis(currentAnimationTime * 10f, Vector3.up) *
//            Quaternion.AngleAxis(currentAnimationTime * 7f, Vector3.right) *
//            Quaternion.AngleAxis(currentAnimationTime * 5f, Vector3.forward);
//    }

//    private void ApplyBioluminescentTangleAnimationq()
//    {
//        // Gentle pulsing motion
//        float pulse = Mathf.Sin(currentAnimationTime) * 0.1f;
//        transform.localScale = Vector3.one * structureBaseScale * (1f + pulse);

//        // Swaying motion
//        float swayX = Mathf.Sin(currentAnimationTime * 0.7f) * 3f;
//        float swayZ = Mathf.Cos(currentAnimationTime * 0.5f) * 2f;
//        transform.rotation = initialRotation * Quaternion.Euler(swayX, 0, swayZ);

//        // Update material properties
//        if (structureMaterials != null && structureMaterials.Length > 0)
//        {
//            float emission = Mathf.Abs(Mathf.Sin(currentAnimationTime * 2f)) * structureEmissionIntensity;
//            structureMaterials[0].SetColor("_EmissionColor", structureEmissionColors[0] * emission);
//        }
//    }

//    private void ApplyChronoDisplacementAnimation()
//    {
//        // Time-based distortion effect
//        float timeWarp = Mathf.Sin(currentAnimationTime * 0.5f) * 0.2f;
//        transform.localScale = Vector3.one * structureBaseScale * (1f + timeWarp);

//        // Rotate with time distortion
//        transform.rotation = initialRotation *
//            Quaternion.AngleAxis(currentAnimationTime * 8f, Vector3.up) *
//            Quaternion.AngleAxis(currentAnimationTime * 6f, Vector3.forward);
//    }

//    private void ApplyGravitationalAnomalyAnimationq()
//    {
//        // Warping, unstable appearance
//        float warp = Mathf.PerlinNoise(
//            currentAnimationTime * 0.5f,
//            randomTimeOffset) * 0.3f;

//        transform.localScale = Vector3.one * structureBaseScale * (1f + warp);

//        // Erratic rotation
//        transform.rotation = initialRotation *
//            Quaternion.AngleAxis(currentAnimationTime * Random.Range(5f, 15f), Vector3.up) *
//            Quaternion.AngleAxis(currentAnimationTime * Random.Range(3f, 10f), Vector3.right);
//    }

//    private void ApplyPlasmaGeodeAnimationq()
//    {
//        // Pulsing with inner glow
//        float pulse = (Mathf.Sin(currentAnimationTime) + 1f) * 0.5f;
//        transform.localScale = Vector3.one * structureBaseScale * (1f + pulse * 0.2f);

//        // Slow rotation
//        transform.rotation = initialRotation *
//            Quaternion.AngleAxis(currentAnimationTime * 3f, Vector3.up);

//        // Update material emission
//        if (structureMaterials != null && structureMaterials.Length > 0)
//        {
//            float emission = pulse * structureEmissionIntensity * 1.5f;
//            structureMaterials[0].SetColor("_EmissionColor", structureEmissionColors[0] * emission);
//        }
//    }

//    private void ApplyDefaultAnimationq()
//    {
//        // Default gentle animation
//        float pulse = Mathf.Sin(currentAnimationTime) * 0.05f;
//        transform.localScale = Vector3.one * structureBaseScale * (1f + pulse);

//        transform.rotation = initialRotation *
//            Quaternion.AngleAxis(currentAnimationTime * 3f, Vector3.up);
//    }

//    private void UpdateLights()
//    {
//        foreach (Light light in structureLights)
//        {
//            if (light != null)
//            {
//                light.intensity = Mathf.Abs(Mathf.Sin(currentAnimationTime * 2f)) * 1.5f + 0.5f;

//                // Random light flares
//                if (Random.value < 0.01f)
//                {
//                    light.intensity *= Random.Range(2f, 5f);
//                }
//            }
//        }
//    }

//    private void UpdateParticles()
//    {
//        foreach (ParticleSystem ps in structureParticleSystems)
//        {
//            if (ps != null)
//            {
//                var main = ps.main;
//                main.startColor = Color.Lerp(
//                    structureEmissionColors[0],
//                    structureEmissionColors[structureEmissionColors.Length > 1 ? 1 : 0],
//                    Mathf.PingPong(currentAnimationTime * 0.5f, 1f)
//                );
//            }
//        }
//    }

//    // Public method to regenerate with new seed
//    public void Regenerateq(int newSeed = -1)
//    {
//        generationSeed = newSeed == -1 ? Random.Range(0, int.MaxValue) : newSeed;
//        Random.InitState(generationSeed);

//        CleanUpExistingComponents();
//        GenerateCompleteStructure();
//    }

//    private void CleanUpExistingComponentsq()
//    {
//        // Clean up lights
//        foreach (Light light in structureLights)
//        {
//            if (light != null) Destroy(light.gameObject);
//        }
//        structureLights.Clear();

//        // Clean up particles
//        foreach (ParticleSystem ps in structureParticleSystems)
//        {
//            if (ps != null) Destroy(ps.gameObject);
//        }
//        structureParticleSystems.Clear();

//        // Clean up physics
//        if (physicsCoroutine != null)
//        {
//            StopCoroutine(physicsCoroutine);
//            physicsCoroutine = null;
//        }

//        Rigidbody rb = GetComponent<Rigidbody>();
//        if (rb != null)
//        {
//            Destroy(rb);
//        }

//        hasPhysicsComponents = false;
//    }

//    void OnDestroy()
//    {
//        CleanUpExistingComponents();

//        // Clean up materials to prevent memory leaks
//        if (structureMaterials != null)
//        {
//            foreach (Material mat in structureMaterials)
//            {
//                if (mat != null) Destroy(mat);
//            }
//        }
//    }

//    // Debug GUI
//    void OnGUI()
//    {
//        if (Debug.isDebugBuild)
//        {
//            GUI.Label(new Rect(10, 10, 300, 20), $"Alien Structure: {structureType}");
//            GUI.Label(new Rect(10, 30, 300, 20), $"Seed: {generationSeed}");

//            if (GUI.Button(new Rect(10, 50, 100, 20), "Regenerate"))
//            {
//                Regenerate();
//            }
//        }
//    }
//}