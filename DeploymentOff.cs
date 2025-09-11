using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;


public enum ObjectiveType { Eliminate, Collect, Explore }

[System.Serializable]
public class PlanetSituation
{
    public BiomeType biome;
    public float gravity = 1f;
    public float temperature = 20;
    public bool hasEarthquakes = false;
    public float earthquakeIntensity = 0.1f;
    public float earthquakeFrequency = 0.1f;
    public float waveSpeed = 0.5f;
    public float waveHeight = 0.2f;
    public Color waterColor = new Color(0.2f, 0.5f, 0.8f, 0.7f);

    [Header("Objective Settings")]
    public ObjectiveType primaryObjective = ObjectiveType.Eliminate;
    public ObjectiveType secondaryObjective = ObjectiveType.Collect;
    public int minObjectives = 1;
    public int maxObjectives = 3;

    [Header("Vegetation Settings")]
    public float treeDensity = 1f;
    public float grassDensity = 1f;
    public float rockDensity = 1f;
}

public class DeploymentOff : MonoBehaviour
{
    [Header("UI References")]
    public Button createPlanetButton;
    public Button backButton;
    public Button deployButton;
    public TextMeshProUGUI planetInfoText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI selectedPlanetText;
    public GameObject loadingPanel;

    [Header("Camera Control")]
    public CinemachineVirtualCamera virtualCamera;
    public Transform defaultCameraTarget;
    public float focusTransitionSpeed = 2f;
   

    [Header("Planet Generation")]
    public GameObject planetPrefab;
    public Transform spawnArea;
    public Material[] planetMaterials;
    public int maxPlanets = 10;
    public Vector2 spawnRangeX = new Vector2(-5, 5);
    public Vector2 spawnRangeY = new Vector2(-5, 5);
    public float minPlanetDistance = 2f;

    [Header("Planet Naming")]
    public string[] planetNamePrefixes = { "PL-", "PX-", "ALPHA-" };
    public string[] planetNameSuffixes = { "Prime", "Secunda", "Tertius" };

    private List<GameObject> spawnedPlanets = new List<GameObject>();
    private Transform currentSelectedPlanet;
    private PlanetData currentPlanetData;
    private const string MATERIALS_PATH = "Materials/Planets";
    private const string PLANET_SCENE = "main1";

    void Start()
    {
        InitializeSystem();
        LoadResources();
    }

    void InitializeSystem()
    {
        createPlanetButton.onClick.AddListener(CreateRandomPlanet);
        backButton.onClick.AddListener(ResetCameraView);
        deployButton.onClick.AddListener(DeployToPlanet);

        UpdateUIElements(false);
        loadingPanel.SetActive(false);
    }

    void LoadResources()
    {
        planetMaterials = Resources.LoadAll<Material>(MATERIALS_PATH);
        if (planetMaterials == null || planetMaterials.Length == 0)
        {
            Debug.LogWarning("No planet materials found in Resources/Materials/Planets folder");
        }
    }

    public void CreateRandomPlanet()
    {
        if (spawnedPlanets.Count >= maxPlanets)
        {
            statusText.text = "Maximum planets reached!";
            return;
        }

        string planetName = GeneratePlanetName();
        CreatePlanet(planetName);
    }

    void CreatePlanet(string planetName)
    {
        Vector3 spawnPosition = CalculateValidSpawnPosition();
        GameObject planet = Instantiate(planetPrefab, spawnPosition, Quaternion.identity, spawnArea);

        ApplyRandomAppearance(planet);
        planet.name = planetName;
        spawnedPlanets.Add(planet);

        SetupPlanetComponents(planet, planetName);
        statusText.text = $"Created planet: {planetName}";
    }

    Vector3 CalculateValidSpawnPosition()
    {
        Vector3 position;
        int attempts = 0;
        const int maxAttempts = 50;

        do
        {
            position = new Vector3(
                Random.Range(spawnRangeX.x, spawnRangeX.y),
                Random.Range(spawnRangeY.x, spawnRangeY.y),
                0
            );
            attempts++;
        } while (!IsPositionValid(position) && attempts < maxAttempts);

        return position;
    }

    bool IsPositionValid(Vector3 position)
    {
        foreach (var planet in spawnedPlanets)
        {
            if (Vector3.Distance(position, planet.transform.position) < minPlanetDistance)
            {
                return false;
            }
        }
        return true;
    }

    void ApplyRandomAppearance(GameObject planet)
    {
        if (planetMaterials.Length == 0) return;

        // Try to get the MeshRenderer directly from the planet or its children
        MeshRenderer renderer = planet.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = planet.GetComponentInChildren<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = planetMaterials[Random.Range(0, planetMaterials.Length)];
        }
        else
        {
            Debug.LogWarning($"No MeshRenderer found on {planet.name} or its children.");
        }

