using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class AdvancedProceduralAlienStructure : MonoBehaviour
{
    private enum StructureType
    {
        QuantumFractal,
        BioluminescentTangle,
        ChronoDisplacementCrystal,
        GravitationalAnomaly,
        PlasmaGeode,
        NeuralNetworkFungus,
        VoidTendrilNexus,
        HyperdimensionalPolyhedron,
        SentientMistFormation,
        RealityGlitchCluster
    }

    [Header("Generation Parameters")]
    [SerializeField] private StructureType type;
    [SerializeField] private int seed = -1;
    [SerializeField][Range(0.5f, 5f)] private float complexity = 1f;

    [Header("Visual Parameters")]
    [SerializeField] private bool enableLights = true;
    [SerializeField] private bool enableParticles = true;
    [SerializeField] private bool enablePhysics = true;

    // References
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    // Procedural parameters
    private float baseScale;
    private float animationSpeed;
    private float distortionAmount;
    private float emissionIntensity;
    private float morphFrequency;

    // Materials
    private Material[] materials;
    private Color[] baseColors;
    private Color[] emissionColors;

    // Animation state
    private float animationTime;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float randomOffset;

    // Components
    private List<Light> lights = new List<Light>();
    private List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    private Rigidbody physicsBody;

    void Awake()
    {
        InitializeComponents();

        // Set random seed if not specified
        if (seed == -1) seed = Random.Range(0, int.MaxValue);
        Random.InitState(seed);
        
        ModularEnemy modularEnemy = GetComponent<ModularEnemy>();

        if (modularEnemy != null)
        {
            this.gameObject.GetComponent<MeshCollider>().enabled = false;
        }else
        {
            this.gameObject.GetComponent<MeshCollider>().enabled = true;
        }

        randomOffset = Random.Range(0f, 1000f);
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Randomize type if not set in inspector
        if (!System.Enum.IsDefined(typeof(StructureType), type))
        {
            type = (StructureType)Random.Range(0, System.Enum.GetValues(typeof(StructureType)).Length);
        }

        // Generate based on complexity
        complexity = Mathf.Clamp(complexity, 0.5f, 5f);

        GenerateStructure();
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        // Ensure we have valid components
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
    }

    void GenerateStructure()
    {
        // Clean up any existing components
        CleanUpExistingComponents();

        // Determine properties based on type
        DetermineStructureProperties();

        // Generate mesh
        GenerateProceduralMesh();

        // Create materials
        CreateProceduralMaterials();

        // Add special effects
        AddSpecialEffects();

        // Set initial state
        UpdateStructure();
    }

    void CleanUpExistingComponents()
    {
        // Clean up lights
        foreach (Light light in lights)
        {
            if (light != null && light.gameObject != null)
                Destroy(light.gameObject);
        }
        lights.Clear();

        // Clean up particle systems
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null && ps.gameObject != null)
                Destroy(ps.gameObject);
        }
        particleSystems.Clear();

        // Clean up physics
        if (physicsBody != null)
            Destroy(physicsBody);
    }

    void DetermineStructureProperties()
    {
        switch (type)
        {
            case StructureType.QuantumFractal:
                baseScale = Random.Range(0.5f, 3f);
                animationSpeed = Random.Range(0.2f, 1.5f);
                distortionAmount = Random.Range(0.1f, 0.8f);
                emissionIntensity = Random.Range(1f, 5f);
                morphFrequency = Random.Range(0.5f, 3f);
                break;

            case StructureType.BioluminescentTangle:
                baseScale = Random.Range(0.8f, 2.5f);
                animationSpeed = Random.Range(0.5f, 2f);
                distortionAmount = Random.Range(0.3f, 1f);
                emissionIntensity = Random.Range(2f, 8f);
                morphFrequency = Random.Range(1f, 4f);
                break;

            case StructureType.ChronoDisplacementCrystal:
                baseScale = Random.Range(1f, 4f);
                animationSpeed = Random.Range(0.1f, 0.8f);
                distortionAmount = Random.Range(0.05f, 0.3f);
                emissionIntensity = Random.Range(3f, 7f);
                morphFrequency = Random.Range(0.2f, 1f);
                break;

            case StructureType.GravitationalAnomaly:
                baseScale = Random.Range(1.5f, 5f);
                animationSpeed = Random.Range(0.8f, 2.5f);
                distortionAmount = Random.Range(0.5f, 1.5f);
                emissionIntensity = Random.Range(4f, 10f);
                morphFrequency = Random.Range(2f, 5f);
                break;

            case StructureType.PlasmaGeode:
                baseScale = Random.Range(0.7f, 2f);
                animationSpeed = Random.Range(1f, 3f);
                distortionAmount = Random.Range(0.2f, 0.6f);
                emissionIntensity = Random.Range(5f, 12f);
                morphFrequency = Random.Range(1.5f, 4f);
                break;

            default:
                baseScale = Random.Range(0.5f, 3f);
                animationSpeed = Random.Range(0.3f, 2f);
                distortionAmount = Random.Range(0.1f, 0.9f);
                emissionIntensity = Random.Range(1f, 6f);
                morphFrequency = Random.Range(0.5f, 3.5f);
                break;
        }

        // Apply complexity modifier
        baseScale *= complexity;
        animationSpeed *= complexity;
        distortionAmount *= Mathf.Clamp(complexity, 0.5f, 2f);
        emissionIntensity *= complexity;
        morphFrequency *= complexity;

        transform.localScale = Vector3.one * baseScale;
    }

    void GenerateProceduralMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralAlienMesh_" + type.ToString();

        switch (type)
        {
            case StructureType.QuantumFractal:
                mesh = GenerateQuantumFractal();
                break;
            case StructureType.BioluminescentTangle:
                mesh = GenerateBioluminescentTangle();
                break;
            case StructureType.ChronoDisplacementCrystal:
                mesh = GenerateChronoDisplacementCrystal();
                break;
            case StructureType.GravitationalAnomaly:
                mesh = GenerateGravitationalAnomaly();
                break;
            case StructureType.PlasmaGeode:
                mesh = GeneratePlasmaGeode();
                break;
            case StructureType.NeuralNetworkFungus:
                mesh = GenerateNeuralNetworkFungus();
                break;
            case StructureType.VoidTendrilNexus:
                mesh = GenerateVoidTendrilNexus();
                break;
            case StructureType.HyperdimensionalPolyhedron:
                mesh = GenerateHyperdimensionalPolyhedron();
                break;
            case StructureType.SentientMistFormation:
                mesh = GenerateSentientMistFormation();
                break;
            case StructureType.RealityGlitchCluster:
                mesh = GenerateRealityGlitchCluster();
                break;
        }

        if (mesh != null)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
        }
        else
        {
            Debug.LogError("Failed to generate mesh for type: " + type);
            // Fallback to a simple cube
            GameObject fallbackCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshFilter.mesh = fallbackCube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(fallbackCube);
        }
    }

    Mesh GenerateQuantumFractal()
    {
        int iterations = Mathf.FloorToInt(5 * complexity);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Start with an icosahedron
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        vertices.AddRange(new Vector3[] {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        });

        // Initial triangles
        triangles.AddRange(new int[] {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        });

        // Subdivide for fractal effect
        for (int i = 0; i < iterations; i++)
        {
            List<int> newTriangles = new List<int>();
            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();

            for (int j = 0; j < triangles.Count; j += 3)
            {
                int a = triangles[j];
                int b = triangles[j + 1];
                int c = triangles[j + 2];

                int ab = GetMiddlePoint(a, b, vertices, middlePointIndexCache);
                int bc = GetMiddlePoint(b, c, vertices, middlePointIndexCache);
                int ca = GetMiddlePoint(c, a, vertices, middlePointIndexCache);

                newTriangles.AddRange(new int[] { a, ab, ca });
                newTriangles.AddRange(new int[] { b, bc, ab });
                newTriangles.AddRange(new int[] { c, ca, bc });
                newTriangles.AddRange(new int[] { ab, bc, ca });
            }

            triangles = newTriangles;
        }

        // Apply quantum distortion
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 vertex = vertices[i];
            float noise = Mathf.PerlinNoise(vertex.x * 2f, vertex.z * 2f);
            vertex += Random.insideUnitSphere * distortionAmount * noise;
            vertices[i] = vertex;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

    int GetMiddlePoint(int p1, int p2, List<Vector3> vertices, Dictionary<long, int> cache)
    {
        long key = (long)Mathf.Min(p1, p2) << 32 | Mathf.Max(p1, p2);

        if (cache.TryGetValue(key, out int ret))
            return ret;

        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = Vector3.Lerp(point1, point2, 0.5f).normalized;

        int i = vertices.Count;
        vertices.Add(middle);
        cache.Add(key, i);
        return i;
    }

    Mesh GenerateBioluminescentTangle()
    {
        int tendrils = Mathf.FloorToInt(8 * complexity);
        int segments = Mathf.FloorToInt(10 * complexity);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Color> colors = new List<Color>();

        // Create central core
        int coreVertices = 16;
        for (int i = 0; i < coreVertices; i++)
        {
            float angle = i * Mathf.PI * 2 / coreVertices;
            vertices.Add(new Vector3(Mathf.Cos(angle) * 0.3f, 0, Mathf.Sin(angle) * 0.3f));
            uv.Add(new Vector2((float)i / coreVertices, 0));
            colors.Add(Color.white);
        }

        // Create tendrils
        for (int t = 0; t < tendrils; t++)
        {
            float angle = t * Mathf.PI * 2 / tendrils;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Random.Range(-0.3f, 0.3f), Mathf.Sin(angle)).normalized;

            Vector3 prevRing = Vector3.zero;
            int prevRingStartIndex = -1;

            for (int s = 0; s <= segments; s++)
            {
                float progress = (float)s / segments;
                float radius = Mathf.Lerp(0.2f, 0.05f, progress);
                float height = progress * Random.Range(1.5f, 3f);

                int ringStartIndex = vertices.Count;

                // Create a ring of vertices
                int ringVertices = Mathf.FloorToInt(8 * (1 - progress * 0.7f));
                for (int r = 0; r < ringVertices; r++)
                {
                    float ringAngle = r * Mathf.PI * 2 / ringVertices;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(ringAngle) * radius,
                        Mathf.Sin(ringAngle) * radius,
                        0
                    );

                    // Rotate offset to align with tendril direction
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, direction);
                    offset = rotation * offset;

                    Vector3 position = direction * height + offset;

                    // Add some organic curvature
                    position += new Vector3(
                        Mathf.Sin(progress * 10f + t) * 0.1f,
                        Mathf.Cos(progress * 8f + t) * 0.1f,
                        Mathf.Sin(progress * 12f + t) * 0.1f
                    );

                    vertices.Add(position);
                    uv.Add(new Vector2((float)r / ringVertices, progress));

                    // Color based on progress
                    Color color = Color.Lerp(
                        new Color(0.2f, 0.8f, 0.3f, 1f),
                        new Color(0.8f, 0.2f, 0.9f, 1f),
                        progress
                    );
                    colors.Add(color);
                }

                // Create triangles between rings
                if (s > 0 && prevRingStartIndex >= 0)
                {
                    for (int r = 0; r < ringVertices; r++)
                    {
                        int current = ringStartIndex + r;
                        int next = ringStartIndex + (r + 1) % ringVertices;
                        int prev = prevRingStartIndex + r;
                        int prevNext = prevRingStartIndex + (r + 1) % ringVertices;

                        triangles.Add(current);
                        triangles.Add(prev);
                        triangles.Add(prevNext);

                        triangles.Add(current);
                        triangles.Add(prevNext);
                        triangles.Add(next);
                    }
                }

                prevRingStartIndex = ringStartIndex;
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.colors = colors.ToArray();
        return mesh;
    }

    // Implementations for other structure types
    Mesh GenerateChronoDisplacementCrystal()
    {
        int sides = 6 + Mathf.FloorToInt(complexity * 4);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Create crystal base
        float height = 2f * complexity;
        float radius = 0.5f;

        // Bottom face
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
        }
        vertices.Add(new Vector3(0, 0, 0)); // Center bottom

        // Top face
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            vertices.Add(new Vector3(Mathf.Cos(angle) * radius * 0.3f, height, Mathf.Sin(angle) * radius * 0.3f));
        }
        vertices.Add(new Vector3(0, height * 1.2f, 0)); // Center top

        // Create side faces
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;

            // Bottom triangle
            triangles.Add(i);
            triangles.Add(next);
            triangles.Add(sides); // Center bottom

            // Side triangles
            triangles.Add(i);
            triangles.Add(i + sides + 1);
            triangles.Add(next);

            triangles.Add(next);
            triangles.Add(i + sides + 1);
            triangles.Add(next + sides + 1);

            // Top triangle
            triangles.Add(i + sides + 1);
            triangles.Add(next + sides + 1);
            triangles.Add(sides * 2 + 1); // Center top
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

    Mesh GenerateGravitationalAnomaly()
    {
        // Create a sphere with inward distortions
        int segments = 10 + Mathf.FloorToInt(complexity * 8);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int y = 0; y <= segments; y++)
        {
            float yAngle = (float)y / segments * Mathf.PI;
            for (int x = 0; x <= segments; x++)
            {
                float xAngle = (float)x / segments * Mathf.PI * 2;

                Vector3 vertex = new Vector3(
                    Mathf.Sin(yAngle) * Mathf.Cos(xAngle),
                    Mathf.Cos(yAngle),
                    Mathf.Sin(yAngle) * Mathf.Sin(xAngle)
                );

                // Add distortion
                float distortion = Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.5f;
                vertex *= 1f - distortion * 0.5f;

                vertices.Add(vertex);
            }
        }

        // Create triangles
        for (int y = 0; y < segments; y++)
        {
            for (int x = 0; x < segments; x++)
            {
                int current = y * (segments + 1) + x;
                int next = current + segments + 1;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);

                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

    Mesh GeneratePlasmaGeode()
    {
        // Create a geode with crystal formations inside
        int segments = 12 + Mathf.FloorToInt(complexity * 6);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Outer shell (half sphere)
        for (int y = 0; y <= segments / 2; y++)
        {
            float yAngle = (float)y / (segments / 2) * Mathf.PI;
            for (int x = 0; x <= segments; x++)
            {
                float xAngle = (float)x / segments * Mathf.PI * 2;

                Vector3 vertex = new Vector3(
                    Mathf.Sin(yAngle) * Mathf.Cos(xAngle),
                    Mathf.Cos(yAngle),
                    Mathf.Sin(yAngle) * Mathf.Sin(xAngle)
                ) * 1f;

                vertices.Add(vertex);
            }
        }

        // Inner crystals
        int crystalCount = 5 + Mathf.FloorToInt(complexity * 3);
        for (int i = 0; i < crystalCount; i++)
        {
            float angle = i * Mathf.PI * 2 / crystalCount;
            float height = Random.Range(0.3f, 0.7f);
            float length = Random.Range(0.2f, 0.5f);

            Vector3 basePos = new Vector3(
                Mathf.Cos(angle) * 0.5f,
                -0.2f,
                Mathf.Sin(angle) * 0.5f
            );

            Vector3 tipPos = basePos + new Vector3(
                Mathf.Cos(angle) * 0.2f,
                height,
                Mathf.Sin(angle) * 0.2f
            );

            // Add crystal vertices
            int baseIndex = vertices.Count;
            vertices.Add(basePos);
            vertices.Add(tipPos);
            vertices.Add(basePos + new Vector3(length, 0, 0));
            vertices.Add(basePos + new Vector3(-length, 0, 0));
            vertices.Add(basePos + new Vector3(0, 0, length));
            vertices.Add(basePos + new Vector3(0, 0, -length));

            // Create crystal faces
            triangles.Add(baseIndex); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 4); triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 5);
        }

        // Create outer shell triangles
        for (int y = 0; y < segments / 2; y++)
        {
            for (int x = 0; x < segments; x++)
            {
                int current = y * (segments + 1) + x;
                int next = current + segments + 1;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);

                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        return mesh;
    }

    Mesh GenerateNeuralNetworkFungus() { return GenerateFallbackMesh(); }
    Mesh GenerateVoidTendrilNexus() { return GenerateFallbackMesh(); }
    Mesh GenerateHyperdimensionalPolyhedron() { return GenerateFallbackMesh(); }
    Mesh GenerateSentientMistFormation() { return GenerateFallbackMesh(); }
    Mesh GenerateRealityGlitchCluster() { return GenerateFallbackMesh(); }

    Mesh GenerateFallbackMesh()
    {
        // Fallback to a more complex icosphere
        return GenerateQuantumFractal();
    }

    void CreateProceduralMaterials()
    {
        int materialCount = 1;
        if (type == StructureType.QuantumFractal || type == StructureType.PlasmaGeode)
            materialCount = 2;
        else if (type == StructureType.RealityGlitchCluster)
            materialCount = 3;

        materials = new Material[materialCount];
        baseColors = new Color[materialCount];
        emissionColors = new Color[materialCount];

        for (int i = 0; i < materialCount; i++)
        {
            materials[i] = new Material(Shader.Find("Standard"));

            // Generate alien colors
            float hue = Random.value;
            baseColors[i] = Color.HSVToRGB(hue, Random.Range(0.7f, 1f), Random.Range(0.3f, 0.8f));
            emissionColors[i] = Color.HSVToRGB((hue + 0.5f) % 1f, Random.Range(0.8f, 1f), Random.Range(0.8f, 1f));

            materials[i].color = baseColors[i];
            materials[i].SetColor("_EmissionColor", emissionColors[i] * emissionIntensity);
            materials[i].SetFloat("_Metallic", Random.Range(0.1f, 0.9f));
            materials[i].SetFloat("_Glossiness", Random.Range(0.2f, 0.8f));
            materials[i].EnableKeyword("_EMISSION");

            // Apply special properties based on type
            switch (type)
            {
                case StructureType.QuantumFractal:
                    materials[i].SetFloat("_Mode", 3); // Transparent
                    materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    materials[i].renderQueue = 3000;
                    break;

                case StructureType.BioluminescentTangle:
                    materials[i].EnableKeyword("_NORMALMAP");
                    materials[i].SetTexture("_BumpMap", CreateProceduralNormalMap());
                    break;

                case StructureType.PlasmaGeode:
                    if (i == 0)
                    {
                        materials[i].SetFloat("_Mode", 3); // Transparent outer shell
                        materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        materials[i].renderQueue = 3000;
                    }
                    break;
            }
        }

        meshRenderer.materials = materials;
    }

    Texture2D CreateProceduralNormalMap()
    {
        int size = 256;
        Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGB24, true);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float xf = (float)x / size;
                float yf = (float)y / size;

                // Generate organic-looking normal patterns
                float noise1 = Mathf.PerlinNoise(xf * 10f, yf * 10f);
                float noise2 = Mathf.PerlinNoise(xf * 20f + 5f, yf * 20f + 5f);

                Vector3 normal = new Vector3(
                    noise1 * 2f - 1f,
                    noise2 * 2f - 1f,
                    1f
                ).normalized;

                Color color = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f
                );

                normalMap.SetPixel(x, y, color);
            }
        }

        normalMap.Apply();
        return normalMap;
    }

    void AddSpecialEffects()
    {
        if (!enableLights && !enableParticles && !enablePhysics)
            return;

        // Add lights
        if (enableLights)
        {
            switch (type)
            {
                case StructureType.QuantumFractal:
                    AddPointLights(3, 5f, emissionColors[0] * 2f);
                    break;

                case StructureType.BioluminescentTangle:
                    AddPointLights(8, 3f, emissionColors[0] * 1.5f);
                    break;

                case StructureType.PlasmaGeode:
                    AddPointLights(5, 4f, emissionColors[0] * 2f);
                    break;

                default:
                    AddPointLights(2, 3f, emissionColors[0] * 1.2f);
                    break;
            }
        }

        // Add particles
        if (enableParticles)
        {
            switch (type)
            {
                case StructureType.BioluminescentTangle:
                    AddParticles(10, emissionColors[0]);
                    break;

                case StructureType.SentientMistFormation:
                    AddParticles(15, emissionColors[0]);
                    break;

                case StructureType.RealityGlitchCluster:
                    AddParticles(8, emissionColors[0]);
                    break;
            }
        }

        // Add physics for some types
        if (enablePhysics && (type == StructureType.GravitationalAnomaly || Random.value > 0.7f))
        {
            AddPhysicsComponents();
        }
    }

    void AddPointLights(int count, float range, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject lightObj = new GameObject("StructureLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Random.insideUnitSphere * 1.5f;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = Random.Range(0.5f, 2f);
            light.color = color;
            light.renderMode = LightRenderMode.ForcePixel;

            lights.Add(light);
        }
    }

    void AddParticles(int count, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject particleObj = new GameObject("StructureParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Random.insideUnitSphere * 0.5f;

            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSize = Random.Range(0.05f, 0.2f);
            main.startLifetime = Random.Range(1f, 3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = Random.Range(5f, 20f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            particleSystems.Add(ps);
        }
    }

    void AddPhysicsComponents()
    {
        physicsBody = gameObject.AddComponent<Rigidbody>();
        physicsBody.isKinematic = true;
        physicsBody.useGravity = false;

        // Add some force to make it float
        StartCoroutine(ApplyRandomForces());
    }

    IEnumerator ApplyRandomForces()
    {
        while (true)
        {
            if (physicsBody != null)
            {
                Vector3 force = Random.insideUnitSphere * Random.Range(0.1f, 0.5f);
                physicsBody.AddForce(force, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }

    void Update()
    {
        animationTime += Time.deltaTime * animationSpeed;
        UpdateStructure();
    }

    void UpdateStructure()
    {
        // Apply continuous transformation based on type
        switch (type)
        {
            case StructureType.QuantumFractal:
                ApplyQuantumFractalAnimation();
                break;

            case StructureType.BioluminescentTangle:
                ApplyBioluminescentTangleAnimation();
                break;

            case StructureType.ChronoDisplacementCrystal:
                ApplyChronoDisplacementCrystalAnimation();
                break;

            case StructureType.GravitationalAnomaly:
                ApplyGravitationalAnomalyAnimation();
                break;

            case StructureType.PlasmaGeode:
                ApplyPlasmaGeodeAnimation();
                break;

            default:
                ApplyDefaultAnimation();
                break;
        }

        // Update lights
        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.intensity = Mathf.Abs(Mathf.Sin(animationTime * 2f)) * 1.5f + 0.5f;

                // Random light flares
                if (Random.value < 0.01f)
                {
                    light.intensity *= Random.Range(2f, 5f);
                }
            }
        }

        // Update particles
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = Color.Lerp(
                    emissionColors[0],
                    emissionColors[emissionColors.Length > 1 ? 1 : 0],
                    Mathf.PingPong(animationTime * 0.5f, 1f)
                );
            }
        }
    }

    void ApplyQuantumFractalAnimation()
    {
        // Morph the mesh vertices
        Mesh mesh = meshFilter.mesh;
        if (mesh != null && mesh.vertices != null)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            if (vertices.Length == normals.Length)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    float timeOffset = randomOffset + i * 0.1f;
                    float distortion =
                        Mathf.PerlinNoise(vertices[i].x * morphFrequency + animationTime, vertices[i].z * morphFrequency) *
                        Mathf.PerlinNoise(vertices[i].y * morphFrequency, animationTime * 0.5f) *
                        distortionAmount;

                    vertices[i] += normals[i] * Mathf.Sin(animationTime + timeOffset) * distortion;
                }

                mesh.vertices = vertices;
                mesh.RecalculateNormals();
            }
        }

        // Pulse the scale
        transform.localScale = Vector3.one * baseScale * (1f + Mathf.Sin(animationTime) * 0.1f);

        // Rotate in multiple dimensions
        transform.rotation = originalRotation *
            Quaternion.AngleAxis(animationTime * 10f, Vector3.up) *
            Quaternion.AngleAxis(animationTime * 7f, Vector3.right) *
            Quaternion.AngleAxis(animationTime * 5f, Vector3.forward);
    }

    void ApplyBioluminescentTangleAnimation()
    {
        // Gentle pulsing motion
        float pulse = Mathf.Sin(animationTime) * 0.1f;
        transform.localScale = Vector3.one * baseScale * (1f + pulse);

        // Swaying motion
        float swayX = Mathf.Sin(animationTime * 0.7f) * 3f;
        float swayZ = Mathf.Cos(animationTime * 0.5f) * 2f;
        transform.rotation = originalRotation * Quaternion.Euler(swayX, 0, swayZ);

        // Update material properties
        if (materials != null && materials.Length > 0)
        {
            float emission = Mathf.Abs(Mathf.Sin(animationTime * 2f)) * emissionIntensity;
            materials[0].SetColor("_EmissionColor", emissionColors[0] * emission);
        }
    }

    void ApplyChronoDisplacementCrystalAnimation()
    {
        // Slow, precise rotation
        transform.rotation = originalRotation * Quaternion.AngleAxis(animationTime * 5f, Vector3.up);

        // Subtle pulsing
        float pulse = Mathf.Sin(animationTime * 0.3f) * 0.05f;
        transform.localScale = Vector3.one * baseScale * (1f + pulse);
    }

    void ApplyGravitationalAnomalyAnimation()
    {
        // Strong, irregular pulsing
        float pulse = Mathf.PerlinNoise(animationTime * 0.5f, 0) * 0.3f;
        transform.localScale = Vector3.one * baseScale * (1f + pulse);

        // Erratic rotation
        transform.rotation = originalRotation *
            Quaternion.AngleAxis(Mathf.PerlinNoise(animationTime * 0.7f, 0) * 360f, Vector3.up) *
            Quaternion.AngleAxis(Mathf.PerlinNoise(0, animationTime * 0.7f) * 360f, Vector3.right);
    }

    void ApplyPlasmaGeodeAnimation()
    {
        // Gentle rotation
        transform.rotation = originalRotation * Quaternion.AngleAxis(animationTime * 3f, Vector3.up);

        // Pulsing emission
        if (materials != null && materials.Length > 1)
        {
            float emission = (Mathf.Sin(animationTime * 2f) + 1f) * 0.5f * emissionIntensity;
            materials[1].SetColor("_EmissionColor", emissionColors[1] * emission);
        }
    }

    void ApplyDefaultAnimation()
    {
        // Default animation for unspecified types
        transform.rotation = originalRotation * Quaternion.AngleAxis(animationTime * 8f, Vector3.up);

        float pulse = Mathf.Sin(animationTime) * 0.05f;
        transform.localScale = Vector3.one * baseScale * (1f + pulse);
    }

    // Public method to regenerate with new seed
    public void Regenerate(int newSeed = -1)
    {
        seed = newSeed == -1 ? Random.Range(0, int.MaxValue) : newSeed;
        Random.InitState(seed);
        GenerateStructure();
    }

    // Debug GUI
    void OnGUI()
    {
        //if (Debug.isDebugBuild)
        //{
        //    GUI.Label(new Rect(10, 10, 300, 20), $"Alien Structure: {type}");
        //    GUI.Label(new Rect(10, 30, 300, 20), $"Seed: {seed}");
        //    GUI.Label(new Rect(10, 50, 300, 20), $"Complexity: {complexity}");

        //    if (GUI.Button(new Rect(10, 70, 100, 20), "Regenerate"))
        //    {
        //        Regenerate();
        //    }
        //}
    }

    // Clean up when destroyed
    void OnDestroy()
    {
        CleanUpExistingComponents();

        // Clean up materials
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                if (mat != null)
                    DestroyImmediate(mat);
            }
        }
    }
}