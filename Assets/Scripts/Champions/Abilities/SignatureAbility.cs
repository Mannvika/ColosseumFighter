using UnityEngine;
using System.Collections;


[CreateAssetMenu(fileName = "SignatureAbility", menuName = "Scriptable Objects/Ability/SignatureAbility")]
public abstract class SignatureAbility : AbilityBase
{
    public float maxCharge;
    public float chargePerSecond;
    public float chargePerDamageDealt;
    public float chargePerDamageTaken;
    public override void Activate(PlayerController parent, bool isServer)
    {
        if (parent.GetCurrentCharge() < maxCharge) return;
        
        parent.StartCoroutine(SignatureRoutine(parent, isServer));
    }

    public virtual IEnumerator SignatureRoutine(PlayerController parent, bool isServer)
    {
        yield return new WaitForFixedUpdate();
        EndAbility(parent, isServer);
    }


    public override void EndAbility(PlayerController parent, bool isServer)
    {
        if(isServer)
        {
            parent.ResetCharge();
        }
        parent.currentState = PlayerState.Normal;
        Debug.Log("Signature ability ended.");
    }
}
