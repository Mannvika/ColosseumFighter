using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "DashAbility", menuName = "Scriptable Objects/DashAbility")]
public class DashAbility : AbilityBase
{
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;

    public override void Activate(PlayerController parent)
    {
        Vector2 dashDir = parent.CurrentMovementDirection.normalized;

        if (dashDir == Vector2.zero)
        {
            dashDir = parent.transform.up;
        }

        parent.currentState = PlayerState.Dashing;
        parent.StartCoroutine(DashRoutine(parent, dashDir));
    }


    IEnumerator DashRoutine(PlayerController parent, Vector2 dir)
    {
        Rigidbody2D rb = parent.GetComponent<Rigidbody2D>();
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {

            rb.linearVelocity = dir * dashSpeed; 
            
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        EndAbility(parent);
    }

    public override void EndAbility(PlayerController parent)
    {
        parent.currentState = PlayerState.Normal;
    }
}