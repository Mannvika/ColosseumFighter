using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/RangedAttack")]
public class RangedAttack : AbilityBase
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Firing;
        if (isServer)
        {
            SpawnProjectile(parent);
        }
    }

    public void ProcessHold(PlayerController parent, bool isServer)
    {
        if(IsOnCooldown(parent)) return;

        SetCooldown(parent);

        if(isServer)
        {
            SpawnProjectile(parent);
        }
    }

    private void SpawnProjectile(PlayerController parent)
    {
        Vector2 spawnPosition = (Vector2)parent.transform.position + ((Vector2)parent.transform.up * 1f);
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, parent.transform.rotation);
        NetworkProjectile proj = projectile.GetComponent<NetworkProjectile>();
        proj.ShooterId = parent.OwnerClientId;
        proj.speed.Value = projectileSpeed;
        proj.damage = damage;
        projectile.GetComponent<NetworkObject>().Spawn();
    }
    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        // Debug.Log("Projectile ability ended.");
    }

    private bool IsOnCooldown(PlayerController parent)
    {
        return parent.IsAbilityOnCooldown(this);
    }

    private void SetCooldown(PlayerController parent)
    {
        parent.SetAbilityCooldown(this);
    }
}
