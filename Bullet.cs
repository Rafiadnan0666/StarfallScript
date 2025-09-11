using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20000f;          
    public float damage = 10f;            
    public TargetType currentTarget;      
    public GameObject explodePrefab;     
    public Meledak explodeOnImpact;

    // Prefabs for impact effects and also the sfx
    [Header("Impact Effects")]
    [Tooltip("Prefab for explosion effect on impact")]

    public GameObject GroundIm;
    public AudioClip GroundImSfx;
    public GameObject PlayerImpact;
    public AudioClip PlayerImpactSfx;
    public GameObject EnemyImpact;
    public AudioClip EnemyImpactSfx;
    public GameObject MetalIm;
    public AudioClip MetalImSfx;
    public GameObject impactEffect;
    public GameObject shooter;

    [SerializeField] private AudioSource audioSource;

    public enum TargetType { Player, Enemy }
    public enum Meledak { Yes, No }

    private Rigidbody rb;

    void OnTriggerEnter(Collider other)
    {
        // Don't hit the shooter or other bullets
        if (other.gameObject == shooter || other.GetComponent<Bullet>() != null)
            return;

        // Apply damage to aircraft
        AdvancedFighterJet jet = other.GetComponent<AdvancedFighterJet>();
        if (jet != null)
        {
            Destroy(jet.gameObject);
        }

        // Damage player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by bullet!");
        }

        // Create impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Bullet requires a Rigidbody component!");
            Destroy(gameObject);
            return;
        }

        if (currentTarget == TargetType.Enemy)
        {
            SetVelocity(transform.forward);
        }
        else if (currentTarget == TargetType.Player)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Vector3 direction = (playerObject.transform.position - transform.position).normalized;
                SetVelocity(direction);
            }
            else
            {
                Debug.LogWarning("No object with tag 'Player' found.");
                Destroy(gameObject);
            }
        }

        // Destroy the bullet after 1 second if no collision occurs
        Destroy(gameObject, 1f);
    }

  

    private void SetVelocity(Vector3 direction)
    {
        rb.linearVelocity = direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet") && collision.gameObject != gameObject)
            return;

        if (explodeOnImpact == Meledak.Yes)
            CreateExplosion();

        if (collision.gameObject.CompareTag("Ground"))
        {
            Instantiate(GroundIm, transform.position, Quaternion.identity);
            if (GroundImSfx != null)
                AudioSource.PlayClipAtPoint(GroundImSfx, transform.position);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            Instantiate(PlayerImpact, transform.position, Quaternion.identity);
            if (PlayerImpactSfx != null)
                AudioSource.PlayClipAtPoint(PlayerImpactSfx, transform.position);
        }
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            Instantiate(EnemyImpact, transform.position, Quaternion.identity);
            if (EnemyImpactSfx != null)
                AudioSource.PlayClipAtPoint(EnemyImpactSfx, transform.position);
        }
        else
        {
            Instantiate(MetalIm, transform.position, Quaternion.identity);
            if (MetalImSfx != null)
                AudioSource.PlayClipAtPoint(MetalImSfx, transform.position);
        }

        Destroy(gameObject);
    }






    private void CreateExplosion()
    {
        if (explodePrefab != null)
        {
            Instantiate(explodePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Explosion prefab is not assigned.");
        }
    }
}


