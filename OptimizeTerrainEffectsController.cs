using System.Collections.Generic;
using UnityEngine;

public abstract class QuantumEffectBase : MonoBehaviour
{
    protected int currentLOD;
    protected Mesh proceduralMesh;
    protected Material proceduralMaterial;

    public abstract void Initialize();
    public abstract void SetLODLevel(int lodLevel);
    public abstract void ApplyQuantumFluctuation();
    public abstract void CleanUp();

    protected Material CreateProceduralMaterial(string shaderName, Color baseColor)
    {
        Material mat = new Material(Shader.Find(shaderName));
        mat.color = baseColor;
        mat.SetColor("_EmissionColor", baseColor * 0.5f);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    protected void AddParticleSystem(GameObject target, Color particleColor, int maxParticles)
    {
        ParticleSystem ps = target.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = particleColor;
        main.startSize = 0.1f;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateProceduralMaterial("Particles/Standard Unlit", particleColor);
    }

    protected Mesh GenerateIcosahedron(float radius)
    {
        Mesh mesh = new Mesh();

        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        Vector3[] vertices = {
            new Vector3(-1f,  t, 0f).normalized * radius,
            new Vector3( 1f,  t, 0f).normalized * radius,
            new Vector3(-1f, -t, 0f).normalized * radius,
            new Vector3( 1f, -t, 0f).normalized * radius,
            new Vector3(0f, -1f,  t).normalized * radius,
            new Vector3(0f,  1f,  t).normalized * radius,
            new Vector3(0f, -1f, -t).normalized * radius,
            new Vector3(0f,  1f, -t).normalized * radius,
            new Vector3( t, 0f, -1f).normalized * radius,
            new Vector3( t, 0f,  1f).normalized * radius,
            new Vector3(-t, 0f, -1f).normalized * radius,
            new Vector3(-t, 0f,  1f).normalized * radius
        };

        int[] triangles = {
            0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
            1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
            3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
            4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}

public class QuantumSingularity : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;
    private SphereCollider _gravityField;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateIcosahedron(1f);
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(1f, 0.2f, 0.2f, 0.7f));
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(1f, 0.2f, 0.2f, 0.7f), 120);
        _particleSystem = GetComponent<ParticleSystem>();

        var velocity = _particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.radial = new ParticleSystem.MinMaxCurve(-2f);

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 5f;
        _glowLight.color = new Color(1f, 0.1f, 0.1f);
        _glowLight.intensity = 2f;

        _gravityField = gameObject.AddComponent<SphereCollider>();
        _gravityField.isTrigger = true;
        _gravityField.radius = 3f;
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 25f : (lodLevel == 1 ? 12f : 5f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float pulse = Mathf.PingPong(Time.time, 1f);
        proceduralMaterial.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.1f) * pulse);

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startSize = 0.05f + pulse * 0.1f;
        }

        if (_glowLight != null)
        {
            _glowLight.intensity = 1.5f + pulse;
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);
    }
}

