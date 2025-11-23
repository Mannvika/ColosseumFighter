using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public abstract class AbilityBase : ScriptableObject
{
    public float cooldown;

    public abstract void Activate(PlayerController parent);

    public virtual void EndAbility(PlayerController parent)
    {
        // Default implementation (can be overridden)
    }
}
