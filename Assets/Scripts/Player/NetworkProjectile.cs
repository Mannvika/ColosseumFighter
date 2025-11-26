using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkProjectile : NetworkBehaviour
{
    public float speed = 20f;
    public float damage = 10f;
    public float lifetime = 3f;

    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();

        if (IsServer)
        {
            rb.linearVelocity = transform.up * speed; 
            
            Destroy(gameObject, lifetime); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(damage);
            
            GetComponent<NetworkObject>().Despawn(); 
        }
    }
}