public class PlasmaGeode : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateGeodeMesh();
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(0.1f, 0.8f, 0.9f, 0.7f));
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(0.2f, 0.9f, 1f, 0.6f), 90);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 4f;
        _glowLight.color = new Color(0.2f, 0.8f, 1f);
        _glowLight.intensity = 1.6f;
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 18f : (lodLevel == 1 ? 9f : 4f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float energy = Mathf.PerlinNoise(Time.time * 2f, 0) * 0.5f + 0.5f;
        Color plasmaColor = Color.Lerp(new Color(0.1f, 0.5f, 0.8f), new Color(0.3f, 1f, 1f), energy);
        proceduralMaterial.color = plasmaColor;
        proceduralMaterial.SetColor("_EmissionColor", plasmaColor * energy);

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = plasmaColor;
        }

        if (_glowLight != null)
        {
            _glowLight.color = plasmaColor;
            _glowLight.intensity = 1f + energy;
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);
    }

    private Mesh GenerateGeodeMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int segments = 16;
        float radius = 1f;

        for (int i = 0; i <= segments / 2; i++)
        {
            float lat = Mathf.PI * i / segments;
            for (int j = 0; j <= segments; j++)
            {
                float lon = 2 * Mathf.PI * j / segments;

                float x = Mathf.Sin(lat) * Mathf.Cos(lon) * radius;
                float y = Mathf.Cos(lat) * radius;
                float z = Mathf.Sin(lat) * Mathf.Sin(lon) * radius;

                vertices.Add(new Vector3(x, y, z));
            }
        }

        for (int i = 0; i < segments / 2; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int first = (i * (segments + 1)) + j;
                int second = first + segments + 1;

                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }

        int crystalCount = 8;
        int vertexOffset = vertices.Count;

        for (int c = 0; c < crystalCount; c++)
        {
            float angle = 2 * Mathf.PI * c / crystalCount;
            Vector3 center = new Vector3(
                Mathf.Cos(angle) * 0.3f,
                -0.2f,
                Mathf.Sin(angle) * 0.3f
            );

            vertices.Add(center + new Vector3(0, 0.4f, 0));
            vertices.Add(center + new Vector3(-0.1f, 0, -0.1f));
            vertices.Add(center + new Vector3(0.1f, 0, -0.1f));
            vertices.Add(center + new Vector3(0.1f, 0, 0.1f));
            vertices.Add(center + new Vector3(-0.1f, 0, 0.1f));

            int crystalOffset = vertexOffset + c * 5;

            triangles.Add(crystalOffset); triangles.Add(crystalOffset + 1); triangles.Add(crystalOffset + 2);
            triangles.Add(crystalOffset); triangles.Add(crystalOffset + 2); triangles.Add(crystalOffset + 3);
            triangles.Add(crystalOffset); triangles.Add(crystalOffset + 3); triangles.Add(crystalOffset + 4);
            triangles.Add(crystalOffset); triangles.Add(crystalOffset + 4); triangles.Add(crystalOffset + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}

public class NeuralNetworkFungus : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;
    private LineRenderer[] _neuralConnections;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateFungusMesh();
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(0.8f, 0.2f, 0.8f, 0.7f));
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(0.9f, 0.3f, 0.9f, 0.6f), 70);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 3.5f;
        _glowLight.color = new Color(0.8f, 0.2f, 0.8f);
        _glowLight.intensity = 1.4f;

        CreateNeuralConnections();
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 15f : (lodLevel == 1 ? 8f : 3f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }

        if (_neuralConnections != null)
        {
            foreach (var connection in _neuralConnections)
            {
                connection.enabled = lodLevel < 2;
            }
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float pulse = Mathf.PingPong(Time.time * 2f, 1f);
        Color pulseColor = Color.Lerp(new Color(0.7f, 0.1f, 0.7f), new Color(1f, 0.5f, 1f), pulse);
        proceduralMaterial.color = pulseColor;
        proceduralMaterial.SetColor("_EmissionColor", pulseColor * 0.5f);

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = pulseColor;
        }

        if (_glowLight != null)
        {
            _glowLight.color = pulseColor;
            _glowLight.intensity = 1f + pulse * 0.5f;
        }

        if (_neuralConnections != null)
        {
            foreach (var connection in _neuralConnections)
            {
                connection.startColor = pulseColor;
                connection.endColor = pulseColor;
            }
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);

        if (_neuralConnections != null)
        {
            foreach (var connection in _neuralConnections)
            {
                if (connection != null) DestroyImmediate(connection.gameObject);
            }
        }
    }

    private Mesh GenerateFungusMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int segments = 16;
        float capRadius = 0.8f;
        float stemRadius = 0.2f;
        float height = 1.2f;

        for (int i = 0; i <= segments; i++)
        {
            float lat = Mathf.PI * 0.5f * i / segments;
            for (int j = 0; j <= segments; j++)
            {
                float lon = 2 * Mathf.PI * j / segments;

                float r = Mathf.Sin(lat) * capRadius;
                float y = Mathf.Cos(lat) * capRadius * 0.5f + height * 0.7f;

                float x = Mathf.Cos(lon) * r;
                float z = Mathf.Sin(lon) * r;

                float noise = Mathf.PerlinNoise(x * 3f, z * 3f) * 0.1f;
                x += noise;
                z += noise;

                vertices.Add(new Vector3(x, y, z));
            }
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float y = t * height * 0.7f;
            float radius = stemRadius * (1 - t * 0.3f);

            for (int j = 0; j <= segments; j++)
            {
                float angle = 2 * Mathf.PI * j / segments;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                vertices.Add(new Vector3(x, y, z));
            }
        }

        int capVertexOffset = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int first = (i * (segments + 1)) + j + capVertexOffset;
                int second = first + segments + 1;

                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }

        int stemVertexOffset = (segments + 1) * (segments + 1);
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int first = (i * (segments + 1)) + j + stemVertexOffset;
                int second = first + segments + 1;

                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private void CreateNeuralConnections()
    {
        int connectionCount = Random.Range(4, 8);
        _neuralConnections = new LineRenderer[connectionCount];

        for (int i = 0; i < connectionCount; i++)
        {
            GameObject connectionObj = new GameObject("NeuralConnection");
            connectionObj.transform.SetParent(transform);
            connectionObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = connectionObj.AddComponent<LineRenderer>();
            lr.material = CreateProceduralMaterial("Particles/Standard Unlit", new Color(0.9f, 0.3f, 0.9f, 0.7f));
            lr.startWidth = 0.05f;
            lr.endWidth = 0.02f;
            lr.positionCount = 6;

            for (int j = 0; j < lr.positionCount; j++)
            {
                float t = j / (float)(lr.positionCount - 1);
                float x = Mathf.Sin(t * Mathf.PI) * 0.5f;
                float y = 0.5f + t * 0.8f;
                float z = Mathf.Cos(t * Mathf.PI) * 0.5f;

                lr.SetPosition(j, new Vector3(x, y, z));
            }

            _neuralConnections[i] = lr;
        }
    }
}

