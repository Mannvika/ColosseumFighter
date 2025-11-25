using UnityEngine;

[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Ability/SignatureAbility")]
public class SignatureAbility : AbilityBase
{
    public float damage;
    public float range;

    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.UsingSignatureAbility;
        Debug.Log("Signature ability activated, dealing " + damage + " damage.");
        EndAbility(parent, isServer);
    }

    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Signature ability ended.");
    }
}
