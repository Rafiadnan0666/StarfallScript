using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 20f;
    public float fovAngle = 110f;
    public float hearingRange = 15f;
    public float soundThreshold = 5f;
    public LayerMask detectionLayers;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float jumpForce = 8f;
    public float jumpCooldown = 3f;
    public float landingRecoveryTime = 0.5f;
    public float groundCheckDistance = 0.2f;

    [Header("Combat Settings")]
    public float health = 100f;
    public float fireRate = 1f;
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;

    [Header("Audio Settings")]
    public AudioClip moveSound;
    public AudioClip shootSound;
    public AudioClip jumpChargeSound;
    public AudioClip jumpSound;
    public AudioClip landSound;

    [Header("Body Parts")]
    public Transform upperBody;
    public Transform lowerBody;
    public Transform[] legs;
    public float bodySeparation = 0.5f;
    public float legMovementSpeed = 5f;
    public float legMovementAmount = 0.2f;

    private Transform player;
    private AudioSource audioSource;
    private Rigidbody rb;
    private bool playerDetected;
    private Vector3 lastKnownPosition;
    private float nextFireTime;
    private float lastJumpTime;
    private bool isGrounded;
    private bool isJumping;
    private bool isPreparingJump;
    private Vector3[] defaultLegPositions;
    private Quaternion[] defaultLegRotations;
    private Vector3 currentVelocity;
    private Vector3 lastPosition;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        defaultLegPositions = new Vector3[legs.Length];
        defaultLegRotations = new Quaternion[legs.Length];
        for (int i = 0; i < legs.Length; i++)
        {
            defaultLegPositions[i] = legs[i].localPosition;
            defaultLegRotations[i] = legs[i].localRotation;
        }

        StartCoroutine(ScanEnvironment());
        StartCoroutine(MoveLegs());
    }

    void Update()
    {
        CheckGroundStatus();
        UpdateCurrentVelocity();

        if (player != null)
        {
            DetectPlayer();

            if (playerDetected)
            {
                EngagePlayer();
            }
            else
            {
                InvestigateArea();
            }
        }

        UpdateBodyStacking();
    }

    void UpdateCurrentVelocity()
    {
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void CheckGroundStatus()
    {
        RaycastHit hit;
        isGrounded = Physics.Raycast(lowerBody.position, Vector3.down, out hit, groundCheckDistance);

        if (isJumping && isGrounded)
        {
            OnLand();
        }
    }

    void UpdateBodyStacking()
    {
        
        upperBody.position = lowerBody.position + Vector3.up * bodySeparation;

    
        upperBody.rotation = Quaternion.LookRotation(Vector3.down, transform.forward);
    }

    void DetectPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - upperBody.position).normalized;
            float angle = Vector3.Angle(directionToPlayer, upperBody.forward);

            if (angle < fovAngle * 0.5f)
            {
                RaycastHit hit;
                if (Physics.Raycast(upperBody.position, directionToPlayer, out hit, detectionRange, detectionLayers))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        playerDetected = true;
                        lastKnownPosition = player.position;
                        return;
                    }
                }
            }
        }
        if (distanceToPlayer <= hearingRange)
        {
            Player playerMovement = player.GetComponent<Player>();
            if (playerMovement != null && playerMovement.speed > soundThreshold)
            {
                playerDetected = true;
                lastKnownPosition = player.position;
                return;
            }
        }

        playerDetected = false;
    }

    void EngagePlayer()
    {
        FaceTarget(player.position);
        if (Vector3.Distance(transform.position, player.position) > 5f)
        {
            Vector3 moveDirection = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
        }
        else
        {
            Vector3 right = Vector3.Cross(Vector3.up, (player.position - transform.position).normalized);
            rb.linearVelocity = right * moveSpeed * 0.5f;
        }

        TryShoot();
    }

    void InvestigateArea()
    {
        if (lastKnownPosition != Vector3.zero)
        {

            if (Vector3.Distance(transform.position, lastKnownPosition) > 1f)
            {
                FaceTarget(lastKnownPosition);
                rb.linearVelocity = transform.forward * moveSpeed;
            }
            else
            {

                StartCoroutine(ScanArea());

                // Consider jumping to nearby position
                if (Time.time > lastJumpTime + jumpCooldown && isGrounded && !isPreparingJump)
                {
                    StartCoroutine(PrepareJump());
                }
            }
        }
        else
        {
            // Wander randomly
            if (isGrounded && !isPreparingJump)
            {
                if (Random.value < 0.01f)
                {
                    transform.Rotate(0, Random.Range(-90, 90), 0);
                }
                rb.linearVelocity = transform.forward * moveSpeed * 0.5f;
            }
        }
    }

    IEnumerator ScanArea()
    {
        float scanTime = 3f;
        float scanSpeed = 90f;
        float startTime = Time.time;

        while (Time.time < startTime + scanTime)
        {
            upperBody.Rotate(0, scanSpeed * Time.deltaTime, 0);
            yield return null;
        }
    }

    IEnumerator PrepareJump()
    {
        isPreparingJump = true;

   
        bodySeparation *= 0.5f;
        PlaySound(jumpChargeSound);

        yield return new WaitForSeconds(1f);

    
        Vector3 jumpTarget = FindJumpTarget();
        Vector3 jumpDirection = (jumpTarget - transform.position).normalized;

 
        Jump(jumpDirection);

    
        yield return new WaitForSeconds(0.2f);
        bodySeparation = 0.5f;

        isPreparingJump = false;
    }

    Vector3 FindJumpTarget()
    {

        Collider[] interestingPoints = Physics.OverlapSphere(transform.position, 10f);
        foreach (var point in interestingPoints)
        {
            if (point.CompareTag("PointOfInterest"))
            {
                return point.transform.position;
            }
        }

  
        return transform.position + transform.forward * 5f + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
    }

    void Jump(Vector3 direction)
    {
        isJumping = true;
        lastJumpTime = Time.time;
        rb.AddForce((direction + Vector3.up) * jumpForce, ForceMode.Impulse);
        PlaySound(jumpSound);
    }

    void OnLand()
    {
        isJumping = false;
        PlaySound(landSound);
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    void TryShoot()
    {
        if (Time.time >= nextFireTime && !isJumping && !isPreparingJump)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void Shoot()
    {
        if (shootPoint == null || bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = shootPoint.forward * bulletSpeed;
        }

        PlaySound(shootSound);
    }

    IEnumerator MoveLegs()
    {
        while (true)
        {
            if (currentVelocity.magnitude > 0.1f && isGrounded && !isJumping && !isPreparingJump)
            {

                for (int i = 0; i < legs.Length; i++)
                {
                    float phase = Mathf.Sin(Time.time * legMovementSpeed + (i * Mathf.PI / legs.Length));
                    legs[i].localPosition = defaultLegPositions[i] + new Vector3(0, 0, phase * legMovementAmount);
                    legs[i].localRotation = defaultLegRotations[i] * Quaternion.Euler(phase * 30f, 0, 0);
                }
            }
            else
            {
   
                for (int i = 0; i < legs.Length; i++)
                {
                    legs[i].localPosition = Vector3.Lerp(legs[i].localPosition, defaultLegPositions[i], Time.deltaTime * 5f);
                    legs[i].localRotation = Quaternion.Lerp(legs[i].localRotation, defaultLegRotations[i], Time.deltaTime * 5f);
                }
            }

            yield return null;
        }
    }

    IEnumerator ScanEnvironment()
    {
        while (true)
        {
            if (!playerDetected)
            {
        
                Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange * 0.5f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        lastKnownPosition = hit.transform.position;
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) Die(); 
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Draw FOV
        if (upperBody != null)
        {
            Gizmos.color = Color.red;
            Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * upperBody.forward * detectionRange;
            Vector3 right = Quaternion.Euler(0, fovAngle * 0.5f, 0) * upperBody.forward * detectionRange;
            Gizmos.DrawLine(upperBody.position, upperBody.position + left);
            Gizmos.DrawLine(upperBody.position, upperBody.position + right);
        }
    }
}