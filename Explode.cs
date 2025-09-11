using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explode : MonoBehaviour
{
    public float fieldOfImpact;
    public float explosionForce;
    public GameObject explosion;

    private void OnCollisionEnter(Collision collision)
    {
        Explosion();
        Destroy(gameObject);
    }

    private void Explosion()
    {
        GameObject _explosion = Instantiate(explosion, transform.position, transform.rotation);
        Collider[] colliders = Physics.OverlapSphere(transform.position, fieldOfImpact);
        foreach (Collider target in colliders)
        {
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, fieldOfImpact);
            }
        }
        Destroy(_explosion, 3f);
    }
}
