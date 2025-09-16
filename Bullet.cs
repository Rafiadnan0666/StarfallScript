using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour, IPooledObject
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
    private Coroutine deactivateCoroutine;

    public void OnObjectSpawn()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Bullet requires a Rigidbody component!");
            gameObject.SetActive(false);
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
                gameObject.SetActive(false);
            }
        }

        // Deactivate the bullet after 1 second if no collision occurs
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
        }
        deactivateCoroutine = StartCoroutine(DeactivateAfterTime(1f));
    }

    private IEnumerator DeactivateAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }

    private void SetVelocity(Vector3 direction)
    {
        rb.linearVelocity = direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Don't hit the shooter or other bullets
        if (other.gameObject == shooter || other.GetComponent<Bullet>() != null)
            return;

        // Apply damage to aircraft
        AdvancedFighterJet jet = other.GetComponent<AdvancedFighterJet>();
        if (jet != null)
        {
            // Assuming the jet has a TakeDamage method
            // jet.TakeDamage(damage);
            Destroy(jet.gameObject); // Keeping original logic for now
        }

        // Damage player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit by bullet!");
            // Assuming player has a TakeDamage method
            // other.GetComponent<Player>().TakeDamage(damage);
        }

        // Create impact effect from pool
        if (impactEffect != null)
        {
            PoolManager.Instance.SpawnFromPool("impact", transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    { 
        if (collision.gameObject.CompareTag("Bullet") && collision.gameObject != gameObject)
            return;

        if (explodeOnImpact == Meledak.Yes)
            CreateExplosion();

        if (collision.gameObject.CompareTag("Ground"))
        {
            if (GroundIm != null)
                PoolManager.Instance.SpawnFromPool("groundImpact", transform.position, Quaternion.identity);
            if (GroundImSfx != null)
                AudioSource.PlayClipAtPoint(GroundImSfx, transform.position);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            if (PlayerImpact != null)
                PoolManager.Instance.SpawnFromPool("playerImpact", transform.position, Quaternion.identity);
            if (PlayerImpactSfx != null)
                AudioSource.PlayClipAtPoint(PlayerImpactSfx, transform.position);
        }
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            if (EnemyImpact != null)
                PoolManager.Instance.SpawnFromPool("enemyImpact", transform.position, Quaternion.identity);
            if (EnemyImpactSfx != null)
                AudioSource.PlayClipAtPoint(EnemyImpactSfx, transform.position);
        }
        else
        {
            if (MetalIm != null)
                PoolManager.Instance.SpawnFromPool("metalImpact", transform.position, Quaternion.identity);
            if (MetalImSfx != null)
                AudioSource.PlayClipAtPoint(MetalImSfx, transform.position);
        }

        gameObject.SetActive(false);
    }

    private void CreateExplosion()
    {
        if (explodePrefab != null)
        {
            PoolManager.Instance.SpawnFromPool("explosion", transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Explosion prefab is not assigned.");
        }
    }
}