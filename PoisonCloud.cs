using UnityEngine;

public class PoisonCloud : MonoBehaviour
{
    public float poisonRadius = 3f;
    public float poisonDuration = 5f;
    public float damagePerSecond = 3f;

    private float timer;

    void Start()
    {
        timer = poisonDuration;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }

        // Damage players in the cloud
        Collider[] colliders = Physics.OverlapSphere(transform.position, poisonRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                col.GetComponent<Player>().TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}