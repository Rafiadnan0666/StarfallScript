using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class QuantumVoidTree : MonoBehaviour
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
    }

    [Header("Tree Structure")]
    public int branchLevels = 5;
    public int minBranchesPerNode = 2;
    public int maxBranchesPerNode = 4;
    public float startLength = 30f;
    public float lengthDecay = 0.65f;
    public float startRadius = 3f;
    public float radiusDecay = 0.75f;
    public float curvatureFactor = 0.4f;
    public float gravityInfluence = 0.15f;
    public float spreadFactor = 1.2f;

    [Header("Vine Properties")]
    public bool generateVines = true;
    public int vineCount = 20;
    public float vineLengthMin = 10f;
    public float vineLengthMax = 25f;
    public float vineThickness = 0.4f;
    public float vineDroop = 0.5f;
    public float vineCurvature = 0.7f;

    [Header("Appearance")]
    public Gradient trunkColorGradient;
    public Gradient vineColorGradient;
    public float emissionIntensity = 3f;
    public float pulseSpeed = 0.5f;
    public float pulseAmount = 0.25f;

    [Header("Lighting")]
    public bool addLights = true;
    public float lightIntensity = 3.5f;
    public float lightRange = 12f;
    public Color lightColor = Color.cyan;
    public int lightsPerBranchLevel = 4;

    private List<BranchNode> nodes = new List<BranchNode>();
    private List<BranchNode> vineNodes = new List<BranchNode>();
    private List<Light> pointLights = new List<Light>();

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

        // Generate the tree structure
        GenerateSkeleton(Vector3.zero, Vector3.up, startLength, startRadius, 0);

        // Generate vines if enabled
        if (generateVines)
        {
            GenerateVines();
        }

        // Create the mesh
        Mesh mesh = GenerateMesh();
        var mf = GetComponent<MeshFilter>();
        var mc = GetComponent<MeshCollider>();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        // Create materials
        ApplyMaterials();

        // Add lights if enabled
        if (addLights)
        {
            AddLights();
        }

        // Name the tree with a cosmic variety name
        gameObject.name = GenerateCosmicName();
    }

    void Update()
    {
        // Pulse emission for dynamic effect
        PulseEmission();
    }

    void GenerateSkeleton(Vector3 pos, Vector3 dir, float length, float radius, int depth)
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

        nodes.Add(new BranchNode
        {
            start = pos,
            end = end,
            startRadius = radius,
            endRadius = endRadius,
            depth = depth,
            isVine = false
        });

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

            GenerateSkeleton(end, branchDir, childLength, childRadius, depth + 1);
        }
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
                isVine = true
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

        // Create rings of vertices
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i % segments) / (float)segments * Mathf.PI * 2f;
            Vector3 circle = Mathf.Cos(angle) * side + Mathf.Sin(angle) * up;

            // Start ring
            verts.Add(start + circle * startRadius);
            uvs.Add(new Vector2(i / (float)segments, 0));

            // End ring
            verts.Add(end + circle * endRadius);
            uvs.Add(new Vector2(i / (float)segments, 1));
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
        mesh.RecalculateNormals();

        return mesh;
    }

    void ApplyMaterials()
    {
        var mr = GetComponent<MeshRenderer>();
        Material[] materials = new Material[2];

        // Trunk material
        Material trunkMat = new Material(Shader.Find("Standard"));
        trunkMat.EnableKeyword("_EMISSION");
        Color trunkColor = trunkColorGradient.Evaluate(Random.value);
        trunkMat.color = trunkColor;
        trunkMat.SetColor("_EmissionColor", trunkColor * emissionIntensity);
        trunkMat.name = "QuantumTrunkMaterial";
        materials[0] = trunkMat;

        // Vine material
        Material vineMat = new Material(Shader.Find("Standard"));
        vineMat.EnableKeyword("_EMISSION");
        Color vineColor = vineColorGradient.Evaluate(Random.value);
        vineMat.color = vineColor;
        vineMat.SetColor("_EmissionColor", vineColor * emissionIntensity * 1.2f);
        vineMat.name = "QuantumVineMaterial";
        materials[1] = vineMat;

        mr.sharedMaterials = materials;
    }

    void PulseEmission()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterials.Length < 2) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount + 1f;

        // Pulse trunk material
        Material trunkMat = mr.sharedMaterials[0];
        Color trunkEmission = trunkMat.GetColor("_EmissionColor");
        trunkMat.SetColor("_EmissionColor", trunkEmission * pulse);

        // Pulse vine material with slight offset
        Material vineMat = mr.sharedMaterials[1];
        Color vineEmission = vineMat.GetColor("_EmissionColor");
        vineMat.SetColor("_EmissionColor", vineEmission * (pulse + 0.1f));
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

    string GenerateCosmicName()
    {
        string[] prefixes = { "Quantum", "Void", "Celestial", "Astral", "Nebula", "Cosmic", "Ethereal", "Luminous" };
        string[] cores = { "Vine", "Arbor", "Tree", "Growth", "Weaver", "Tendril", "Spire", "Canopy" };
        string[] suffixes = { "of the Void", "Eternal", "Radiant", "Umbral", "of Light", "of Stars", "Transcendent" };

        return $"{prefixes[Random.Range(0, prefixes.Length)]} {cores[Random.Range(0, cores.Length)]} {suffixes[Random.Range(0, suffixes.Length)]}";
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