public class VoidTendrilNexus : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;
    private LineRenderer[] _voidTendrils;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateIcosahedron(1);
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(0.1f, 0.1f, 0.1f, 0.9f));
        proceduralMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.05f, 0.05f));
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(0.05f, 0.05f, 0.1f, 0.8f), 80);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 4f;
        _glowLight.color = new Color(0.05f, 0.05f, 0.1f);
        _glowLight.intensity = 0.8f;

        CreateVoidTendrils();
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 12f : (lodLevel == 1 ? 6f : 2f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }

        if (_voidTendrils != null)
        {
            foreach (var tendril in _voidTendrils)
            {
                tendril.enabled = lodLevel < 2;
            }
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float voidPulse = Mathf.PerlinNoise(Time.time * 1.5f, 0) * 0.3f;
        Color voidColor = new Color(0.05f + voidPulse, 0.05f + voidPulse, 0.1f + voidPulse * 0.5f);
        proceduralMaterial.SetColor("_EmissionColor", voidColor);

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = voidColor;
        }

        if (_glowLight != null)
        {
            _glowLight.color = voidColor;
            _glowLight.intensity = 0.5f + voidPulse * 0.5f;
        }

        if (_voidTendrils != null)
        {
            foreach (var tendril in _voidTendrils)
            {
                AnimateVoidTendril(tendril);
            }
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);

        if (_voidTendrils != null)
        {
            foreach (var tendril in _voidTendrils)
            {
                if (tendril != null) DestroyImmediate(tendril.gameObject);
            }
        }
    }

    private void CreateVoidTendrils()
    {
        int tendrilCount = Random.Range(5, 9);
        _voidTendrils = new LineRenderer[tendrilCount];

        for (int i = 0; i < tendrilCount; i++)
        {
            GameObject tendrilObj = new GameObject("VoidTendril");
            tendrilObj.transform.SetParent(transform);
            tendrilObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = tendrilObj.AddComponent<LineRenderer>();
            lr.material = CreateProceduralMaterial("Particles/Standard Unlit", new Color(0.1f, 0.1f, 0.2f, 0.8f));
            lr.startWidth = 0.08f;
            lr.endWidth = 0.02f;
            lr.positionCount = 10;

            for (int j = 0; j < lr.positionCount; j++)
            {
                float t = j / (float)(lr.positionCount - 1);
                float angle = 2 * Mathf.PI * i / tendrilCount;
                float x = Mathf.Cos(angle) * (0.2f + t * 1.5f);
                float y = Mathf.Sin(t * Mathf.PI * 0.5f) * 0.5f;
                float z = Mathf.Sin(angle) * (0.2f + t * 1.5f);

                x += Random.Range(-0.1f, 0.1f);
                z += Random.Range(-0.1f, 0.1f);

                lr.SetPosition(j, new Vector3(x, y, z));
            }

            _voidTendrils[i] = lr;
        }
    }

    private void AnimateVoidTendril(LineRenderer tendril)
    {
        Vector3[] positions = new Vector3[tendril.positionCount];
        tendril.GetPositions(positions);

        float time = Time.time;

        for (int i = 1; i < positions.Length; i++)
        {
            float t = i / (float)(positions.Length - 1);
            float wave = Mathf.Sin(time * 1.2f + i * 0.7f) * 0.15f;
            float twist = Mathf.Cos(time * 0.8f + i * 0.5f) * 0.1f;

            positions[i] += new Vector3(wave, twist, wave);
        }

        tendril.SetPositions(positions);
    }
}

