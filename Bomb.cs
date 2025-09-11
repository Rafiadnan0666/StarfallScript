using UnityEngine;

public class Bomb : MonoBehaviour
{
    public AdvancedFighterJet.Team releaseTeam;
    public float explosionRadius = 30f;
    public float damage = 200f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public AudioClip whistleSound;

    private Rigidbody rb;
    private bool hasExploded = false;
    private AudioSource audioSource;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Start with forward velocity from aircraft
        rb.linearVelocity = transform.forward * 50f;

        // Play whistle sound as bomb falls
        if (whistleSound != null && audioSource != null)
        {
            audioSource.clip = whistleSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Apply gravity
        rb.AddForce(Physics.gravity * rb.mass);

        // Increase whistle pitch as bomb accelerates
        if (audioSource != null)
        {
            audioSource.pitch = 0.8f + Mathf.Clamp01(rb.linearVelocity.magnitude / 100f) * 0.5f;
        }

        // Orient with velocity
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        Explode();
    }

    void Explode()
    {
        hasExploded = true;

        // Create explosion effect
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        // Damage nearby objects
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            AdvancedFighterJet jet = hit.GetComponent<AdvancedFighterJet>();
            if (jet != null && jet.team != releaseTeam)
            {
                // Apply damage to aircraft
                Destroy(jet.gameObject);
            }

            // Damage player if nearby
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Player damaged by bomb blast!");
            }

            // You could add terrain deformation here
        }

        // Hide the bomb
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Stop whistle sound
        if (audioSource != null) audioSource.Stop();

        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}