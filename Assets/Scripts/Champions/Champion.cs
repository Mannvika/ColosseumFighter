using UnityEngine;

[CreateAssetMenu(fileName = "Champion", menuName = "Scriptable Objects/Champion")]
public class Champion : ScriptableObject
{
    public string championName;
    public float maxHealth;
    public float moveSpeed;
    public float blockMoveMultiplier;
    public float attackDamage;

    [Header("Abilities")]
    // Dash
    public AbilityBase dashAbility;
    // Block
    public AbilityBase blockAbility;
    // Melee Attack
    public AbilityBase meleeAttack;
    // E
    public AbilityBase primaryAbility;
    // Q
    public AbilityBase signatureAbility;
}
