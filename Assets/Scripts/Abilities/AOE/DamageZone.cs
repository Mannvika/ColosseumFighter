using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DamageZone : NetworkBehaviour
{
    [HideInInspector]
    public ulong CasterID;
    public float DamagePerTick;
    public float Duration;
    public float TickRate = 0.5f;

    private Dictionary<ulong, float> _victimTimers = new Dictionary<ulong, float>();

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            Destroy(gameObject, Duration);
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        List<ulong> keys = new List<ulong>(_victimTimers.Keys);
        foreach (ulong key in keys)
        {
            _victimTimers[key] -= Time.deltaTime;
        }            
    }

    public override void OnDestroy()
    {
        _victimTimers.Clear();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if(!IsServer) return;
        
        if(other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            if(player.OwnerClientId == CasterID) return;

            if(!_victimTimers.ContainsKey(player.OwnerClientId) || _victimTimers[player.OwnerClientId] <= 0)
            {
                ApplyDamage(player);
                _victimTimers[player.OwnerClientId] = TickRate;
            
            }
        }
    }

    private void ApplyDamage(PlayerController player)
    {
        player.GetComponent<Health>().TakeDamage(DamagePerTick);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(CasterID, out var client))
        {
            var ownerPlayer = client.PlayerObject.GetComponent<PlayerController>();
            if (ownerPlayer != null)
            {
                ownerPlayer.OnDamageDealt(DamagePerTick);
            }
        }
    }
}
