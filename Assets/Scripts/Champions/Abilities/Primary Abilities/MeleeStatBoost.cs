using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/Abilities/MeleeStatBoost")]
public class MeleeStatBoost : AbilityBase
{
    public float damageMultiplier;
    public int duration;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent._abilitySystem.RegisterStatWait(this, StatType.Damage);
        parent.Stats.AddModifier(StatType.Damage, damageMultiplier, -1, duration);    
    }
    public override void OnEnd(PlayerController parent, bool isServer)
    {
        base.OnEnd(parent, isServer);
    }
}
