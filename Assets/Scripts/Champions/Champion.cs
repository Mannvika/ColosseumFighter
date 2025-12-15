using UnityEngine;

[CreateAssetMenu(fileName = "Champion", menuName = "Scriptable Objects/Champion")]
public class Champion : ScriptableObject
{
    public string championName;
    public float maxHealth;
    public float moveSpeed;
    public float acceleration = 50f;


    [Header("Abilities")]
    public DashAbility dashAbility;
    public BlockAbility blockAbility;
    public AbilityBase meleeAttack;
    public AbilityBase primaryAbility;
    public SignatureAbility signatureAbility;
    public RangedAttack projectileAbility;
}
