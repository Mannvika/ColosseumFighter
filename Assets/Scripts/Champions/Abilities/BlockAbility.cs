using UnityEngine;

[CreateAssetMenu(fileName = "BlockAbility", menuName = "Scriptable Objects/BlockAbility")]
public class BlockAbility : AbilityBase
{
    public override void Activate(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Blocking;
        Debug.Log("Block ability activated.");
    }

    public override void EndAbility(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        Debug.Log("Block ability ended.");
    }
}
