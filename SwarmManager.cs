using System.Collections.Generic;
using UnityEngine;

public class SwarmManager : MonoBehaviour
{
    public static SwarmManager instance;

    public List<StarfallEnemy> swarmers = new List<StarfallEnemy>();
    public Dictionary<StarfallEnemy, SwarmerRole> swarmerRoles = new Dictionary<StarfallEnemy, SwarmerRole>();

    public enum SwarmerRole { Attacker, Distractor }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        AssignSwarmerRoles();
    }

    public void RegisterSwarmer(StarfallEnemy swarmer)
    {
        swarmers.Add(swarmer);
    }

    public void UnregisterSwarmer(StarfallEnemy swarmer)
    {
        swarmers.Remove(swarmer);
        swarmerRoles.Remove(swarmer);
    }

    void AssignSwarmerRoles()
    {
        // Every 5 seconds, re-assign roles
        if (Time.frameCount % 300 == 0)
        {
            // Find the closest swarmer to the player
            float closestDistance = Mathf.Infinity;
            StarfallEnemy closestSwarmer = null;
            foreach (StarfallEnemy swarmer in swarmers)
            {
                float distanceToPlayer = Vector3.Distance(swarmer.transform.position, Camera.main.transform.position);
                if (distanceToPlayer < closestDistance)
                {
                    closestDistance = distanceToPlayer;
                    closestSwarmer = swarmer;
                }
            }

            // Assign roles based on distance to the player
            foreach (StarfallEnemy swarmer in swarmers)
            {
                if (swarmer == closestSwarmer)
                {
                    swarmerRoles[swarmer] = SwarmerRole.Attacker;
                }
                else
                {
                    swarmerRoles[swarmer] = SwarmerRole.Distractor;
                }
            }
        }
    }
}
