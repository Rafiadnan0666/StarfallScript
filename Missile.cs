using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public AdvancedFighterJet.Team launchTeam;

    [Header("Performance")]
    public float speed = 100f;
    public float acceleration = 50f;
    public float turnRate = 90f;
    public float maxLifetime = 30f;

    [Header("Warhead")]
    public float explosionRadius = 20f;
    public float damage = 100f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    [Header("Visual Effects")]
    public TrailRenderer trail;

    // Internal components
    private Rigidbody rb;
    private bool isActive = true;
    private float activationTime;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        activationTime = Time.time;
        currentSpeed = speed * 0.5f; // Start slower then accelerate

        // Setup visual effects
        if (trail != null)
        {
            trail.startColor = launchTeam == AdvancedFighterJet.Team.Red ?
                new Color(1f, 0.3f, 0.2f, 0.8f) : new Color(0.2f, 0.3f, 1f, 0.8f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);
        }

        // Self-destruct after max lifetime
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (!isActive) return;

        // Accelerate over time
        currentSpeed = Mathf.Min(speed, currentSpeed + acceleration * Time.deltaTime);

        if (target != null && target.gameObject.activeInHierarchy)
        {
            // Calculate lead for targeting (predict target position)
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 targetVelocity = targetRb.linearVelocity;
                float timeToIntercept = Vector3.Distance(transform.position, target.position) / currentSpeed;
                Vector3 predictedPosition = target.position + targetVelocity * timeToIntercept;

                // Turn toward predicted position
                Quaternion targetRotation = Quaternion.LookRotation((predictedPosition - transform.position).normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.deltaTime);
            }
            else
            {
                // Simple targeting for non-rigidbody targets
                Quaternion targetRotation = Quaternion.LookRotation((target.position - transform.position).normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnRate * Time.deltaTime);
            }
        }
        else
        {
            // If no target, continue straight
            rb.linearVelocity = transform.forward * currentSpeed;
            return;
        }

        // Move forward
        rb.linearVelocity = transform.forward * currentSpeed;

        // Update visual effects based on speed
        if (trail != null)
        {
            trail.widthMultiplier = Mathf.Clamp01(currentSpeed / speed) * 0.3f;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Don't collide with objects from the same team
        AdvancedFighterJet jet = other.GetComponent<AdvancedFighterJet>();
        if (jet != null && jet.team == launchTeam) return;

        // Don't collide with other missiles
        if (other.GetComponent<Missile>() != null) return;

        Explode();
    }

    void Explode()
    {
        isActive = false;

        // Create explosion effect
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 0.7f);
        }

        // Damage nearby objects
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            AdvancedFighterJet jet = hit.GetComponent<AdvancedFighterJet>();
            if (jet != null && jet.team != launchTeam)
            {
                // Apply damage to aircraft
                Destroy(jet.gameObject);
            }

            // Damage player if nearby
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Player damaged by missile explosion!");
            }
        }

        // Disable visuals but keep the gameobject for a moment
        if (trail != null) trail.autodestruct = true;

        // Hide the missile mesh
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Destroy after a short delay
        Destroy(gameObject, 2f);
    }
}