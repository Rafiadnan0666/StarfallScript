using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bosses2 : MonoBehaviour
{
    private Transform player;
    public Transform tip;
    public GameObject bulletPrefab;
    public GameObject digParticlePrefab;
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
    public float digDepth = -10f;
    public float sideStrikeOffset = 8f;

    public AudioClip strikeSound;
    public AudioClip flyingSound;
    public AudioClip roarSound;
    public AudioClip digSound;
    public AudioClip hitSound;

    private List<Transform> bodySegments = new List<Transform>();
    private List<Vector3> bodyPositions = new List<Vector3>();
    private Vector3 velocity = Vector3.zero;
    private bool isStriking = false;
    private bool isStaring = false;
    private AudioSource audioSource;
    private float nextStrikeTime;

    public float health;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        GenerateBodySegments();
        player = FindAnyObjectByType<Player>().playerCam;
        foreach (var segment in bodySegments)
        {
            bodyPositions.Add(transform.position);
        }

        ScheduleNextStrike();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (Time.time >= nextStrikeTime && distanceToPlayer <= stopDistance)
        {
            DeepStrikeMechanic();
        }
        else
        {
            WanderGracefully();
        }

        MoveBody();
    }

 



    void FireBullet()
    {
        PlaySound(strikeSound, 1f);
        if (tip != null && bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, tip.position, tip.rotation);
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = tip.forward * bulletSpeed;
            }
            foreach (var segment in bodySegments)
            {
                Collider segmentCollider = segment.GetComponent<Collider>();
                if (segmentCollider != null)
                {
                    Collider bulletCollider = bullet.GetComponent<Collider>();
                    if (bulletCollider != null)
                    {
                        Physics.IgnoreCollision(bulletCollider, segmentCollider);
                    }
                }
            }
        }
    }

   
    void WanderGracefully()
    {
        if (isStriking || isStaring) return;
        Vector3 randomOffset = new Vector3(
            Mathf.PerlinNoise(Time.time * 0.5f, 0f) * wanderDistance - wanderDistance / 2,
            Mathf.Sin(Time.time * 0.8f) * wanderDistance / 5,
            Mathf.PerlinNoise(0f, Time.time * 0.5f) * wanderDistance - wanderDistance / 2
        );

        Vector3 wanderTarget = player.position + randomOffset;
        transform.position = Vector3.SmoothDamp(transform.position, wanderTarget, ref velocity, smoothTime);
        Vector3 lookDirection = (player.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        
        if (Random.Range(0f, 1f) < 0.01f) 
        {
            StartCoroutine(SwoopTowardPlayer());
        }

        
        if (!audioSource.isPlaying)
        {
            PlaySound(flyingSound, 0.5f);
        }
    }

    IEnumerator SwoopTowardPlayer()
    {
        Vector3 swoopPosition = player.position + (Random.insideUnitSphere * 5f);
        swoopPosition.y = Mathf.Clamp(swoopPosition.y, player.position.y, player.position.y + 5f);

        float swoopDuration = 1.5f;
        float elapsedTime = 0f;

        while (elapsedTime < swoopDuration)
        {
            transform.position = Vector3.Lerp(transform.position, swoopPosition, elapsedTime / swoopDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    void DeepStrikeMechanic()
    {
        if (isStriking) return;

        StartCoroutine(DeepStrikeSequence());
    }

    IEnumerator DeepStrikeSequence()
    {
        isStriking = true;
        Vector3 starePosition = player.position + new Vector3(0f, 5f, 5f);
        float hoverDuration = 2f;
        float elapsedTime = 0f;

        PlaySound(roarSound, 1f);

        while (elapsedTime < hoverDuration)
        {
            transform.position = Vector3.Lerp(transform.position, starePosition, elapsedTime / hoverDuration);
            elapsedTime += Time.deltaTime;

           
            Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(stareTime);
        FireBullet();

        ScheduleNextStrike();
        isStriking = false;
    }


    void MoveBody()
    {
        bodyPositions.Insert(0, transform.position);

        for (int i = 0; i < bodySegments.Count; i++)
        {
            Vector3 targetPosition = bodyPositions[Mathf.Clamp(Mathf.RoundToInt(i * segmentSpacing), 0, bodyPositions.Count - 1)];
            bodySegments[i].position = Vector3.Lerp(bodySegments[i].position, targetPosition, bodySpeed * Time.deltaTime);

            if (i > 0)
            {
                Vector3 direction = bodySegments[i - 1].position - bodySegments[i].position;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                bodySegments[i].rotation = Quaternion.Slerp(bodySegments[i].rotation, targetRotation, bodySpeed * Time.deltaTime);
            }
        }

        while (bodyPositions.Count > segmentCount * segmentSpacing)
        {
            bodyPositions.RemoveAt(bodyPositions.Count - 1);
        }
    }

    void GenerateBodySegments()
    {
        foreach (var segment in bodySegments)
        {
            Destroy(segment.gameObject);
        }
        bodySegments.Clear();

        Vector3 previousPosition = transform.position;
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 segmentPosition = previousPosition - transform.forward * segmentSpacing;
            GameObject segment = Instantiate(bodySegmentPrefab, segmentPosition, Quaternion.identity);
            bodySegments.Add(segment.transform);
            previousPosition = segmentPosition;
        }
    }

    void ScheduleNextStrike()
    {
        nextStrikeTime = Time.time + Random.Range(strikeCooldown, strikeCooldown * 2f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            health -= 3;
            PlaySound(hitSound, 1f);
        }
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
