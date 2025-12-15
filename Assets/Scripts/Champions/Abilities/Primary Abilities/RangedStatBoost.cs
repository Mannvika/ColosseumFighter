using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/Abilities/RangedStatBoost")]
public class RangedStatBoost : AbilityBase
{
    public float damageMultiplier;
    public int numberOfShots;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.Stats.AddModifier(StatType.RangedDamage, damageMultiplier, -1, numberOfShots);    
    }
    public override void OnEnd(PlayerController parent, bool isServer)
    {
        
    }
}