        float randomScale = Random.Range(0.8f, 1.2f);
        planet.transform.localScale = Vector3.one * randomScale;
    }


    void SetupPlanetComponents(GameObject planet, string planetName)
    {
        PlanetData planetData = planet.AddComponent<PlanetData>();
        planetData.planetName = planetName;
        planetData.situation = GenerateRandomPlanetSituation();

        SphereCollider collider = planet.AddComponent<SphereCollider>();
        collider.radius = 1.2f;

        PlanetInteraction interaction = planet.AddComponent<PlanetInteraction>();
        interaction.Initialize(this);
    }

    PlanetSituation GenerateRandomPlanetSituation()
    {
        return new PlanetSituation
        {
            biome = GetRandomBiome(),
            gravity = Random.Range(0.3f, 2.5f),
            temperature = Random.Range(-200, 500),
            hasEarthquakes = Random.value > 0.7f,
            earthquakeIntensity = Random.Range(0.05f, 0.3f),
            earthquakeFrequency = Random.Range(0.01f, 0.2f),
            waveSpeed = Random.Range(0.1f, 1.5f),
            waveHeight = Random.Range(0.1f, 0.5f),
            waterColor = GenerateRandomWaterColor(),
            treeDensity = Random.Range(0.5f, 1.5f),
            rockDensity = Random.Range(0.5f, 1.5f)
        };
    }

    public void SelectPlanet(Transform planetTransform)
    {
        currentSelectedPlanet = planetTransform;
        currentPlanetData = planetTransform.GetComponent<PlanetData>();

        UpdateCameraFocus(planetTransform);
        UpdateUIElements(true);
        DisplayPlanetInfo(currentPlanetData);
    }

    void UpdateCameraFocus(Transform target)
    {
        virtualCamera.Follow = target;
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_XDamping = focusTransitionSpeed;
            transposer.m_YDamping = focusTransitionSpeed;
            transposer.m_ZDamping = focusTransitionSpeed;
        }
    }

    void DisplayPlanetInfo(PlanetData planetData)
    {
        selectedPlanetText.text = $"Selected: {planetData.planetName}";

        planetInfoText.text = $"<b>Planet Name:</b> {planetData.planetName}\n" +
                             $"<b>Biome:</b> {planetData.situation.biome}\n" +
                             $"<b>Gravity:</b> {planetData.situation.gravity:F2} G\n" +
                             $"<b>Temperature:</b> {planetData.situation.temperature}°C\n" +
                             $"<b>Seismic Activity:</b> {(planetData.situation.hasEarthquakes ? "High" : "Low")}\n" +
                             $"<b>Primary Objective:</b> {planetData.situation.primaryObjective}";
    }

    public void ResetCameraView()
    {
        virtualCamera.Follow = defaultCameraTarget;
        currentSelectedPlanet = null;
        currentPlanetData = null;
        UpdateUIElements(false);
    }

    void UpdateUIElements(bool planetSelected)
    {
        backButton.gameObject.SetActive(planetSelected);
        deployButton.gameObject.SetActive(planetSelected);
        planetInfoText.gameObject.SetActive(planetSelected);
        selectedPlanetText.gameObject.SetActive(planetSelected);
    }

    public void DeployToPlanet()
    {
        if (currentSelectedPlanet == null) return;

        loadingPanel.SetActive(true);
        SavePlanetData();
        GeneratePlanetNeighbors();
        StartCoroutine(LoadPlanetScene());
    }

    IEnumerator LoadPlanetScene()
    {
        PlanetManager.Instance.GenerateRandomSituation();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(PLANET_SCENE);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    void SavePlanetData()
    {
        PlayerPrefs.SetString("CurrentPlanet", currentPlanetData.planetName);
        PlayerPrefs.SetString("PlanetSituation", JsonUtility.ToJson(currentPlanetData.situation));
    }

    void GeneratePlanetNeighbors()
    {
        PlayerPrefs.SetString("NorthNeighbor", GeneratePlanetName());
        PlayerPrefs.SetString("SouthNeighbor", GeneratePlanetName());
        PlayerPrefs.SetString("EastNeighbor", GeneratePlanetName());
        PlayerPrefs.SetString("WestNeighbor", GeneratePlanetName());
    }

    string GeneratePlanetName()
    {
        string prefix = planetNamePrefixes[Random.Range(0, planetNamePrefixes.Length)];
        string suffix = planetNameSuffixes[Random.Range(0, planetNameSuffixes.Length)];
        string code = Random.Range(100, 1000).ToString();

        return $"{prefix}{suffix}-{code}";
    }

    BiomeType GetRandomBiome()
    {
        var values = System.Enum.GetValues(typeof(BiomeType));
        return (BiomeType)values.GetValue(Random.Range(0, values.Length));
    }

    Color GenerateRandomWaterColor()
    {
        return Random.ColorHSV(0.5f, 0.7f, 0.5f, 1f, 0.5f, 1f, 0.6f, 0.9f);
    }

    void OnDestroy()
    {
        createPlanetButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        deployButton.onClick.RemoveAllListeners();
    }
}

public class PlanetInteraction : MonoBehaviour
{
    private DeploymentOff deploymentSystem;
    private float highlightScale = 1.1f;
    private Vector3 originalScale;

    public void Initialize(DeploymentOff system)
    {
        deploymentSystem = system;
        originalScale = transform.localScale;
    }

    void OnMouseDown()
    {
        deploymentSystem?.SelectPlanet(transform);
    }

    void OnMouseEnter()
    {
        transform.localScale = originalScale * highlightScale;
    }

    void OnMouseExit()
    {
        transform.localScale = originalScale;
    }
}

public class PlanetData : MonoBehaviour
{
    public string planetName;
    public PlanetSituation situation;
}



