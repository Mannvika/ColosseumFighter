using UnityEngine;
using Unity.Netcode;

public class Health : NetworkBehaviour, IDamageable
{
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    public void TakeDamage(float amount)
    {
        // Only the server should modify health
        if(!IsServer) return;
        PlayerController parent = GetComponent<PlayerController>();

        currentHealth.Value -= amount * parent.Stats.GetStat(StatType.Defense, 1.0f);

        // Temp get related player controller object and increase charge
        if(parent != null && parent.championData != null && parent.championData.signatureAbility != null)
        {
            parent.Resources.SignatureCharge.Value = Mathf.Min(parent.Resources.SignatureCharge.Value + amount * parent.championData.signatureAbility.chargePerDamageTaken, parent.championData.signatureAbility.maxCharge);
        }

        if (currentHealth.Value <= 0)
        {
            GameManager.instance.OnPlayerDied(OwnerClientId);
            Debug.Log($"[Server] {gameObject.name} has DIED.");
        }
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"[Client] My health went from {oldHealth} to {newHealth}");
    }
}
