using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkProjectile : NetworkBehaviour
{
    public NetworkVariable<float> speed = new NetworkVariable<float>(20f);
    public float damage = 10f;
    public float lifetime = 3f;

    // The ID of the player who shot this projectile    
    public ulong ShooterId;

    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = transform.up * speed.Value; 

        if (IsServer)
        {
            // Server schedules destruction after lifetime expires
            Destroy(gameObject, lifetime); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Prevent self-collision
        if (other.TryGetComponent<NetworkObject>(out NetworkObject obj))
        {
            if(obj.OwnerClientId == ShooterId) return;
        }

        // Apply damage if the other object is damageable
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(damage);
            
            GetComponent<NetworkObject>().Despawn(); 
        }
    }
}