public class HyperdimensionalPolyhedron : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateHyperdimensionalMesh();
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(0.6f, 0.1f, 0.6f, 0.8f));
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(0.7f, 0.2f, 0.7f, 0.6f), 100);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 4.5f;
        _glowLight.color = new Color(0.6f, 0.1f, 0.6f);
        _glowLight.intensity = 1.7f;
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 20f : (lodLevel == 1 ? 10f : 4f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float phase = Mathf.PingPong(Time.time * 1.5f, 1f);
        Color phaseColor = Color.Lerp(new Color(0.5f, 0.1f, 0.5f), new Color(0.8f, 0.3f, 0.8f), phase);
        proceduralMaterial.color = phaseColor;
        proceduralMaterial.SetColor("_EmissionColor", phaseColor * (0.5f + phase * 0.5f));

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startColor = phaseColor;
        }

        if (_glowLight != null)
        {
            _glowLight.color = phaseColor;
            _glowLight.intensity = 1.2f + phase * 0.8f;
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);
    }

    private Mesh GenerateHyperdimensionalMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float size = 0.7f;

        Vector3[] cubeVertices = {
            new Vector3(-size, -size, -size),
            new Vector3(size, -size, -size),
            new Vector3(size, size, -size),
            new Vector3(-size, size, -size),
            new Vector3(-size, -size, size),
            new Vector3(size, -size, size),
            new Vector3(size, size, size),
            new Vector3(-size, size, size)
        };

        vertices.AddRange(cubeVertices);

        float wProjection = 0.3f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 vertex = cubeVertices[i];
            Vector3 projected = new Vector3(
                vertex.x / (1 + wProjection),
                vertex.y / (1 + wProjection),
                vertex.z / (1 + wProjection)
            );
            vertices.Add(projected);
        }

        int[] cubeTriangles = {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 7, 3, 0, 4, 7,
            1, 2, 6, 1, 6, 5,
            3, 6, 2, 3, 7, 6,
            0, 1, 5, 0, 5, 4
        };

        triangles.AddRange(cubeTriangles);

        for (int i = 0; i < cubeTriangles.Length; i++)
        {
            triangles.Add(cubeTriangles[i] + 8);
        }

        for (int i = 0; i < 8; i++)
        {
            triangles.Add(i);
            triangles.Add(i + 8);
            triangles.Add((i + 1) % 8);

            triangles.Add(i + 8);
            triangles.Add((i + 1) % 8 + 8);
            triangles.Add((i + 1) % 8);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}

public class SentientMistFormation : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateMistMesh();
        mf.mesh = proceduralMesh;

        proceduralMaterial = CreateProceduralMaterial("Standard", new Color(0.8f, 0.8f, 1f, 0.4f));
        proceduralMaterial.SetFloat("_Glossiness", 0.2f);
        mr.material = proceduralMaterial;

        AddParticleSystem(gameObject, new Color(0.9f, 0.9f, 1f, 0.3f), 150);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 6f;
        _glowLight.color = new Color(0.8f, 0.8f, 1f);
        _glowLight.intensity = 1.0f;
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 30f : (lodLevel == 1 ? 15f : 7f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        float mistSway = Mathf.Sin(Time.time * 0.5f) * 0.2f;
        transform.localScale = Vector3.one * (1f + mistSway);

        if (_particleSystem != null)
        {
            var main = _particleSystem.main;
            main.startSize = 0.4f + mistSway * 0.2f;
        }
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (proceduralMaterial != null) DestroyImmediate(proceduralMaterial);
    }

    private Mesh GenerateMistMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int cloudCount = 4;
        int segments = 10;

        for (int c = 0; c < cloudCount; c++)
        {
            Vector3 center = Random.insideUnitSphere * 0.4f;
            float radius = Random.Range(0.4f, 0.7f);
            int vertexOffset = vertices.Count;

            for (int i = 0; i <= segments; i++)
            {
                float lat = Mathf.PI * i / segments;
                for (int j = 0; j <= segments; j++)
                {
                    float lon = 2 * Mathf.PI * j / segments;

                    float x = Mathf.Sin(lat) * Mathf.Cos(lon) * radius;
                    float y = Mathf.Cos(lat) * radius;
                    float z = Mathf.Sin(lat) * Mathf.Sin(lon) * radius;

                    vertices.Add(center + new Vector3(x, y, z));
                }
            }

            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int first = (i * (segments + 1)) + j + vertexOffset;
                    int second = first + segments + 1;

                    triangles.Add(first);
                    triangles.Add(second);
                    triangles.Add(first + 1);

                    triangles.Add(second);
                    triangles.Add(second + 1);
                    triangles.Add(first + 1);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}

