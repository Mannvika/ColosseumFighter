using UnityEngine;

[CreateAssetMenu(fileName = "BlockAbility", menuName = "Scriptable Objects/BlockAbility")]
public class BlockAbility : AbilityBase
{

    public float moveMultiplier;
    public float damageMultiplier;
    public float maxCharge;
    public float chargePerSecond;
    public float dischargePerSecond;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Blocking;
        
        if(parent.Visuals != null)
        {
            parent.Visuals.ChangeColor(parent.Visuals.blockingColor);
        }
    }

    public override void OnEnd(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        // Debug.Log("Block ability ended.");
        parent.Visuals.ChangeColor(parent.Visuals.InitialColor);
        base.OnEnd(parent, isServer);
    }
}
