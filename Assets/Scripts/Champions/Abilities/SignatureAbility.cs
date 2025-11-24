using UnityEngine;

[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Ability/SignatureAbility")]
public class SignatureAbility : AbilityBase
{
    public float damage;
    public float range;

    public override void Activate(PlayerController parent)
    {
        parent.currentState = PlayerState.UsingSignatureAbility;
        Debug.Log("Signature ability activated, dealing " + damage + " damage.");
        EndAbility(parent);
    }

    public override void EndAbility(PlayerController parent)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Signature ability ended.");
    }
}
