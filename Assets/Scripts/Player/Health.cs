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
        if(!IsServer) return;

        currentHealth.Value -= amount;

        // Temp get related player controller object and increase charge
        PlayerController parent = GetComponent<PlayerController>();
        if(parent != null && parent.championData.signatureAbility != null && parent.championData.signatureAbility != null)
        {
             parent.currentSignatureCharge.Value = Mathf.Min(parent.currentSignatureCharge.Value + amount * parent.championData.signatureAbility.chargePerDamageTaken, parent.championData.signatureAbility.maxCharge);
        }
        else
        {
            
        }

        Debug.Log($"[Server] {gameObject.name} took {amount} damage. HP: {currentHealth.Value}");

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
