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
    Firing,
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

    [HideInInspector] 
    public Vector2 CurrentMovementDirection;

    public NetworkVariable<float> currentSignatureCharge = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

        if(championData != null && championData.signatureAbility != null)
        {
            if(!IsServer) return;
            currentSignatureCharge.Value = Mathf.Min(currentSignatureCharge.Value + championData.signatureAbility.chargePerSecond * Time.deltaTime, championData.signatureAbility.maxCharge);
            
        }

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
            if (input.IsProjectilePressed)
            {
                TryUseAbility(championData.projectileAbility, PlayerState.Firing);
            }
            if(input.IsPrimaryAbilityPressed)
            {
                TryUseAbility(championData.primaryAbility, PlayerState.UsingPrimaryAbility);
            }
            if(input.IsSignatureAbilityPressed)
            {
                TryUseAbility(championData.signatureAbility, PlayerState.UsingSignatureAbility);
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
        else if (currentState == PlayerState.Firing)
        {
            
            rb.linearVelocity = input.Movement * (championData.moveSpeed * championData.fireMoveMultiplier);
            Debug.Log($"[Controller] State is Firing. Input: {input.IsProjectilePressed}");
            if(championData.projectileAbility != null)
            {
                championData.projectileAbility.ProcessHold(this, IsServer);
            }
            if (!input.IsProjectilePressed)
            {
                if(championData.projectileAbility != null) championData.projectileAbility.EndAbility(this, IsServer);
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

        ability.Activate(this, IsServer);
        cooldowns[ability] = Time.time + ability.cooldown;    
    }

    public bool IsAbilityOnCooldown(AbilityBase ability)
    {
        if (cooldowns.ContainsKey(ability))
        {
            return Time.time < cooldowns[ability];
        }
        return false;
    }

    public void SetAbilityCooldown(AbilityBase ability, float cooldown)
    {
        cooldowns[ability] = Time.time + cooldown;
    }
    private bool CanTransitionTo(PlayerState newState)
    {
        if (newState == PlayerState.Normal) return true;

        switch (currentState)
        {
            case PlayerState.Normal:
                return true;

            case PlayerState.Blocking:
                if (newState == PlayerState.Blocking) return true;
                return false;

            case PlayerState.Dashing:
            case PlayerState.UsingPrimaryAbility:
            case PlayerState.UsingSignatureAbility:
            case PlayerState.Stunned:
                return false;
        }

        return false;
    }

    public void OnDamageDealt(float damage)
    {
        if(!IsServer) return;

        currentSignatureCharge.Value = Mathf.Min(currentSignatureCharge.Value + damage * championData.signatureAbility.chargePerDamageDealt, championData.signatureAbility.maxCharge);
    }

    public float GetCurrentCharge()
    {
        return currentSignatureCharge.Value;
    }

    public void ResetCharge()
    {
        if(IsServer)
        {
            currentSignatureCharge.Value = 0f;
        }
    }
}

