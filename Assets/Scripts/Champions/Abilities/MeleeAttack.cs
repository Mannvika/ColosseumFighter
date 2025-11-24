using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Scriptable Objects/MeleeAttack")]
public class MeleeAttack : AbilityBase
{
    public float damage;
    public float range;
    public float attackSpeed;
    public override void Activate(PlayerController parent)
    {
        parent.currentState = PlayerState.Attacking;
        Debug.Log("Did a melee attack dealing " + damage + " damage.");
        EndAbility(parent);
    }

    public override void EndAbility(PlayerController parent)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Melee attack ended.");
    }
}
