using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign kalau mau manual; kalau kosong akan dicari by tag 'Player'")]
    public Transform playerTransform;

    [Header("Global Enemy Pool")]
    public GameObject[] enemyPrefabs;      // Non-boss
    public GameObject[] bossPrefabs;       // Boss pool (role Boss)
    public int maxEnemies = 30;

    [Header("Boss Settings")]
    [Tooltip("Level minimal untuk mulai mengizinkan boss spawn")]
    public int minBossLevel = 5;
    [Tooltip("Cooldown minimal antar boss spawn (detik)")]
    public float bossCooldown = 60f;
    [Tooltip("Peluang boss dipilih saat Controlled spawn (0..1)")]
    [Range(0f, 1f)] public float bossChance = 0.15f;

    [Header("Environment")]
    public LayerMask groundMask = -1;      // default: semua
    public LayerMask obstacleMask = 0;     // opsi: layer tembok/batu/kolider lain
    public bool preferNavMesh = true;      // kalau ada NavMesh, coba snap ke navmesh

    [Header("UI")]
    public TextMeshProUGUI alertText;

    // runtime
    private readonly List<GameObject> activeEnemies = new List<GameObject>(128);
    private DifficultyManager difficultyManager;
    private float healthModifier = 1f, damageModifier = 1f, speedModifier = 1f;
    private float lastBossTime = -999f;

    void Awake()
    {
        if (playerTransform == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player"); // <-- amanin NRE
            if (playerGO != null) playerTransform = playerGO.transform;
            else Debug.LogWarning("[EnemyController] Player dengan tag 'Player' tidak ditemukan. Assign manual di Inspector.");
        }
        difficultyManager = FindObjectOfType<DifficultyManager>();
    }

    void Update()
    {
        // Bersihin entry null (musuh yang sudah mati/destroyed)
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
            if (activeEnemies[i] == null) activeEnemies.RemoveAt(i);
    }

    // ==================== API buat Spawner ====================

    public bool CanSpawnMore(int requestCount = 1)
    {
        // Cek kapasitas global
        return (activeEnemies.Count + requestCount) <= maxEnemies;
    }

    public Transform GetPlayer() => playerTransform;

    public GameObject GetControlledPrefab()
    {
        int level = difficultyManager != null ? difficultyManager.difficultyLevel : 1;
        // Mungkin spawn boss?
        if (level >= minBossLevel && bossPrefabs != null && bossPrefabs.Length > 0)
        {
            if (Time.time - lastBossTime >= bossCooldown && Random.value <= bossChance)
            {
                lastBossTime = Time.time;
                ShowBoss("⚠ BOSS DETECTED!");
                return bossPrefabs[Random.Range(0, bossPrefabs.Length)];
            }
        }
        // Kalau bukan boss, pilih berdasar role/level
        return ChooseEnemyPrefabByLevel(level);
    }

    public GameObject GetUncontrolledPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    public void RegisterSpawn(GameObject enemy)
    {
        if (enemy == null) return;
        activeEnemies.Add(enemy);
        ApplyCurrentModifiers(enemy);
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        int idx = activeEnemies.IndexOf(enemy);
        if (idx >= 0) activeEnemies.RemoveAt(idx);
    }

    public int GetActiveCount() => activeEnemies.Count;

    public LayerMask GroundMask => groundMask;
    public LayerMask ObstacleMask => obstacleMask;
    public bool PreferNavMesh => preferNavMesh;

    // ==================== Difficulty Modifiers ====================

    public void ApplyDifficultyModifiers(float healthMod, float damageMod, float speedMod, float _unusedSpawnRate)
    {
        healthModifier = healthMod;
        damageModifier = damageMod;
        speedModifier = speedMod;

        // Apply ke musuh yang sudah eksis
        for (int i = 0; i < activeEnemies.Count; i++)
            ApplyCurrentModifiers(activeEnemies[i]);
    }

    private void ApplyCurrentModifiers(GameObject enemy)
    {
        if (enemy == null) return;

        // Contoh: kalau punya script ModularEnemy
        // (biarkan idempoten: bisa set base*mod atau gunakan metoda SetModifiers di enemy)
        var modular = enemy.GetComponent<ModularEnemy>();
        if (modular != null)
        {
            // Pastikan enemy script punya cara aman mengaplikasikan scaling (hindari dobel kali bbrp kali)
            modular.ApplyScaling(healthModifier, damageModifier, speedModifier);
        }
    }

    // ==================== Internal helpers ====================

    private void ShowBoss(string message)
    {
        if (alertText == null) return;
        StopAllCoroutines();
        StartCoroutine(BossFlicker(message));
    }

    private System.Collections.IEnumerator BossFlicker(string msg)
    {
        alertText.text = msg;
        alertText.color = Color.red;
        float t = 0f, dur = 3f;
        while (t < dur)
        {
            t += Time.deltaTime;
            alertText.alpha = Mathf.PingPong(t * 2f, 1f);
            yield return null;
        }
        alertText.text = "";
    }

    private GameObject ChooseEnemyPrefabByLevel(int level)
    {
        // Asumsi prefab punya komponen StarfallEnemy dengan enum role (Grazer, Scavenger, Predator, Swarmer, Boss)
        var candidates = new List<GameObject>(enemyPrefabs?.Length ?? 0);
        if (enemyPrefabs == null) return null;

        foreach (var p in enemyPrefabs)
        {
            if (p == null) continue;
            var info = p.GetComponent<StarfallEnemy>();
            if (info == null) continue;

            switch (info.role)
            {
                case StarfallEnemy.EnemyRole.Grazer:
                case StarfallEnemy.EnemyRole.Scavenger:
                    if (level <= 2) candidates.Add(p);
                    break;

                case StarfallEnemy.EnemyRole.Predator:
                case StarfallEnemy.EnemyRole.Swarmer:
                    if (level >= 3) candidates.Add(p);
                    break;
                    // Boss tidak masuk sini — boss dari bossPrefabs
            }
        }
        if (candidates.Count == 0) return enemyPrefabs.Length > 0 ? enemyPrefabs[Random.Range(0, enemyPrefabs.Length)] : null;
        return candidates[Random.Range(0, candidates.Count)];
    }
}
