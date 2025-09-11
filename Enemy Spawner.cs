using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnerType { Controlled, Uncontrolled }

    [Header("Mode")]
    public SpawnerType mode = SpawnerType.Controlled;

    [Header("Timing")]
    [Tooltip("Jarak waktu antar percobaan spawn")]
    public float spawnInterval = 5f;
    [Tooltip("Berapa musuh diminta tiap tick (akan dibatasi EnemyController)")]
    public int batchSize = 1;

    [Header("Placement")]
    [Tooltip("Radius cari titik di sekitar spawner")]
    public float spawnRadius = 25f;
    [Tooltip("Minimal jarak dari Player")]
    public float minDistanceFromPlayer = 10f;
    [Tooltip("Cek clearance agar tidak nancep/ketiban")]
    public float clearanceRadius = 0.6f;
    [Tooltip("Ketinggian awal raycast turun ke tanah")]
    public float raycastHeight = 50f;
    [Tooltip("Percobaan maksimum mencari titik valid per musuh")]
    public int attemptsPerSpawn = 12;

    [Header("Layers")]
    public LayerMask groundMask = -1;
    public LayerMask obstacleMask = 0;
    public LayerMask avoidMaskForClearance = 0; // opsional: Enemy, Props, Obstacle

    [Header("NavMesh")]
    public bool preferNavMesh = true;
    [Tooltip("Seberapa jauh toleransi snap ke navmesh")]
    public float navMeshMaxDistance = 3f;

    private EnemyController controller;
    private float lastSpawnTime;

    void Awake()
    {
        controller = FindObjectOfType<EnemyController>();
        if (controller == null)
            Debug.LogError($"[{name}] EnemyController tidak ditemukan di scene.");

        // Auto inherit masks kalau belum diset
        if (groundMask == -1) groundMask = controller != null ? controller.GroundMask : groundMask;
        if (obstacleMask == 0) obstacleMask = controller != null ? controller.ObstacleMask : obstacleMask;
        if (preferNavMesh == false && controller != null) preferNavMesh = controller.PreferNavMesh;
    }

    void Update()
    {
        if (controller == null) return;
        if (Time.time - lastSpawnTime < spawnInterval) return;

        // Berhenti kalau kapasitas global penuh
        if (!controller.CanSpawnMore()) { lastSpawnTime = Time.time; return; }

        int toSpawn = Mathf.Min(batchSize, Mathf.Max(0, controller.maxEnemies - controller.GetActiveCount()));
        if (toSpawn <= 0) { lastSpawnTime = Time.time; return; }

        for (int i = 0; i < toSpawn; i++)
        {
            GameObject prefab = (mode == SpawnerType.Controlled) ? controller.GetControlledPrefab()
                                                                 : controller.GetUncontrolledPrefab();
            if (prefab == null) continue;

            if (TryGetValidPosition(out Vector3 pos, out Quaternion rot))
            {
                var go = Instantiate(prefab, pos, rot);
                controller.RegisterSpawn(go);
            }
        }

        lastSpawnTime = Time.time;
    }

    private bool TryGetValidPosition(out Vector3 pos, out Quaternion rot)
    {
        rot = Quaternion.identity;
        pos = transform.position;

        var player = controller.GetPlayer();
        Vector3 center = transform.position;

        for (int attempt = 0; attempt < attemptsPerSpawn; attempt++)
        {
            // Pilih titik acak di sekitar spawner
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadius * 0.4f, spawnRadius);
            Vector3 candidate = center + new Vector3(circle.x, 0f, circle.y);

            // Raycast ke bawah untuk cari tanah
            if (Physics.Raycast(candidate + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask))
            {
                Vector3 groundPoint = hit.point + Vector3.up * 0.05f; // sedikit di atas tanah

                // Minimal jarak dari player
                if (player != null && Vector3.Distance(groundPoint, player.position) < minDistanceFromPlayer)
                    continue;

                // Clearance dari obstacle/objek lain
                if (clearanceRadius > 0.01f && avoidMaskForClearance.value != 0)
                {
                    var cols = Physics.OverlapSphere(groundPoint, clearanceRadius, avoidMaskForClearance);
                    if (cols != null && cols.Length > 0) continue;
                }

                // Opsional: snap ke navmesh
                if (preferNavMesh && NavMesh.SamplePosition(groundPoint, out NavMeshHit nHit, navMeshMaxDistance, NavMesh.AllAreas))
                {
                    groundPoint = nHit.position;
                }

                pos = groundPoint;
                // Orientasi mengikuti normal permukaan (opsional)
                rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return true;
            }
        }
        // gagal dapat lokasi valid
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        if (clearanceRadius > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, clearanceRadius);
        }
    }
#endif
}