public class RealityGlitchCluster : QuantumEffectBase
{
    private ParticleSystem _particleSystem;
    private Light _glowLight;
    private Material[] _glitchMaterials;

    public override void Initialize()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();

        proceduralMesh = GenerateGlitchMesh();
        mf.mesh = proceduralMesh;

        _glitchMaterials = new Material[3];
        _glitchMaterials[0] = CreateProceduralMaterial("Standard", new Color(1f, 0f, 0f, 0.7f));
        _glitchMaterials[1] = CreateProceduralMaterial("Standard", new Color(0f, 1f, 0f, 0.7f));
        _glitchMaterials[2] = CreateProceduralMaterial("Standard", new Color(0f, 0f, 1f, 0.7f));
        mr.material = _glitchMaterials[0];

        AddParticleSystem(gameObject, new Color(1f, 1f, 1f, 0.5f), 100);
        _particleSystem = GetComponent<ParticleSystem>();

        _glowLight = gameObject.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.range = 4f;
        _glowLight.color = Color.white;
        _glowLight.intensity = 1.5f;
    }

    public override void SetLODLevel(int lodLevel)
    {
        currentLOD = lodLevel;

        if (_particleSystem != null)
        {
            var emission = _particleSystem.emission;
            emission.rateOverTime = lodLevel == 0 ? 25f : (lodLevel == 1 ? 12f : 5f);
        }

        if (_glowLight != null)
        {
            _glowLight.enabled = lodLevel < 2;
        }
    }

    public override void ApplyQuantumFluctuation()
    {
        int materialIndex = Random.Range(0, 3);
        GetComponent<MeshRenderer>().material = _glitchMaterials[materialIndex];

        Vector3[] vertices = proceduralMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (Random.value < 0.1f)
            {
                vertices[i] += Random.insideUnitSphere * 0.2f;
            }
        }
        proceduralMesh.vertices = vertices;
        proceduralMesh.RecalculateNormals();
    }

    public override void CleanUp()
    {
        if (proceduralMesh != null) DestroyImmediate(proceduralMesh);
        if (_glitchMaterials != null)
        {
            foreach (var mat in _glitchMaterials)
            {
                if (mat != null) DestroyImmediate(mat);
            }
        }
    }

    private Mesh GenerateGlitchMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int cubeCount = 5;
        for (int i = 0; i < cubeCount; i++)
        {
            Vector3 center = Random.insideUnitSphere * 0.5f;
            float size = Random.Range(0.2f, 0.6f);
            int vertexOffset = vertices.Count;

            vertices.Add(center + new Vector3(-size, -size, -size));
            vertices.Add(center + new Vector3(size, -size, -size));
            vertices.Add(center + new Vector3(size, size, -size));
            vertices.Add(center + new Vector3(-size, size, -size));
            vertices.Add(center + new Vector3(-size, -size, size));
            vertices.Add(center + new Vector3(size, -size, size));
            vertices.Add(center + new Vector3(-size, -size, size));
            vertices.Add(center + new Vector3(size, -size, size));
            vertices.Add(center + new Vector3(size, size, size));
            vertices.Add(center + new Vector3(-size, size, size));

            int[] cubeTriangles = {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 7, 3, 0, 4, 7,
                1, 2, 6, 1, 6, 5,
                3, 6, 2, 3, 7, 6,
                0, 1, 5, 0, 5, 4
            };

            for (int j = 0; j < cubeTriangles.Length; j++)
            {
                triangles.Add(cubeTriangles[j] + vertexOffset);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}