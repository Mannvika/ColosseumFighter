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

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : NetworkBehaviour
{
    [Header("Champion")]
    public Champion championData;

    [Header("State")]
    public PlayerState currentState = PlayerState.Normal;
    private PlayerInputHandler _inputHandler;
    private Rigidbody2D rb;

    [HideInInspector] public Vector2 CurrentMovementDirection;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        GetComponent<Health>().currentHealth.Value = championData.maxHealth;
    }

    private Dictionary<AbilityBase, float> cooldowns = new Dictionary<AbilityBase, float>();

    void FixedUpdate()
    {
        if(!IsOwner) return;

        ProcessPlayerInput(_inputHandler.CurrentInput);
        _inputHandler.ResetInputs();
    }

    public void ProcessPlayerInput(PlayerNetworkInputData input)
    {
        CurrentMovementDirection = input.Movement;
        RotatePlayerTowardsMouse(input.MousePosition);

        if (currentState == PlayerState.Normal)
        {
            rb.linearVelocity = input.Movement * championData.moveSpeed;
            if (input.IsBlockPressed)
            {
                TryUseAbility(championData.blockAbility, PlayerState.Blocking);
            }
            if (input.IsDashPressed)
            {
                TryUseAbility(championData.dashAbility, PlayerState.Dashing);
            }
            if (input.IsMeleePressed)
            {
                TryUseAbility(championData.meleeAttack, PlayerState.Normal);
            }
        }
        else if (currentState == PlayerState.Blocking)
        {
            rb.linearVelocity = input.Movement * (championData.moveSpeed * championData.blockMoveMultiplier);
            if(!input.IsBlockPressed)
            {
                if(championData.blockAbility != null) championData.blockAbility.EndAbility(this, IsServer); 
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void RotatePlayerTowardsMouse(Vector2 mousePos)
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
            championData.blockAbility.EndAbility(this, IsServer);
        }

        if (activeState != PlayerState.Normal)
        {
            currentState = activeState;
        }

        ability.Activate(this, IsServer);
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
                if (newState == PlayerState.Blocking) return true;
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
