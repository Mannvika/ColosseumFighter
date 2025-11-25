using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public abstract class AbilityBase : ScriptableObject
{
    public float cooldown;

    public abstract void Activate(PlayerController parent, bool isServer);

    public virtual void EndAbility(PlayerController parent, bool isServer)
    {
        // Default implementation (can be overridden)
    }
}
