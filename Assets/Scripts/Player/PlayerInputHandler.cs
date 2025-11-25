using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour
{
    [Header("Input Setup")]
    public InputAction moveAction;
    public InputAction meleeAction;
    public InputAction blockAction;
    public InputAction dashAction;
    
    public PlayerNetworkInputData CurrentInput;

    private bool _meleePressedLastFrame = false;
    private bool _dashPressedLastFrame = false;
    private Camera _mainCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) 
        {
            enabled = false;
            return; 
        }

        _mainCamera = Camera.main;
        EnableInputs();
    }

    private void Update()
    {
        if(!IsOwner) return;

        if (meleeAction.WasPerformedThisFrame())
        {
            _meleePressedLastFrame = true;
        }
        if(dashAction.WasPerformedThisFrame())
        {
            _dashPressedLastFrame = true;
        }

        CurrentInput = new PlayerNetworkInputData
        {
            Movement = moveAction.ReadValue<Vector2>(),
            IsMeleePressed = meleeAction.IsPressed() || _meleePressedLastFrame,
            IsBlockPressed = blockAction.IsPressed(),           
            IsDashPressed = dashAction.WasPerformedThisFrame() || _dashPressedLastFrame,
            MousePosition = GetMouseWorldPosition()
        };
    }

    public void ResetInputs()
    {
        _meleePressedLastFrame = false;
        _dashPressedLastFrame = false;
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (Mouse.current == null || _mainCamera == null) return Vector2.zero;
        return _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    private void EnableInputs()
    {
        moveAction.Enable();
        meleeAction.Enable();
        blockAction.Enable();
        dashAction.Enable();
    }

    private void DisableInputs()
    {
        moveAction.Disable();
        meleeAction.Disable();
        blockAction.Disable();
        dashAction.Disable();
    }
    
    private void OnDisable()
    {
        if (IsOwner)
        {
            DisableInputs();
        }
    }
}