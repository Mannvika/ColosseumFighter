using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/RangedAttack")]
public class RangedAttack : AbilityBase
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public float moveMultiplier;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Firing;

        if(IsOnCooldown(parent)) return;


        if (isServer)
        {
            SpawnProjectile(parent);
        }

        SetCooldown(parent);
    }

    public override void ProcessHold(PlayerController parent, bool isServer)
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

        projectile.GetComponent<NetworkObject>().Spawn();

        proj.ShooterId = parent.OwnerClientId;
        proj.speed.Value = projectileSpeed;
        proj.damage = damage;

        if(projectile.TryGetComponent<Rigidbody2D>(out Rigidbody2D rigidbody))
        {
            rigidbody.linearVelocity = parent.transform.up * projectileSpeed;
        }
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
