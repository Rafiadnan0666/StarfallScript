using UnityEngine;

public class ProjectileExplosive : MonoBehaviour
{
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public float damage = 50f;

    void OnCollisionEnter(Collision collision)
    {
        // Explode on impact
        Explode();
    }

    void Explode()
    {
        // Create explosion effect
        // Find all colliders in the explosion radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // Apply damage and force to all colliders
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            // Apply damage to player
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Player>().TakeDamage(damage);
            }
        }

        // Destroy the projectile
        Destroy(gameObject);
    }
}