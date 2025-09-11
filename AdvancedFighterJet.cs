using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource), typeof(CapsuleCollider))]
public class AdvancedFighterJet : MonoBehaviour
{
    public enum Team { Red, Blue }
    public Team team;

    [Header("Aircraft Type")]
    public JetModel jetModel = JetModel.F16;
    public enum JetModel
    {
        F16, F22, F35, F15,
        JAS39, Eurofighter,
        Su27, Su35, Su57,
        Mig29, Mig35
    }

    [Header("Flight Performance")]
    public float maxSpeed = 300f;
    public float cruiseSpeed = 200f;
    public float minSpeed = 100f;
    public float afterburnerMultiplier = 1.5f;
    public float turnRate = 45f;
    public float climbRate = 40f;
    public float rollRate = 80f;
    public float agility = 2f;
    public float stability = 5f;
    public float thrustForce = 50f;
    public float maxAltitude = 5000f;
    public float patrolRadius = 10000f;
    public float health = 100f;

    [Header("Terrain Following")]
    public float minTerrainAltitude = 150f;
    public float maxTerrainAltitude = 800f;
    public float idealTerrainAltitude = 300f;
    public float terrainAvoidanceForce = 15f;
    public float terrainPredictionDistance = 500f;
    public LayerMask terrainMask = 1;

    [Header("Sensors & Detection")]
    public float radarRange = 3000f;
    public float radarFOV = 120f;
    public float visualRange = 8000f;
    public float groundScanRange = 1000f;
    public float detectionUpdateInterval = 0.5f;

    [Header("Weapon Systems")]
    public float gunRange = 1200f;
    public float gunDamage = 25f;
    public float gunFireRate = 10f;
    public int gunBurstCount = 5;
    public float gunSpread = 0.5f;
    public float gunCooldown = 2f;
    public float lockOnTime = 1.5f;
    public GameObject bulletPrefab;
    public GameObject blastBulletPrefab;
    public float blastRadius = 50f;
    public float blastDamage = 100f;
    public float groundStrikeRange = 2000f;
    public float groundAttackCooldown = 5f;

    [Header("Prefabs & References")]
    public GameObject gunMuzzleFlash;
    public Transform gunTransform;
    public Transform[] missileTransforms;
    public GameObject afterburnerEffect;
    public TrailRenderer contrailEffect;
    public float size = 1f;
    public GameObject explosionEffect;

    [Header("Aircraft Prefabs")]
    public GameObject f16Prefab;
    public GameObject f22Prefab;
    public GameObject f35Prefab;
    public GameObject f15Prefab;
    public GameObject jas39Prefab;
    public GameObject eurofighterPrefab;
    public GameObject su27Prefab;
    public GameObject su35Prefab;
    public GameObject su57Prefab;
    public GameObject mig29Prefab;
    public GameObject mig35Prefab;

    [Header("Audio")]
    public AudioClip engineSound;
    public AudioClip afterburnerSound;
    public AudioClip gunSound;
    public AudioClip radarPingSound;
    public AudioClip lockOnSound;
    public AudioClip missileLaunchSound;
    public AudioClip explosionSound;

    // Internal components
    private Rigidbody rb;
    private AudioSource audioSource;
    private AudioSource radarAudioSource;
    private CapsuleCollider col;
    private GameObject aircraftModel;
    private Terrain[] allTerrains;
    private Bounds flightAreaBounds;

    // Internal state
    private float currentSpeed;
    public float heath = 100f;
    private bool afterburnerActive = false;
    private float lastGunFireTime = 0f;
    private float lastGroundAttackTime = 0f;
    private float contrailTime = 0f;
    private Vector3 terrainNormal = Vector3.up;
    private float currentTerrainHeight;
    private bool isOverTerrain = true;
    private Vector3 patrolCenter;
    private float lastDetectionUpdate = 0f;
    private float gunCooldownEnd = 0f;
    private int burstCount = 0;
    private bool isEvading = false;
    private float evadeEndTime = 0f;
    private Vector3 evadeDirection = Vector3.zero;

    // AI State
    [SerializeField] private AIState currentState = AIState.Patrol;
    private Vector3 currentWaypoint;
    private Vector3 groundTarget;
    private float waypointChangeTime = 0f;
    private float stateChangeTime = 0f;

