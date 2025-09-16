using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Spawns **one** main object (with Health) and paints two dense grass layers that hug the generated path.
/// Attach this on the same GameObject that has both a Terrain + TerrainGenerator.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class ObjSpawnAndGrassDenseSpawnerController : MonoBehaviour
{
    [Header("Main Object (spawned only once)")]
    public GameObject objectPrefab;
    [Range(1, 1000)] public int objectMaxHealth = 200;
    public Vector3 objectUpOffset = new Vector3(0, 0.5f, 0);

    [Header("Grass Prototypes (exactly two)")]
    public Texture2D grassTextureA;
    public Texture2D grassTextureB;

    [Range(256, 1024)] public int grassResolution = 512;
    [Range(8, 32)] public int grassPatchesPerChunk = 16;
    [Range(0.5f, 5f)] public float grassMinWidth = 1f;
    [Range(0.5f, 5f)] public float grassMaxWidth = 2.4f;
    [Range(0.5f, 5f)] public float grassMinHeight = 1f;
    [Range(0.5f, 5f)] public float grassMaxHeight = 2.4f;

    [Header("Path Following")]
    [Tooltip("How wide (world units) the grass strip follows the path.")]
    public float pathGrassWidth = 12f;

    Terrain terrain;
    TerrainData data;
    List<Vector3> pathPts;

    BiomeType currentBiome;
    void Start()
    {
        terrain = GetComponent<Terrain>();
        data = terrain.terrainData;

        var gen = GetComponent<TerrainGenerator>();
        if (gen != null)
        {
            currentBiome = gen.currentBiome;
        }

        CachePath();
        SpawnMainObject();
        SetupGrassPrototypes();
        PaintDenseGrassAlongPath();
    }


    void CachePath()
    {
        // Grab private path list from TerrainGenerator via reflection
        var gen = GetComponent<TerrainGenerator>();
        if (gen == null) return;

        FieldInfo fi = typeof(TerrainGenerator).GetField("pathPoints", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null)
        {
            pathPts = fi.GetValue(gen) as List<Vector3>;
        }
    }

    #region MainObject

    void SpawnMainObject()
    {
        if (objectPrefab == null) return;

        Vector3 spawnPos = PickSpawnPosition();
        GameObject obj = Instantiate(objectPrefab, spawnPos + objectUpOffset, Quaternion.identity, transform);

        // attach Health if missing
        Health h = obj.GetComponent<Health>();
        if (h == null)
        {
            h = obj.AddComponent<Health>();
        }
        h.maxHealth = objectMaxHealth;
    }

    Vector3 PickSpawnPosition()
    {
        if (pathPts != null && pathPts.Count > 0)
        {
            List<Vector3> validPoints = new List<Vector3>();
            foreach (var pt in pathPts)
            {
                Vector3 tryPos = pt;
                tryPos.y = terrain.SampleHeight(tryPos);
                float slope = terrain.terrainData.GetSteepness(
                    tryPos.x / terrain.terrainData.size.x,
                    tryPos.z / terrain.terrainData.size.z
                );
                if (slope < 20f)
                    validPoints.Add(tryPos);
            }

            if (validPoints.Count > 0)
            {
                return validPoints[Random.Range(0, validPoints.Count)];
            }

        }

        // fallback ke tengah
        Vector3 centre = new Vector3(
            terrain.terrainData.size.x * 0.5f,
            0,
            terrain.terrainData.size.z * 0.5f
        );
        centre.y = terrain.SampleHeight(centre);
        return centre;
    }


    #endregion

    #region Grass

    void SetupGrassPrototypes()
    {
        DetailPrototype[] prototypes = new DetailPrototype[2];

        prototypes[0] = CreateDetailPrototype(grassTextureA);
        prototypes[1] = CreateDetailPrototype(grassTextureB);

        data.detailPrototypes = prototypes;
        data.SetDetailResolution(grassResolution, grassPatchesPerChunk);
    }

    DetailPrototype CreateDetailPrototype(Texture2D tex)
    {
        var d = new DetailPrototype
        {
            prototypeTexture = tex,
            renderMode = DetailRenderMode.GrassBillboard,
            minWidth = grassMinWidth,
            maxWidth = grassMaxWidth,
            minHeight = grassMinHeight,
            maxHeight = grassMaxHeight,
            healthyColor = Color.Lerp(Color.green, Color.yellow, 0.2f),
            dryColor = Color.Lerp(Color.green, Color.yellow, 0.7f),
            noiseSpread = 0.2f,
            usePrototypeMesh = false
        };
        return d;
    }

    void PaintDenseGrassAlongPath()
    {
        // Safety checks
        if (data.detailPrototypes.Length < 2)
        {
            Debug.LogWarning("Need exactly two grass prototypes before painting.");
            return;
        }

        int[,] layer0 = new int[grassResolution, grassResolution];
        int[,] layer1 = new int[grassResolution, grassResolution];

        bool hasPath = pathPts != null && pathPts.Count > 1;
        float mult = GetBiomeGrassMultiplier();           // <-- new
        int baseDens = 4;                                   // tweak if needed
        int maxDens = Mathf.Clamp(Mathf.RoundToInt(baseDens * mult), 1, 15);

        // Iterate over each detail‑pixel
        for (int y = 0; y < grassResolution; y++)
        {
            for (int x = 0; x < grassResolution; x++)
            {
                // Map detail‑coords → world‑coords
                float nx = x / (float)grassResolution;
                float nz = y / (float)grassResolution;
                Vector3 worldPos = new Vector3(nx * data.size.x, 0, nz * data.size.z);

                // Paint everywhere *or* within path strip
                if (!hasPath || DistToPath(worldPos) <= pathGrassWidth)
                {
                    layer0[y, x] = maxDens;
                    layer1[y, x] = maxDens;
                }
            }
        }

        // Commit both layers
        data.SetDetailLayer(0, 0, 0, layer0);
        data.SetDetailLayer(0, 0, 1, layer1);

        Debug.Log($"🌾 Grass painted (multiplier {mult}, density {maxDens}).");
    }

    float GetBiomeGrassMultiplier()
    {
        switch (currentBiome)
        {
            case BiomeType.Forest: return 2.5f;   // thick undergrowth
            case BiomeType.Desert: return 1.4f;   // sparse tufts
            case BiomeType.Snowy: return 1.7f;   // patchy
            default: return 3f;     // grassland baseline
        }
    }

    float DistToPath(Vector3 world)
    {
        float min = float.MaxValue;
        for (int i = 0; i < pathPts.Count - 1; i++)
        {
            Vector3 a = pathPts[i];
            Vector3 b = pathPts[i + 1];
            Vector3 projected = ProjectPointLine(world, a, b);
            float d = Vector3.Distance(world, projected);
            if (d < min) min = d;
        }
        return min;
    }

    Vector3 ProjectPointLine(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ap = point - a;
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    #endregion
}
