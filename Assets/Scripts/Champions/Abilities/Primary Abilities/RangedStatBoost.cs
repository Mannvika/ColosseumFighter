using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack", menuName = "Scriptable Objects/Abilities/RangedStatBoost")]
public class RangedStatBoost : AbilityBase
{
    public float damageIncrease;
    public int numberOfShots;
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.rangedDamageIncrease = damageIncrease;
        parent.boostShotsRemaining = numberOfShots;
    }
    public override void EndAbility(PlayerController parent, bool isServer)
    {
        
    }
}
