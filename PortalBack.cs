using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PortalBack : MonoBehaviour
{
    [Header("Portal Settings")]
    public GameObject portalEffectPrefab;
    public AudioClip portalSound;
    public GameObject CanvasLoadTerrain;

    [Header("Transition Settings")]
    public float transitionDuration = 3f;
    public float cameraFocusDuration = 2f;

    private PortalMain targetPortalMain;
    private GameObject portalEffectInstance;
    private AudioSource audioSource;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isTransitioning = false;
    private float detectionCooldown = 1f;
    private float lastDetectionTime = 0f;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        mainCamera = Camera.main;
        CanvasLoadTerrain.SetActive(false);

        // Create portal effect on start
        if (portalEffectPrefab != null)
        {
            portalEffectInstance = Instantiate(portalEffectPrefab, transform.position, transform.rotation);
            PortalEffect effectScript = portalEffectInstance.AddComponent<PortalEffect>();
            effectScript.Initialize(GetComponent<Renderer>().material.color);
        }

        // Initial target detection
        FindAndSetTargetPortal();
    }

    void Update()
    {
        // Periodically check for target portal with cooldown to optimize performance
        if (targetPortalMain == null && Time.time - lastDetectionTime > detectionCooldown)
        {
            FindAndSetTargetPortal();
            lastDetectionTime = Time.time;
        }
    }

    private void FindAndSetTargetPortal()
    {
        // Find the closest PortalMain in the scene
        PortalMain[] portalMains = FindObjectsOfType<PortalMain>();
        PortalMain closestPortal = null;
        float closestDistance = Mathf.Infinity;

        foreach (PortalMain portalMain in portalMains)
        {
            float distance = Vector3.Distance(transform.position, portalMain.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPortal = portalMain;
            }
        }

        if (closestPortal != null)
        {
            targetPortalMain = closestPortal;
            // Register with the target portal
            targetPortalMain.RegisterPortalBack(this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isTransitioning && targetPortalMain != null)
        {
            StartCoroutine(PortalTransition(collision.gameObject));
        }
    }

    private IEnumerator PortalTransition(GameObject player)
    {
        isTransitioning = true;

        // Disable player control sementara
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null) playerMovement.enabled = false;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = true; // freeze dulu biar nggak chaos saat transisi
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Mainin efek & suara portal
        if (portalSound != null) audioSource.PlayOneShot(portalSound);
        if (CanvasLoadTerrain != null) CanvasLoadTerrain.SetActive(true);

        // Simpan posisi kamera sekarang
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;

        // Kamera transisi sementara
        Camera transitionCamera = CreateTransitionCamera();

        // Kamera cinematic fokus ke portal
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

        // Tunggu sedikit biar cinematic terasa
        yield return new WaitForSeconds(transitionDuration);

        // Hancurin terrain lama (jika ada)
        Terrain oldTerrain = FindObjectOfType<Terrain>();
        if (oldTerrain != null)
        {
            Destroy(oldTerrain.gameObject);
        }

        // Teleport player ke portal main
        Vector3 exitPos = targetPortalMain.transform.position + targetPortalMain.transform.forward * 2f;
        player.transform.position = exitPos;
        player.transform.rotation = targetPortalMain.transform.rotation;

        // Enable physics & kasih dorongan ke depan
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.AddForce(targetPortalMain.transform.forward * 500f); // adjust force sesuai kebutuhan
        }

        // Kamera balik ke main
        Destroy(transitionCamera.gameObject);
        mainCamera.gameObject.SetActive(true);

        if (CanvasLoadTerrain != null) CanvasLoadTerrain.SetActive(false);

        // Aktifkan kembali kontrol player
        if (playerMovement != null) playerMovement.enabled = true;

        isTransitioning = false;

        // Increase difficulty (opsional)
        DifficultyManager difficultyManager = FindObjectOfType<DifficultyManager>();
        if (difficultyManager != null)
        {
            difficultyManager.IncreaseDifficulty();
        }
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

    // Method to check if this portal back is available
    public bool IsAvailable()
    {
        if (!gameObject.activeInHierarchy) return false;

        // Check if there are any objects blocking the portal
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player") || col.CompareTag("Enemy"))
            {
                return false;
            }
        }

        return true;
    }

    void OnDrawGizmos()
    {
        // Draw connection line to target portal if assigned
        if (targetPortalMain != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPortalMain.transform.position);

            // Draw arrow indicator
            Vector3 direction = (targetPortalMain.transform.position - transform.position).normalized;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(targetPortalMain.transform.position, right * 0.5f);
            Gizmos.DrawRay(targetPortalMain.transform.position, left * 0.5f);
        }

        // Draw availability indicator
        Gizmos.color = IsAvailable() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}