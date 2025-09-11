using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class VoidrootQuantumStructure : MonoBehaviour
{
    [Header("Root Generation Settings")]
    [Range(3, 20)] public int rootCount = 8;
    [Range(10, 50)] public int segmentsPerRoot = 30;
    [Range(5f, 30f)] public float rootLength = 18f;
    [Range(0.2f, 2f)] public float baseThickness = 0.8f;
    [Range(0f, 0.5f)] public float thicknessVariation = 0.2f;
    [Range(0.5f, 5f)] public float curlIntensity = 2.5f;
    [Range(0f, 1f)] public float gravityInfluence = 0.3f;

    [Header("Branching Settings")]
    [Range(0, 5)] public int maxBranchLevels = 2;
    [Range(0f, 1f)] public float branchChance = 0.4f;
    [Range(0.3f, 0.9f)] public float branchScaleFactor = 0.6f;

    [Header("Terrain Interaction")]
    public bool attachToTerrain = true;
    public float terrainOffset = 0.1f;
    public LayerMask terrainLayerMask = 1;
    [Range(0f, 1f)] public float terrainInfluence = 0.7f;

    [Header("Quantum Animation")]
    [Range(0.1f, 2f)] public float pulseSpeed = 0.7f;
    [Range(0f, 0.3f)] public float pulseIntensity = 0.15f;
    [Range(0.1f, 2f)] public float swaySpeed = 0.4f;
    [Range(0f, 1f)] public float swayIntensity = 0.6f;
    [Range(0f, 0.5f)] public float phaseVariance = 0.2f;

    [Header("Void Energy Appearance")]
    public Color coreEnergyColor = new Color(0.15f, 0.05f, 0.8f, 1f);
    public Color tipEnergyColor = new Color(0.6f, 0.4f, 1f, 1f);
    [Range(1f, 5f)] public float glowIntensity = 3f;
    [Range(0f, 1f)] public float energyNoiseScale = 0.5f;
    [Range(4, 12)] public int radialResolution = 8;

    // Private state variables
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Vector2> uvs;
    private List<Color> colors;
    private List<Vector3> normals;

    private List<RootData> roots;
    private Vector3[] originalVertices;
    private Material proceduralMaterial;

    // Shader property IDs
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EnergyColor = Shader.PropertyToID("_EnergyColor");
    private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");

    // Root data structure
    private class RootData
    {
        public Vector3 origin;
        public Vector3[] segments;
        public float[] thicknesses;
        public float pulseOffset;
        public float swayOffset;
        public int branchLevel;
        public RootData parent;
        public List<RootData> children;
    }

    void Start()
    {
        InitializeComponents();
        GenerateRootSystem();
        CreateProceduralMaterial();
    }

    void Update()
    {
        AnimateRoots();
        UpdateMaterialProperties();
    }

    void InitializeComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh();
        mesh.name = "Voidroot Quantum Structure";
        meshFilter.mesh = mesh;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        colors = new List<Color>();
        normals = new List<Vector3>();

        roots = new List<RootData>();
    }

    void GenerateRootSystem()
    {
        // Clear previous data
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
        roots.Clear();

        // Generate primary roots
        for (int i = 0; i < rootCount; i++)
        {
            float angle = i * Mathf.PI * 2 / rootCount;
            float distance = Random.Range(1.5f, 3f);
            Vector3 origin = transform.position + new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);

            RootData root = new RootData
            {
                origin = origin,
                pulseOffset = Random.Range(0f, Mathf.PI * 2),
                swayOffset = Random.Range(0f, Mathf.PI * 2),
                branchLevel = 0,
                parent = null,
                children = new List<RootData>()
            };

            GenerateRootPath(root);
            roots.Add(root);

            // Generate branches recursively
            if (maxBranchLevels > 0)
            {
                GenerateBranches(root, 1);
            }
        }

        // Generate mesh for all roots
        foreach (RootData root in roots)
        {
            GenerateRootMesh(root);
        }

        UpdateMesh();

        // Set up mesh collider
        meshCollider.sharedMesh = mesh;
        originalVertices = mesh.vertices;
    }

    void GenerateRootPath(RootData root)
    {
        int segmentCount = segmentsPerRoot - (root.branchLevel * 3); // Branches are shorter
        root.segments = new Vector3[segmentCount];
        root.thicknesses = new float[segmentCount];

        Vector3 currentPos = root.origin;
        Vector3 direction = CalculateInitialDirection(root);

        // If this is a branch, start from parent's segment
        if (root.parent != null)
        {
            int parentSegment = Random.Range(segmentCount / 2, root.parent.segments.Length - 2);
            currentPos = root.parent.segments[parentSegment];
            direction = (root.parent.segments[parentSegment + 1] - root.parent.segments[parentSegment]).normalized;

            // Add some divergence from parent direction
            direction = Quaternion.AngleAxis(Random.Range(-45f, 45f), Vector3.up) * direction;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);

            // Calculate thickness (thicker at base, thinner at tips)
            float thickness = baseThickness * Mathf.Pow(1 - t, 0.7f) *
                             Random.Range(1f - thicknessVariation, 1f + thicknessVariation) *
                             Mathf.Pow(branchScaleFactor, root.branchLevel);
            root.thicknesses[i] = thickness;

            // Apply curl and noise to direction
            float noiseX = Mathf.PerlinNoise(root.origin.x * 0.1f, i * energyNoiseScale) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(root.origin.z * 0.1f, i * energyNoiseScale) * 2f - 1f;
            float noiseY = (Mathf.PerlinNoise(root.origin.y * 0.1f, i * energyNoiseScale) * 2f - 1f) * (1 - gravityInfluence);

            Vector3 curl = new Vector3(noiseX, noiseY, noiseZ).normalized * curlIntensity * 0.01f;
            direction = (direction + curl).normalized;

            // Apply gravity influence
            direction = Vector3.Lerp(direction, Vector3.down, gravityInfluence * t).normalized;

            // Move along direction
            float segmentLength = rootLength / segmentsPerRoot * Mathf.Pow(branchScaleFactor, root.branchLevel);
            currentPos += direction * segmentLength;

            // Terrain attachment
            if (attachToTerrain && i > segmentCount / 3)
            {
                RaycastHit hit;
                Vector3 rayStart = currentPos + Vector3.up * 3f;
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 6f, terrainLayerMask))
                {
                    float influence = Mathf.Lerp(0, terrainInfluence, (i - segmentCount / 3f) / (segmentCount * 0.66f));
                    currentPos = Vector3.Lerp(currentPos, hit.point + Vector3.up * terrainOffset, influence);

                    // When hitting terrain, roots tend to spread along the surface
                    if (i > segmentCount * 0.8f)
                    {
                        Vector3 surfaceNormal = hit.normal;
                        Vector3 surfaceTangent = Vector3.Cross(surfaceNormal, Vector3.up).normalized;
                        if (surfaceTangent.magnitude > 0.1f)
                        {
                            direction = Vector3.Lerp(direction, surfaceTangent, 0.1f).normalized;
                        }
                    }
                }
            }

            root.segments[i] = currentPos;
        }
    }

    Vector3 CalculateInitialDirection(RootData root)
    {
        if (root.parent != null)
        {
            // Branches tend to grow outward from parent
            Vector3 awayFromParent = (root.origin - root.parent.origin).normalized;
            return Vector3.Lerp(Vector3.down, awayFromParent, 0.5f).normalized;
        }
        else
        {
            // Primary roots start growing downward with some outward component
            Vector3 fromCenter = (root.origin - transform.position).normalized;
            return Vector3.Lerp(Vector3.down, fromCenter, 0.3f).normalized;
        }
    }

    void GenerateBranches(RootData parent, int currentLevel)
    {
        if (currentLevel > maxBranchLevels) return;

        for (int i = 5; i < parent.segments.Length - 5; i++)
        {
            if (Random.value < branchChance * Mathf.Pow(0.7f, currentLevel - 1))
            {
                Vector3 branchOrigin = parent.segments[i];

                RootData branch = new RootData
                {
                    origin = branchOrigin,
                    pulseOffset = Random.Range(0f, Mathf.PI * 2),
                    swayOffset = Random.Range(0f, Mathf.PI * 2),
                    branchLevel = currentLevel,
                    parent = parent,
                    children = new List<RootData>()
                };

                GenerateRootPath(branch);
                parent.children.Add(branch);
                roots.Add(branch);

                // Recursively generate sub-branches
                GenerateBranches(branch, currentLevel + 1);
            }
        }
    }

    void GenerateRootMesh(RootData root)
    {
        int segmentCount = root.segments.Length;
        int vertexIndexOffset = vertices.Count;

        // Create vertices for each segment
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 position = root.segments[i];
            float currentThickness = root.thicknesses[i];

            // Calculate direction for orientation
            Vector3 direction = (i < segmentCount - 1) ?
                (root.segments[i + 1] - position).normalized :
                (position - root.segments[i - 1]).normalized;

            // Handle case where direction is ambiguous
            if (direction.magnitude < 0.1f) direction = Vector3.down;

            // Calculate perpendicular vectors for radial distribution
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(direction, Vector3.right).normalized;

            Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular).normalized;

            // Create ring of vertices
            for (int j = 0; j < radialResolution; j++)
            {
                float angle = j * Mathf.PI * 2 / radialResolution;
                Vector3 circleOffset = (perpendicular * Mathf.Cos(angle) + perpendicular2 * Mathf.Sin(angle)) * currentThickness;
                vertices.Add(position + circleOffset);

                // Calculate normal (pointing outward from center)
                Vector3 normal = (vertices[vertices.Count - 1] - position).normalized;
                normals.Add(normal);

                // UVs - along the root and around it
                uvs.Add(new Vector2(t, j / (float)radialResolution));

                // Vertex color for energy flow (more intense at tips)
                float energyValue = Mathf.Lerp(0.3f, 1.2f, t) * Random.Range(0.9f, 1.1f);
                Color energyColor = Color.Lerp(coreEnergyColor, tipEnergyColor, t);
                colors.Add(new Color(energyColor.r, energyColor.g, energyColor.b, energyValue));
            }
        }

        // Create triangles for the root
        for (int i = 0; i < segmentCount - 1; i++)
        {
            for (int j = 0; j < radialResolution; j++)
            {
                int current = vertexIndexOffset + i * radialResolution + j;
                int next = vertexIndexOffset + i * radialResolution + (j + 1) % radialResolution;
                int below = vertexIndexOffset + (i + 1) * radialResolution + j;
                int belowNext = vertexIndexOffset + (i + 1) * radialResolution + (j + 1) % radialResolution;

                // First triangle
                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(below);

                // Second triangle
                triangles.Add(next);
                triangles.Add(belowNext);
                triangles.Add(below);
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();
        mesh.RecalculateBounds();

        // Store original vertices for animation
        originalVertices = mesh.vertices;
    }

    void CreateProceduralMaterial()
    {
        proceduralMaterial = new Material(Shader.Find("Standard"));
        proceduralMaterial.name = "VoidrootEnergyMaterial";

        // Set material properties
        proceduralMaterial.color = new Color(0.05f, 0.02f, 0.1f);
        proceduralMaterial.SetColor(EmissionColor, coreEnergyColor * glowIntensity);
        proceduralMaterial.EnableKeyword("_EMISSION");

        // Make material appear otherworldly
        proceduralMaterial.SetFloat("_Metallic", 0.9f);
        proceduralMaterial.SetFloat("_Glossiness", 0.1f);
        proceduralMaterial.SetFloat("_BumpScale", 0.3f);

        meshRenderer.material = proceduralMaterial;
    }

    void AnimateRoots()
    {
        if (originalVertices == null || originalVertices.Length != vertices.Count)
            return;

        Vector3[] animatedVertices = new Vector3[originalVertices.Length];
        Color[] animatedColors = new Color[colors.Count];

        int vertexIndex = 0;

        foreach (RootData root in roots)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed + root.pulseOffset) * pulseIntensity;
            float sway = Mathf.Sin(Time.time * swaySpeed + root.swayOffset) * swayIntensity;

            int segmentCount = root.segments.Length;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 segmentCenter = root.segments[i];

                for (int j = 0; j < radialResolution; j++)
                {
                    int origIndex = vertexIndex;
                    Vector3 originalVertex = originalVertices[origIndex];

                    // Calculate offset from center for this vertex
                    Vector3 toVertex = originalVertex - segmentCenter;
                    float distToCenter = toVertex.magnitude;

                    if (distToCenter > 0.01f)
                    {
                        // Apply pulsing effect (expands/contracts radially)
                        Vector3 pulseOffset = toVertex * (pulse * (1 - t)) / distToCenter;

                        // Apply swaying motion (more pronounced at tips)
                        float swayFactor = sway * t;
                        Vector3 swayOffset = transform.right *
                                            Mathf.PerlinNoise(origIndex * 0.1f, Time.time * 0.5f) *
                                            swayFactor;
                        swayOffset += transform.forward *
                                     Mathf.PerlinNoise(origIndex * 0.1f + 100f, Time.time * 0.5f) *
                                     swayFactor;

                        animatedVertices[origIndex] = originalVertex + pulseOffset + swayOffset;
                    }
                    else
                    {
                        animatedVertices[origIndex] = originalVertex;
                    }

                    // Update energy glow with pulse
                    Color originalColor = colors[origIndex];
                    float pulseGlow = 1f + pulse * 0.3f * (1 - t);
                    animatedColors[origIndex] = new Color(
                        originalColor.r,
                        originalColor.g,
                        originalColor.b,
                        originalColor.a * pulseGlow
                    );

                    vertexIndex++;
                }
            }
        }

        mesh.vertices = animatedVertices;
        mesh.colors = animatedColors;
        mesh.RecalculateNormals();

        // Update collider occasionally for performance
        if (Time.frameCount % 45 == 0)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    void UpdateMaterialProperties()
    {
        // Subtle variation in energy color over time
        float hueShift = Mathf.PerlinNoise(Time.time * 0.05f, 0f) * 0.1f;
        Color variedEnergyColor = Color.Lerp(coreEnergyColor, tipEnergyColor, hueShift);

        proceduralMaterial.SetColor(EmissionColor, variedEnergyColor * glowIntensity);

        // Add scrolling energy effect using noise
        proceduralMaterial.SetFloat(NoiseScale, energyNoiseScale);
        proceduralMaterial.SetFloat("_DetailAlbedoScale", Time.time * 0.1f);
    }

    void OnDestroy()
    {
        // Clean up procedural material
        if (proceduralMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(proceduralMaterial);
            }
            else
            {
                DestroyImmediate(proceduralMaterial);
            }
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (roots != null)
        {
            Gizmos.color = Color.magenta;
            foreach (RootData root in roots)
            {
                if (root.segments != null && root.segments.Length > 1)
                {
                    for (int i = 0; i < root.segments.Length - 1; i++)
                    {
                        Gizmos.DrawLine(root.segments[i], root.segments[i + 1]);
                    }
                }
            }
        }
    }
}