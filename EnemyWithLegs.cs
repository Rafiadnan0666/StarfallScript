using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody), typeof(AudioSource))]
public class StarfallEnemy : MonoBehaviour
{
    // ───────────── ENUMS ─────────────
    public enum EnemyRole { Predator, Scavenger, Grazer, Swarmer, Boss }
    public enum AIState { Idle, Patrol, Chase, Flee, Attack, Feeding, Special, Investigating, Flank, TakeCover, Charge, WindUp, Recover }
    public enum EnemyMorphology { Blob, Spider, Bipedal, Quadruped, Centipede, Worm, Tentacle, Hybrid, Avian, Amphibious, Crystalline }
    public enum AttackStyle { None, Spit, Charge, RetreatAndShoot, DrillSpin, Lash, Bite, Beam, Swarm, Sonic, Poison, CrystalSpike }

    // ───────────── TUNING CONSTANTS ─────────────
    private const float PATROL_SPEED_MULTIPLIER = 0.4f;
    private const float ATTACK_APPROACH_SPEED_MULTIPLIER = 0.8f;
    private const float ATTACK_CIRCLE_SPEED_MULTIPLIER = 0.6f;
    private const float CHARGE_SPEED_MULTIPLIER = 1.5f;
    private const float INVESTIGATE_SPEED_MULTIPLIER = 0.6f;
    private const float OBSTACLE_AVOID_DISTANCE = 2f;
    private const float TARGET_PREDICTION_FACTOR = 0.3f;
    private const float MIN_ATTACK_DISTANCE_FACTOR = 0.8f;
    private const float IDEAL_ATTACK_DISTANCE_FACTOR = 0.5f;
    private const float CIRCLE_FORWARD_COMPONENT_MAX = 0.3f;
    private const float OBSTACLE_AVOID_LERP = 0.7f;
    private const float DECELERATION_BRAKING_FACTOR = 0.5f;
    private const float GROUND_ALIGN_MIN_SLOPE = 5f;
    private const float LEG_MOVE_THRESHOLD_BONUS = 0.5f;
    private const float LEG_ADJUST_SPEED = 2f;
    private const float TENTACLE_PHASE_MULTIPLIER = 0.3f;
    private const float TENTACLE_ROTATION_SPEED = 2f;
    private const float TENTACLE_MOVE_SPEED = 3f;
    private const float STATE_PERSISTENCE_MIN = 0.5f;
    private const float STATE_PERSISTENCE_MAX = 1.5f;
    private const float COVER_FIND_ATTEMPTS = 10f;
    private const float COVER_FIND_RADIUS = 10f;
    private const float SWARM_CHECK_RADIUS = 10f;
    private const float HEARING_RANGE_FACTOR = 0.4f;
    private const float FORCE_FIELD_PULSE_SPEED = 2f;
    private const float FORCE_FIELD_PULSE_AMPLITUDE = 0.1f;
    private const float FORCE_FIELD_PULSE_BASE = 1f;

    // ───────────── INSPECTOR FIELDS ─────────────
    [Header("Core Configuration")]
    public EnemyRole role = EnemyRole.Predator;
    public EnemyMorphology morphology = EnemyMorphology.Spider;
    public AttackStyle attack = AttackStyle.None;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float maxMoveSpeed = 3f;
    public float acceleration = 8f;
    public float deceleration = 10f;
    public float turnSpeed = 540f;
    public float decisionInterval = 1.5f;
    public float scanRadius = 15f;
    public float forceFieldRadius = 2f;
    public float forceFieldStrength = 5f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public LayerMask groundMask;
    public LayerMask deformationMask;
    public float memoryDuration = 10f;
    public float attackRange = 8f;
    public float attackWindUpTime = 0.5f;
    public float attackRecoveryTime = 0.5f;

    [Header("Morphology")]
    public int minLegs = 4, maxLegs = 8;
    public int minBodySegments = 3, maxBodySegments = 10;
    public int minTentacles = 4, maxTentacles = 12;
    public float minSize = 0.8f, maxSize = 2f;
    public float legSpacing = 0.9f;
    public float segmentSpacing = 0.7f;
    public float wiggleIntensity = 0.3f;

    [Header("Leg Motion")]
    public float stepSpeed = 5f;
    public float stepHeight = 0.3f;
    public float legSwingRadius = 0.3f;
    public float maxLegReach = 1.2f;
    public float legLiftMultiplier = 1.5f;
    public float stepPredictionDistance = 0.5f;
    public float legSynchronization = 0.7f;
    public float legUpdateSpread = 0.2f;

    [Header("Tentacle Motion")]
    public float tentacleWaveSpeed = 2f;
    public float tentacleWaveAmplitude = 0.5f;
    public float tentacleReachDistance = 3f;
    public float tentacleGrabSpeed = 4f;

    [Header("Combat")]
    public GameObject spitProjectile;
    public GameObject beamEffect;
    public Transform firePoint;
    public float spitForce = 20f;
    public float attackCooldown = 2.5f;
    public float chargeForce = 35f;
    public float biteDamage = 25f;
    public float biteRange = 2f;
    public float beamDamage = 10f;
    public float beamDuration = 3f;
    public float terrainDeformationStrength = 0.2f;
    public float terrainDeformationRadius = 1f;

    [Header("Materials & Effects")]
    public Material baseMaterial;
    public Color[] possibleColors;
    public GameObject deathEffect;
    public GameObject damageEffect;
    public GameObject forceFieldEffect;
    public GameObject terrainDeformationEffect;

    [Header("Corpse")]
    public float corpseLifetime = 20f;
    public bool leaveCorpse = true;
    public GameObject[] organPrefabs;

    [Header("Audio")]
    public AudioClip[] movementSounds;
    public AudioClip[] attackSounds;
    public AudioClip[] damageSounds;
    public AudioClip[] deathSounds;
    public AudioClip[] vocalSounds;
    public float minSoundDelay = 2f;
    public float maxSoundDelay = 8f;
    public float footstepSyncThreshold = 0.3f;

    // ───────────── PRIVATE FIELDS ─────────────
    private Rigidbody rb;
    private AudioSource audioSource;
    private Transform target;
    private float decisionTimer;
    private bool isDead;
    private float attackTimer;
    private float scale;
    private Vector3 desiredVelocity;
    private Vector3 currentVelocity;
    private float bodyWaveTimer;
    private Vector3 lastGroundNormal = Vector3.up;
    private float groundCheckDistance = 1.5f;
    private float groundAlignmentSpeed = 5f;
    private GameObject forceFieldInstance;
    private bool isBeaming;
    private float beamTimer;
    private LineRenderer beamRenderer;
    private List<GameObject> organs = new List<GameObject>();
    private bool organsExposed = false;
    private float soundTimer;
    private float nextSoundTime;
    private Vector3 lastKnownTargetPosition;
    private float targetMemoryTimer;
    private bool hasTargetMemory;
    private float idleTimer;
    private Vector3 patrolDestination;
    private bool hasPatrolDestination;
    private AIState state = AIState.Idle;
    private float statePersistanceTimer;
    private float windUpTimer;
    private float recoveryTimer;
    private float lastFootstepTime;
    private float footstepInterval;

    // Body parts
    private GameObject mainBody;
    private List<Limb> limbs = new List<Limb>();
    private List<BodySegment> bodySegments = new List<BodySegment>();
    private List<Tentacle> tentacles = new List<Tentacle>();

    // Optimization
    private Collider[] scanResults = new Collider[32];
    private RaycastHit groundHit;
    private Vector3 lastPosition;
    private float sqrScanRadius;
    private float sqrAttackRange;
    private int legUpdateIndex;

    // Animation parameters
    private float bodyBobAmount = 0.05f;
    private float bodyBobSpeed = 2f;
    private float bodyBobOffset;
    private float bodyTiltAmount = 5f;
    private float bodySwayAmount = 3f;

    // Structures
    [System.Serializable]
    public class Limb
    {
        public Transform upperLeg;
        public Transform lowerLeg;
        public Transform foot;
        public Vector3 restPosition;
        public Vector3 basePosition;
        public Vector3 tipPosition;
        public float phaseOffset;
        public bool isFrontLeg;
        public int orderIndex;
        public float reachDistance;
        public bool isDamaged;
        public GameObject limbObject;
        public Collider limbCollider;
        public Vector3 targetPosition;
        public Vector3 startPosition;
        public float timer;
        public bool isMoving;
        public float liftProgress;
        public Vector3 worldRestPosition;
        public float gaitCycleOffset;
    }

    [System.Serializable]
    public class BodySegment
    {
        public Transform transform;
        public Vector3 baseLocalPosition;
        public Quaternion baseLocalRotation;
        public List<Limb> segmentLimbs = new List<Limb>();
        public float waveOffset;
        public GameObject organ;
        public GameObject segmentObject;
        public Collider segmentCollider;
    }

    [System.Serializable]
    public class Tentacle
    {
        public List<Transform> segments = new List<Transform>();
        public List<Vector3> basePositions = new List<Vector3>();
        public List<Quaternion> baseRotations = new List<Quaternion>();
        public float flexibility;
        public float waveOffset;
        public bool isGrabbing;
        public Transform grabTarget;
        public float grabProgress;
        public GameObject tentacleObject;
        public Collider tentacleCollider;
    }

    // ───────────── POOLING ─────────────
    private static Queue<GameObject> effectPool = new Queue<GameObject>();
    private static int effectPoolSize = 20;

    [System.Serializable]
    public class MorphologyData
    {
        public int minLegs, maxLegs;
        public int minBodySegments, maxBodySegments;
        public int minTentacles, maxTentacles;
        public float minSize, maxSize;
        public float legSpacing;
        public float segmentSpacing;
        public float wiggleIntensity;
    }

    private MorphologyData morphologyData;

    private bool isRegistered = false;

    public GameObject swarmManagerPrefab;

