using UnityEngine;
using System.Collections;


[CreateAssetMenu(fileName = "GenericSignature", menuName = "Scriptable Objects/Abilities/GenericSignature")]
public class SignatureAbility : AbilityBase
{
    public float maxCharge;
    public float chargePerSecond;
    public float chargePerDamageDealt;
    public float chargePerDamageTaken;

    public AbilityBase abilityToCast;
    public override void Activate(PlayerController parent, bool isServer)
    {
        if (parent.Resources.SignatureCharge.Value < maxCharge)
        {
            if(parent.currentActiveAbility == this)
            {
                parent.currentActiveAbility = null;
                return;
            }
        }
        
        if(isServer)
        {
            parent.Resources.ResetSignatureCharge();
        }

        parent.currentActiveAbility = abilityToCast;

        abilityToCast.Activate(parent, isServer);
    }

    public override void OnEnd(PlayerController parent, bool isServer)
    {
        abilityToCast.OnEnd(parent, isServer);
    }
}