    // Targets
    private GameObject player;
    private AdvancedFighterJet currentTarget;
    private float targetLockProgress = 0f;
    private bool isTargetLocked = false;
    private List<AdvancedFighterJet> detectedTargets = new List<AdvancedFighterJet>();
    private List<AdvancedFighterJet> wingmen = new List<AdvancedFighterJet>();
    private List<Vector3> groundTargets = new List<Vector3>();

    // G-force effects
    private float currentGForce = 1f;
    private float maxGForce = 9f;
    private float gForceSmooth = 0f;

    // Animation
    private float flapPosition = 0f;
    private float rudderPosition = 0f;

    // Formation
    private bool isLeader = false;
    private AdvancedFighterJet formationLeader;
    private Vector3 formationOffset = Vector3.zero;

    // Events
    public System.Action<AdvancedFighterJet> OnTargetDestroyed;
    public System.Action<AdvancedFighterJet> OnWingmanAdded;

    // AI States
    private enum AIState
    {
        Patrol,
        Engage,
        Attack,
        GroundStrike,
        Evade,
        Regroup,
        ReturnToBase
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        col = GetComponent<CapsuleCollider>();

        // Create secondary audio source for radar
        radarAudioSource = gameObject.AddComponent<AudioSource>();
        radarAudioSource.spatialBlend = 1f;
        radarAudioSource.volume = 0.3f;
        radarAudioSource.maxDistance = 5000f;

        // Find all terrains to determine flight area
        allTerrains = FindObjectsOfType<Terrain>();
        CalculateFlightAreaBounds();

        // Find player
        player = GameObject.FindGameObjectWithTag("Player");

        // Set patrol center to initial position
        patrolCenter = transform.position;

        // Initialize audio
        if (engineSound != null)
        {
            audioSource.clip = engineSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Initialize aircraft model based on type
        InitializeAircraftModel();

        // Generate ground targets
        GenerateGroundTargets();
    }

    void Start()
    {
        // Start with cruising speed
        currentSpeed = cruiseSpeed;

        // Start detection coroutine
        StartCoroutine(UpdateDetections());

        // Start AI behavior coroutine
        StartCoroutine(AIBehavior());

        // Generate initial waypoint
        GenerateNewWaypoint();
    }

    void FixedUpdate()
    {
        HandleFlightPhysics();
        HandleWeaponSystems();
        HandleTerrainInteraction();
        HandleSpecialEffects();
        HandleBoundaryConstraints();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Handle bullet hits
        if (collision.gameObject.CompareTag("Bullet") || collision.gameObject.CompareTag("BlastBullet"))
        {
            float damage = collision.gameObject.CompareTag("BlastBullet") ? blastDamage : gunDamage;
            heath -= damage;

            if (heath <= 0f)
            {
                Explode();
                return;
            }

            if (currentState != AIState.Evade)
            {
                currentState = AIState.Evade;
                evadeEndTime = Time.time + 5f;
                stateChangeTime = Time.time;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw radar range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radarRange);

        // Draw visual range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visualRange);

        // Draw gun range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, gunRange);

        // Draw blast radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, blastRadius);

        // Draw current waypoint if in patrol mode
        if (Application.isPlaying && currentState == AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWaypoint, 50f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }

        // Draw ground targets
        Gizmos.color = Color.red;
        foreach (Vector3 target in groundTargets)
        {
            Gizmos.DrawWireSphere(target, 25f);
            Gizmos.DrawLine(transform.position, target);
        }

        // Draw terrain avoidance ray
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, terrainPredictionDistance, terrainMask))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawSphere(hit.point, 10f);
        }

