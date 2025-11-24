using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
public enum PlayerState
{
    Normal,
    Attacking,
    Dashing,
    Blocking,
    UsingPrimaryAbility,
    UsingSignatureAbility,
    Stunned
}

public class PlayerController : NetworkBehaviour
{
    [Header("Champion")]
    public Champion championData;

    [Header("State")]
    public PlayerState currentState = PlayerState.Normal;
    private Camera mainCamera;
    private Rigidbody2D rb;

    [Header("Input Setup")]
    public InputAction moveAction;
    public InputAction dashAction;
    public InputAction blockAction;
    public InputAction primaryAbilityAction;
    public InputAction signatureAbilityAction;
    public InputAction meleeAction;

    private Vector2 moveInput;
    public Vector2 MoveInput => moveInput;
    public Vector2 mousePos;

    private Dictionary<AbilityBase, float> cooldowns = new Dictionary<AbilityBase, float>();

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
        blockAction.Enable();
        primaryAbilityAction.Enable();
        signatureAbilityAction.Enable();
        meleeAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
        blockAction.Disable();
        primaryAbilityAction.Disable();
        signatureAbilityAction.Disable();
        meleeAction.Disable();
    }

    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        mainCamera = Camera.main;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        if (moveAction.bindings.Count == 0)
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }

        meleeAction.AddBinding("<Mouse>/leftButton");
        meleeAction.performed += ctx => TryUseAbility(championData.meleeAttack, PlayerState.Normal); // Usually doesn't lock movement, but you can change state if desired

        // Block (Right Click - Hold)
        blockAction.AddBinding("<Mouse>/rightButton");
        blockAction.performed += ctx => TryUseAbility(championData.blockAbility, PlayerState.Blocking);
        blockAction.canceled += ctx => 
        { 
            if(currentState == PlayerState.Blocking)
            {
                if(championData.blockAbility != null) championData.blockAbility.EndAbility(this); 
                currentState = PlayerState.Normal;
            }
        };

        // Dash (Space)
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.performed += ctx => TryUseAbility(championData.dashAbility, PlayerState.Dashing);

        // Primary (E)
        primaryAbilityAction.AddBinding("<Keyboard>/e");
        primaryAbilityAction.performed += ctx => TryUseAbility(championData.primaryAbility, PlayerState.UsingPrimaryAbility);

        // Signature (Q)
        signatureAbilityAction.AddBinding("<Keyboard>/q");
        signatureAbilityAction.performed += ctx => TryUseAbility(championData.signatureAbility, PlayerState.UsingSignatureAbility);
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;

        moveInput = moveAction.ReadValue<Vector2>();

        if(Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            mousePos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }
    }

    void FixedUpdate()
    {
        if(!IsOwner) return;
        if (currentState == PlayerState.Normal)
        {
            rb.linearVelocity = moveInput * championData.moveSpeed;
            RotatePlayerTowardsMouse();
        }
        else if (currentState == PlayerState.Blocking)
        {
            rb.linearVelocity = moveInput * (championData.moveSpeed * championData.blockMoveMultiplier);
            RotatePlayerTowardsMouse();
        }
        else if (currentState == PlayerState.Dashing)
        {
            // Movement handled by Dash Ability
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void RotatePlayerTowardsMouse()
    {
        Vector2 lookDir = mousePos - rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
    }

    public void TryUseAbility(AbilityBase ability, PlayerState activeState)
    {
        if (ability == null) return;
        
        if (!CanTransitionTo(activeState)) return;
        if (IsAbilityOnCooldown(ability)) return;

        if (currentState == PlayerState.Blocking && activeState != PlayerState.Blocking)
        {
            championData.blockAbility.EndAbility(this);
        }

        if (activeState != PlayerState.Normal)
        {
            currentState = activeState;
        }

        ability.Activate(this);
        cooldowns[ability] = Time.time + ability.cooldown;    
    }

    private bool IsAbilityOnCooldown(AbilityBase ability)
    {
        if (cooldowns.ContainsKey(ability))
        {
            return Time.time < cooldowns[ability];
        }
        return false;
    }

    private bool CanTransitionTo(PlayerState newState)
    {
        if (newState == PlayerState.Normal) return true;

        switch (currentState)
        {
            case PlayerState.Normal:
                return true;

            case PlayerState.Blocking:
                if (newState == PlayerState.Dashing) return false;
                if(newState == PlayerState.Blocking) return true;
                if (newState == PlayerState.UsingPrimaryAbility) return true;
                if (newState == PlayerState.UsingSignatureAbility) return true;
                return false;

            case PlayerState.Dashing:
            case PlayerState.UsingPrimaryAbility:
            case PlayerState.UsingSignatureAbility:
            case PlayerState.Stunned:
                return false;
        }

        return false;
    }
}
