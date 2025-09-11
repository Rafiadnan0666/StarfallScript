using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Swarm,
    VoidSpitter,
    BursterDrone
}

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public class ModularEnemy : MonoBehaviour
{
    [Header("General Settings")]
    public EnemyType enemyType;
    public float health = 100f;
    public bool isFloating = true;

    [Header("Detection Settings")]
    public float detectionRange = 20f;
    public float fovAngle = 110f;
    public float hearingRange = 15f;
    public float soundThreshold = 5f;
    public LayerMask detectionLayers;
    public LayerMask groundLayers;

    [Header("Ground Check Settings")]
    public float groundCheckDistance = 5f;
    public float minGroundDistance = 1.5f;
    public float maxGroundDistance = 3f;
    public float groundAdjustSpeed = 5f;
    public float groundStabilizationForce = 10f;

    [Header("Combat Settings")]
    public float fireRate = 1f;
    public Transform shootPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;

    [Header("Explosion Settings (Burster Drone)")]
    public GameObject explosionEffect;
    public float explosionRadius = 5f;
    public float explosionDamage = 100f;
    public float armDistance = 3f;
    public float beepInterval = 0.5f;


    [Header("Look Around Settings")]
    public float lookAroundInterval = 6f;
    public float lookAroundDuration = 1.5f;
    public int lookAroundRays = 8;
    public float lookAroundRayLength = 12f;
    private float nextLookAroundTime;
    private bool isLookingAround;
    private float lookAroundTimer;

    [Header("Line Renderer Effect")]
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.08f;
    private LineRenderer lineRenderer;
    private Vector3 lineTarget;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float retreatDistance = 5f;
    public float floatHeight = 2f;
    public float floatSpeed = 1f;
    public float floatAmplitude = 0.1f;
    public float maxDistanceFromBase = 10f;
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;
    public float wanderSpeedMultiplier = 0.5f;
    public float stabilityDamping = 2f;
    public float maxTiltAngle = 15f;

    [Header("Audio Settings")]
    public AudioClip moveSound;
    public AudioClip shootSound;
    public AudioClip explodeSound;
    public AudioClip detectSound;
    public AudioClip beepSound;
    public AudioClip chargeSound;
    [Range(0, 1)] public float audioVolume = 0.7f;

    private Transform player;
    private Transform clone;
    private Rigidbody rb;
    private AudioSource audioSource;
    private float nextFireTime;
    private bool playerDetected;
    private Vector3 lastKnownPosition;
    private Vector3 basePosition;
    private float originalY;
    private float lastBeepTime;
    private bool isArmed;
    private List<ModularEnemy> swarmGroup = new();
    private Vector3 wanderTarget;
    private float nextWanderTime = 1f;
    private bool isWandering;
    private float currentGroundDistance;
    private bool isGrounded;
    private Vector3 smoothDampVelocity;
    private float lastGroundCheckTime;
    private float currentFloatOffset;
    private Quaternion targetRotation;
    private Vector3 currentGroundNormal = Vector3.up;
    private float dodgeCooldown = 0f;
    private float nextDodgeTime = 0f;
    private float lowHealthThreshold = 30f;
    private bool isHiding = false;
    private Vector3 hideTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = audioVolume;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        clone = GameObject.FindGameObjectWithTag("Clone")?.transform;
        player = clone || player == null ? clone : player;
        basePosition = transform.position;
        originalY = transform.position.y;
        targetRotation = transform.rotation;

        if (enemyType == EnemyType.BursterDrone)
        {
            maxDistanceFromBase *= 10f; // Wander twice as far
            wanderRadius *= 10f;
        }

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.enabled = false;

        if (isFloating)
        {
            rb.useGravity = false;
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
        }

        StartCoroutine(ScanEnvironment());
        StartCoroutine(FloatAnimation());
        StartCoroutine(GroundCheck());
    }

    public void ApplyScaling(float healthMod, float damageMod, float speedMod)
    {
        health *= healthMod;
 
        moveSpeed *= speedMod;

        explosionDamage *= damageMod;
      
    }

    void Update()
    {
        if (player == null) return;

        // Expand wander area over time
        if (Vector3.Distance(transform.position, basePosition) > maxDistanceFromBase)
            maxDistanceFromBase += Time.deltaTime * 0.2f;

        DetectPlayer();
        DetectBulletsAndDodge();
        CheckLowHealthAndHide();

        ConstrainToArea();
        MaintainGroundDistance();
        StabilizeOrientation();
        if (enemyType == EnemyType.Swarm) FindSwarmMembers();
        if (transform.position.y - basePosition.y > 10f)
            rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);

        switch (enemyType)
        {
            case EnemyType.Swarm: SwarmBehavior(); break;
            case EnemyType.VoidSpitter: VoidSpitterBehavior(); break;
            case EnemyType.BursterDrone: BursterDroneBehavior(); break;
        }

        if (!playerDetected && !isHiding)
        {
            if (!isLookingAround && Time.time > nextLookAroundTime)
            {
                isLookingAround = true;
                lookAroundTimer = 0f;
                nextLookAroundTime = Time.time + lookAroundInterval + Random.Range(-4f, 4f);
            }
            if (isLookingAround)
                LookAround();
            else
                WanderBehavior();
        }
        else if (isHiding)
        {
            HideBehindWall();
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
    void DetectBulletsAndDodge()
    {
        if (Time.time < nextDodgeTime) return;
        Collider[] hits = Physics.OverlapSphere(transform.position, 8f, detectionLayers);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bullet"))
            {
                Vector3 bulletDir = (transform.position - hit.transform.position).normalized;
                Vector3 dodgeDir = Vector3.Cross(bulletDir, Vector3.up).normalized;
                rb.AddForce(dodgeDir * 12f, ForceMode.VelocityChange);
                nextDodgeTime = Time.time + dodgeCooldown;
                break;
            }
        }
    }

    void CheckLowHealthAndHide()
    {
        if (health > lowHealthThreshold)
        {
            isHiding = false;
            return;
        }
        // Raycast in several directions to find a wall
        float bestDist = 0f;
        Vector3 bestDir = Vector3.zero;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, 10f, detectionLayers))
            {
                if (hit.collider.CompareTag("Wall") && hit.distance > bestDist)
                {
                    bestDist = hit.distance;
                    bestDir = dir;
                }
            }
        }
        if (bestDist > 0f)
        {
            hideTarget = transform.position + bestDir * (bestDist - 1.5f);
            isHiding = true;
        }
    }


    void HideBehindWall()
    {
        Vector3 dir = (hideTarget - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
        FaceTarget(hideTarget, true);
        if (Vector3.Distance(transform.position, hideTarget) < 1f)
            isHiding = false;
    }
    void LookAround()
    {
        lookAroundTimer += Time.deltaTime;
        rb.linearVelocity = Vector3.zero;

        float lookAngle = Mathf.PingPong(Time.time * 60f, 360f);
        targetRotation = Quaternion.Euler(0, lookAngle, 0);

        bool foundPlayer = false;
        for (int i = 0; i < lookAroundRays; i++)
        {
            float angle = i * (360f / lookAroundRays);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, lookAroundRayLength, detectionLayers))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    foundPlayer = true;
                    playerDetected = true;
                    lastKnownPosition = hit.point;
                    PlaySound(detectSound);
                    break;
                }
            }
            // Draw each ray with LineRenderer (optional: use Debug.DrawLine for multiple rays)
            if (i == 0) // Only show the first ray with LineRenderer for effect
            {
                lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.5f);
                lineRenderer.SetPosition(1, transform.position + dir * lookAroundRayLength);
                lineRenderer.enabled = true;
            }
        }

        if (lookAroundTimer > lookAroundDuration || foundPlayer)
        {
            isLookingAround = false;
            lineRenderer.enabled = false;
        }
    }


    void FindSwarmMembers()
    {
        swarmGroup.Clear();

        ModularEnemy[] allEnemies = FindObjectsOfType<ModularEnemy>();
        foreach (var enemy in allEnemies)
        {
            if (enemy != this && enemy.enemyType == EnemyType.Swarm)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < detectionRange)
                {
                    swarmGroup.Add(enemy);
                }
            }
        }
    }


    void FixedUpdate()
    {
        if (isFloating)
        {
            ApplyStabilizationForces();
            ApplyFloatOffset();
        }
    }

    void ApplyFloatOffset()
    {
        if (isGrounded)
        {
            Vector3 position = transform.position;
            float targetY = position.y + (floatHeight - currentGroundDistance) + currentFloatOffset;
            position.y = Mathf.Lerp(position.y, targetY, Time.deltaTime * 5f);
            rb.MovePosition(position);
        }
    }


    void StabilizeOrientation()
    {
        if (isFloating)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void ApplyStabilizationForces()
    {
        if (rb.angularVelocity.magnitude > 0.1f)
        {
            rb.AddTorque(-rb.angularVelocity * stabilityDamping, ForceMode.Acceleration);
        }

        Quaternion target = Quaternion.FromToRotation(transform.up, currentGroundNormal) * rb.rotation;
        Vector3 torque = CalculateTorque(target);
        rb.AddTorque(torque * stabilityDamping, ForceMode.Acceleration);

        float tiltAngle = Vector3.Angle(transform.up, Vector3.up);
        if (tiltAngle > maxTiltAngle)
        {
            Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(tiltAxis * tiltAngle * 0.1f, ForceMode.Acceleration);
        }
    }

    Vector3 CalculateTorque(Quaternion targetRotation)
    {
        Quaternion q = targetRotation * Quaternion.Inverse(transform.rotation);
        q.ToAngleAxis(out float angle, out Vector3 axis);
        angle = Mathf.DeltaAngle(0, angle);
        angle *= Mathf.Deg2Rad;
        return axis.normalized * angle * stabilityDamping;
    }

    IEnumerator GroundCheck()
    {
        while (true)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayers))
            {
                currentGroundDistance = hit.distance;
                currentGroundNormal = hit.normal;
                isGrounded = true;

                if (isFloating)
                {
                    targetRotation = Quaternion.FromToRotation(transform.up, currentGroundNormal) * transform.rotation;
                }
            }
            else
            {
                isGrounded = false;
                currentGroundNormal = Vector3.up;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void MaintainGroundDistance()
    {
        if (!isFloating || !isGrounded) return;

        float heightError = (floatHeight - currentGroundDistance);
        float correctionForce = heightError * groundAdjustSpeed;

        rb.AddForce(Vector3.up * correctionForce, ForceMode.Acceleration);
    }



    void WanderBehavior()
    {
        if (Time.time >= nextWanderTime || Vector3.Distance(transform.position, wanderTarget) < 0.5f)
        {
            SetNewWanderTarget();
            nextWanderTime = Time.time + wanderInterval;
        }

        Vector3 direction = (wanderTarget - transform.position).normalized;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, direction * moveSpeed * wanderSpeedMultiplier, Time.deltaTime * 2f);
        FaceTarget(wanderTarget, true);

        if (Random.value < 0.01f)
        {
            PlaySound(moveSound, false);
        }
    }

    void SetNewWanderTarget()
    {
        Vector3 baseWanderOrigin = enemyType == EnemyType.Swarm && swarmGroup.Count > 0
            ? CalculateGroupCenter()
            : transform.position;

        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        wanderTarget = baseWanderOrigin + randomOffset;

        // Ground check for Y placement
        if (Physics.Raycast(wanderTarget + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f, groundLayers))
        {
            wanderTarget.y = hit.point.y + floatHeight;
        }
        else
        {
            wanderTarget.y = baseWanderOrigin.y + floatHeight;
        }

        // Stay inside max radius
        if (Vector3.Distance(wanderTarget, basePosition) > maxDistanceFromBase)
        {
            wanderTarget = basePosition + (wanderTarget - basePosition).normalized * maxDistanceFromBase * 0.9f;
        }
    }


    IEnumerator FloatAnimation()
    {
        while (isFloating)
        {
            currentFloatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            yield return null;
        }
    }

    void ConstrainToArea()
    {
        float distanceFromBase = Vector3.Distance(transform.position, basePosition);
        if (distanceFromBase > maxDistanceFromBase)
        {
            Vector3 direction = (basePosition - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            if (isFloating && !isGrounded)
            {
                rb.AddForce(Vector3.down * 2f, ForceMode.Acceleration);
            }
        }
    }

    void FaceTarget(Vector3 target, bool smooth = false)
    {
        Vector3 direction = (target - transform.position).normalized;
        if (isFloating)
        {
            direction = Vector3.ProjectOnPlane(direction, currentGroundNormal).normalized;
        }
        else
        {
            direction.y = 0;
        }

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            if (smooth)
            {
                targetRotation = Quaternion.Slerp(targetRotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                targetRotation = lookRotation;
            }
        }
    }

    void SwarmBehavior()
    {
        FindSwarmMembers();

        if (playerDetected)
        {
            Vector3 toPlayer = (player.position - transform.position).normalized;
            rb.linearVelocity = toPlayer * moveSpeed;
            FaceTarget(player.position);
            TryShoot();
            return;
        }

        Vector3 groupCenter = CalculateGroupCenter();

        if (Time.time >= nextWanderTime || Vector3.Distance(groupCenter, wanderTarget) < 1f)
        {
            SetNewWanderTarget();
            nextWanderTime = Time.time + wanderInterval;
        }

        Vector3 toTarget = (wanderTarget - transform.position).normalized;
        Vector3 toCenter = (groupCenter - transform.position).normalized;

        Vector3 moveDir = (toTarget * 0.7f + toCenter * 0.3f).normalized;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, moveDir * moveSpeed * wanderSpeedMultiplier, Time.deltaTime * 2f);

        FaceTarget(transform.position + moveDir, true);
    }



    Vector3 CalculateGroupCenter()
    {
        Vector3 center = transform.position;
        int count = 1;

        for (int i = swarmGroup.Count - 1; i >= 0; i--)
        {
            if (swarmGroup[i] == null)
            {
                swarmGroup.RemoveAt(i);
                continue;
            }

            center += swarmGroup[i].transform.position;
            count++;
        }

        return center / count;
    }


    void VoidSpitterBehavior()
    {
        if (playerDetected)
        {
            FaceTarget(player.position);

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < retreatDistance)
            {
                Vector3 retreatDir = (transform.position - player.position).normalized;
                rb.linearVelocity = retreatDir * moveSpeed;
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
                if (Time.time >= nextFireTime)
                {
                    StartCoroutine(ChargeAndShoot());
                    StartCoroutine(ChargeAndShoot());
                    StartCoroutine(ChargeAndShoot());
                    TryShoot();
                    TryShoot();
                    TryShoot();
                    nextFireTime = Time.time + 1f / fireRate;
                }
            }
        }
    }

    IEnumerator ChargeAndShoot()
    {
        PlaySound(chargeSound);

        Vector3 originalScale = transform.localScale;
        float chargeTime = 0.5f;
        float timer = 0f;

        while (timer < chargeTime)
        {
            float scale = 1 + Mathf.PingPong(timer * 2, 0.1f);
            transform.localScale = originalScale * scale;
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        TryShoot();
    }

    void BursterDroneBehavior()
    {
        if (playerDetected)
        {
            FaceTarget(player.position);
            Vector3 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < armDistance && !isArmed)
            {
                ArmForExplosion();
            }

            if (distance < 2f && isArmed)
            {
                Explode();
            }

            if (isArmed && Time.time > lastBeepTime + beepInterval)
            {
                float beepPitch = Mathf.Lerp(2f, 1f, distance / armDistance);
                audioSource.pitch = beepPitch;
                PlaySound(beepSound);
                lastBeepTime = Time.time;
            }
        }
    }

    void ArmForExplosion()
    {
        isArmed = true;
        PlaySound(chargeSound, true);
    }

    void TryShoot()
    {
        if (shootPoint && bulletPrefab)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
            if (bullet.TryGetComponent<Rigidbody>(out var bulletRb))
                bulletRb.linearVelocity = shootPoint.forward * bulletSpeed;

            PlaySound(shootSound);
        }
    }

    void Explode()
    {
        if (explosionEffect)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float damage = explosionDamage * (1 - Mathf.Clamp01(distance / explosionRadius));
                Player playerScript = hit.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(damage);

                    // Knockback
                    Rigidbody playerRb = hit.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        Vector3 forceDir = (hit.transform.position - transform.position).normalized;
                        float force = Mathf.Lerp(600f, 200f, distance / explosionRadius);
                        playerRb.AddForce(forceDir * force + Vector3.up * 200f, ForceMode.Impulse);
                    }
                }
            }
        }

        PlaySound(explodeSound);
        Destroy(gameObject);
    }


    void DetectPlayer()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= detectionRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle < fovAngle * 0.5f)
            {
                if (Physics.Raycast(transform.position, dirToPlayer, out RaycastHit hit, detectionRange, detectionLayers))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        if (!playerDetected) PlaySound(detectSound);
                        playerDetected = true;
                        lastKnownPosition = player.position;
                        return;
                    }
                }
            }
        }

        if (dist <= hearingRange)
        {
            var movement = player.GetComponent<Player>();
            if (movement != null && movement.speed > soundThreshold)
            {
                if (!playerDetected) PlaySound(detectSound);
                playerDetected = true;
                lastKnownPosition = player.position;
                return;
            }
        }

        playerDetected = false;

    }

    void PlaySound(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;

        if (loop)
        {
            if (audioSource.clip != clip || !audioSource.isPlaying)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }

    IEnumerator ScanEnvironment()
    {
        while (true)
        {
            if (!playerDetected)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f;
                    Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
                    if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange * 0.5f, detectionLayers))
                    {
                        if (hit.collider.CompareTag("Player"))
                        {
                            playerDetected = true;
                            lastKnownPosition = hit.transform.position;
                            PlaySound(detectSound);
                            break;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f)
        {
            if (enemyType == EnemyType.BursterDrone && isArmed)
            {
                Explode();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Bullet"))
        {
            TakeDamage(25f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * transform.forward * detectionRange;
        Vector3 right = Quaternion.Euler(0, fovAngle * 0.5f, 0) * transform.forward * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);

        if (enemyType == EnemyType.BursterDrone)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, armDistance);
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(basePosition, maxDistanceFromBase);

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        if (!playerDetected)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(wanderTarget, 0.3f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }
}