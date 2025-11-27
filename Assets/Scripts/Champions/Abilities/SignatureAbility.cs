using UnityEngine;

[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Ability/SignatureAbility")]
public class SignatureAbility : AbilityBase
{
    public float damage;
    public float range;
    public float maxCharge;
    public float chargePerSecond;
    public float chargePerDamageDealt;
    public float chargePerDamageTaken;

    public override void Activate(PlayerController parent, bool isServer)
    {
        if (parent.GetCurrentCharge() < maxCharge) return;

        parent.currentState = PlayerState.UsingSignatureAbility;
        Debug.Log("Signature ability activated, dealing " + damage + " damage.");
        
        // TODO: Implement signature ability logic here.

        if (isServer) parent.ResetCharge();

        EndAbility(parent, isServer);
    }

    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Signature ability ended.");
    }
}
