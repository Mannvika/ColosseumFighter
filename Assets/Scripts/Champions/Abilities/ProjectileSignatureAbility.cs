using UnityEngine;
using Unity.Netcode;
using System.Collections;

[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Abilities/SignatureAbility/ProjectileSignature")]
public class ProjectileSignatureAbility : SignatureAbility
{
    public GameObject projectilePrefab;
    public float projectileSpeed;
    public float damage;
    public int numberOfProjectiles;
    public float timeBetweenProjectiles;

    public override IEnumerator SignatureRoutine(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.UsingSignatureAbility;
        for(int i = 0; i < numberOfProjectiles; i++)
        {
            if (isServer)
            {
                SpawnProjectile(parent);
            }
            yield return new WaitForSeconds(timeBetweenProjectiles);
        }
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
    
}
