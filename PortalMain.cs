using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PortalMain : MonoBehaviour
{
    [Header("Portal Settings")]
    public GameObject Terrain;
    public GameObject portalEffectPrefab;
    public AudioClip portalSound;
    public GameObject CanvasLoadTerrain;

    [Header("Transition Settings")]
    public float transitionDuration = 3f;
    public float cameraFocusDuration = 2f;

    public List<PortalBack> portalBackPoints = new List<PortalBack>();
    private GameObject portalEffectInstance;
    private AudioSource audioSource;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isTransitioning = false;
    private float detectionCooldown = 2f;
    private float lastDetectionTime = 0f;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        mainCamera = Camera.main;

        // Create portal effect on start
        if (portalEffectPrefab != null)
        {
            portalEffectInstance = Instantiate(portalEffectPrefab, transform.position, transform.rotation);
            PortalEffect effectScript = portalEffectInstance.AddComponent<PortalEffect>();
            effectScript.Initialize(GetComponent<Renderer>().material.color);
        }

        // Initial detection of portal backs
        FindAndRegisterPortalBacks();
    }

    void Update()
    {
        // Periodically check for new portal backs with cooldown to optimize performance
        if (Time.time - lastDetectionTime > detectionCooldown)
        {
            FindAndRegisterPortalBacks();
            lastDetectionTime = Time.time;
        }
    }

    private void FindAndRegisterPortalBacks()
    {
        // Find all PortalBacks in the scene
        PortalBack[] allBacks = FindObjectsOfType<PortalBack>();

        // Clear the list and add all found portal backs
        portalBackPoints.Clear();
        foreach (PortalBack portalBack in allBacks)
        {
            if (portalBack != null)
            {
                portalBackPoints.Add(portalBack);
            }
        }
    }

    // Public method for portal backs to register themselves
    public void RegisterPortalBack(PortalBack portalBack)
    {
        if (!portalBackPoints.Contains(portalBack))
        {
            portalBackPoints.Add(portalBack);
        }
    }

  private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Player") && !isTransitioning)
    {
        // Trigger Difficulty Manager
        DifficultyManager difficultyManager = FindObjectOfType<DifficultyManager>();
            StartCoroutine(PortalTransition(collision.gameObject));
            if (difficultyManager != null)
        {
            difficultyManager.IncreaseDifficulty(); 
        }
       
    }
}

    private void TeleportPlayerRandom(GameObject player, Terrain terrainComp)
    {
        if (terrainComp == null || player == null) return;

        Vector3 terrainSize = terrainComp.terrainData.size;
        Vector3 terrainPos = terrainComp.transform.position;

        // Random XZ dalam bounds terrain
        float randX = Random.Range(0f, terrainSize.x);
        float randZ = Random.Range(0f, terrainSize.z);

        // Dapetin height
        float terrainY = terrainComp.SampleHeight(new Vector3(randX, 0, randZ)) + terrainPos.y;

        // Spawn di atas terrain
        Vector3 newPos = new Vector3(randX + terrainPos.x, terrainY + 2f, randZ + terrainPos.z);
        player.transform.position = newPos;
        player.transform.rotation = Quaternion.identity;
    }




    private IEnumerator PortalTransition(GameObject player)
    {
        isTransitioning = true;

        // Disable player control
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null) playerRb.isKinematic = true;

        // Create portal effect
        if (portalEffectPrefab != null)
        {
            portalEffectInstance = Instantiate(portalEffectPrefab, transform.position, transform.rotation);
            PortalEffect effectScript = portalEffectInstance.AddComponent<PortalEffect>();
            effectScript.Initialize(GetComponent<Renderer>().material.color);
        }

        // Play sound
        if (portalSound != null) audioSource.PlayOneShot(portalSound);

        // Show loading canvas
        if (CanvasLoadTerrain != null) CanvasLoadTerrain.SetActive(true);

        // Store original camera pos/rot
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        // Create transition camera
        Camera transitionCamera = CreateTransitionCamera();

        // Transition cinematic (zoom into portal)
        float timer = 0f;
        while (timer < cameraFocusDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / cameraFocusDuration;

            transitionCamera.transform.position = Vector3.Lerp(
                originalCameraPosition,
                transform.position + transform.forward * 2f,
                progress
            );

            transitionCamera.transform.rotation = Quaternion.Slerp(
                originalCameraRotation,
                Quaternion.LookRotation(transform.position - transitionCamera.transform.position),
                progress
            );

            yield return null;
        }

        // --- Spawn terrain baru ---
        GameObject newTerrainObj = null;
        Terrain terrainComp = null;
        if (Terrain != null)
        {
            newTerrainObj = Instantiate(Terrain, Vector3.zero, Quaternion.identity);
            terrainComp = newTerrainObj.GetComponent<Terrain>();
        }

        // Tunggu terrain initialize minimal 1 frame
        yield return null;

        // Extra wait supaya procedural terrain benar-benar siap (opsional)
        yield return new WaitForSeconds(0.2f);

        // Teleport player ke posisi random di atas terrain
        if (terrainComp != null)
        {
            TeleportPlayerRandom(player, terrainComp);
        }
        else
        {
            // fallback kalau terrain gagal
            player.transform.position = transform.position + transform.forward * 5f;
        }

        // --- Restore kamera & kontrol ---
        Destroy(transitionCamera.gameObject);
        mainCamera.gameObject.SetActive(true);

        if (CanvasLoadTerrain != null) CanvasLoadTerrain.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerRb != null) playerRb.isKinematic = false;

        isTransitioning = false;
    }


    private Camera CreateTransitionCamera()
    {
        // Create a new camera for the transition
        GameObject cameraObj = new GameObject("TransitionCamera");
        Camera transitionCamera = cameraObj.AddComponent<Camera>();

        // Copy settings from main camera
        transitionCamera.CopyFrom(mainCamera);
        transitionCamera.depth = mainCamera.depth + 1; // Make it render on top

        // Disable main camera during transition
        mainCamera.gameObject.SetActive(false);

        return transitionCamera;
    }

    private PortalBack FindAvailablePortalBack()
    {
        foreach (PortalBack portalBack in portalBackPoints)
        {
            if (portalBack != null && portalBack.IsAvailable())
            {
                return portalBack;
            }
        }
        return null;
    }

    void OnDrawGizmos()
    {
        // Draw connection to all portal backs
        foreach (PortalBack portalBack in portalBackPoints)
        {
            if (portalBack != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, portalBack.transform.position);
            }
        }

        // Draw availability indicator
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}