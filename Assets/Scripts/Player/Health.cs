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

        currentHealth.Value -= amount;

        // Temp get related player controller object and increase charge
        PlayerController parent = GetComponent<PlayerController>();
        if(parent != null && parent.championData.signatureAbility != null && parent.championData.signatureAbility != null)
        {
            parent.currentSignatureCharge.Value = Mathf.Min(parent.currentSignatureCharge.Value + amount * parent.championData.signatureAbility.chargePerDamageTaken, parent.championData.signatureAbility.maxCharge);
        }

        if (currentHealth.Value <= 0)
        {
            Debug.Log($"[Server] {gameObject.name} has DIED.");
        }
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"[Client] My health went from {oldHealth} to {newHealth}");
    }
}
