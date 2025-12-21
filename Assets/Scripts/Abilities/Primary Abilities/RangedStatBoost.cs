using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/Abilities/RangedStatBoost")]
public class RangedStatBoost : AbilityBase
{
    public float damageMultiplier;
    public int numberOfShots;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.AbilitySystem.RegisterStatWait(this, StatType.RangedDamage);
        parent.Stats.AddModifier(StatType.RangedDamage, damageMultiplier, numberOfShots, -1);    
    }
    public override void OnEnd(PlayerController parent, bool isServer)
    {
        base.OnEnd(parent, isServer);
    }
}
