using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CosmicVoidTree : MonoBehaviour
{
    [System.Serializable]
    public class BranchNode
    {
        public Vector3 start;
        public Vector3 end;
        public float startRadius;
        public float endRadius;
        public int depth;
        public bool isVine;
        public string name;
        public float pulseOffset;
        public float swayOffset;
    }

    [Header("Tree Structure")]
    [Range(3, 8)] public int branchLevels = 5;
    [Range(1, 6)] public int minBranchesPerNode = 2;
    [Range(2, 8)] public int maxBranchesPerNode = 4;
    [Range(10f, 50f)] public float startLength = 30f;
    [Range(0.4f, 0.9f)] public float lengthDecay = 0.65f;
    [Range(1f, 5f)] public float startRadius = 3f;
    [Range(0.5f, 0.9f)] public float radiusDecay = 0.75f;
    [Range(0.1f, 1f)] public float curvatureFactor = 0.4f;
    [Range(0f, 0.5f)] public float gravityInfluence = 0.15f;
    [Range(0.5f, 2f)] public float spreadFactor = 1.2f;
    [Range(0f, 1f)] public float tanglingFactor = 0.3f;

    [Header("Vine Properties")]
    public bool generateVines = true;
    [Range(5, 40)] public int vineCount = 20;
    [Range(5f, 30f)] public float vineLengthMin = 10f;
    [Range(15f, 40f)] public float vineLengthMax = 25f;
    [Range(0.2f, 1f)] public float vineThickness = 0.4f;
    [Range(0.1f, 1f)] public float vineDroop = 0.5f;
    [Range(0.1f, 1f)] public float vineCurvature = 0.7f;

    [Header("Root Properties")]
    [Range(5, 30)] public int rootCount = 15;
    [Range(0.3f, 2.5f)] public float rootBaseThickness = 1f;
    [Range(0f, 0.5f)] public float rootThicknessVariation = 0.2f;
    [Range(0.5f, 8f)] public float rootCurlIntensity = 4f;
    [Range(0f, 1f)] public float rootGravityInfluence = 0.4f;
    [Range(0.5f, 3f)] public float rootSpreadRadius = 1.8f;
    [Range(0f, 1f)] public float rootTanglingFactor = 0.6f;

    [Header("Appearance")]
    public Gradient trunkColorGradient;
    public Gradient vineColorGradient;
    public Gradient rootColorGradient;
    [Range(1f, 10f)] public float emissionIntensity = 3f;
    [Range(0.1f, 2f)] public float pulseSpeed = 0.5f;
    [Range(0f, 0.5f)] public float pulseAmount = 0.25f;
    [Range(0.1f, 2f)] public float swaySpeed = 0.4f;
    [Range(0f, 1f)] public float swayIntensity = 0.6f;
    [Range(4, 10)] public int radialResolution = 6;

    [Header("Lighting")]
    public bool addLights = true;
    [Range(1f, 10f)] public float lightIntensity = 3.5f;
    [Range(5f, 20f)] public float lightRange = 12f;
    public Color lightColor = Color.cyan;
    [Range(1, 10)] public int lightsPerBranchLevel = 4;

    [Header("Quantum Effects")]
    [Range(0f, 1f)] public float distortionAmount = 0.3f;
    [Range(0.1f, 3f)] public float morphFrequency = 1f;
    public bool enablePhysics = false;

    private List<BranchNode> nodes = new List<BranchNode>();
    private List<BranchNode> vineNodes = new List<BranchNode>();
    private List<BranchNode> rootNodes = new List<BranchNode>();
    private List<Light> pointLights = new List<Light>();

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Material trunkMaterial;
    private Material vineMaterial;
    private Material rootMaterial;

    private float animationTime;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float randomOffset;

    // Root name generation components
    private string[] rootPrefixes = { "Void", "Quantum", "Ethereal", "Celestial", "Astral", "Cosmic", "Nebula", "Shadow", "Obsidian", "Umbra" };
    private string[] rootSuffixes = { "Tendril", "Root", "Vine", "Filament", "Fiber", "Strand", "Cord", "Thread", "Vein" };
    private Dictionary<int, string> rootNames = new Dictionary<int, string>();

    void Start()
    {
        // Initialize gradients if not set
        if (trunkColorGradient.alphaKeys.Length == 0)
        {
            trunkColorGradient = CreateQuantumGradient();
        }
        if (vineColorGradient.alphaKeys.Length == 0)
        {
            vineColorGradient = CreateVineGradient();
        }
        if (rootColorGradient.alphaKeys.Length == 0)
        {
            rootColorGradient = CreateRootGradient();
        }

        randomOffset = Random.Range(0f, 1000f);
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Generate the tree structure
        GenerateSkeleton(Vector3.zero, Vector3.up, startLength, startRadius, 0, null);

        // Generate roots
        GenerateRootSystem();

        // Generate vines if enabled
        if (generateVines)
        {
            GenerateVines();
        }

        // Create the mesh
        mesh = GenerateMesh();
        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        // Store original vertices for animation
        originalVertices = mesh.vertices;

        // Create materials
        ApplyMaterials();

        // Add lights if enabled
        if (addLights)
        {
            AddLights();
        }

        // Add physics if enabled
        if (enablePhysics)
        {
            AddPhysicsComponents();
        }

        // Name the tree with a cosmic variety name
        gameObject.name = GenerateCosmicName();
    }

    void Update()
    {
        animationTime += Time.deltaTime;

        // Pulse emission for dynamic effect
        PulseEmission();

        // Animate the tree
        AnimateTree();
    }

    void GenerateSkeleton(Vector3 pos, Vector3 dir, float length, float radius, int depth, BranchNode parent)
    {
        if (depth >= branchLevels) return;

        // Calculate end position with some curvature and spread
        Vector3 curvedDir = (dir + Random.insideUnitSphere * curvatureFactor).normalized;

        // Apply spread factor to make branches spread out more
        if (depth > 0)
        {
            curvedDir = Vector3.Slerp(curvedDir, Vector3.Normalize(curvedDir + Vector3.up * 0.2f), spreadFactor * depth / branchLevels);
        }

        Vector3 end = pos + curvedDir * length;

        // Apply gravity influence to lower branches
        if (depth > 1)
        {
            end += Vector3.down * gravityInfluence * depth;
        }

        float endRadius = radius * radiusDecay;

        BranchNode newNode = new BranchNode
        {
            start = pos,
            end = end,
            startRadius = radius,
            endRadius = endRadius,
            depth = depth,
            isVine = false,
            pulseOffset = Random.Range(0f, Mathf.PI * 2),
            swayOffset = Random.Range(0f, Mathf.PI * 2)
        };

        nodes.Add(newNode);

        // Apply tangling effect - branches attract each other
        if (tanglingFactor > 0 && depth > 0)
        {
            Vector3 tanglingForce = CalculateTanglingForce(newNode, nodes);
            end = Vector3.Lerp(end, end + tanglingForce, tanglingFactor * 0.1f);
            newNode.end = end;
        }

        // Determine number of branches for this node
        int branches = Random.Range(minBranchesPerNode, maxBranchesPerNode + 1);

        for (int i = 0; i < branches; i++)
        {
            // Calculate branch direction with some randomness and spread
            float angleVariation = 35f + Random.Range(15f, 50f);
            Vector3 branchDir = Quaternion.AngleAxis(angleVariation, Random.onUnitSphere) * dir;

            // Make branches spread out more naturally
            if (depth > 0)
            {
                float horizontalSpread = Random.Range(0.7f, 1.3f);
                branchDir = new Vector3(
                    branchDir.x * horizontalSpread,
                    Mathf.Max(0.1f, branchDir.y * 0.8f),
                    branchDir.z * horizontalSpread
                ).normalized;
            }

            // Reduce length and radius for child branches
            float childLength = length * lengthDecay * Random.Range(0.8f, 1.1f);
            float childRadius = endRadius * radiusDecay * Random.Range(0.7f, 1f);

            GenerateSkeleton(end, branchDir, childLength, childRadius, depth + 1, newNode);
        }
    }

    Vector3 CalculateTanglingForce(BranchNode currentNode, List<BranchNode> allNodes)
    {
        Vector3 totalForce = Vector3.zero;
        int nearbyBranches = 0;

        foreach (BranchNode otherNode in allNodes)
        {
            if (otherNode == currentNode) continue;

            float distance = Vector3.Distance(currentNode.end, otherNode.end);
            if (distance < 5f && distance > 0.1f)
            {
                Vector3 forceDir = (otherNode.end - currentNode.end).normalized;
                float forceStrength = Mathf.Clamp01(1 - distance / 5f) * 0.3f;
                totalForce += forceDir * forceStrength;
                nearbyBranches++;
            }
        }

        return nearbyBranches > 0 ? totalForce / nearbyBranches : Vector3.zero;
    }

    void GenerateRootSystem()
    {
        // Generate primary roots
        for (int i = 0; i < rootCount; i++)
        {
            float angle = i * Mathf.PI * 2 / rootCount;
            float distance = Random.Range(rootSpreadRadius * 0.7f, rootSpreadRadius * 1.3f);
            Vector3 origin = transform.position + new Vector3(
                Mathf.Cos(angle) * distance,
                -1f,
                Mathf.Sin(angle) * distance
            );

            BranchNode root = new BranchNode
            {
                start = origin,
                end = origin + Vector3.down * Random.Range(3f, 8f),
                startRadius = rootBaseThickness,
                endRadius = rootBaseThickness * 0.4f,
                depth = -2,
                isVine = false,
                pulseOffset = Random.Range(0f, Mathf.PI * 2),
                swayOffset = Random.Range(0f, Mathf.PI * 2),
                name = GenerateRootName(i)
            };

            // Apply root-specific curvature
            Vector3 rootDir = (root.end - root.start).normalized;
            float noiseX = Mathf.PerlinNoise(root.start.x * 0.1f, i * 0.1f) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(root.start.z * 0.1f, i * 0.1f) * 2f - 1f;
            float noiseY = (Mathf.PerlinNoise(root.start.y * 0.1f, i * 0.1f) * 2f - 1f) * (1 - rootGravityInfluence);

            Vector3 curl = new Vector3(noiseX, noiseY, noiseZ).normalized * rootCurlIntensity * 0.01f;
            rootDir = (rootDir + curl).normalized;

            // Apply gravity influence
            rootDir = Vector3.Lerp(rootDir, Vector3.down, rootGravityInfluence).normalized;
            root.end = root.start + rootDir * Random.Range(5f, 15f);

            rootNodes.Add(root);
            rootNames[i] = root.name;
        }
    }

    string GenerateRootName(int id)
    {
        string prefix = rootPrefixes[id % rootPrefixes.Length];
        string suffix = rootSuffixes[(id * 7) % rootSuffixes.Length];
        return $"{prefix}{suffix}-{id % 100:00}";
    }

    void GenerateVines()
    {
        // Select random branches to attach vines to (prefer middle-depth branches)
        var possibleAttachPoints = nodes.Where(n => n.depth > 1 && n.depth < branchLevels - 1).ToList();

        if (possibleAttachPoints.Count == 0) return;

        for (int i = 0; i < vineCount; i++)
        {
            // Select a random attachment point
            var attachPoint = possibleAttachPoints[Random.Range(0, possibleAttachPoints.Count)];
            Vector3 attachPos = Vector3.Lerp(attachPoint.start, attachPoint.end, Random.Range(0.3f, 0.8f));

            // Determine vine length
            float vineLength = Random.Range(vineLengthMin, vineLengthMax);

            // Create vine with droop and curvature effects
            Vector3 vineDir = Vector3.down + Random.insideUnitSphere * vineCurvature;
            Vector3 vineEnd = attachPos + vineDir.normalized * vineLength;

            // Add some randomness to the end position
            vineEnd += Random.insideUnitSphere * vineLength * vineDroop;

            vineNodes.Add(new BranchNode
            {
                start = attachPos,
                end = vineEnd,
                startRadius = vineThickness,
                endRadius = vineThickness * 0.4f,
                depth = -1,
                isVine = true,
                pulseOffset = Random.Range(0f, Mathf.PI * 2),
                swayOffset = Random.Range(0f, Mathf.PI * 2)
            });
        }
    }

    Mesh GenerateMesh()
    {
        List<CombineInstance> combines = new List<CombineInstance>();

        // Generate trunk and branches
        foreach (var branch in nodes)
        {
            int segments = Mathf.Max(8, Mathf.CeilToInt(branch.startRadius * 2f));
            Mesh tube = GenerateTube(branch.start, branch.end, branch.startRadius, branch.endRadius, segments, false);
            combines.Add(new CombineInstance { mesh = tube, transform = Matrix4x4.identity });
        }

        // Generate vines
        foreach (var vine in vineNodes)
        {
            int segments = Mathf.Max(6, Mathf.CeilToInt(vine.startRadius * 3f));
            Mesh tube = GenerateTube(vine.start, vine.end, vine.startRadius, vine.endRadius, segments, true);
            combines.Add(new CombineInstance { mesh = tube, transform = Matrix4x4.identity });
        }

        // Generate roots
        foreach (var root in rootNodes)
        {
            int segments = Mathf.Max(6, Mathf.CeilToInt(root.startRadius * 3f));
            Mesh tube = GenerateTube(root.start, root.end, root.startRadius, root.endRadius, segments, false);
            combines.Add(new CombineInstance { mesh = tube, transform = Matrix4x4.identity });
        }

        Mesh combined = new Mesh();
        combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combined.CombineMeshes(combines.ToArray(), true, false, false);
        combined.RecalculateNormals();
        combined.RecalculateTangents();
        combined.RecalculateBounds();

        return combined;
    }

    Mesh GenerateTube(Vector3 start, Vector3 end, float startRadius, float endRadius, int segments, bool isVine)
    {
        Mesh mesh = new Mesh();
        mesh.name = isVine ? "QuantumVineSegment" : "QuantumBranchSegment";

        Vector3 dir = (end - start).normalized;
        float length = Vector3.Distance(start, end);

        // Create orthogonal basis
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        if (side.magnitude < 0.1f) side = Vector3.Cross(dir, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(side, dir).normalized;

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Color> colors = new List<Color>();

        // Create rings of vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) / (float)segments * Mathf.PI * 2f;
            Vector3 circle = Mathf.Cos(angle) * side + Mathf.Sin(angle) * up;

            // Start ring
            verts.Add(start + circle * startRadius);
            uvs.Add(new Vector2(i / (float)segments, 0));

            // Color based on position
            float colorValue = Mathf.PerlinNoise(start.x * 0.1f, start.z * 0.1f);
            colors.Add(new Color(colorValue, colorValue, colorValue, 1f));

            // End ring
            verts.Add(end + circle * endRadius);
            uvs.Add(new Vector2(i / (float)segments, 1));

            colorValue = Mathf.PerlinNoise(end.x * 0.1f, end.z * 0.1f);
            colors.Add(new Color(colorValue, colorValue, colorValue, 1f));
        }

        // Create triangles
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int i2 = (i * 2 + 2) % ((segments + 1) * 2);
            int i3 = (i * 2 + 3) % ((segments + 1) * 2);

            tris.AddRange(new int[] { i0, i2, i1, i1, i2, i3 });
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        return mesh;
    }

    void ApplyMaterials()
    {
        var mr = GetComponent<MeshRenderer>();
        Material[] materials = new Material[3];

        // Trunk material
        trunkMaterial = new Material(Shader.Find("Standard"));
        trunkMaterial.EnableKeyword("_EMISSION");
        Color trunkColor = trunkColorGradient.Evaluate(Random.value);
        trunkMaterial.color = trunkColor;
        trunkMaterial.SetColor("_EmissionColor", trunkColor * emissionIntensity);
        trunkMaterial.name = "QuantumTrunkMaterial";
        materials[0] = trunkMaterial;

        // Vine material
        vineMaterial = new Material(Shader.Find("Standard"));
        vineMaterial.EnableKeyword("_EMISSION");
        Color vineColor = vineColorGradient.Evaluate(Random.value);
        vineMaterial.color = vineColor;
        vineMaterial.SetColor("_EmissionColor", vineColor * emissionIntensity * 1.2f);
        vineMaterial.name = "QuantumVineMaterial";
        materials[1] = vineMaterial;

        // Root material
        rootMaterial = new Material(Shader.Find("Standard"));
        rootMaterial.EnableKeyword("_EMISSION");
        Color rootColor = rootColorGradient.Evaluate(Random.value);
        rootMaterial.color = rootColor;
        rootMaterial.SetColor("_EmissionColor", rootColor * emissionIntensity * 0.8f);
        rootMaterial.name = "QuantumRootMaterial";
        materials[2] = rootMaterial;

        mr.sharedMaterials = materials;
    }

    void PulseEmission()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterials.Length < 3) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount + 1f;

        // Pulse trunk material
        trunkMaterial.SetColor("_EmissionColor", trunkColorGradient.Evaluate(Mathf.PingPong(Time.time * 0.1f, 1f)) * emissionIntensity * pulse);

        // Pulse vine material with slight offset
        vineMaterial.SetColor("_EmissionColor", vineColorGradient.Evaluate(Mathf.PingPong(Time.time * 0.1f + 0.3f, 1f)) * emissionIntensity * 1.2f * (pulse + 0.1f));

        // Pulse root material
        rootMaterial.SetColor("_EmissionColor", rootColorGradient.Evaluate(Mathf.PingPong(Time.time * 0.1f + 0.6f, 1f)) * emissionIntensity * 0.8f * pulse);
    }

    void AnimateTree()
    {
        if (originalVertices == null || mesh == null) return;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        if (vertices.Length != originalVertices.Length) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Apply gentle swaying and pulsing to all vertices
            float timeOffset = randomOffset + i * 0.1f;
            float distortion =
                Mathf.PerlinNoise(originalVertices[i].x * morphFrequency + animationTime, originalVertices[i].z * morphFrequency) *
                Mathf.PerlinNoise(originalVertices[i].y * morphFrequency, animationTime * 0.5f) *
                distortionAmount;

            vertices[i] = originalVertices[i] + normals[i] * Mathf.Sin(animationTime + timeOffset) * distortion;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        // Apply gentle sway to the entire tree
        float swayX = Mathf.Sin(animationTime * swaySpeed) * swayIntensity * 0.5f;
        float swayZ = Mathf.Cos(animationTime * swaySpeed * 0.7f) * swayIntensity * 0.3f;
        transform.rotation = originalRotation * Quaternion.Euler(swayX, 0, swayZ);
    }

    void AddLights()
    {
        // Create a container for lights
        GameObject lightsContainer = new GameObject("QuantumLights");
        lightsContainer.transform.SetParent(transform);
        lightsContainer.transform.localPosition = Vector3.zero;

        // Add lights to random branches
        var lightBranches = nodes.Where(n => n.depth > 0 && n.depth < branchLevels - 1)
                                .OrderBy(x => Random.value)
                                .Take(lightsPerBranchLevel * branchLevels)
                                .ToList();

        foreach (var branch in lightBranches)
        {
            Vector3 lightPos = Vector3.Lerp(branch.start, branch.end, Random.Range(0.3f, 0.7f));

            GameObject lightObj = new GameObject("QuantumLight");
            lightObj.transform.SetParent(lightsContainer.transform);
            lightObj.transform.position = lightPos;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = lightRange;
            light.intensity = lightIntensity;
            light.color = lightColor;
            light.shadows = LightShadows.Soft;

            pointLights.Add(light);

            // Add slight flicker for organic feel
            if (Random.value > 0.7f)
            {
                FlickeringLight flicker = lightObj.AddComponent<FlickeringLight>();
                flicker.minIntensity = lightIntensity * 0.8f;
                flicker.maxIntensity = lightIntensity * 1.2f;
                flicker.flickerSpeed = Random.Range(0.5f, 2f);
            }
        }

        // Add lights to roots
        for (int i = 0; i < rootNodes.Count; i += 3)
        {
            if (i >= rootNodes.Count) break;

            Vector3 lightPos = Vector3.Lerp(rootNodes[i].start, rootNodes[i].end, 0.5f);

            GameObject lightObj = new GameObject("RootLight");
            lightObj.transform.SetParent(lightsContainer.transform);
            lightObj.transform.position = lightPos;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = lightRange * 0.7f;
            light.intensity = lightIntensity * 0.8f;
            light.color = Color.Lerp(lightColor, Color.red, 0.3f);
            light.shadows = LightShadows.Soft;

            pointLights.Add(light);
        }
    }

    void AddPhysicsComponents()
    {
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Add some force to make it float
        StartCoroutine(ApplyRandomForces());
    }

    IEnumerator ApplyRandomForces()
    {
        while (true)
        {
            Vector3 force = Random.insideUnitSphere * Random.Range(0.1f, 0.3f);
            transform.position += force * Time.deltaTime;

            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }

    Gradient CreateQuantumGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = new Color(0.1f, 0.8f, 1f); // Cyan
        colorKeys[0].time = 0f;
        colorKeys[1].color = new Color(0.8f, 0.1f, 1f); // Purple
        colorKeys[1].time = 0.5f;
        colorKeys[2].color = new Color(0.2f, 0.1f, 0.8f); // Deep Blue
        colorKeys[2].time = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    Gradient CreateVineGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = new Color(0.9f, 0.2f, 0.9f); // Magenta
        colorKeys[0].time = 0f;
        colorKeys[1].color = new Color(0.6f, 0.1f, 0.8f); // Purple
        colorKeys[1].time = 0.5f;
        colorKeys[2].color = new Color(0.3f, 0.8f, 1f); // Light Blue
        colorKeys[2].time = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    Gradient CreateRootGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = new Color(0.6f, 0.1f, 0.3f); // Deep Red
        colorKeys[0].time = 0f;
        colorKeys[1].color = new Color(0.3f, 0.1f, 0.6f); // Deep Purple
        colorKeys[1].time = 0.5f;
        colorKeys[2].color = new Color(0.1f, 0.3f, 0.6f); // Deep Blue
        colorKeys[2].time = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f;
        alphaKeys[1].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    string GenerateCosmicName()
    {
        string[] prefixes = { "Quantum", "Void", "Celestial", "Astral", "Nebula", "Cosmic", "Ethereal", "Luminous" };
        string[] cores = { "Vine", "Arbor", "Tree", "Growth", "Weaver", "Tendril", "Spire", "Canopy" };
        string[] suffixes = { "of the Void", "Eternal", "Radiant", "Umbral", "of Light", "of Stars", "Transcendent" };

        return $"{prefixes[Random.Range(0, prefixes.Length)]} {cores[Random.Range(0, cores.Length)]} {suffixes[Random.Range(0, suffixes.Length)]}";
    }

    // Public method to get root name by index
    public string GetRootName(int index)
    {
        if (index >= 0 && index < rootNodes.Count)
        {
            return rootNames.ContainsKey(index) ? rootNames[index] : "Unknown";
        }
        return "Invalid";
    }

    // Public method to get all active root names
    public List<string> GetAllRootNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < rootNodes.Count; i++)
        {
            if (rootNames.ContainsKey(i))
            {
                names.Add(rootNames[i]);
            }
        }
        return names;
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (nodes != null)
        {
            Gizmos.color = Color.green;
            foreach (BranchNode branch in nodes)
            {
                Gizmos.DrawLine(branch.start, branch.end);
            }
        }

        if (vineNodes != null)
        {
            Gizmos.color = Color.magenta;
            foreach (BranchNode vine in vineNodes)
            {
                Gizmos.DrawLine(vine.start, vine.end);
            }
        }

        if (rootNodes != null)
        {
            Gizmos.color = Color.red;
            foreach (BranchNode root in rootNodes)
            {
                Gizmos.DrawLine(root.start, root.end);
            }
        }
    }
}

// Helper class for light flickering
public class FlickeringLight : MonoBehaviour
{
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;
    public float flickerSpeed = 1f;

    private Light light;
    private float baseIntensity;
    private float offset;

    void Start()
    {
        light = GetComponent<Light>();
        baseIntensity = light.intensity;
        offset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + offset, 0f);
        light.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise) * baseIntensity;
    }
}