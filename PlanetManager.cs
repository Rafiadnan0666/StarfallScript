using UnityEngine;

public class PlanetManager : MonoBehaviour
{
    public static PlanetManager Instance;

    public PlanetSituation currentSituation;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Optional: Create a random PlanetSituation
    public void GenerateRandomSituation()
    {
        currentSituation = new PlanetSituation
        {
            biome = (BiomeType)Random.Range(0, System.Enum.GetValues(typeof(BiomeType)).Length),
            gravity = Random.Range(0.5f, 2f),
            temperature = Random.Range(-50, 50),
            hasEarthquakes = Random.value > 0.7f,
            earthquakeIntensity = Random.Range(0.05f, 0.3f),
            earthquakeFrequency = Random.Range(0.05f, 0.3f),
            waveSpeed = Random.Range(0.3f, 1f),
            waveHeight = Random.Range(0.1f, 0.5f),
            waterColor = new Color(Random.value, Random.value, Random.value, 0.7f),
            primaryObjective = ObjectiveType.Eliminate,
            secondaryObjective = ObjectiveType.Collect,
            minObjectives = 1,
            maxObjectives = 3,
            treeDensity = Random.Range(0.2f, 2f),
            grassDensity = Random.Range(0.2f, 2f),
            rockDensity = Random.Range(0.2f, 2f)
        };
    }
}
