using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bosses1 : MonoBehaviour
{
    private Transform player;
    public Transform tip;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float headSpeed = 5f;
    public float bodySpeed = 5f;
    public float stopDistance = 10f;
    public float wanderDistance = 20f;
    public float smoothTime = 0.5f;
    public GameObject bodySegmentPrefab;
    public int segmentCount = 10;
    public float segmentSpacing = 0.5f;
    public float strikeCooldown = 3f;
    public float stareTime = 2f; 
    public float rotationSpeed = 1.5f;

    public AudioClip strikeSound;
    public AudioClip flyingSound;
    public AudioClip roarSound;
    public AudioClip deathSound;
    public AudioClip hitSound;


    public float health;

    private List<Transform> bodySegments = new List<Transform>();
    private List<Vector3> bodyPositions = new List<Vector3>();
    private Vector3 velocity = Vector3.zero;
    private bool isStriking = false;
    private bool isStaring = false;
    private float lastStrikeTime = -Mathf.Infinity;
    private AudioSource audioSource;
    private float timeSinceLastRoar;

    private bool inStrikePosition;
    private bool isDying;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        GenerateBodySegments();

        player = FindAnyObjectByType<Player>().playerCam;
        foreach (var segment in bodySegments)
        {
            bodyPositions.Add(transform.position);
        }
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (Time.time - lastStrikeTime >= strikeCooldown && distanceToPlayer <= stopDistance)
        {
            ApproachAndStareBeforeStrike();
        }
        else
        {
            WanderGracefully();
        }

        MoveBody();
    }

    void ApproachAndStareBeforeStrike()
    {
        if (isStriking) return;

        // Gradual approach and smooth rotation toward player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, player.position) > stopDistance)
        {
            transform.position = Vector3.SmoothDamp(transform.position, player.position, ref velocity, smoothTime);
            inStrikePosition = false;
        }
        else
        {
            if (!inStrikePosition)
            {
                inStrikePosition = true;
                StartCoroutine(StareBeforeStrike());
            }
        }
    }

    IEnumerator StareBeforeStrike()
    {
        isStaring = true;

        // Play a roar sound upon stopping to stare
        PlaySound(roarSound, 0.8f);

   
        yield return new WaitForSeconds(stareTime);

        Strike();
        isStaring = false;
    }

    void Strike()
    {
        isStriking = true;
        lastStrikeTime = Time.time;

        // Play strike sound
        PlaySound(strikeSound, 1f);

        // Fire bullet
        if (tip != null && bulletPrefab != null)
        {
            GameObject bullet = PoolManager.Instance.SpawnFromPool("bossBullet", tip.position, tip.rotation);
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = tip.forward * bulletSpeed;
            }

            // Ignore collision between bullet and body segments
            Collider bulletCollider = bullet.GetComponent<Collider>();
            if (bulletCollider != null)
            {
                foreach (var segment in bodySegments)
                {
                    Collider segmentCollider = segment.GetComponent<Collider>();
                    if (segmentCollider != null)
                    {
                        Physics.IgnoreCollision(bulletCollider, segmentCollider);
                    }
                }
            }
        }

        Invoke(nameof(ResetStrike), 1.5f); // Longer strike reset for smoother experience
    }

    void WanderGracefully()
    {
        if (isStriking || isStaring) return;

        Vector3 wanderTarget = player.position + new Vector3(
            Mathf.PerlinNoise(Time.time * 0.5f, 0f) * wanderDistance - wanderDistance / 2,
            Mathf.Sin(Time.time * 0.8f) * wanderDistance / 2,
            Mathf.PerlinNoise(0f, Time.time * 0.5f) * wanderDistance - wanderDistance / 2
        );

        transform.position = Vector3.SmoothDamp(transform.position, wanderTarget, ref velocity, smoothTime);

        Quaternion targetRotation = Quaternion.LookRotation(wanderTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Play flying sound with pitch variation based on movement speed
        float speed = velocity.magnitude;
        PlaySound(flyingSound, Mathf.Clamp(speed / 10f, 0.3f, 1f), 0.8f, 1.2f);

        // Random roars between 6-12 seconds
        if (Time.time - timeSinceLastRoar > Random.Range(6f, 12f))
        {
            PlaySound(roarSound, 5f);
            timeSinceLastRoar = Time.time;
        }
    }

    void MoveBody()
    {
        bodyPositions.Insert(0, transform.position);

        Quaternion previousRotation = transform.rotation;

        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (i * segmentSpacing < bodyPositions.Count)
            {
                bodySegments[i].position = Vector3.Lerp(
                    bodySegments[i].position,
                    bodyPositions[Mathf.RoundToInt(i * segmentSpacing)],
                    bodySpeed * Time.deltaTime
                );

                bodySegments[i].rotation = Quaternion.Lerp(
                    bodySegments[i].rotation,
                    previousRotation,
                    bodySpeed * Time.deltaTime
                );
            }
        }

        while (bodyPositions.Count > segmentCount * segmentSpacing)
        {
            bodyPositions.RemoveAt(bodyPositions.Count - 1);
        }
    }

     private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            health -= 10;
            PlaySound(hitSound, 1f);

            if (health<= 0)
            {
                //StartCoroutine(BossDeath());
                BoxCollider boxCollider = collision.gameObject.GetComponent<BoxCollider>();
                boxCollider.enabled = false;

               
                foreach (Transform segment in bodySegments)
                {
                    if (segment != null)
                    {
                        AddRigidbodyToSegment(segment.gameObject);
                    }
                }

                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = 5f;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;

                //gameObject.GetComponent<Bosses1>().enabled = false;
            }
        }
    }

    private IEnumerator BossDeath()
    {
        isDying = true;
        PlaySound(deathSound, 1f);

        // Add Rigidbody to the boss for collapse
        AddRigidbodyIfNecessary();

        // Make body segments collapse
        foreach (Transform segment in bodySegments)
        {
            if (segment != null)
            {
                AddRigidbodyToSegment(segment.gameObject);
            }
        }

        yield return new WaitForSeconds(5f);

       
    }

    void GenerateBodySegments()
    {
        // Clear previous segments
        foreach (var segment in bodySegments)
        {
            Destroy(segment.gameObject);
        }
        bodySegments.Clear();

        // Add new segments
        Vector3 previousPosition = transform.position;
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 segmentPosition = previousPosition - transform.forward * segmentSpacing;
            GameObject segment = Instantiate(bodySegmentPrefab, segmentPosition, Quaternion.identity);
            bodySegments.Add(segment.transform);
            previousPosition = segmentPosition;
        }
    }

    private void AddRigidbodyIfNecessary()
    {
        if (!gameObject.TryGetComponent<Rigidbody>(out Rigidbody existingRb))
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
    }

    private void AddRigidbodyToSegment(GameObject segment)
    {
        if (!segment.TryGetComponent<Rigidbody>(out Rigidbody existingRb))
        {
            Rigidbody rb = segment.AddComponent<Rigidbody>();
            rb.mass = 2f; // Lighter mass for segments
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
    }

    void ResetStrike()
    {
        isStriking = false;
        inStrikePosition = false;
    }

    void PlaySound(AudioClip clip, float volume, float minPitch = 1f, float maxPitch = 1f)
    {
        if (audioSource == null || clip == null) return;

        if (!audioSource.isPlaying || audioSource.clip != clip)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
