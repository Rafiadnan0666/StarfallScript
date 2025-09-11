using UnityEngine;
using TMPro;

public class DifficultyManager : MonoBehaviour
{
    [Header("Difficulty Settings")]
    public int difficultyLevel = 1;
    public int maxDifficultyLevel = 10;
    public float difficultyIncreaseInterval = 60f;
    public TextMeshProUGUI difficultyText;

    [Header("Enemy Scaling")]
    public float healthMultiplierPerLevel = 1.2f;
    public float damageMultiplierPerLevel = 1.15f;
    public float speedMultiplierPerLevel = 1.05f;
    public float spawnRateMultiplierPerLevel = 1.1f; // disediakan kalau nanti mau dipakai

    [Header("Control")]
    [Tooltip("Kalau true, level tidak akan auto naik (hold tier).")]
    public bool lockDifficulty = false;

    private EnemyController enemyController;
    private float lastDifficultyIncreaseTime;

    void Start()
    {
        enemyController = FindObjectOfType<EnemyController>();
        lastDifficultyIncreaseTime = Time.time;
        UpdateDifficultyText();
        // Terapkan modifier awal
        ApplyToController();
    }

    void Update()
    {
        if (lockDifficulty) return;

        if (Time.time - lastDifficultyIncreaseTime >= difficultyIncreaseInterval && difficultyLevel < maxDifficultyLevel)
        {
            IncreaseDifficulty();
        }
    }

    public void IncreaseDifficulty()
    {
        if (difficultyLevel >= maxDifficultyLevel) return;
        difficultyLevel++;
        lastDifficultyIncreaseTime = Time.time;
        ApplyToController();
        UpdateDifficultyText();
    }

    public void SetTier(int newLevel, bool lockAfterSet = false)
    {
        difficultyLevel = Mathf.Clamp(newLevel, 1, maxDifficultyLevel);
        lockDifficulty = lockAfterSet;
        lastDifficultyIncreaseTime = Time.time;
        ApplyToController();
        UpdateDifficultyText();
    }

    private void ApplyToController()
    {
        if (enemyController == null) return;

        float h = Mathf.Pow(healthMultiplierPerLevel, difficultyLevel);
        float d = Mathf.Pow(damageMultiplierPerLevel, difficultyLevel);
        float s = Mathf.Pow(speedMultiplierPerLevel, difficultyLevel);
        float r = Mathf.Pow(spawnRateMultiplierPerLevel, difficultyLevel);

        enemyController.ApplyDifficultyModifiers(h, d, s, r);
    }

    private void UpdateDifficultyText()
    {
        if (difficultyText == null) return;

        difficultyText.text = $"Threat Level: {difficultyLevel}" + (lockDifficulty ? " (HOLD)" : "");
        StopAllCoroutines();
        StartCoroutine(FadeText());
    }

    private System.Collections.IEnumerator FadeText()
    {
        float fadeDuration = 2f;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.PingPong(timer / fadeDuration, 1f);
            var c = difficultyText.color;
            difficultyText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        var c2 = difficultyText.color;
        difficultyText.color = new Color(c2.r, c2.g, c2.b, 0f);
    }
}
