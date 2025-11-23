using UnityEngine;

[CreateAssetMenu(fileName = "DashAbility", menuName = "Scriptable Objects/DashAbility")]
public class DashAbility : AbilityBase
{
    public float dashDistance = 5f;
    public float dashTime = 0.2f;

    public override void Activate(PlayerController parent)
    {
        parent.StartCoroutine(DashCoroutine(parent));
    }

    public override void EndAbility(PlayerController parent)
    {
        parent.currentState = PlayerState.Normal;
    }

    System.Collections.IEnumerator DashCoroutine(PlayerController parent)
    {
        Vector2 dashDirection = parent.MoveInput.normalized;
        if (dashDirection == Vector2.zero)
        {
            dashDirection = Vector2.up;
        }

        Vector2 startPosition = parent.transform.position;
        Vector2 targetPosition = startPosition + dashDirection * dashDistance;
        float elapsedTime = 0f;

        while (elapsedTime < dashTime)
        {
            parent.transform.position = Vector2.Lerp(startPosition, targetPosition, (elapsedTime / dashTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        parent.transform.position = targetPosition;
        EndAbility(parent);
    }

}
