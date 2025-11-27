using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/Abilities/ProjectileAbility")]
public class ProjectileAbility : AbilityBase
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.UsingPrimaryAbility;
        if(isServer)
        {
            Debug.Log("[Ability] SERVER Attempting to Spawn"); 
            SpawnProjectile(parent);
        }
        else{ Debug.Log("Shot Projectile"); }
        EndAbility(parent, isServer);
    }

    private void SpawnProjectile(PlayerController parent)
    {
        Vector2 spawnPosition = (Vector2)parent.transform.position + ((Vector2)parent.transform.up * 1f);
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, parent.transform.rotation);
        projectile.GetComponent<NetworkProjectile>().speed = projectileSpeed;
        projectile.GetComponent<NetworkProjectile>().damage = damage;
        projectile.GetComponent<NetworkObject>().Spawn();
    }
    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Projectile ability ended.");
    }
}
