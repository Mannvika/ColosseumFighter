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

        if (meleeAction.WasPerformedThisFrame()) _meleePressedLastFrame = true;
        if (dashAction.WasPerformedThisFrame()) _dashPressedLastFrame = true;
        if (primaryAction.WasPerformedThisFrame()) _primaryPressedLastFrame = true;
        if (signatureAction.WasPerformedThisFrame()) _signaturePressedLastFrame = true;

        bool isDash = dashAction.WasPerformedThisFrame() || _dashPressedLastFrame;
        bool isMelee = meleeAction.IsPressed() || _meleePressedLastFrame;
        bool isPrimary = primaryAction.WasPerformedThisFrame() || _primaryPressedLastFrame;
        bool isSignature = signatureAction.WasPerformedThisFrame() || _signaturePressedLastFrame;
        bool isProjectile = projAction.IsPressed(); 
        bool isBlock = blockAction.IsPressed();     

        // Mapping must match PlayerNetworkInputData helpers:
        // 0: Dash, 1: Melee, 2: Primary, 3: Sig, 4: Proj, 5: Block
        
        byte flags = 0;
        
        if (isDash)      flags |= (1 << 0);
        if (isMelee)     flags |= (1 << 1);
        if (isPrimary)   flags |= (1 << 2);
        if (isSignature) flags |= (1 << 3);
        if (isProjectile) flags |= (1 << 4);
        if (isBlock)     flags |= (1 << 5);

        CurrentInput = new PlayerNetworkInputData
        {
            Movement = moveAction.ReadValue<Vector2>(),
            MousePosition = GetMouseWorldPosition(),
            ButtonFlags = flags,
            Tick = 0
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
        if(_mainCamera == null) _mainCamera = Camera.main;

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

    private void OnEnable()
    {
        if (IsOwner)
        {
            EnableInputs();
        }
    }

    private void OnDisable()
    {
        if (IsOwner)
        {
            DisableInputs();
        }
    }
}