using System.Collections;
using UnityEngine;

public class Bosses3 : MonoBehaviour
{
    private Transform player;
    public GameObject projectilePrefab;
    public GameObject boulderPrefab;
    public GameObject circlingProjectilePrefab;
    public Light stareLight;

    public float circleSpeed = 1.5f;
    public float circleRadius = 7f;
    public float smoothMoveSpeed = 0.2f;
    public float stareDuration = 3f;
    public float volleyCooldown = 6f;
    public float projectileSpeed = 12f;
    public float ultimateCooldown = 20f;
    public float lightMaxIntensity = 5f;
    public int volleyCount = 5;
    public int circlingProjectileCount = 6;
    public int health = 100;
    public int maxHealth;

    public Transform tip;

    public AudioClip menacingStareSound;
    public AudioClip volleySound;
    public AudioClip ultimateSound;
    public AudioClip hitSound;
    public AudioClip deathSound;

    private Vector3 currentVelocity;
    private int currentHealth;
    private bool isStaring;
    private bool isDying;
    private float nextVolleyTime;
    private float nextUltimateTime;
    private float angleOffset;
    private AudioSource audioSource;
    [SerializeField] private GameObject[] circlingProjectiles;

    private void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

        player = FindAnyObjectByType<Player>().playerCam;
        circlingProjectiles = new GameObject[circlingProjectileCount];
        for (int i = 0; i < circlingProjectileCount; i++)
        {
            circlingProjectiles[i] = Instantiate(
                circlingProjectilePrefab,
                transform.position,
                Quaternion.identity
            );
        }
        if (stareLight != null) stareLight.intensity = 0;
    }

    private void Update()
    {
        if (player == null || isDying) return;

        if (!isStaring)
        {
            CirclePlayerSmoothly();
            UpdateCirclingProjectiles();
        }

        if (Time.time >= nextVolleyTime && !isStaring)
        {
            StartCoroutine(MenacingStareAndVolley());
        }

        if (Time.time >= nextUltimateTime)
        {
            StartCoroutine(UltimateAttack());
            nextUltimateTime = Time.time + ultimateCooldown;
        }
    }

    private void CirclePlayerSmoothly()
    {
        angleOffset += circleSpeed * Time.deltaTime;
        Vector3 targetPosition = player.position + new Vector3(
            Mathf.Cos(angleOffset) * circleRadius,
            0f,
            Mathf.Sin(angleOffset) * circleRadius
        );

        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothMoveSpeed);
        transform.LookAt(Vector3.Lerp(transform.position + transform.forward, player.position, 0.2f));
    }

    private void UpdateCirclingProjectiles()
    {
        for (int i = 0; i < circlingProjectileCount; i++)
        {
            float angle = angleOffset + i * Mathf.PI * 2 / circlingProjectileCount;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * circleRadius / 2,
                1f,
                Mathf.Sin(angle) * circleRadius / 2
            );

            if (circlingProjectiles[i] != null)
            {
                circlingProjectiles[i].transform.position = Vector3.Lerp(
                    circlingProjectiles[i].transform.position,
                    transform.position + offset,
                    smoothMoveSpeed
                );
                circlingProjectiles[i].transform.LookAt(player);
            }
        }
    }

    private IEnumerator MenacingStareAndVolley()
    {
        isStaring = true;
        PlaySound(menacingStareSound);

        // Light intensity fade-in
        yield return LightTransition(0, lightMaxIntensity, stareDuration / 2);

        // Fire projectiles in a volley
        PlaySound(volleySound);
        yield return FireProjectileVolley();

        // Light intensity fade-out
        yield return LightTransition(lightMaxIntensity, 0, stareDuration / 2);

        isStaring = false;
        nextVolleyTime = Time.time + volleyCooldown;
    }

    private IEnumerator LightTransition(float startIntensity, float targetIntensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (stareLight != null)
                stareLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }
    }

    private IEnumerator FireProjectileVolley()
    {
        for (int i = 0; i < volleyCount; i++)
        {
            if (tip != null && projectilePrefab != null)
            {
                GameObject bullet = Instantiate(projectilePrefab, tip.position, tip.rotation);
                Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
                if (bulletRb != null)
                {
                    Vector3 targetPosition = PredictPlayerPosition(player.position, player.GetComponent<Rigidbody>().linearVelocity, tip.position, projectileSpeed);
                    Vector3 direction = (targetPosition - tip.position).normalized;
                    bulletRb.linearVelocity = direction * projectileSpeed;
                }
            }
            yield return new WaitForSeconds(0.2f); // Delay between volleys
        }
    }

    private IEnumerator UltimateAttack()
    {
        PlaySound(ultimateSound);

        // Create dramatic effect with delay
        yield return new WaitForSeconds(2f);

        GameObject boulder = Instantiate(boulderPrefab, tip.position + Vector3.up * 2, tip.rotation);
        Rigidbody rb = boulder.GetComponent<Rigidbody>();

        Vector3 targetPosition = PredictPlayerPosition(player.position, player.GetComponent<Rigidbody>().linearVelocity, tip.position, projectileSpeed);
        Vector3 direction = (targetPosition - tip.position).normalized;

        rb.linearVelocity = direction * projectileSpeed;
    }

    private Vector3 PredictPlayerPosition(Vector3 playerPosition, Vector3 playerVelocity, Vector3 shooterPosition, float projectileSpeed)
    {
        Vector3 toPlayer = playerPosition - shooterPosition;
        float a = Vector3.Dot(playerVelocity, playerVelocity) - (projectileSpeed * projectileSpeed);
        float b = 2 * Vector3.Dot(playerVelocity, toPlayer);
        float c = Vector3.Dot(toPlayer, toPlayer);
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            return playerPosition; // No prediction possible, return current position
        }

        float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float t = Mathf.Max(t1, t2);

        return playerPosition + playerVelocity * t;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            currentHealth -= 10;
            PlaySound(hitSound);

            if (currentHealth <= 0)
            {
                //StartCoroutine(BossDeath());
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
