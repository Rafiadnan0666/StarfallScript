using UnityEngine;

// Additional class for blast projectiles
public class BlastProjectile : MonoBehaviour
{
    private Vector3 target;
    private float damage;
    private float radius;
    private AdvancedFighterJet.Team team;
    private float speed = 100f;

    public void Initialize(Vector3 targetPosition, float blastDamage, float blastRadius, AdvancedFighterJet.Team projectileTeam)
    {
        target = targetPosition;
        damage = blastDamage;
        radius = blastRadius;
        team = projectileTeam;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 5f)
        {
            Explode();
        }
    }

    void Explode()
    {
        // Create explosion effect
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in colliders)
        {
            AdvancedFighterJet jet = hit.GetComponent<AdvancedFighterJet>();
            if (jet != null && jet.team != team)
            {
                jet.health -= damage * (1f - Vector3.Distance(transform.position, hit.transform.position) / radius);
            }
        }

        Destroy(gameObject);
    }
}
