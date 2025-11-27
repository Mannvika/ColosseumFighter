using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/RangedAttack")]
public class RangedAttack : AbilityBase
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public float fireRate;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Firing;

    }

    public void ProcessHold(PlayerController parent, bool isServer)
    {
        Debug.Log($"[Ability] Checking Cooldown... OnCooldown: {IsOnCooldown(parent)}");
        if(IsOnCooldown(parent)) return;

        SetCooldown(parent, fireRate);

        if(isServer)
        {
            Debug.Log("[Ability] SERVER Attempting to Spawn"); 
            SpawnProjectile(parent);
        }
        else{ Debug.Log("Shooting"); }
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

    private bool IsOnCooldown(PlayerController parent)
    {
        return parent.IsAbilityOnCooldown(this);
    }

    private void SetCooldown(PlayerController parent, float cooldown)
    {
        parent.SetAbilityCooldown(this, cooldown);
    }
}
