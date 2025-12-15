using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public abstract class AbilityBase : ScriptableObject
{
    public float cooldown;
    public bool stopMovementOnActivate;
    public bool allowRotation = true;
    public float moveSpeedMultiplier = 1.0f;
    public bool startCooldownOnEnd = false;

    public abstract void Activate(PlayerController parent, bool isServer);

    public virtual void ProcessHold(PlayerController parent, bool isServer)
    {
        
    }

    public virtual void OnEnd(PlayerController parent, bool isServer)
    {
        if(parent.currentActiveAbility == this)
        {
            parent.currentActiveAbility = null;
            parent.currentState = PlayerState.Normal;
        }
    }
}