    void Start()
    {
        InitializeComponents();
        InitializeEnemy();
        InitializeForceField();
        InitializeAudio();
        InitializeSwarmManager();
        PrewarmEffectPool();
    }
    void PrewarmEffectPool()
    {
        if (damageEffect != null)
        {
            for (int i = 0; i < effectPoolSize; i++)
            {
                GameObject effect = Instantiate(damageEffect);
                effect.SetActive(false);
                effectPool.Enqueue(effect);
            }
        }
    }
    void InitializeAudio()
    {
        nextSoundTime = Random.Range(minSoundDelay, maxSoundDelay);
    }
    void InitializeSwarmManager()
    {
        if (role == EnemyRole.Swarmer)
        {
            if (SwarmManager.instance == null)
            {
                Debug.LogWarning("SwarmManager instance is missing! Creating default one.");
                GameObject managerObj = new GameObject("SwarmManager");
                managerObj.AddComponent<SwarmManager>();
            }
            SwarmManager.instance.RegisterSwarmer(this);
        }
    }
    void InitializeForceField()
    {
        if (forceFieldEffect != null && Random.value > 0.7f)
        {
            forceFieldInstance = Instantiate(forceFieldEffect, transform);
            forceFieldInstance.transform.localScale = Vector3.one * forceFieldRadius * 2f;
        }
    }
    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 25f;
    }

    void OnDestroy()
    {
        if (role == EnemyRole.Swarmer && SwarmManager.instance != null)
        {
            SwarmManager.instance.UnregisterSwarmer(this);
        }

        foreach (GameObject effect in effectPool)
        {
            if (effect != null) Destroy(effect);
        }
        effectPool.Clear();
    }
    public interface IDamageable
    {
        void TakeDamage(float damage);
        float GetHealth();
    }



    void RandomizeConfiguration()
    {
        // Randomize role with weighted probabilities
        float roleRand = Random.value;
        if (roleRand < 0.4f) role = EnemyRole.Predator;
        else if (roleRand < 0.65f) role = EnemyRole.Scavenger;
        else if (roleRand < 0.8f) role = EnemyRole.Grazer;
        else if (roleRand < 0.95f) role = EnemyRole.Swarmer;
        else role = EnemyRole.Boss;

        // Randomize morphology
        morphology = (EnemyMorphology)Random.Range(0, System.Enum.GetValues(typeof(EnemyMorphology)).Length);

        // Set morphology data
        morphologyData = new MorphologyData();
        switch (morphology)
        {
            case EnemyMorphology.Spider:
                morphologyData.minLegs = 4;
                morphologyData.maxLegs = 8;
                break;
            case EnemyMorphology.Bipedal:
                morphologyData.minLegs = 2;
                morphologyData.maxLegs = 2;
                break;
            case EnemyMorphology.Quadruped:
                morphologyData.minLegs = 4;
                morphologyData.maxLegs = 4;
                break;
            case EnemyMorphology.Centipede:
                morphologyData.minBodySegments = 5;
                morphologyData.maxBodySegments = 15;
                morphologyData.minLegs = 10;
                morphologyData.maxLegs = 30;
                break;
            case EnemyMorphology.Worm:
                morphologyData.minBodySegments = 5;
                morphologyData.maxBodySegments = 15;
                break;
            case EnemyMorphology.Tentacle:
                morphologyData.minTentacles = 4;
                morphologyData.maxTentacles = 12;
                break;
        }

        // Randomize attack style based on role
        switch (role)
        {
            case EnemyRole.Predator:
                attack = (AttackStyle)Random.Range(1, 5);
                break;
            case EnemyRole.Scavenger:
                attack = Random.value > 0.7f ? AttackStyle.Spit : AttackStyle.None;
                break;
            case EnemyRole.Grazer:
                attack = AttackStyle.None;
                break;
            case EnemyRole.Swarmer:
                attack = AttackStyle.Swarm;
                break;
            case EnemyRole.Boss:
                attack = (AttackStyle)Random.Range(5, System.Enum.GetValues(typeof(AttackStyle)).Length);
                break;
        }

        // Randomize stats based on role and morphology
        health = Random.Range(80f, 150f) * (role == EnemyRole.Boss ? 3f : 1f);
        maxMoveSpeed = Random.Range(2f, 6f);
        acceleration = Random.Range(6f, 12f);
        turnSpeed = Random.Range(360f, 720f);
        attackRange = Random.Range(5f, 12f);

        // Scale stats based on morphology
        if (morphology == EnemyMorphology.Blob || morphology == EnemyMorphology.Worm)
        {
            maxMoveSpeed *= 0.7f;
            acceleration *= 0.8f;
        }
        else if (morphology == EnemyMorphology.Avian)
        {
            maxMoveSpeed *= 1.3f;
            acceleration *= 1.2f;
        }
        else if (morphology == EnemyMorphology.Crystalline)
        {
            health *= 1.5f;
            maxMoveSpeed *= 0.6f;
        }
    }

    void InitializeEnemy()
    {
        RandomizeConfiguration();
        scale = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * scale;

        GenerateMorphology();

        decisionTimer = Random.Range(0, decisionInterval);
        attackTimer = attackCooldown;
        idleTimer = Random.Range(2f, 5f);

        sqrScanRadius = scanRadius * scanRadius;
        sqrAttackRange = attackRange * attackRange;
        lastPosition = transform.position;

        // Calculate footstep interval based on speed
        footstepInterval = 1f / (maxMoveSpeed * 0.8f);
    }

    void GenerateMorphology()
    {
        CreateMainBody();

        switch (morphology)
        {
            case EnemyMorphology.Spider: GenerateSpiderMorphology(); break;
            case EnemyMorphology.Bipedal: GenerateBipedalMorphology(); break;
            case EnemyMorphology.Quadruped: GenerateQuadrupedMorphology(); break;
            case EnemyMorphology.Centipede: GenerateCentipedeMorphology(); break;
            case EnemyMorphology.Worm: GenerateWormMorphology(); break;
            case EnemyMorphology.Tentacle: GenerateTentacleMorphology(); break;
            case EnemyMorphology.Hybrid: GenerateHybridMorphology(); break;
            case EnemyMorphology.Avian: GenerateAvianMorphology(); break;
            case EnemyMorphology.Amphibious: GenerateAmphibiousMorphology(); break;
            case EnemyMorphology.Crystalline: GenerateCrystallineMorphology(); break;
            case EnemyMorphology.Blob: GenerateBlobMorphology(); break;
        }
    }


    void CreateMainBody()
    {
        mainBody = new GameObject("MainBody");
        mainBody.transform.SetParent(transform);
        mainBody.transform.localPosition = Vector3.zero;

        Mesh bodyMesh = CreateProceduralMesh(1f);
        mainBody.AddComponent<MeshFilter>().mesh = bodyMesh;

        Renderer bodyRenderer = mainBody.AddComponent<MeshRenderer>();
        if (baseMaterial && possibleColors.Length > 0)
        {
            Material enemyMat = new Material(baseMaterial);
            enemyMat.color = possibleColors[Random.Range(0, possibleColors.Length)];
            enemyMat.mainTexture = GenerateProceduralTexture();

            if (morphology == EnemyMorphology.Crystalline)
            {
                enemyMat.SetFloat("_Metallic", Random.Range(0.7f, 1f));
                enemyMat.SetFloat("_Glossiness", Random.Range(0.8f, 1f));
            }

            bodyRenderer.material = enemyMat;
        }

        MeshCollider bodyCollider = mainBody.AddComponent<MeshCollider>();
        bodyCollider.convex = true;

        Rigidbody bodyRb = mainBody.AddComponent<Rigidbody>();
        bodyRb.mass = 5f * scale;
        bodyRb.linearDamping = 1f;
        bodyRb.angularDamping = 1f;

        ConfigurableJoint joint = mainBody.AddComponent<ConfigurableJoint>();
        joint.connectedBody = rb;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        JointDrive angularXDrive = new JointDrive();
        angularXDrive.positionSpring = 100f;
        angularXDrive.positionDamper = 5f;
        angularXDrive.maximumForce = 50f;

        joint.angularXDrive = angularXDrive;
        joint.angularYZDrive = angularXDrive;

        SoftJointLimit linearLimit = new SoftJointLimit();
        linearLimit.limit = 0.1f;
        joint.linearLimit = linearLimit;

        SoftJointLimit angularLimit = new SoftJointLimit();
        angularLimit.limit = 5f;
        joint.angularZLimit = angularLimit;
        joint.lowAngularXLimit = angularLimit;
        joint.highAngularXLimit = angularLimit;
    }

    Mesh CreateProceduralMesh(float radius)
    {
        // Create a more structured mesh instead of random triangles
        Mesh mesh = new Mesh();

        // Implementation for a proper sphere or capsule mesh
        // This is a simplified version - you'd want to use proper mesh generation
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Create a basic icosphere or similar structured mesh
        int subdivisions = 1;
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        vertices.Add(new Vector3(-1, t, 0).normalized * radius);
        vertices.Add(new Vector3(1, t, 0).normalized * radius);
        vertices.Add(new Vector3(-1, -t, 0).normalized * radius);
        vertices.Add(new Vector3(1, -t, 0).normalized * radius);

        vertices.Add(new Vector3(0, -1, t).normalized * radius);
        vertices.Add(new Vector3(0, 1, t).normalized * radius);
        vertices.Add(new Vector3(0, -1, -t).normalized * radius);
        vertices.Add(new Vector3(0, 1, -t).normalized * radius);

        vertices.Add(new Vector3(t, 0, -1).normalized * radius);
        vertices.Add(new Vector3(t, 0, 1).normalized * radius);
        vertices.Add(new Vector3(-t, 0, -1).normalized * radius);
        vertices.Add(new Vector3(-t, 0, 1).normalized * radius);

        // Create faces (triangles) for the icosahedron
        triangles.AddRange(new int[] { 0, 11, 5 });
        triangles.AddRange(new int[] { 0, 5, 1 });
        triangles.AddRange(new int[] { 0, 1, 7 });
        // Add more faces...

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }


    void GenerateSpiderMorphology()
    {
        int legCount = Random.Range(morphologyData.minLegs, morphologyData.maxLegs + 1);
        if (legCount % 2 != 0) legCount++; // Ensure even number of legs

        GenerateLegs(legCount);

        // Add abdomen for spider-like appearance
        CreateAbdomen(0.7f);
    }

    void GenerateLegs(int legCount)
    {
        for (int i = 0; i < legCount; i++)
        {
            float angle = (i / (float)legCount) * 360f;
            float elevation = Mathf.PI / 4f;

            Vector3 localPos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * Mathf.Cos(elevation),
                Mathf.Sin(elevation),
                Mathf.Sin(angle * Mathf.Deg2Rad) * Mathf.Cos(elevation)
            ) * scale;

            bool isFront = i < legCount / 2;

            GameObject legObj = new GameObject("Leg_" + i);
            legObj.transform.SetParent(transform);
            legObj.transform.localPosition = localPos;

            CapsuleCollider legCollider = legObj.AddComponent<CapsuleCollider>();
            legCollider.radius = 0.1f * scale;
            legCollider.height = maxLegReach * scale;
            legCollider.direction = 1;

            Limb limb = new Limb();
            limb.limbObject = legObj;
            limb.limbCollider = legCollider;
            limb.restPosition = localPos;
            limb.worldRestPosition = transform.TransformPoint(localPos);
            limb.basePosition = transform.TransformPoint(localPos);
            limb.tipPosition = limb.basePosition - transform.up * (maxLegReach * scale);
            limb.phaseOffset = (i / (float)legCount) * Mathf.PI * 2f;
            limb.gaitCycleOffset = (i / (float)legCount) * Mathf.PI * 2f; // For gait synchronization
            limb.isFrontLeg = isFront;
            limb.orderIndex = i;
            limb.reachDistance = maxLegReach * scale;
            limb.isDamaged = false;
            limb.timer = limb.phaseOffset;
            limb.isMoving = false;
            limb.liftProgress = 0f;

            if (Physics.Raycast(limb.basePosition, -transform.up, out groundHit,
                groundCheckDistance * scale, groundMask))
            {
                limb.targetPosition = groundHit.point;
                limb.startPosition = groundHit.point;
            }
            else
            {
                limb.targetPosition = limb.tipPosition;
                limb.startPosition = limb.tipPosition;
            }

            limbs.Add(limb);
            CreateProceduralLeg(limb);
        }
    }

    void CreateAbdomen(float sizeMultiplier)
    {
        GameObject abdomen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        abdomen.name = "Abdomen";
        abdomen.transform.SetParent(mainBody.transform);

        // Position at the rear of the body
        abdomen.transform.localPosition = new Vector3(0, 0, -0.8f * scale);
        abdomen.transform.localScale = Vector3.one * scale * sizeMultiplier;

        // Randomize shape slightly
        abdomen.transform.localScale = new Vector3(
            abdomen.transform.localScale.x * Random.Range(0.9f, 1.3f),
            abdomen.transform.localScale.y * Random.Range(0.9f, 1.1f),
            abdomen.transform.localScale.z * Random.Range(1.1f, 1.5f)
        );

        // Match material
        Renderer abdomenRenderer = abdomen.GetComponent<Renderer>();
        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (abdomenRenderer && bodyRenderer)
        {
            abdomenRenderer.material = bodyRenderer.material;
        }
    }

    void CreateProceduralLeg(Limb limb)
    {
        // Randomize leg segment proportions
        float upperLength = Random.Range(0.4f, 0.6f);
        float lowerLength = Random.Range(0.3f, 0.5f);
        float footSize = Random.Range(0.1f, 0.2f);

        // Create upper leg
        GameObject upperLeg = new GameObject("UpperLeg");
        upperLeg.transform.SetParent(limb.limbObject.transform);
        upperLeg.transform.localPosition = Vector3.zero;
        upperLeg.AddComponent<MeshFilter>().mesh = CreateProceduralMesh(upperLength, 0.15f * scale);
        upperLeg.AddComponent<MeshRenderer>();

        // Create lower leg
        GameObject lowerLeg = new GameObject("LowerLeg");
        lowerLeg.transform.SetParent(limb.limbObject.transform);
        lowerLeg.transform.localPosition = new Vector3(0, -upperLength, 0) * scale;
        lowerLeg.AddComponent<MeshFilter>().mesh = CreateProceduralMesh(lowerLength, 0.1f * scale);
        lowerLeg.AddComponent<MeshRenderer>();

        // Create foot
        GameObject foot = new GameObject("Foot");
        foot.transform.SetParent(limb.limbObject.transform);
        foot.transform.localPosition = new Vector3(0, -upperLength - lowerLength, 0) * scale;
        foot.AddComponent<MeshFilter>().mesh = CreateProceduralMesh(footSize, 0.15f * scale);
        foot.AddComponent<MeshRenderer>();

        // Set materials
        Renderer upperRenderer = upperLeg.GetComponent<Renderer>();
        Renderer lowerRenderer = lowerLeg.GetComponent<Renderer>();
        Renderer footRenderer = foot.GetComponent<Renderer>();

        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (bodyRenderer)
        {
            if (upperRenderer) upperRenderer.material = bodyRenderer.material;
            if (lowerRenderer) lowerRenderer.material = bodyRenderer.material;
            if (footRenderer) footRenderer.material = bodyRenderer.material;
        }

        // Apply random color variations for more organic look
        if (upperRenderer && possibleColors.Length > 1)
        {
            Color legColor = Color.Lerp(bodyRenderer.material.color,
                                       possibleColors[Random.Range(0, possibleColors.Length)],
                                       0.2f);
            upperRenderer.material.color = legColor;
            lowerRenderer.material.color = legColor;
            footRenderer.material.color = legColor;
        }

        // Assign to limb
        limb.upperLeg = upperLeg.transform;
        limb.lowerLeg = lowerLeg.transform;
        limb.foot = foot.transform;
    }

    void CreateProceduralTentacle(int index, Vector3 basePosition)
    {
        int segments = Random.Range(4, 8);

        // Create tentacle base object
        GameObject tentacleObj = new GameObject("Tentacle_" + index);
        tentacleObj.transform.SetParent(transform);
        tentacleObj.transform.localPosition = basePosition;

        // Create tentacle collider
        CapsuleCollider tentacleCollider = tentacleObj.AddComponent<CapsuleCollider>();
        tentacleCollider.radius = 0.1f * scale;
        tentacleCollider.height = tentacleReachDistance * scale;
        tentacleCollider.direction = 1; // Y-axis

        // Create tentacle data
        Tentacle tentacle = new Tentacle();
        tentacle.tentacleObject = tentacleObj;
        tentacle.tentacleCollider = tentacleCollider;
        tentacle.flexibility = Random.Range(0.7f, 1.3f);
        tentacle.waveOffset = Random.Range(0f, Mathf.PI * 2f);
        tentacle.isGrabbing = false;
        tentacle.grabProgress = 0f;

        float segmentLength = tentacleReachDistance * scale / segments;

        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();

        for (int i = 0; i < segments; i++)
        {
            // Create tentacle segment
            GameObject segment = new GameObject("TentacleSegment_" + index + "_" + i);
            segment.transform.SetParent(tentacleObj.transform);
            segment.transform.localPosition = new Vector3(0, -i * segmentLength, 0);

            // Vary segment size for more organic look
            float segmentSize = (0.3f - i * 0.04f) * scale * Random.Range(0.9f, 1.1f);
            segment.AddComponent<MeshFilter>().mesh = CreateProceduralMesh(segmentSize, segmentSize);
            segment.AddComponent<MeshRenderer>();

            // Set material
            Renderer segmentRenderer = segment.GetComponent<Renderer>();
            if (segmentRenderer && bodyRenderer)
            {
                segmentRenderer.material = bodyRenderer.material;

                // Add color variation
                if (i > 0 && possibleColors.Length > 1)
                {
                    Color segmentColor = Color.Lerp(bodyRenderer.material.color,
                                                   possibleColors[Random.Range(0, possibleColors.Length)],
                                                   i * 0.1f);
                    segmentRenderer.material.color = segmentColor;
                }
            }

            tentacle.segments.Add(segment.transform);
            tentacle.basePositions.Add(segment.transform.localPosition);
            tentacle.baseRotations.Add(segment.transform.localRotation);
        }

        tentacles.Add(tentacle);
    }

    Mesh CreateProceduralMesh(float height, float radius)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate a random number of vertices
        int vertexCount = Random.Range(10, 20);

        // Generate random vertices
        for (int i = 0; i < vertexCount; i++)
        {
            vertices.Add(Random.onUnitSphere * radius);
        }

        // Generate random triangles
        for (int i = 0; i < vertexCount - 2; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    void GenerateTentacleMorphology()
    {
        int tentacleCount = Random.Range(morphologyData.minTentacles, morphologyData.maxTentacles + 1);
        GenerateTentacles(tentacleCount);
    }

    void GenerateTentacles(int tentacleCount)
    {
        for (int i = 0; i < tentacleCount; i++)
        {
            float angle = (i / (float)tentacleCount) * 360f;
            float elevation = Mathf.PI / 3f; // 60 degrees up from horizontal

            Vector3 basePos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * Mathf.Cos(elevation),
                Mathf.Sin(elevation),
                Mathf.Sin(angle * Mathf.Deg2Rad) * Mathf.Cos(elevation)
            ) * scale;

            // Initialize tentacle
            CreateProceduralTentacle(i, basePos);
        }
    }

    void GenerateHybridMorphology()
    {
        // Combine different morphological features
        int legCount = Random.Range(morphologyData.minLegs / 2, morphologyData.maxLegs / 2 + 1);
        if (legCount % 2 != 0) legCount++;

        int tentacleCount = Random.Range(morphologyData.minTentacles / 2, morphologyData.maxTentacles / 2 + 1);

        // Generate legs
        GenerateLegs(legCount);

        // Generate tentacles
        GenerateTentacles(tentacleCount);

        // Add some body segments for extra detail
        int extraSegments = Random.Range(1, 4);
        for (int i = 0; i < extraSegments; i++)
        {
            CreateBodySegment(i, 0.4f);
        }
    }

    void GenerateAvianMorphology()
    {
        // Avian creatures have wings instead of legs
        int wingCount = 2;
        GenerateLegs(wingCount);

        // Make wings look different from legs
        foreach (Limb limb in limbs)
        {
            limb.upperLeg.localScale = new Vector3(0.3f, 0.1f, 1f) * scale;
            limb.lowerLeg.localScale = new Vector3(0.2f, 0.08f, 0.8f) * scale;
            limb.foot.localScale = new Vector3(0.15f, 0.05f, 0.4f) * scale;

            // Add feather-like details
            AddWingDetails(limb);
        }

        // Add beak
        CreateBeak();
    }

    void AddWingDetails(Limb limb)
    {
        // Add some feather-like details to wings
        for (int i = 0; i < 3; i++)
        {
            GameObject feather = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feather.name = "Feather";
            feather.transform.SetParent(limb.upperLeg);
            feather.transform.localPosition = new Vector3(0, 0, i * 0.3f);
            feather.transform.localScale = new Vector3(0.5f, 0.05f, 0.2f);
            feather.transform.localRotation = Quaternion.Euler(0, 0, i * 15f);

            Renderer featherRenderer = feather.GetComponent<Renderer>();
            Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
            if (featherRenderer && bodyRenderer)
            {
                featherRenderer.material = bodyRenderer.material;
            }
        }
    }

    void CreateBeak()
    {
        GameObject beak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beak.name = "Beak";
        beak.transform.SetParent(mainBody.transform);
        beak.transform.localPosition = new Vector3(0, 0, 0.8f);
        beak.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        beak.transform.localRotation = Quaternion.Euler(0, 180f, 0);

        Renderer beakRenderer = beak.GetComponent<Renderer>();
        if (beakRenderer)
        {
            beakRenderer.material.color = Color.yellow;
        }
    }

    void GenerateAmphibiousMorphology()
    {
        // Amphibious creatures have webbed feet and streamlined bodies
        int legCount = Random.Range(4, 6);
        GenerateLegs(legCount);

        // Make feet webbed
        foreach (Limb limb in limbs)
        {
            limb.foot.localScale = new Vector3(0.2f, 0.1f, 0.4f) * scale;

            // Add webbing between toes
            AddWebbing(limb);
        }

        // Streamline main body
        mainBody.transform.localScale = new Vector3(1.2f, 0.8f, 1.5f) * scale;

        // Add tail
        CreateTail();
    }


    void AddWebbing(Limb limb)
    {
        // Add webbing between foot segments
        for (int i = 0; i < 2; i++)
        {
            GameObject web = GameObject.CreatePrimitive(PrimitiveType.Quad);
            web.name = "Web";
            web.transform.SetParent(limb.foot);
            web.transform.localPosition = new Vector3(i * 0.1f, 0, 0);
            web.transform.localScale = new Vector3(0.2f, 0.1f, 0.1f);
            web.transform.localRotation = Quaternion.Euler(90, 0, 0);

            Renderer webRenderer = web.GetComponent<Renderer>();
            if (webRenderer)
            {
                webRenderer.material.color = Color.black;
            }
        }
    }

    void CreateTail()
    {
        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tail.name = "Tail";
        tail.transform.SetParent(mainBody.transform);
        tail.transform.localPosition = new Vector3(0, 0, -1f);
        tail.transform.localScale = new Vector3(0.4f, 0.4f, 1.2f);

        Renderer tailRenderer = tail.GetComponent<Renderer>();
        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (tailRenderer && bodyRenderer)
        {
            tailRenderer.material = bodyRenderer.material;
        }
    }

    void GenerateCrystallineMorphology()
    {
        // Crystalline enemies have geometric features and sharp edges
        int crystalCount = Random.Range(4, 8);

        for (int i = 0; i < crystalCount; i++)
        {
            CreateCrystal(i);
        }

        // Make main body more angular
        mainBody.transform.localScale = Vector3.one * scale * 0.8f;

        // Add sharp legs
        GenerateLegs(Random.Range(4, 6));

        // Make legs more crystalline
        foreach (Limb limb in limbs)
        {
            limb.upperLeg.localScale = new Vector3(0.1f, 0.5f, 0.1f) * scale;
            limb.lowerLeg.localScale = new Vector3(0.08f, 0.4f, 0.08f) * scale;
            limb.foot.localScale = new Vector3(0.1f, 0.1f, 0.3f) * scale;
        }
    }

    void CreateCrystal(int index)
    {
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Crystal_" + index;
        crystal.transform.SetParent(mainBody.transform);

        // Random position around body
        float angle = (index / (float)8) * 360f;
        float distance = Random.Range(0.5f, 0.8f) * scale;
        crystal.transform.localPosition = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
            Random.Range(-0.3f, 0.3f) * scale,
            Mathf.Sin(angle * Mathf.Deg2Rad) * distance
        );

        // Random rotation and scale
        crystal.transform.localRotation = Random.rotation;
        crystal.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f) * scale;

        // Make crystal pointed
        crystal.transform.localScale = new Vector3(
            crystal.transform.localScale.x,
            crystal.transform.localScale.y * 1.5f,
            crystal.transform.localScale.z
        );

        Renderer crystalRenderer = crystal.GetComponent<Renderer>();
        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (crystalRenderer && bodyRenderer)
        {
            crystalRenderer.material = bodyRenderer.material;

            // Make crystals slightly transparent
            Color crystalColor = crystalRenderer.material.color;
            crystalColor.a = 0.8f;
            crystalRenderer.material.color = crystalColor;
        }
    }

    void GenerateBipedalMorphology()
    {
        GenerateLegs(2);

        // Add arms
        CreateArms();
    }

    void CreateArms()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject arm = new GameObject("Arm_" + i);
            arm.transform.SetParent(mainBody.transform);
            arm.transform.localPosition = new Vector3(i == 0 ? -0.5f : 0.5f, 0, 0);

            // Create arm segments
            GameObject upperArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            upperArm.name = "UpperArm";
            upperArm.transform.SetParent(arm.transform);
            upperArm.transform.localPosition = Vector3.zero;
            upperArm.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f) * scale;

            GameObject lowerArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lowerArm.name = "LowerArm";
            lowerArm.transform.SetParent(arm.transform);
            lowerArm.transform.localPosition = new Vector3(0, -0.5f, 0);
            lowerArm.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f) * scale;

            // Add hands
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "Hand";
            hand.transform.SetParent(arm.transform);
            hand.transform.localPosition = new Vector3(0, -0.9f, 0);
            hand.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f) * scale;

            // Set materials
            Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
            if (bodyRenderer)
            {
                upperArm.GetComponent<Renderer>().material = bodyRenderer.material;
                lowerArm.GetComponent<Renderer>().material = bodyRenderer.material;
                hand.GetComponent<Renderer>().material = bodyRenderer.material;
            }
        }
    }

    void GenerateQuadrupedMorphology()
    {
        GenerateLegs(4);

        // Add tail
        CreateTail();
    }

    void GenerateCentipedeMorphology()
    {
        int segmentCount = Random.Range(morphologyData.minBodySegments, morphologyData.maxBodySegments + 1);
        int legsPerSegment = 2;

        // Create body segments
        for (int i = 0; i < segmentCount; i++)
        {
            CreateBodySegment(i, 0.6f);
        }

        // Create legs for each segment
        GenerateLegs(segmentCount * legsPerSegment);
    }

    void CreateBodySegment(int index, float sizeMultiplier)
    {
        // Create segment object
        GameObject segmentObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        segmentObj.name = "BodySegment_" + index;
        segmentObj.transform.SetParent(transform);
        segmentObj.transform.localPosition = new Vector3(0, 0, index * segmentSpacing * scale);
        segmentObj.transform.localScale = Vector3.one * scale * sizeMultiplier;

        // Set material
        Renderer segmentRenderer = segmentObj.GetComponent<Renderer>();
        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (segmentRenderer && bodyRenderer)
        {
            segmentRenderer.material = bodyRenderer.material;

            // Add color variation for segments
            if (index > 0)
            {
                Color segmentColor = Color.Lerp(bodyRenderer.material.color,
                                               possibleColors[Random.Range(0, possibleColors.Length)],
                                               index * 0.05f);
                segmentRenderer.material.color = segmentColor;
            }
        }

        // Create segment collider
        CapsuleCollider segmentCollider = segmentObj.GetComponent<CapsuleCollider>();
        segmentCollider.height = 1.2f;

        // Add rigidbody
        Rigidbody segmentRb = segmentObj.AddComponent<Rigidbody>();
        segmentRb.mass = 2f * scale;

        // Connect with configurable joint
        ConfigurableJoint joint = segmentObj.AddComponent<ConfigurableJoint>();
        if (index == 0)
        {
            joint.connectedBody = mainBody.GetComponent<Rigidbody>();
        }
        else
        {
            joint.connectedBody = bodySegments[index - 1].segmentObject.GetComponent<Rigidbody>();
        }

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // Set angular motion limits
        JointDrive angularXDrive = new JointDrive();
        angularXDrive.positionSpring = 80f;
        angularXDrive.positionDamper = 4f;
        angularXDrive.maximumForce = 40f;

        joint.angularXDrive = angularXDrive;
        joint.angularYZDrive = angularXDrive;

        SoftJointLimit linearLimit = new SoftJointLimit();
        linearLimit.limit = 0.1f;
        joint.linearLimit = linearLimit;

        SoftJointLimit angularLimit = new SoftJointLimit();
        angularLimit.limit = 10f;
        joint.angularZLimit = angularLimit;
        joint.lowAngularXLimit = angularLimit;
        joint.highAngularXLimit = angularLimit;

        // Create segment data
        BodySegment segment = new BodySegment();
        segment.segmentObject = segmentObj;
        segment.segmentCollider = segmentCollider;
        segment.transform = segmentObj.transform;
        segment.baseLocalPosition = segmentObj.transform.localPosition;
        segment.baseLocalRotation = segmentObj.transform.localRotation;
        segment.waveOffset = index * 0.3f;

        bodySegments.Add(segment);
    }

    void GenerateWormMorphology()
    {
        // Worm morphology - segmented body without legs
        int segmentCount = Random.Range(morphologyData.minBodySegments, morphologyData.maxBodySegments + 1);

        for (int i = 0; i < segmentCount; i++)
        {
            CreateBodySegment(i, 0.8f);
        }
    }

    void GenerateBlobMorphology()
    {
        // Blob morphology - just the main body with no additional parts
        mainBody.transform.localScale = Vector3.one * scale * 1.2f;

        // Make the blob more squishy by adjusting joint limits
        ConfigurableJoint joint = mainBody.GetComponent<ConfigurableJoint>();
        if (joint)
        {
            SoftJointLimit angularLimit = new SoftJointLimit();
            angularLimit.limit = 15f;
            joint.angularZLimit = angularLimit;
            joint.lowAngularXLimit = angularLimit;
            joint.highAngularXLimit = angularLimit;
        }

        // Add some blob-like features
        for (int i = 0; i < 3; i++)
        {
            CreateBlobFeature(i);
        }
    }

    void CreateBlobFeature(int index)
    {
        GameObject feature = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        feature.name = "BlobFeature_" + index;
        feature.transform.SetParent(mainBody.transform);

        // Random position on blob surface
        feature.transform.localPosition = Random.onUnitSphere * 0.5f;
        feature.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f) * scale;

        Renderer featureRenderer = feature.GetComponent<Renderer>();
        Renderer bodyRenderer = mainBody.GetComponentInChildren<Renderer>();
        if (featureRenderer && bodyRenderer)
        {
            featureRenderer.material = bodyRenderer.material;

            // Slightly different color
            Color featureColor = Color.Lerp(bodyRenderer.material.color,
                                          Color.gray,
                                          0.3f);
            featureRenderer.material.color = featureColor;
        }
    }

    void Update()
    {
        if (isDead) return;

        UpdateAI();
        UpdateAttack();
        UpdateAnimation();
        UpdateTentacleMovement();
        UpdateForceField();
        UpdateAudio();
        UpdateTargetMemory();
        UpdateStateTimers();
    }
    void UpdateTargetMemory()
    {
        if (hasTargetMemory)
        {
            targetMemoryTimer -= Time.deltaTime;
            if (targetMemoryTimer <= 0f)
            {
                hasTargetMemory = false;
            }
        }
    }

    void UpdateStateTimers()
    {
        if (statePersistanceTimer > 0)
        {
            statePersistanceTimer -= Time.deltaTime;
        }
    }
    void FixedUpdate()
    {
        if (isDead) return;

        UpdateMovement();
        UpdateGroundAlignment();
        UpdateLegMovementFixed();
    }
    void UpdateLimbPosition(Limb limb, float speedRatio)
    {
        limb.worldRestPosition = transform.TransformPoint(limb.restPosition);
        Vector3 predictedMovement = currentVelocity.normalized * stepPredictionDistance * speedRatio;
        Vector3 idealPosition = limb.worldRestPosition + predictedMovement;

        if (Physics.Raycast(idealPosition + Vector3.up * (maxLegReach * scale * 0.5f),
            -Vector3.up, out groundHit, maxLegReach * scale * 1.5f, groundMask))
        {
            idealPosition = groundHit.point;
        }
        else
        {
            idealPosition = limb.worldRestPosition - Vector3.up * (maxLegReach * scale * 0.5f);
        }

        float distanceToIdeal = Vector3.Distance(limb.targetPosition, idealPosition);
        float moveThreshold = maxLegReach * scale * legSynchronization * (1f + speedRatio * LEG_MOVE_THRESHOLD_BONUS);

        if (!limb.isMoving && distanceToIdeal > moveThreshold)
        {
            StartLegMovement(limb, idealPosition);
        }
    }

    void UpdateLimbMovement(Limb limb, float frequency)
    {
        if (!limb.isMoving)
        {
            // Keep foot planted with slight adjustment
            limb.targetPosition = Vector3.Lerp(limb.targetPosition, limb.worldRestPosition - Vector3.up * (maxLegReach * scale * 0.5f),
                Time.fixedDeltaTime * LEG_ADJUST_SPEED);
            limb.limbObject.transform.position = limb.targetPosition;
            ResetLegJoints(limb);
            return;
        }

        limb.timer += Time.fixedDeltaTime * frequency;

        if (limb.timer >= 1f)
        {
            limb.isMoving = false;
            limb.liftProgress = 0f;
            return;
        }

        float liftHeight = Mathf.Sin(limb.timer * Mathf.PI) * stepHeight * scale;
        limb.liftProgress = liftHeight / (stepHeight * scale);

        Vector3 newPosition = Vector3.Lerp(limb.startPosition, limb.targetPosition, limb.timer);
        newPosition += Vector3.up * liftHeight;

        limb.targetPosition = newPosition;
        limb.limbObject.transform.position = newPosition;
        UpdateLegJoints(limb);
    }

    void UpdateLegMovementFixed()
    {
        if (limbs.Count == 0) return;

        // Spread leg updates across frames
        int legsToUpdate = Mathf.CeilToInt(limbs.Count * legUpdateSpread);
        for (int i = 0; i < legsToUpdate; i++)
        {
            legUpdateIndex = (legUpdateIndex + 1) % limbs.Count;
            Limb limb = limbs[legUpdateIndex];

            if (limb.isDamaged) continue;

            float speedRatio = Mathf.Clamp01(currentVelocity.magnitude / maxMoveSpeed);
            UpdateLimbPosition(limb, speedRatio);

            float stepFrequency = stepSpeed * speedRatio;
            UpdateLimbMovement(limb, stepFrequency);
        }
    }
    void StartLegMovement(Limb limb, Vector3 targetPos)
    {
        limb.isMoving = true;
        limb.startPosition = limb.targetPosition;
        limb.targetPosition = targetPos;
        limb.timer = 0f;
        limb.liftProgress = 0f;
    }



    void UpdateAI()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            MakeDecision();
        }

        if (target == null)
        {
            FindTarget();
        }
        else
        {
            ValidateTarget();
        }
    }


    void MakeDecision()
    {
        if (statePersistanceTimer > 0 || state == AIState.WindUp || state == AIState.Recover)
            return;

        if (target == null || !hasTargetMemory)
        {
            state = AIState.Patrol;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position,
            target != null ? target.position : lastKnownTargetPosition);
        bool hasVisibleTarget = CheckLineOfSight();

        switch (role)
        {
            case EnemyRole.Predator:
                HandlePredatorDecision(distanceToTarget, hasVisibleTarget);
                break;
            case EnemyRole.Scavenger:
                HandleScavengerDecision(distanceToTarget, hasVisibleTarget);
                break;
            case EnemyRole.Grazer:
                HandleGrazerDecision(distanceToTarget, hasVisibleTarget);
                break;
            case EnemyRole.Swarmer:
                HandleSwarmerDecision(distanceToTarget, hasVisibleTarget);
                break;
            case EnemyRole.Boss:
                HandleBossDecision(distanceToTarget, hasVisibleTarget);
                break;
        }

        statePersistanceTimer = Random.Range(STATE_PERSISTENCE_MIN, STATE_PERSISTENCE_MAX);
    }
    public float GetHealth()
    {
        return health;
    }

    void HandleScavengerDecision(float distance, bool visible)
    {
        IDamageable targetHealth = target.GetComponent<IDamageable>();
        float targetHealthPercent = targetHealth != null ? targetHealth.GetHealth() / 100f : 1f;

        if (health < maxHealth * 0.3f || (visible && targetHealthPercent > 0.75f))
        {
            state = AIState.Flee;
        }
        else if (visible && targetHealthPercent < 0.5f && distance <= attackRange)
        {
            state = AIState.WindUp;
        }
        else if (hasTargetMemory)
        {
            state = AIState.Investigating;
        }
        else
        {
            state = AIState.Patrol;
        }
    }

    void HandleSwarmerDecision(float distance, bool visible)
    {
        if (SwarmManager.instance.swarmerRoles.TryGetValue(this, out var swarmerRole))
        {
            if (swarmerRole == SwarmManager.SwarmerRole.Attacker)
            {
                if (visible && distance <= attackRange * MIN_ATTACK_DISTANCE_FACTOR)
                {
                    state = Random.value < 0.5f ? AIState.Charge : AIState.WindUp;
                }
                else if (visible || hasTargetMemory)
                {
                    state = AIState.Chase;
                }
                else
                {
                    state = AIState.Patrol;
                }
            }
            else // Distractor
            {
                state = visible ? AIState.Flank : AIState.Patrol;
            }
        }
        else
        {
            state = AIState.Patrol;
        }
    }
    Vector3 CalculateInvestigateDirection()
    {
        if (!hasTargetMemory) return Vector3.zero;
        return (lastKnownTargetPosition - transform.position).normalized;
    }

    Vector3 CalculateFlankDirection()
    {
        if (target == null) return Vector3.zero;

        Vector3 toTarget = (target.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(toTarget, Vector3.up).normalized;

        // Choose flanking side based on situation
        float side = Mathf.Sign(Random.Range(-1f, 1f));
        return (right * side + toTarget * 0.3f).normalized;
    }
    Vector3 FindCoverPosition()
    {
        for (int i = 0; i < COVER_FIND_ATTEMPTS; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * COVER_FIND_RADIUS;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                // Check if the point is hidden from the target
                if (target != null && Physics.Raycast(hit.position, (target.position - hit.position).normalized,
                    Vector3.Distance(hit.position, target.position), obstacleMask))
                {
                    return hit.position;
                }
            }
        }
        return Vector3.zero;
    }

    float CalculateMovementSpeed()
    {
        return state switch
        {
            AIState.Patrol => maxMoveSpeed * PATROL_SPEED_MULTIPLIER,
            AIState.Attack => maxMoveSpeed * ATTACK_CIRCLE_SPEED_MULTIPLIER,
            AIState.Charge => maxMoveSpeed * CHARGE_SPEED_MULTIPLIER,
            AIState.Investigating => maxMoveSpeed * INVESTIGATE_SPEED_MULTIPLIER,
            AIState.Flee => maxMoveSpeed * 1.2f, // Flee faster
            _ => maxMoveSpeed
        };
    }

    Vector3 CalculateTakeCoverDirection()
    {
        if (target == null) return Vector3.zero;

        Vector3 coverPosition = FindCoverPosition();
        if (coverPosition != Vector3.zero)
        {
            return (coverPosition - transform.position).normalized;
        }

        // Fallback: move away from target
        return (transform.position - target.position).normalized;
    }
    Vector3 CalculateChargeDirection()
    {
        if (target == null) return Vector3.zero;
        return (target.position - transform.position).normalized;
    }

    Vector3 CalculateAttackDirection()
    {
        if (target == null) return Vector3.zero;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > attackRange * MIN_ATTACK_DISTANCE_FACTOR)
        {
            // Move toward target but maintain attack distance
            return (target.position - transform.position).normalized;
        }
        else
        {
            // Circle around target
            Vector3 toTarget = (target.position - transform.position).normalized;
            Vector3 right = Vector3.Cross(toTarget, Vector3.up).normalized;

            // Add some forward/backward movement variation
            float forwardComponent = Mathf.Clamp((distance - attackRange * IDEAL_ATTACK_DISTANCE_FACTOR) /
                attackRange, -CIRCLE_FORWARD_COMPONENT_MAX, CIRCLE_FORWARD_COMPONENT_MAX);

            return (right + toTarget * forwardComponent).normalized;
        }
    }
    Vector3 FindSafePosition(Vector3 preferredDirection)
    {
        for (int i = 0; i < COVER_FIND_ATTEMPTS; i++)
        {
            // Try positions in the preferred direction with some variation
            Vector3 testDirection = Quaternion.Euler(0, Random.Range(-45f, 45f), 0) * preferredDirection;
            Vector3 testPosition = transform.position + testDirection * COVER_FIND_RADIUS;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPosition, out hit, 2f, NavMesh.AllAreas))
            {
                // Check if this position provides cover from the target
                if (target != null && Physics.Raycast(hit.position, (target.position - hit.position).normalized,
                    Vector3.Distance(hit.position, target.position), obstacleMask))
                {
                    return hit.position;
                }
            }
        }
        return Vector3.zero;
    }
    Vector3 CalculateFleeDirection()
    {
        if (target == null)
            return hasTargetMemory ? (transform.position - lastKnownTargetPosition).normalized : Vector3.zero;

        Vector3 fleeDirection = (transform.position - target.position).normalized;

        // Find safer position rather than just running away
        Vector3 safePosition = FindSafePosition(fleeDirection);
        if (safePosition != Vector3.zero)
        {
            return (safePosition - transform.position).normalized;
        }

        return fleeDirection;
    }

    Vector3 CalculateChaseDirection()
    {
        if (target == null)
            return hasTargetMemory ? (lastKnownTargetPosition - transform.position).normalized : Vector3.zero;

        Vector3 direction = (target.position - transform.position).normalized;
        Rigidbody targetRb = target.GetComponent<Rigidbody>();

        if (targetRb != null)
        {
            direction = (target.position + targetRb.linearVelocity * TARGET_PREDICTION_FACTOR - transform.position).normalized;
        }
        return direction;
    }
    void GenerateNewPatrolDestination()
    {
        hasPatrolDestination = true;
        Vector2 randomCircle = Random.insideUnitCircle * 8f;
        patrolDestination = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(patrolDestination, out hit, 3f, NavMesh.AllAreas))
        {
            patrolDestination = hit.position;
        }
    }
    Vector3 CalculatePatrolDirection()
    {
        if (!hasPatrolDestination || Vector3.Distance(transform.position, patrolDestination) < 1.5f)
        {
            GenerateNewPatrolDestination();
        }
        return (patrolDestination - transform.position).normalized;
    }
    Vector3 CalculateMovementDirection()
    {
        switch (state)
        {
            case AIState.Patrol: return CalculatePatrolDirection();
            case AIState.Chase: return CalculateChaseDirection();
            case AIState.Flee: return CalculateFleeDirection();
            case AIState.Attack: return CalculateAttackDirection();
            case AIState.Charge: return CalculateChargeDirection();
            case AIState.Investigating: return CalculateInvestigateDirection();
            case AIState.Flank: return CalculateFlankDirection();
            case AIState.TakeCover: return CalculateTakeCoverDirection();
            default: return Vector3.zero;
        }
    }
    bool CheckLineOfSight()
    {
        if (target == null) return false;

        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToTarget,
            distance, obstacleMask);
    }
   
    void Decelerate()
    {
        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero,
            deceleration * DECELERATION_BRAKING_FACTOR * Time.fixedDeltaTime);
    }
    void UpdateFootsteps()
    {
        if (Time.time - lastFootstepTime > footstepInterval &&
            currentVelocity.magnitude > maxMoveSpeed * footstepSyncThreshold)
        {
            lastFootstepTime = Time.time;
            PlayMovementSound();
        }
    }
    void PlayMovementSound()
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled || movementSounds.Length == 0) return;

        AudioClip clipToPlay = movementSounds[Random.Range(0, movementSounds.Length)];
        audioSource.volume = 0.3f + (currentVelocity.magnitude / maxMoveSpeed) * 0.4f;
        audioSource.pitch = 0.9f + (currentVelocity.magnitude / maxMoveSpeed) * 0.2f;
        audioSource.PlayOneShot(clipToPlay);
    }



    void ApplyMovement()
    {
        if (currentVelocity.magnitude > 0.1f)
        {
            // Smooth rotation toward movement direction
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation,
                turnSpeed * Time.fixedDeltaTime));

            // Apply movement with ground alignment
            Vector3 moveVector = currentVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveVector);

            // Terrain deformation for heavy movement
            if (currentVelocity.magnitude > maxMoveSpeed * 0.7f)
            {
                DeformTerrain(transform.position, terrainDeformationRadius * 0.5f,
                    terrainDeformationStrength * 0.3f);
            }

            // Check for footsteps
            UpdateFootsteps();
        }
        else
        {
            // Gentle braking when not moving
            Decelerate();
        }
    }
    void SmoothVelocityInterpolation()
    {
        float currentAccel = desiredVelocity.magnitude > currentVelocity.magnitude ?
            acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            desiredVelocity,
            currentAccel * Time.fixedDeltaTime
        );
    }

    float CalculateTargetScore(Collider candidate, float distance, Vector3 direction)
    {
        float score = 0f;

        // Line of sight check
        bool canSee = !Physics.Raycast(transform.position + Vector3.up * 0.5f, direction,
            distance, obstacleMask);

        // Hearing range check
        bool canHear = distance < scanRadius * HEARING_RANGE_FACTOR;

        if (canSee || canHear)
        {
            // Base score based on distance (closer is better)
            score = 1f - Mathf.Clamp01(distance / scanRadius);

            // Priority for visible targets
            if (canSee) score *= 1.5f;

            // Consider target health for scavengers
            if (role == EnemyRole.Scavenger)
            {
                IDamageable targetHealth = candidate.GetComponent<IDamageable>();
                if (targetHealth != null)
                {
                    float healthPercent = targetHealth.GetHealth() / 100f;
                    score *= (1f - healthPercent); // Prefer weaker targets
                }
            }
        }

        return score;
    }

    void ValidateTarget()
    {
        float sqrDist = (transform.position - target.position).sqrMagnitude;

        if (sqrDist > sqrScanRadius * 4f) // Increased threshold for forgetting
        {
            target = null;
        }
        else
        {
            lastKnownTargetPosition = target.position;
            hasTargetMemory = true;
            targetMemoryTimer = memoryDuration;
        }
    }

    void HandleBossDecision(float distance, bool visible)
    {
        if (health < maxHealth * 0.5f)
        {
            attackCooldown = 1.5f;
            attack = (AttackStyle)Random.Range(5, System.Enum.GetValues(typeof(AttackStyle)).Length);
        }

        if (visible && distance <= attackRange * MIN_ATTACK_DISTANCE_FACTOR)
            state = AIState.WindUp;
        else if (visible && distance <= attackRange * 1.5f)
            state = AIState.Special;
        else if (visible || hasTargetMemory)
            state = AIState.Chase;
        else
            state = AIState.Patrol;
    }

    void HandleGrazerDecision(float distance, bool visible)
    {
        if (health < maxHealth * 0.5f && visible)
        {
            state = AIState.Flee;
        }
        else if (visible && distance < 4f)
        {
            state = AIState.WindUp;
        }
        else
        {
            state = AIState.Patrol;
        }
    }

    void HandlePredatorDecision(float distance, bool visible)
    {
        if (health < maxHealth * 0.25f)
        {
            state = AIState.Flee;
        }
        else if (visible && distance <= attackRange * MIN_ATTACK_DISTANCE_FACTOR)
        {
            state = Random.value < 0.2f ? AIState.Flank : AIState.WindUp;
        }
        else if (visible)
        {
            state = Random.value < 0.3f ? AIState.TakeCover : AIState.Chase;
        }
        else if (hasTargetMemory)
        {
            state = AIState.Investigating;
        }
        else
        {
            state = AIState.Patrol;
        }
    }



    Vector3 FindCover()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                // Check if the point is hidden from the player
                if (!Physics.Raycast(hit.position, (target.position - hit.position).normalized, Vector3.Distance(hit.position, target.position), obstacleMask))
                {
                    return hit.position;
                }
            }
        }
        return Vector3.zero;
    }

    int GetNearbySwarmers()
    {
        int count = 0;
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f); // 10f is the radius to check for other swarmers
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy") && col.GetComponent<StarfallEnemy>().role == EnemyRole.Swarmer)
            {
                count++;
            }
        }
        return count;
    }


    void FindTarget()
    {
        int hits = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, scanResults, targetMask);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        for (int i = 0; i < hits; i++)
        {
            Vector3 dirToTarget = (scanResults[i].transform.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, scanResults[i].transform.position);

            // LOS check OR hearing range
            bool canSee = !Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToTarget, dist, obstacleMask);
            bool canHear = dist < scanRadius * 0.4f;

            if (canSee || canHear)
            {
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = scanResults[i].transform;
                }
            }
        }

        if (closestTarget != null)
        {
            target = closestTarget;
            hasTargetMemory = true;
            lastKnownTargetPosition = target.position;
            targetMemoryTimer = memoryDuration;
        }
    }


    void UpdateMovement()
    {
        // Reset desired velocity at start of each frame
        desiredVelocity = Vector3.zero;

        if (state == AIState.Idle || state == AIState.Feeding)
        {
            // Smoothly decelerate to zero
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero,
                deceleration * Time.fixedDeltaTime);
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        float moveSpeed = maxMoveSpeed;

        switch (state)
        {
            case AIState.Patrol:
                if (!hasPatrolDestination || Vector3.Distance(transform.position, patrolDestination) < 1.5f)
                {
                    hasPatrolDestination = true;
                    Vector2 randomCircle = Random.insideUnitCircle * 8f;
                    patrolDestination = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                    // Ensure destination is on navmesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(patrolDestination, out hit, 3f, NavMesh.AllAreas))
                    {
                        patrolDestination = hit.position;
                    }
                }
                moveDirection = (patrolDestination - transform.position).normalized;
                moveSpeed *= 0.4f; // Slower patrol speed
                break;

            case AIState.Chase:
                if (target != null)
                {
                    moveDirection = (target.position - transform.position).normalized;
                    // Add slight prediction for moving targets
                    Rigidbody targetRb = target.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        moveDirection = (target.position + targetRb.linearVelocity * 0.3f - transform.position).normalized;
                    }
                }
                break;

            case AIState.Flee:
                if (target != null)
                {
                    moveDirection = (transform.position - target.position).normalized;
                }
                break;

            case AIState.Attack:
                if (target != null)
                {
                    float distance = Vector3.Distance(transform.position, target.position);

                    if (distance > attackRange * 0.8f)
                    {
                        // Move toward target but maintain attack distance
                        moveDirection = (target.position - transform.position).normalized;
                        moveSpeed *= 0.8f;
                    }
                    else
                    {
                        // Circle around target
                        Vector3 toTarget = (target.position - transform.position).normalized;
                        Vector3 right = Vector3.Cross(toTarget, Vector3.up).normalized;

                        // Add some forward/backward movement variation
                        float forwardComponent = Mathf.Clamp((distance - attackRange * 0.5f) / attackRange, -0.3f, 0.3f);
                        moveDirection = (right + toTarget * forwardComponent).normalized;
                        moveSpeed *= 0.6f;
                    }
                }
                break;

            case AIState.Charge:
                if (target != null)
                {
                    moveDirection = (target.position - transform.position).normalized;
                    moveSpeed *= 1.5f; // Charge speed boost
                }
                break;

            case AIState.Investigating:
                if (hasTargetMemory)
                {
                    moveDirection = (lastKnownTargetPosition - transform.position).normalized;
                    moveSpeed *= 0.6f;

                    if (Vector3.Distance(transform.position, lastKnownTargetPosition) < 1f)
                    {
                        hasTargetMemory = false;
                        state = AIState.Patrol;
                    }
                }
                break;

            case AIState.Flank:
            case AIState.TakeCover:
                // These states should set their own desiredVelocity in MakeDecision
                break;
        }

        // Apply obstacle avoidance
        if (moveDirection != Vector3.zero)
        {
            moveDirection = ApplyObstacleAvoidance(moveDirection);
            desiredVelocity = moveDirection * moveSpeed;
        }

        // Smooth velocity interpolation
        float currentAccel = desiredVelocity.magnitude > currentVelocity.magnitude ?
            acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            desiredVelocity,
            currentAccel * Time.fixedDeltaTime
        );

        // Apply movement if we have significant velocity
        if (currentVelocity.magnitude > 0.1f)
        {
            // Smooth rotation toward movement direction
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation,
                turnSpeed * Time.fixedDeltaTime));

            // Apply movement with ground alignment
            Vector3 moveVector = currentVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveVector);

            // Terrain deformation for heavy movement
            if (currentVelocity.magnitude > maxMoveSpeed * 0.7f)
            {
                DeformTerrain(transform.position, terrainDeformationRadius * 0.5f,
                    terrainDeformationStrength * 0.3f);
            }
        }
        else
        {
            // Gentle braking when not moving
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero,
                deceleration * 0.5f * Time.fixedDeltaTime);
        }
    }
    Vector3 ApplyObstacleAvoidance(Vector3 desiredVelocity)
    {
        if (desiredVelocity.magnitude < 0.1f) return desiredVelocity;

        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, desiredVelocity.normalized, out hit,
            OBSTACLE_AVOID_DISTANCE, obstacleMask))
        {
            // Try left and right avoidance directions
            Vector3[] testDirections = {
                Vector3.Cross(desiredVelocity, Vector3.up).normalized,
                -Vector3.Cross(desiredVelocity, Vector3.up).normalized,
                (desiredVelocity + Vector3.up * 0.5f).normalized,
                (desiredVelocity - Vector3.up * 0.5f).normalized
            };

            foreach (Vector3 testDir in testDirections)
            {
                if (!Physics.Raycast(rayStart, testDir, OBSTACLE_AVOID_DISTANCE, obstacleMask))
                {
                    return Vector3.Lerp(desiredVelocity.normalized, testDir, OBSTACLE_AVOID_LERP).normalized * desiredVelocity.magnitude;
                }
            }

            // If all directions blocked, reverse
            return -desiredVelocity;
        }

        return desiredVelocity;
    }


    void DeformTerrain(Vector3 position, float radius, float strength)
    {
        // This is a placeholder for the terrain deformation logic.
        // You will need to implement this yourself based on your terrain system.
    }

    Texture2D GenerateProceduralTexture()
    {
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);

        Color color = possibleColors[Random.Range(0, possibleColors.Length)];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                texture.SetPixel(x, y, color * noise);
            }
        }

        texture.Apply();
        return texture;
    }

    void UpdateGroundAlignment()
    {
        Vector3 castStart = transform.position + Vector3.up * 0.5f;
        float castDistance = groundCheckDistance * scale;

        if (Physics.SphereCast(castStart, 0.3f * scale, -Vector3.up, out groundHit,
            castDistance, groundMask))
        {
            lastGroundNormal = groundHit.normal;

            float slopeAngle = Vector3.Angle(Vector3.up, lastGroundNormal);
            if (slopeAngle > GROUND_ALIGN_MIN_SLOPE)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, lastGroundNormal) * transform.rotation;
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                    groundAlignmentSpeed * Time.fixedDeltaTime));
            }
        }
    }



    void UpdateForceField()
    {
        if (forceFieldInstance != null)
        {
            float pulse = Mathf.Sin(Time.time * FORCE_FIELD_PULSE_SPEED) * FORCE_FIELD_PULSE_AMPLITUDE + FORCE_FIELD_PULSE_BASE;
            forceFieldInstance.transform.localScale = Vector3.one * forceFieldRadius * 2f * pulse;

            Collider[] colliders = Physics.OverlapSphere(transform.position, forceFieldRadius);
            foreach (Collider col in colliders)
            {
                if (col.attachedRigidbody != null && col.gameObject != gameObject)
                {
                    Vector3 dir = (col.transform.position - transform.position).normalized;
                    float dist = Vector3.Distance(col.transform.position, transform.position);
                    float force = (1f - dist / forceFieldRadius) * forceFieldStrength;
                    col.attachedRigidbody.AddForce(dir * force, ForceMode.Impulse);
                }
            }
        }
    }
    void PlayVocalSound()
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled || vocalSounds.Length == 0) return;

        AudioClip clipToPlay = vocalSounds[Random.Range(0, vocalSounds.Length)];
        audioSource.volume = 0.5f;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clipToPlay);
    }

    void UpdateAudio()
    {
        soundTimer += Time.deltaTime;

        if (soundTimer >= nextSoundTime)
        {
            PlayVocalSound();
            soundTimer = 0f;
            nextSoundTime = Random.Range(minSoundDelay, maxSoundDelay);
        }
    }

    void PlayRandomSound()
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled) return;

        AudioClip clipToPlay = null;

        // Choose sound based on current state
        if (isDead && deathSounds.Length > 0)
        {
            clipToPlay = deathSounds[Random.Range(0, deathSounds.Length)];
        }
        else if (state == AIState.Attack && attackSounds.Length > 0)
        {
            clipToPlay = attackSounds[Random.Range(0, attackSounds.Length)];
        }
        else if (movementSounds.Length > 0 && currentVelocity.magnitude > maxMoveSpeed * 0.3f)
        {
            clipToPlay = movementSounds[Random.Range(0, movementSounds.Length)];
            audioSource.volume = 0.3f + (currentVelocity.magnitude / maxMoveSpeed) * 0.4f;
            audioSource.pitch = 0.9f + (currentVelocity.magnitude / maxMoveSpeed) * 0.2f;
        }
        else if (vocalSounds.Length > 0)
        {
            clipToPlay = vocalSounds[Random.Range(0, vocalSounds.Length)];
            audioSource.volume = 0.5f;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
    void ExecuteAttack()
    {
        switch (attack)
        {
            case AttackStyle.Spit: SpitAttack(); break;
            case AttackStyle.Charge: ChargeAttack(); break;
            case AttackStyle.Bite: BiteAttack(); break;
            case AttackStyle.Beam: StartBeamAttack(); break;
            case AttackStyle.RetreatAndShoot: RetreatAndShootAttack(); break;
            case AttackStyle.DrillSpin: DrillSpinAttack(); break;
            case AttackStyle.Lash: LashAttack(); break;
            case AttackStyle.Swarm: SwarmAttack(); break;
            case AttackStyle.Sonic: SonicAttack(); break;
            case AttackStyle.Poison: PoisonAttack(); break;
            case AttackStyle.CrystalSpike: CrystalSpikeAttack(); break;
        }
        attackTimer = attackCooldown;
    }

    void UpdateAttack()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (isBeaming) UpdateBeamAttack();
        if (recoveryTimer > 0) recoveryTimer -= Time.deltaTime;

        if (state == AIState.WindUp)
        {
            windUpTimer -= Time.deltaTime;
            if (windUpTimer <= 0)
            {
                state = AIState.Attack;
                ExecuteAttack();
                recoveryTimer = attackRecoveryTime;
                state = AIState.Recover;
            }
            return;
        }

        if (state == AIState.Recover && recoveryTimer <= 0)
        {
            state = AIState.Idle; // Return to decision making after recovery
        }

        if (state == AIState.Attack && attackTimer <= 0 && CanAttack())
        {
            windUpTimer = attackWindUpTime;
            state = AIState.WindUp;
        }
    }

    bool CanAttack()
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= attackRange && CheckLineOfSight();
    }

    void SpitAttack()
    {
        if (spitProjectile && firePoint)
        {
            GameObject projectile = Instantiate(spitProjectile, firePoint.position, firePoint.rotation);
            Rigidbody projRb = projectile.GetComponent<Rigidbody>();
            if (projRb && target)
            {
                Vector3 direction = (target.position - firePoint.position).normalized;
                direction += Random.insideUnitSphere * 0.1f;
                direction.Normalize();

                projRb.AddForce(direction * spitForce, ForceMode.Impulse);

                ProjectileExplosive explosive = projectile.AddComponent<ProjectileExplosive>();
                explosive.explosionRadius = 3f;
                explosive.explosionForce = 10f;
                explosive.damage = 15f;
            }

            PlayAttackSound();
        }
    }
    GameObject GetPooledEffect()
    {
        if (effectPool.Count > 0)
        {
            GameObject effect = effectPool.Dequeue();
            effect.SetActive(true);
            return effect;
        }
        return Instantiate(damageEffect);
    }
    void ReturnEffectToPool(GameObject effect)
    {
        effect.SetActive(false);
        effectPool.Enqueue(effect);
    }
    IEnumerator ReturnEffectAfterDelay(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnEffectToPool(effect);
    }

    void ChargeAttack()
    {
        if (target)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.AddForce(direction * chargeForce, ForceMode.Impulse);

            GameObject chargeEffect = GetPooledEffect();
            if (chargeEffect)
            {
                chargeEffect.transform.position = transform.position;
                chargeEffect.transform.rotation = Quaternion.LookRotation(direction);
                chargeEffect.transform.SetParent(transform);
                StartCoroutine(ReturnEffectAfterDelay(chargeEffect, 1f));
            }

            DeformTerrain(transform.position, terrainDeformationRadius, terrainDeformationStrength);
            PlayAttackSound();
        }
    }

    void BiteAttack()
    {
        if (target && Vector3.Distance(transform.position, target.position) <= biteRange)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(biteDamage);
            }

            GameObject biteEffect = GetPooledEffect();
            if (biteEffect)
            {
                biteEffect.transform.position = transform.position + transform.forward * biteRange * 0.5f;
                StartCoroutine(ReturnEffectAfterDelay(biteEffect, 0.5f));
            }

            PlayAttackSound();
        }
    }

    void RetreatAndShootAttack()
    {
        // Move away from target
        if (target)
        {
            Vector3 retreatDirection = (transform.position - target.position).normalized;
            rb.AddForce(retreatDirection * chargeForce * 0.5f, ForceMode.Impulse);

            // Then shoot
            SpitAttack();
        }
    }

    void DrillSpinAttack()
    {
        // Spin and move toward target
        if (target)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            rb.AddForce(direction * chargeForce * 0.7f, ForceMode.Impulse);
            rb.AddTorque(transform.up * chargeForce * 5f, ForceMode.Impulse);

            // Deform terrain under while drilling
            DeformTerrain(transform.position, terrainDeformationRadius * 0.7f, terrainDeformationStrength * 0.8f);

            PlayAttackSound();
        }
    }

    void LashAttack()
    {
        // Tentacle lash attack
        if (tentacles.Count > 0 && target)
        {
            // Choose a random tentacle to attack with
            int tentacleIndex = Random.Range(0, tentacles.Count);
            tentacles[tentacleIndex].isGrabbing = true;
            tentacles[tentacleIndex].grabTarget = target;
            tentacles[tentacleIndex].grabProgress = 0f;

            PlayAttackSound();
        }
    }

    void SonicAttack()
    {
        // Create a sonic wave that damages in an area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 8f, targetMask);
        foreach (Collider col in hitColliders)
        {
            // Apply sonic damage
            // col.GetComponent<Health>()?.TakeDamage(10f);
        }

        // Visual effect
        if (damageEffect)
        {
            GameObject sonicEffect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            sonicEffect.transform.localScale = Vector3.one * 8f;
            Destroy(sonicEffect, 1f);
        }

        PlayAttackSound();
    }

    void PoisonAttack()
    {
        // Create a poison cloud
        if (spitProjectile && firePoint)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject projectile = Instantiate(spitProjectile, firePoint.position, Quaternion.identity);
                projectile.transform.localScale = Vector3.one * 0.5f;

                Rigidbody projRb = projectile.GetComponent<Rigidbody>();
                if (projRb)
                {
                    Vector3 direction = (Random.insideUnitSphere + Vector3.up * 0.5f).normalized;
                    projRb.AddForce(direction * spitForce * 0.5f, ForceMode.Impulse);
                }

                // Add poison component
                ProjectilePoison poison = projectile.AddComponent<ProjectilePoison>();
                poison.poisonRadius = 3f;
                poison.poisonDuration = 5f;
                poison.damagePerSecond = 3f;
            }
        }

        PlayAttackSound();
    }

    void CrystalSpikeAttack()
    {
        // Create crystal spikes from the ground
        if (spitProjectile && target)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 spawnPos = target.position + new Vector3(
                    Random.Range(-3f, 3f),
                    0,
                    Random.Range(-3f, 3f)
                );

                GameObject spike = Instantiate(spitProjectile, spawnPos, Quaternion.identity);
                spike.transform.localScale = Vector3.one * 0.7f;

                // Make spike emerge from ground
                spike.transform.position = new Vector3(
                    spike.transform.position.x,
                    spike.transform.position.y - 2f,
                    spike.transform.position.z
                );

                // Add upward force
                Rigidbody spikeRb = spike.GetComponent<Rigidbody>();
                if (spikeRb)
                {
                    spikeRb.AddForce(Vector3.up * 15f, ForceMode.Impulse);
                }

                // Add damage component
                ProjectileExplosive explosive = spike.AddComponent<ProjectileExplosive>();
                explosive.explosionRadius = 2f;
                explosive.explosionForce = 5f;
                explosive.damage = 10f;
            }
        }

        PlayAttackSound();
    }

    void PlayAttackSound()
    {
        if (audioSource == null || !audioSource.isActiveAndEnabled || attackSounds.Length == 0) return;

        AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
        audioSource.PlayOneShot(clip, 0.7f);
    }


    void StartBeamAttack()
    {
        if (beamEffect && !isBeaming)
        {
            isBeaming = true;
            beamTimer = beamDuration;

            GameObject beamObj = Instantiate(beamEffect, firePoint.position, Quaternion.identity);
            beamObj.transform.SetParent(firePoint);
            beamRenderer = beamObj.GetComponent<LineRenderer>();

            PlayAttackSound();
        }
    }

    void UpdateBeamAttack()
    {
        if (beamRenderer && target)
        {
            beamRenderer.SetPosition(0, firePoint.position);
            beamRenderer.SetPosition(1, target.position);

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(beamDamage * Time.deltaTime);
            }

            GameObject impactEffect = GetPooledEffect();
            if (impactEffect)
            {
                impactEffect.transform.position = target.position;
                StartCoroutine(ReturnEffectAfterDelay(impactEffect, 0.1f));
            }
        }
    }

    void StopBeamAttack()
    {
        isBeaming = false;
        if (beamRenderer)
        {
            Destroy(beamRenderer.gameObject);
        }
    }

    void SwarmAttack()
    {
        // Create multiple projectiles in a spread pattern
        if (spitProjectile && firePoint)
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject projectile = Instantiate(spitProjectile, firePoint.position, firePoint.rotation);
                Rigidbody projRb = projectile.GetComponent<Rigidbody>();
                if (projRb && target)
                {
                    Vector3 direction = (target.position - firePoint.position).normalized;
                    // Add spread to the projectiles
                    direction += Random.insideUnitSphere * 0.3f;
                    direction.Normalize();

                    projRb.AddForce(direction * spitForce * 0.7f, ForceMode.Impulse);
                }
            }

            PlayAttackSound();
        }
    }

    void UpdateAnimation()
    {
        bodyWaveTimer += Time.deltaTime * stepSpeed * (currentVelocity.magnitude / maxMoveSpeed);

        if (bodySegments.Count > 0)
        {
            for (int i = 0; i < bodySegments.Count; i++)
            {
                float wave = Mathf.Sin(bodyWaveTimer + bodySegments[i].waveOffset) * wiggleIntensity;
                Vector3 newPos = bodySegments[i].baseLocalPosition + Vector3.right * wave;
                bodySegments[i].transform.localPosition = Vector3.Lerp(
                    bodySegments[i].transform.localPosition,
                    newPos,
                    Time.deltaTime * stepSpeed
                );
            }
        }
    }

    void UpdateLegMovement(Limb limb, float frequency)
    {
        limb.timer += Time.fixedDeltaTime * frequency;

        if (limb.timer >= 1f)
        {
            // Movement complete
            limb.isMoving = false;
            limb.liftProgress = 0f;
            return;
        }

        // Calculate parabolic lift
        float liftHeight = Mathf.Sin(limb.timer * Mathf.PI) * stepHeight * scale;
        limb.liftProgress = liftHeight / (stepHeight * scale);

        // Interpolate position with lift
        Vector3 newPosition = Vector3.Lerp(limb.startPosition, limb.targetPosition, limb.timer);
        newPosition += Vector3.up * liftHeight;

        limb.targetPosition = newPosition;
        limb.limbObject.transform.position = newPosition;

        // Animate leg joints
        UpdateLegJoints(limb);
    }

    void UpdateLegJoints(Limb limb)
    {
        if (limb.upperLeg && limb.lowerLeg && limb.foot)
        {
            float upperAngle = Mathf.Lerp(20f, 70f, limb.liftProgress);
            float lowerAngle = Mathf.Lerp(10f, 40f, limb.liftProgress);

            limb.upperLeg.localRotation = Quaternion.Euler(upperAngle, 0, 0);
            limb.lowerLeg.localRotation = Quaternion.Euler(lowerAngle, 0, 0);
        }
    }

    void ResetLegJoints(Limb limb)
    {
        if (limb.upperLeg && limb.lowerLeg && limb.foot)
        {
            limb.upperLeg.localRotation = Quaternion.Euler(30f, 0, 0);
            limb.lowerLeg.localRotation = Quaternion.Euler(20f, 0, 0);
        }
    }

    void UpdateTentacleMovement()
    {
        if (tentacles.Count == 0) return;

        for (int i = 0; i < tentacles.Count; i++)
        {
            if (tentacles[i].isGrabbing && tentacles[i].grabTarget != null)
            {
                UpdateTentacleGrab(i);
            }
            else
            {
                UpdateTentacleIdleMovement(i);
            }
        }
    }

    void UpdateTentacleIdleMovement(int tentacleIndex)
    {
        float wave = Mathf.Sin(Time.time * tentacleWaveSpeed + tentacles[tentacleIndex].waveOffset) *
                    tentacleWaveAmplitude * tentacles[tentacleIndex].flexibility;

        for (int j = 0; j < tentacles[tentacleIndex].segments.Count; j++)
        {
            Transform segment = tentacles[tentacleIndex].segments[j];
            float phase = j * TENTACLE_PHASE_MULTIPLIER;

            Vector3 newPos = tentacles[tentacleIndex].basePositions[j] +
                            new Vector3(Mathf.Sin(Time.time + phase) * wave, 0, Mathf.Cos(Time.time + phase) * wave);

            segment.localPosition = Vector3.Lerp(segment.localPosition, newPos, Time.deltaTime * TENTACLE_MOVE_SPEED);

            Quaternion newRot = Quaternion.Euler(
                Mathf.Sin(Time.time * 0.7f + phase) * 15f * tentacles[tentacleIndex].flexibility,
                Mathf.Cos(Time.time * 0.5f + phase) * 10f * tentacles[tentacleIndex].flexibility,
                Mathf.Sin(Time.time * 0.9f + phase) * 12f * tentacles[tentacleIndex].flexibility
            );

            segment.localRotation = Quaternion.Slerp(segment.localRotation, newRot, Time.deltaTime * TENTACLE_ROTATION_SPEED);
        }
    }

    void UpdateTentacleGrab(int tentacleIndex)
    {
        Tentacle tentacle = tentacles[tentacleIndex];
        tentacle.grabProgress += Time.deltaTime * tentacleGrabSpeed;

        if (tentacle.grabProgress >= 1f)
        {
            tentacle.isGrabbing = false;
            tentacle.grabProgress = 0f;

            IDamageable damageable = tentacle.grabTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(10f);
            }
        }
        else
        {
            StartCoroutine(AnimateTentacleGrab(tentacle));
        }
    }
    IEnumerator AnimateTentacleGrab(Tentacle tentacle)
    {
        Vector3 startPos = tentacle.segments[0].position;
        Vector3 endPos = tentacle.grabTarget.position;

        for (int i = 0; i < tentacle.segments.Count; i++)
        {
            Transform segment = tentacle.segments[i];
            float segmentProgress = tentacle.grabProgress * (i + 1) / tentacle.segments.Count;

            segment.position = Vector3.Lerp(startPos, endPos, segmentProgress);
            segment.rotation = Quaternion.LookRotation((endPos - segment.position).normalized);

            yield return null;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        GameObject damageEffectInstance = GetPooledEffect();
        if (damageEffectInstance)
        {
            damageEffectInstance.transform.position = transform.position;
            StartCoroutine(ReturnEffectAfterDelay(damageEffectInstance, 0.5f));
        }

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // Disable enemy components
        GetComponent<Collider>().enabled = false;
        rb.isKinematic = true;
        this.enabled = false;

        // Play death sound
        if (deathSounds.Length > 0)
        {
            AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(clip);
        }

        // Create death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Leave corpse
        if (leaveCorpse)
        {
            // Detach body parts
            foreach (Limb limb in limbs)
            {
                limb.limbObject.transform.SetParent(null);
                limb.limbObject.AddComponent<Rigidbody>();
                limb.limbObject.GetComponent<Collider>().enabled = true;
            }
            foreach (BodySegment segment in bodySegments)
            {
                segment.segmentObject.transform.SetParent(null);
                segment.segmentObject.AddComponent<Rigidbody>();
                segment.segmentObject.GetComponent<Collider>().enabled = true;
            }
            foreach (Tentacle tentacle in tentacles)
            {
                tentacle.tentacleObject.transform.SetParent(null);
                tentacle.tentacleObject.AddComponent<Rigidbody>();
                tentacle.tentacleObject.GetComponent<Collider>().enabled = true;
            }

            // Destroy main body after a delay
            Destroy(gameObject, corpseLifetime);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}