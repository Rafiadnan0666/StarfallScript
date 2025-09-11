using UnityEngine;

public class ProjectilePoison : MonoBehaviour
{
    public float poisonRadius = 3f;
    public float poisonDuration = 5f;
    public float damagePerSecond = 3f;

    void OnCollisionEnter(Collision collision)
    {
        // Create poison cloud on impact
        CreatePoisonCloud();
        Destroy(gameObject);
    }

    void CreatePoisonCloud()
    {
        // Create a poison cloud that damages over time
        GameObject poisonCloud = new GameObject("PoisonCloud");
        poisonCloud.transform.position = transform.position;
        PoisonCloud cloud = poisonCloud.AddComponent<PoisonCloud>();
        cloud.poisonRadius = poisonRadius;
        cloud.poisonDuration = poisonDuration;
        cloud.damagePerSecond = damagePerSecond;
    }
}