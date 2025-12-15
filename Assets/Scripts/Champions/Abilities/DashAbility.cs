using UnityEngine;

[CreateAssetMenu(fileName = "DashAbility", menuName = "Scriptable Objects/DashAbility")]
public class DashAbility : AbilityBase
{
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;

    public override void Activate(PlayerController parent, bool isServer)
    {
        Vector2 dashDir = parent.CurrentMovementDirection;
        if(dashDir == Vector2.zero)
        {
            dashDir = parent.transform.up;
        }

        parent.SetDashDirection(dashDir.normalized);

        parent.currentState = PlayerState.Dashing;
    }

    public override void OnEnd(PlayerController parent, bool isServer)
    {
        parent.currentState = PlayerState.Normal;
        base.OnEnd(parent, isServer);
    }
}