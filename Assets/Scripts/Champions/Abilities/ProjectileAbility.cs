using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileAbility", menuName = "Scriptable Objects/Ability/ProjectileAbility")]
public class ProjectileAbility : AbilityBase
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Firing;
        /*GameObject projectile = Instantiate(projectilePrefab, parent.transform.position + parent.transform.forward, Quaternion.identity);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = parent.transform.forward * projectileSpeed;
        }*/
        Debug.Log("Launching a projectile dealing " + damage + " damage.");
    }

    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Projectile ability ended.");
    }
}
