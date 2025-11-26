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
    public InputAction projAction;
    public InputAction primaryAction;
    public InputAction signatureAction;

    public PlayerNetworkInputData CurrentInput;

    private bool _meleePressedLastFrame = false;
    private bool _dashPressedLastFrame = false;
    private bool _primaryPressedLastFrame = false;
    private bool _signaturePressedLastFrame = false;
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
        if(primaryAction.WasPerformedThisFrame())
        {
            _primaryPressedLastFrame = true;
        }
        if(signatureAction.WasPerformedThisFrame())
        {
            _signaturePressedLastFrame = true;
        }

        CurrentInput = new PlayerNetworkInputData
        {
            Movement = moveAction.ReadValue<Vector2>(),
            IsMeleePressed = meleeAction.IsPressed() || _meleePressedLastFrame,
            IsBlockPressed = blockAction.IsPressed(),           
            IsDashPressed = dashAction.WasPerformedThisFrame() || _dashPressedLastFrame,
            IsPrimaryAbilityPressed = primaryAction.WasPerformedThisFrame() || _primaryPressedLastFrame,
            IsSignatureAbilityPressed = signatureAction.WasPerformedThisFrame() || _signaturePressedLastFrame,
            IsProjectilePressed = projAction.IsPressed(),
            MousePosition = GetMouseWorldPosition()
        };
    }

    public void ResetInputs()
    {
        _meleePressedLastFrame = false;
        _dashPressedLastFrame = false;
        _primaryPressedLastFrame = false;
        _signaturePressedLastFrame = false;
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
        projAction.Enable();
        primaryAction.Enable();
        signatureAction.Enable();
    }

    private void DisableInputs()
    {
        moveAction.Disable();
        meleeAction.Disable();
        blockAction.Disable();
        dashAction.Disable();
        projAction.Disable();
        primaryAction.Disable();
        signatureAction.Disable();
    }
    
    private void OnDisable()
    {
        if (IsOwner)
        {
            DisableInputs();
        }
    }
}