using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;
    private PlayerController _controller;
    private Vector2 _dashDirection;
    private int _dashStartTick;

    public void Initialize(PlayerController controller, Rigidbody2D rb)
    {
        _controller = controller;
        _rb = rb;
    }

    public void RotateTowards(Vector2 mousePos)
    {
        if (_controller.currentState == PlayerState.Dashing || 
            _controller.currentState == PlayerState.Stunned) return;

        if (Vector2.Distance(mousePos, _rb.position) < 0.3f) return;

        Vector2 lookDir = mousePos - _rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        _rb.rotation = angle;
    }

    public void StartDash(Vector2 inputDirection)
    {
        _dashDirection = inputDirection == Vector2.zero ? (Vector2)_controller.transform.up : inputDirection.normalized;
        _dashStartTick = _controller.CurrentTick;
    }

    public void TickPhysics(PlayerNetworkInputData input, float speedMultiplier, bool stopMovement)
    {
        Vector2 targetVelocity = Vector2.zero;
        float currentAccel = _controller.championData.acceleration;
        float finalMoveSpeed = _controller.championData.moveSpeed * speedMultiplier;

        if (stopMovement)
        {
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, Vector2.zero, 10000f * Time.fixedDeltaTime);
            return;
        }

        if (_controller.currentState == PlayerState.Dashing)
        {
            float duration = _controller.championData.dashAbility.dashDuration;
            float dashProgress = (float)(_controller.CurrentTick - _dashStartTick) / (duration / Time.fixedDeltaTime);
            
            float currentDashSpeed = Mathf.Lerp(_controller.championData.dashAbility.dashSpeed, 0f, dashProgress);
            
            targetVelocity = _dashDirection * currentDashSpeed;
            currentAccel = 100000f; 
        }
        else 
        {
            if (input.Movement == Vector2.zero)
            {
                currentAccel = 10000f;
            }
            else
            {
                targetVelocity = input.Movement * finalMoveSpeed;
            }
        }

        _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, currentAccel * Time.fixedDeltaTime);
    }

    public bool IsDashFinished(int currentTick, float dashDuration, float tickRate)
    {
        int durationInTicks = Mathf.CeilToInt(dashDuration / tickRate);
        return currentTick >= _dashStartTick + durationInTicks;
    }
}