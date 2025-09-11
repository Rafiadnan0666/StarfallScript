using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Health))]
public class Objective : MonoBehaviour
{
    [Header("General Settings")]
    public string objectiveName;
    [TextArea] public string description;
    public bool isCompleted;

    [Header("UI")]
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI missionDescriptionText;

    [Header("World-Space Health Bar")]
    public Canvas worldSpaceCanvas;
    public Image healthBarFill;
    public TextMeshProUGUI healthText;

    [Header("Visual Effects")]
    public GameObject[] completionVFX;
    public GameObject hitVFX;
    public float shrinkFactor = 0.8f;
    public float shrinkDuration = 0.2f;
    public float returnDuration = 0.5f;

    [Header("Destroy Settings")]
    public GameObject[] spawnOnDestroyPrefabs;
    private Transform spawnParent;

    private Health healthComp;
    private Camera mainCam;
    private Vector3 originalScale;
    private bool isShrinking = false;

    // Predefined mission names for randomization
    private string[] missionNames = {
        "Destroy Enemy Core", "Eliminate Power Source", "Neutralize Threat",
        "Obliterate Target", "Demolish Structure", "Destroy Energy Converter",
        "Eradicate Contamination", "Annihilate Hostile Object", "Terminate Objective"
    };

    void Awake()
    {
        healthComp = GetComponent<Health>();
        spawnParent = spawnParent ?? transform;
        gameObject.tag = "Objective";
        mainCam = Camera.main;
        originalScale = transform.localScale;

        // Randomize mission name and description
        objectiveName = missionNames[Random.Range(0, missionNames.Length)];
        description = $"Destroy the {objectiveName} to complete the mission.";

        // Hide canvas at start
        if (worldSpaceCanvas != null)
            worldSpaceCanvas.enabled = false;

        UpdateUI();
    }

    void Update()
    {
        // Make canvas always face the camera
        if (worldSpaceCanvas != null && worldSpaceCanvas.enabled && mainCam != null)
        {
            worldSpaceCanvas.transform.LookAt(
                worldSpaceCanvas.transform.position + mainCam.transform.rotation * Vector3.forward,
                mainCam.transform.rotation * Vector3.up
            );
        }
    }

    public void UpdateUI()
    {
        if (missionNameText != null)
            missionNameText.text = isCompleted ?
                $"<color=green>{objectiveName} - COMPLETED</color>" :
                $"<color=yellow>{objectiveName}</color>";

        if (missionDescriptionText != null)
            missionDescriptionText.text = isCompleted ?
                $"<color=green>✅ Objective Accomplished!</color>" :
                description;

        if (healthBarFill != null && healthComp != null)
        {
            float healthPercent = (float)healthComp.CurrentHealth / healthComp.MaxHealth;
            healthBarFill.fillAmount = healthPercent;

            // Change color based on health
            healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }

        if (healthText != null && healthComp != null)
        {
            healthText.text = $"{healthComp.CurrentHealth}/{healthComp.MaxHealth}";
        }
    }

    public void CompleteObjective()
    {
        if (isCompleted) return;

        isCompleted = true;
        Debug.Log($"✅ Objective Completed: {objectiveName}");

        HandleDestruction();
        UpdateUI();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (healthComp != null)
            {
                healthComp.TakeDamage(10);

                // Show canvas on first hit
                if (worldSpaceCanvas != null && !worldSpaceCanvas.enabled)
                    worldSpaceCanvas.enabled = true;

                // Play hit effect and shrink animation
                PlayHitEffect();
                StartShrinkAnimation();

                UpdateUI();

                if (healthComp.CurrentHealth <= 0)
                    CompleteObjective();
            }
        }
    }

    void HandleDestruction()
    {
        // Play completion VFX
        PlayCompletionVFX();

        // Spawn destruction prefabs
        if (spawnOnDestroyPrefabs != null && spawnOnDestroyPrefabs.Length > 0)
        {
            foreach (GameObject prefab in spawnOnDestroyPrefabs)
            {
                Instantiate(prefab, transform.position, Quaternion.identity, spawnParent);
            }
        }

        // Disable collider and renderer but keep object for effects to finish
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;

        // Disable world space canvas
        if (worldSpaceCanvas != null)
            worldSpaceCanvas.enabled = false;

        // Destroy after delay
        Destroy(gameObject, 3f);
    }

    void PlayCompletionVFX()
    {
        if (completionVFX == null || completionVFX.Length == 0) return;

        foreach (GameObject vfx in completionVFX)
        {
            GameObject effect = Instantiate(vfx, transform.position, Quaternion.identity, spawnParent);
            Destroy(effect, 2f);
        }
    }

    void PlayHitEffect()
    {
        if (hitVFX != null)
        {
            GameObject effect = Instantiate(hitVFX, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    void StartShrinkAnimation()
    {
        if (!isShrinking)
        {
            isShrinking = true;
            StartCoroutine(ShrinkAndReturn());
        }
    }

    System.Collections.IEnumerator ShrinkAndReturn()
    {
        // Shrink
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, originalScale * shrinkFactor, elapsed / shrinkDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to normal size
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale * shrinkFactor, originalScale, elapsed / returnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isShrinking = false;
    }
}