        // Draw flight area bounds
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(flightAreaBounds.center, flightAreaBounds.size);
    }

    #region FixedUpdate Handlers
    private void HandleFlightPhysics()
    {
        ApplyThrust();
        ApplyFlightControls();
        CalculateGForces();
        UpdateAnimations();
    }

    private void HandleWeaponSystems()
    {
        UpdateTargetLock();

        if (currentState == AIState.Attack && IsTargetInFiringArc() && isTargetLocked && Time.time > gunCooldownEnd)
        {
            FireGuns();
        }
        else if (currentState == AIState.GroundStrike && Time.time > lastGroundAttackTime + groundAttackCooldown)
        {
            PerformGroundStrike();
        }
    }

    private void HandleTerrainInteraction()
    {
        ApplyTerrainFollowing();
        CheckForGroundTargets();
    }

    private void HandleSpecialEffects()
    {
        UpdateAudio();
        UpdateContrails();
    }

    private void HandleBoundaryConstraints()
    {
        ClampToGameArea();
    }
    #endregion

    #region Initialization
    private void InitializeAircraftModel()
    {
        GameObject modelPrefab = null;

        switch (jetModel)
        {
            case JetModel.F16: modelPrefab = f16Prefab; break;
            case JetModel.F22: modelPrefab = f22Prefab; break;
            case JetModel.F35: modelPrefab = f35Prefab; break;
            case JetModel.F15: modelPrefab = f15Prefab; break;
            case JetModel.JAS39: modelPrefab = jas39Prefab; break;
            case JetModel.Eurofighter: modelPrefab = eurofighterPrefab; break;
            case JetModel.Su27: modelPrefab = su27Prefab; break;
            case JetModel.Su35: modelPrefab = su35Prefab; break;
            case JetModel.Su57: modelPrefab = su57Prefab; break;
            case JetModel.Mig29: modelPrefab = mig29Prefab; break;
            case JetModel.Mig35: modelPrefab = mig35Prefab; break;
        }

        if (modelPrefab != null)
        {
            aircraftModel = Instantiate(modelPrefab, transform);
            aircraftModel.transform.localPosition = Vector3.zero;
            aircraftModel.transform.localRotation = Quaternion.identity;
        }
    }

    private void CalculateFlightAreaBounds()
    {
        if (allTerrains.Length == 0)
        {
            flightAreaBounds = new Bounds(Vector3.zero, new Vector3(10000, 5000, 10000));
            return;
        }

        Bounds combinedBounds = allTerrains[0].terrainData.bounds;
        combinedBounds.center += allTerrains[0].transform.position;

        for (int i = 1; i < allTerrains.Length; i++)
        {
            Bounds terrainBounds = allTerrains[i].terrainData.bounds;
            terrainBounds.center += allTerrains[i].transform.position;
            combinedBounds.Encapsulate(terrainBounds);
        }

        combinedBounds.Expand(new Vector3(0, maxAltitude * 2, 0));
        combinedBounds.center = new Vector3(combinedBounds.center.x, combinedBounds.center.y + maxAltitude / 2, combinedBounds.center.z);

        flightAreaBounds = combinedBounds;
    }

    private void GenerateGroundTargets()
    {
        groundTargets.Clear();
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(flightAreaBounds.min.x, flightAreaBounds.max.x),
                0,
                Random.Range(flightAreaBounds.min.z, flightAreaBounds.max.z)
            );

            randomPoint.y = GetTerrainHeightAtPosition(randomPoint);
            groundTargets.Add(randomPoint);
        }
    }
    #endregion

    #region Flight Controls
    private void ApplyThrust()
    {
        rb.AddForce(transform.forward * thrustForce * (afterburnerActive ? afterburnerMultiplier : 1f));

        if (rb.linearVelocity.magnitude > currentSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentSpeed;
        }
    }

    private void ApplyFlightControls()
    {
        if (isEvading && Time.time < evadeEndTime)
        {
            ApplyEvasionManeuver();
            return;
        }
        else if (isEvading)
        {
            isEvading = false;
            currentState = AIState.Regroup;
            stateChangeTime = Time.time;
        }

        switch (currentState)
        {
            case AIState.Patrol: PatrolBehavior(); break;
            case AIState.Engage: EngageBehavior(); break;
            case AIState.Attack: AttackBehavior(); break;
            case AIState.GroundStrike: GroundStrikeBehavior(); break;
            case AIState.Evade: EvadeBehavior(); break;
            case AIState.Regroup: RegroupBehavior(); break;
            case AIState.ReturnToBase: ReturnToBaseBehavior(); break;
        }

        ApplyBanking();
        LimitGForces();
    }

    private void ApplyBanking()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float bankAmount = -Mathf.Clamp(localVelocity.x * 0.5f, -60f, 60f);

        Quaternion targetBank = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, bankAmount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetBank, Time.fixedDeltaTime * 2f);
    }

    private void LimitGForces()
    {
        float gForceLimit = Mathf.Clamp(maxGForce - currentGForce, 0.5f, 1f);
        turnRate *= gForceLimit;
        climbRate *= gForceLimit;
    }
    #endregion

    #region Terrain Following
    private void ApplyTerrainFollowing()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, terrainPredictionDistance, terrainMask))
        {
            float requiredPitch = 0f;

            if (hit.distance < terrainPredictionDistance * 0.3f)
            {
                requiredPitch = 30f;
                currentSpeed = Mathf.Max(minSpeed, currentSpeed * 0.9f);
            }
            else if (hit.distance < terrainPredictionDistance * 0.7f)
            {
                requiredPitch = 15f;
            }

            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = Mathf.LerpAngle(euler.x, requiredPitch, Time.fixedDeltaTime * terrainAvoidanceForce);
            transform.rotation = Quaternion.Euler(euler);

            AdjustAltitudeToTerrain();
        }
        else
        {
            AdjustAltitudeToTerrain();
        }
    }

    private void AdjustAltitudeToTerrain()
    {
        // Get current terrain height (deforming terrain supported)
        float terrainHeight = GetTerrainHeightAtPosition(transform.position);
        float minAllowedY = terrainHeight + minTerrainAltitude;
        float idealAllowedY = terrainHeight + idealTerrainAltitude;
        float maxAllowedY = terrainHeight + maxTerrainAltitude;

        // Clamp position so jet never goes below minAllowedY
        if (transform.position.y < minAllowedY)
        {
            Vector3 pos = transform.position;
            pos.y = minAllowedY;
            transform.position = pos;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(0f, rb.linearVelocity.y), rb.linearVelocity.z);
        }

        // If above max terrain altitude, gently descend
        if (transform.position.y > maxAllowedY)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(transform.position.y, maxAllowedY, Time.fixedDeltaTime * 2f);
            transform.position = pos;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(0f, rb.linearVelocity.y), rb.linearVelocity.z);
        }

        // Smoothly adjust pitch to maintain ideal altitude above terrain
        float altitudeError = idealAllowedY - transform.position.y;
        float requiredPitch = Mathf.Clamp(altitudeError * 0.1f, -15f, 15f);
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = Mathf.LerpAngle(euler.x, requiredPitch, Time.fixedDeltaTime * stability);
        transform.rotation = Quaternion.Euler(euler);
    }


    private float GetTerrainHeightAtPosition(Vector3 position)
    {
        float height = 0f;

        foreach (Terrain terrain in allTerrains)
        {
            if (terrain.terrainData.bounds.Contains(position))
            {
                height = terrain.SampleHeight(position);
                break;
            }
        }

        return height;
    }
    #endregion

    #region AI Behaviors
    private IEnumerator AIBehavior()
    {
        while (true)
        {
            UpdateAIState();

            switch (currentState)
            {
                case AIState.Patrol: yield return StartCoroutine(PatrolRoutine()); break;
                case AIState.Engage: yield return StartCoroutine(EngageRoutine()); break;
                case AIState.Attack: yield return StartCoroutine(AttackRoutine()); break;
                case AIState.GroundStrike: yield return StartCoroutine(GroundStrikeRoutine()); break;
                case AIState.Evade: yield return StartCoroutine(EvadeRoutine()); break;
                case AIState.Regroup: yield return StartCoroutine(RegroupRoutine()); break;
                case AIState.ReturnToBase: yield return StartCoroutine(ReturnToBaseRoutine()); break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void UpdateAIState()
    {
        // If an enemy is detected, immediately switch to Engage or Attack
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (CanAttackTarget())
            {
                currentState = AIState.Attack;
                stateChangeTime = Time.time;
                return;
            }
            else
            {
                currentState = AIState.Engage;
                stateChangeTime = Time.time;
                return;
            }
        }
        // If ground targets are available and no air target, switch to GroundStrike
        else if (groundTargets.Count > 0)
        {
            currentState = AIState.GroundStrike;
            stateChangeTime = Time.time;
            return;
        }
        // Otherwise, patrol
        currentState = AIState.Patrol;
        stateChangeTime = Time.time;
    }

    private void PatrolBehavior()
    {
        // If a target is detected, immediately break out of patrol
        if (currentTarget != null)
        {
            UpdateAIState();
            return;
        }

        // Normal patrol movement
        FlyToPosition(currentWaypoint);

        if (Time.time > waypointChangeTime || Vector3.Distance(transform.position, currentWaypoint) < 500f)
        {
            GenerateNewWaypoint();
            waypointChangeTime = Time.time + Random.Range(10f, 20f);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, cruiseSpeed, Time.fixedDeltaTime * 0.5f);
    }


    private void EngageBehavior()
    {
        if (currentTarget != null)
        {
            Vector3 leadPosition = CalculateLeadPosition(currentTarget.transform.position, currentTarget.rb.linearVelocity);
            bool isPlayer = currentTarget.gameObject.CompareTag("Player");
            Vector3 offset = Vector3.zero;

            if (isPlayer)
            {
                Vector3 right = Vector3.Cross(Vector3.up, (leadPosition - transform.position).normalized);
                offset = right * Random.Range(-150f, 150f);
            }
            else
            {
                offset = -currentTarget.transform.forward * 100f;
            }

            Vector3 dogfightPosition = leadPosition + offset;
            FlyToPosition(dogfightPosition);

            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            afterburnerActive = dist > gunRange * 1.5f;
            currentSpeed = Mathf.Lerp(currentSpeed, afterburnerActive ? maxSpeed : cruiseSpeed, Time.fixedDeltaTime * 0.5f);

            if (dist < 200f)
            {
                currentState = AIState.Evade;
                evadeEndTime = Time.time + 2f;
                stateChangeTime = Time.time;
            }
        }
    }

    private void AttackBehavior()
    {
        if (currentTarget != null)
        {
            Vector3 attackOffset = -currentTarget.transform.forward * 150f + Vector3.up * 30f;
            Vector3 attackPosition = currentTarget.transform.position + attackOffset;

            bool isPlayer = currentTarget.gameObject.CompareTag("Player");
            if (isPlayer && Random.value < 0.1f)
            {
                Vector3 rollDir = Vector3.Cross(transform.forward, Vector3.up).normalized;
                FlyToPosition(transform.position + rollDir * 200f + Vector3.up * 50f);
            }
            else
            {
                FlyToPosition(attackPosition);
                FireGuns();
            }

            currentSpeed = Mathf.Lerp(currentSpeed, cruiseSpeed * 0.9f, Time.fixedDeltaTime * 0.5f);

            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (dist < 150f)
            {
                currentState = AIState.Evade;
                evadeEndTime = Time.time + 3f;
                stateChangeTime = Time.time;
            }
        }
    }

    private void GroundStrikeBehavior()
    {
        if (groundTargets.Count > 0)
        {
            // Find closest ground target
            Vector3 closestTarget = groundTargets.OrderBy(t => Vector3.Distance(transform.position, t)).First();
            FlyToPosition(closestTarget + Vector3.up * 200f); // Fly above target
            FireGuns();

            // Dive when close to target
            float dist = Vector3.Distance(transform.position, closestTarget);
            if (dist < groundStrikeRange)
            {
                Vector3 euler = transform.rotation.eulerAngles;
                euler.x = Mathf.LerpAngle(euler.x, 45f, Time.fixedDeltaTime * 2f); // Dive angle
                transform.rotation = Quaternion.Euler(euler);
            }

            currentSpeed = Mathf.Lerp(currentSpeed, cruiseSpeed * 0.8f, Time.fixedDeltaTime * 0.5f);
        }
    }

    private void EvadeBehavior()
    {
        ApplyEvasionManeuver();
        currentSpeed = Mathf.Lerp(currentSpeed, minSpeed, Time.fixedDeltaTime * 0.5f);
    }

    private void RegroupBehavior()
    {
        if (formationLeader != null && !isLeader)
        {
            Vector3 formationPos = formationLeader.transform.position +
                                 formationLeader.transform.TransformDirection(formationOffset);
            FlyToPosition(formationPos);
        }
        else
        {
            FlyToPosition(patrolCenter);
        }

        currentSpeed = Mathf.Lerp(currentSpeed, cruiseSpeed, Time.fixedDeltaTime * 0.5f);
    }

    private void ReturnToBaseBehavior()
    {
        FlyToPosition(patrolCenter);
        afterburnerActive = true;
        currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.fixedDeltaTime * 0.3f);
    }

    private void ApplyEvasionManeuver()
    {
        if (evadeDirection == Vector3.zero || Time.time % 2f < 0.1f)
        {
            evadeDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
        }

        Quaternion targetRotation = Quaternion.LookRotation(evadeDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * agility);

        Vector3 euler = transform.rotation.eulerAngles;
        euler.z += Time.fixedDeltaTime * rollRate * 2f;
        transform.rotation = Quaternion.Euler(euler);
    }

    private void FlyToPosition(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * agility);

        float altitudeDifference = targetPosition.y - transform.position.y;
        Vector3 euler = transform.rotation.eulerAngles;
        euler.x = Mathf.LerpAngle(euler.x, Mathf.Clamp(altitudeDifference * 0.1f, -15f, 15f), Time.fixedDeltaTime * stability);
        transform.rotation = Quaternion.Euler(euler);
    }

    private Vector3 CalculateLeadPosition(Vector3 targetPosition, Vector3 targetVelocity)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        float timeToIntercept = distance / currentSpeed;
        return targetPosition + targetVelocity * timeToIntercept;
    }

    private bool CanAttackTarget()
    {
        if (currentTarget == null) return false;
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float angle = Vector3.Angle(transform.forward, currentTarget.transform.position - transform.position);
        return distance < gunRange && angle < 30f;
    }

    private bool IsTargetInFiringArc()
    {
        if (currentTarget == null) return false;
        float angle = Vector3.Angle(transform.forward, currentTarget.transform.position - transform.position);
        return angle < 5f;
    }

    private bool IsInFormation()
    {
        if (formationLeader == null) return true;
        Vector3 expectedPosition = formationLeader.transform.position +
                                 formationLeader.transform.TransformDirection(formationOffset);
        return Vector3.Distance(transform.position, expectedPosition) < 100f;
    }

    private void GenerateNewWaypoint()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(flightAreaBounds.min.x, flightAreaBounds.max.x),
            Random.Range(flightAreaBounds.center.y - flightAreaBounds.extents.y / 2, flightAreaBounds.center.y + flightAreaBounds.extents.y / 2),
            Random.Range(flightAreaBounds.min.z, flightAreaBounds.max.z)
        );

        float terrainHeight = GetTerrainHeightAtPosition(randomPoint);
        randomPoint.y = Mathf.Max(randomPoint.y, terrainHeight + idealTerrainAltitude);
        currentWaypoint = randomPoint;
    }
    #endregion

    #region Combat
    private void FireGuns()
    {
        // Null checks for currentTarget, bulletPrefab, gunTransform
        if (currentTarget == null || bulletPrefab == null || gunTransform == null)
            return;

        if (Time.time < lastGunFireTime + 1f / gunFireRate || burstCount >= gunBurstCount)
            return;

        Vector3 spread = new Vector3(
            Random.Range(-gunSpread, gunSpread),
            Random.Range(-gunSpread, gunSpread),
            0
        );

        Vector3 fireDirection = (currentTarget.transform.position - gunTransform.position).normalized + spread;

        GameObject bullet = Instantiate(bulletPrefab, gunTransform.position, Quaternion.LookRotation(fireDirection));
        if (bullet != null)
        {
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = fireDirection * currentSpeed * 1.5f;
            }

            bullet.tag = team == Team.Red ? "RedBullet" : "BlueBullet";
        }

        if (gunMuzzleFlash != null && gunTransform != null)
        {
            GameObject flash = Instantiate(gunMuzzleFlash, gunTransform.position, gunTransform.rotation);
            if (flash != null)
            {
                flash.transform.parent = gunTransform;
                Destroy(flash, 0.1f);
            }
        }

        if (gunSound != null)
        {
            AudioSource.PlayClipAtPoint(gunSound, gunTransform.position, 0.5f);
        }

        lastGunFireTime = Time.time;
        burstCount++;

        if (burstCount >= gunBurstCount)
        {
            gunCooldownEnd = Time.time + gunCooldown;
            burstCount = 0;
            isTargetLocked = false;
            targetLockProgress = 0f;
        }
    }

    private void PerformGroundStrike()
    {
        if (groundTargets.Count > 0 && blastBulletPrefab != null)
        {
            Vector3 target = groundTargets[0];

            // Launch blast bullet
            GameObject blastBullet = Instantiate(blastBulletPrefab, transform.position, Quaternion.identity);
            BlastProjectile projectile = blastBullet.GetComponent<BlastProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(target, blastDamage, blastRadius, team);
            }

            // Play missile sound
            if (missileLaunchSound != null)
            {
                AudioSource.PlayClipAtPoint(missileLaunchSound, transform.position, 0.7f);
            }

            // Remove target after attack
            groundTargets.RemoveAt(0);
            lastGroundAttackTime = Time.time;

            // Random terrain blast
            if (Random.value < 0.3f)
            {
                Vector3 randomBlastPos = new Vector3(
                    Random.Range(flightAreaBounds.min.x, flightAreaBounds.max.x),
                    0,
                    Random.Range(flightAreaBounds.min.z, flightAreaBounds.max.z)
                );
                randomBlastPos.y = GetTerrainHeightAtPosition(randomBlastPos);
                CreateExplosion(randomBlastPos);
            }
        }
    }

    private void CheckForGroundTargets()
    {
        // Random chance to create new ground targets
        if (Random.value < 0.01f && groundTargets.Count < 5)
        {
            Vector3 newTarget = new Vector3(
                Random.Range(flightAreaBounds.min.x, flightAreaBounds.max.x),
                0,
                Random.Range(flightAreaBounds.min.z, flightAreaBounds.max.z)
            );
            newTarget.y = GetTerrainHeightAtPosition(newTarget);
            groundTargets.Add(newTarget);
        }
    }

    private void UpdateTargetLock()
    {
        if (currentTarget != null && IsTargetInFiringArc())
        {
            targetLockProgress += Time.fixedDeltaTime / lockOnTime;

            if (targetLockProgress >= 1f && !isTargetLocked)
            {
                isTargetLocked = true;
                if (lockOnSound != null)
                {
                    AudioSource.PlayClipAtPoint(lockOnSound, transform.position, 0.7f);
                }
            }
        }
        else
        {
            targetLockProgress = Mathf.Max(0f, targetLockProgress - Time.fixedDeltaTime / lockOnTime);
            if (targetLockProgress < 0.5f)
            {
                isTargetLocked = false;
            }
        }
    }

    private void Explode()
    {
        CreateExplosion(transform.position);
        Destroy(gameObject);
    }

    private void CreateExplosion(Vector3 position)
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, position, 1f);
        }

        // Damage nearby objects
        Collider[] colliders = Physics.OverlapSphere(position, blastRadius);
        foreach (Collider hit in colliders)
        {
            AdvancedFighterJet jet = hit.GetComponent<AdvancedFighterJet>();
            if (jet != null && jet.team != team)
            {
                jet.heath -= blastDamage * (1f - Vector3.Distance(position, hit.transform.position) / blastRadius);
            }
        }
    }
    #endregion

    #region Detection
    private IEnumerator UpdateDetections()
    {
        while (true)
        {
            detectedTargets.Clear();

            AdvancedFighterJet[] allJets = FindObjectsOfType<AdvancedFighterJet>();
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                AdvancedFighterJet playerJet = playerObj.GetComponent<AdvancedFighterJet>();
                if (playerJet != null && playerJet.team != team)
                {
                    float distance = Vector3.Distance(transform.position, playerObj.transform.position);
                    if (distance <= radarRange)
                    {
                        float angle = Vector3.Angle(transform.forward, playerObj.transform.position - transform.position);
                        if (angle <= radarFOV / 2f)
                        {
                            detectedTargets.Add(playerJet);
                        }
                    }
                }
            }

            foreach (AdvancedFighterJet jet in allJets)
            {
                if (jet == this || jet.team == team) continue;

                float distance = Vector3.Distance(transform.position, jet.transform.position);
                if (distance > radarRange) continue;

                float angle = Vector3.Angle(transform.forward, jet.transform.position - transform.position);
                if (angle > radarFOV / 2f) continue;

                detectedTargets.Add(jet);

                if (radarPingSound != null && radarAudioSource != null)
                {
                    radarAudioSource.pitch = 1f + (1f - distance / radarRange);
                    radarAudioSource.PlayOneShot(radarPingSound, 0.2f);
                }
            }

            if (detectedTargets.Count > 0)
            {
                currentTarget = detectedTargets
                    .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
                    .First();
            }
            else
            {
                currentTarget = null;
                isTargetLocked = false;
                targetLockProgress = 0f;
            }

            yield return new WaitForSeconds(detectionUpdateInterval);
        }
    }
    #endregion

    #region Polish Effects
    private void CalculateGForces()
    {
        Vector3 acceleration = rb.linearVelocity - (rb.linearVelocity - rb.angularVelocity);
        currentGForce = acceleration.magnitude / Physics.gravity.magnitude;
        gForceSmooth = Mathf.Lerp(gForceSmooth, currentGForce, Time.fixedDeltaTime * 2f);
    }

    private void UpdateContrails()
    {
        // Check for destroyed or missing TrailRenderer
        if (contrailEffect == null || !contrailEffect || contrailEffect.gameObject == null)
            return;

        if (gForceSmooth > 3f || transform.position.y > 3000f)
        {
            contrailTime += Time.fixedDeltaTime;
            if (contrailTime > 1f && !contrailEffect.emitting)
            {
                contrailEffect.emitting = true;
            }
        }
        else
        {
            contrailTime = Mathf.Max(0f, contrailTime - Time.fixedDeltaTime);
            if (contrailTime == 0f && contrailEffect.emitting)
            {
                contrailEffect.emitting = false;
            }
        }
    }



    private void UpdateAnimations()
    {
        if (aircraftModel != null)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            flapPosition = Mathf.Lerp(flapPosition, Mathf.Clamp(localVelocity.x * 0.1f, -1f, 1f), Time.fixedDeltaTime * 2f);
            rudderPosition = Mathf.Lerp(rudderPosition, Mathf.Clamp(localVelocity.y * 0.1f, -1f, 1f), Time.fixedDeltaTime * 2f);
        }
    }

    private void UpdateAudio()
    {
        if (audioSource != null && engineSound != null)
        {
            audioSource.pitch = 0.8f + (currentSpeed / maxSpeed) * 0.4f;
            audioSource.volume = 0.5f + (currentSpeed / maxSpeed) * 0.5f;
        }

        if (afterburnerEffect != null)
        {
            AudioSource afterburnerAudio = afterburnerEffect.GetComponent<AudioSource>();
            if (afterburnerAudio != null)
            {
                afterburnerAudio.volume = afterburnerActive ? 1f : 0f;
                afterburnerAudio.pitch = afterburnerActive ? 1f : 0.5f;
            }
        }
    }

    private void ClampToGameArea()
    {
        if (!flightAreaBounds.Contains(transform.position))
        {
            Vector3 directionToCenter = (flightAreaBounds.center - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 0.5f);
            afterburnerActive = true;
        }
    }
    #endregion

    #region Formation & Teamwork
    public void SetFormationLeader(AdvancedFighterJet leader, Vector3 offset)
    {
        formationLeader = leader;
        formationOffset = offset;
        isLeader = (leader == this);

        if (!isLeader && OnWingmanAdded != null)
        {
            OnWingmanAdded(this);
        }
    }

    public void AddWingman(AdvancedFighterJet wingman, Vector3 offset)
    {
        if (!wingmen.Contains(wingman))
        {
            wingmen.Add(wingman);
            wingman.SetFormationLeader(this, offset);
        }
    }

    public void EngageTarget(AdvancedFighterJet target)
    {
        if (isLeader)
        {
            currentTarget = target;
            currentState = AIState.Engage;
            stateChangeTime = Time.time;

            foreach (AdvancedFighterJet wingman in wingmen)
            {
                wingman.currentTarget = target;
                wingman.currentState = AIState.Engage;
                wingman.stateChangeTime = Time.time;
            }
        }
    }
    #endregion

    #region Coroutines for AI States
    private IEnumerator PatrolRoutine()
    {
        while (currentState == AIState.Patrol)
        {
            if (currentTarget != null)
            {
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator EngageRoutine()
    {
        while (currentState == AIState.Engage)
        {
            if (currentTarget == null)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (currentState == AIState.Attack)
        {
            if (currentTarget == null || !CanAttackTarget())
            {
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator GroundStrikeRoutine()
    {
        while (currentState == AIState.GroundStrike)
        {
            if (groundTargets.Count == 0)
            {
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator EvadeRoutine()
    {
        isEvading = true;
        evadeEndTime = Time.time + 3f;

        while (currentState == AIState.Evade && Time.time < evadeEndTime)
        {
            yield return new WaitForSeconds(0.1f);
        }

        isEvading = false;
    }

    private IEnumerator RegroupRoutine()
    {
        while (currentState == AIState.Regroup)
        {
            if (IsInFormation())
            {
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator ReturnToBaseRoutine()
    {
        while (currentState == AIState.ReturnToBase)
        {
            if (Vector3.Distance(transform.position, patrolCenter) < patrolRadius * 0.3f)
            {
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
    }
    #endregion
}

