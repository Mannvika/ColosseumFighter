using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.VisualScripting;
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

    private const int BUFFER_SIZE = 1024;
    private PlayerNetworkInputData[] _inputBuffer = new PlayerNetworkInputData[BUFFER_SIZE];

    private struct SimulationState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public PlayerState State;
    }

    private SimulationState[] _stateBuffer = new SimulationState[BUFFER_SIZE];

    private int _currentTick = 0;
    private float _timer;
    private const float TICK_RATE = 1f / 60f;

    public float POSITION_TOLERANCE;


    private PlayerInputHandler _inputHandler;
    private Rigidbody2D rb;

    [HideInInspector] 
    public Vector2 CurrentMovementDirection;

    public NetworkVariable<float> currentSignatureCharge = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Dictionary<AbilityBase, int> cooldowns = new Dictionary<AbilityBase, int>();

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        _inputHandler = GetComponent<PlayerInputHandler>();

        Time.fixedDeltaTime = TICK_RATE;
    }

    void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        while (_timer >= TICK_RATE)
        {
            _timer -= TICK_RATE;
            RunSimulationTick();
        }
    }

    private void Update()
    {
        if (IsServer && championData != null && championData.signatureAbility != null)
        {
            float chargeAmount = championData.signatureAbility.chargePerSecond * Time.deltaTime;
            currentSignatureCharge.Value = Mathf.Min(currentSignatureCharge.Value + chargeAmount, championData.signatureAbility.maxCharge);
        }
    }

    private void RunSimulationTick()
    {
        if (IsClient && IsOwner)
        {
            PlayerNetworkInputData input = _inputHandler.CurrentInput;
            input.Tick = _currentTick;

            int bufferIndex = _currentTick % BUFFER_SIZE;
            _inputBuffer[bufferIndex] = input;

            ProcessPlayerSimulation(input);
            ProcessInputServerRpc(input);
            _currentTick++;
        }
        else if (IsServer && !IsOwner)
        {
            // wait 
        }
    }

    private void ProcessPlayerSimulation(PlayerNetworkInputData input)
    {
        int bufferIndex = input.Tick % BUFFER_SIZE;
        _inputBuffer[bufferIndex] = input;

        RotatePlayerTowardsMouse(input.MousePosition);

        ApplyStateLogic(input);

        ApplyMovementPhysics(input);

        _stateBuffer[bufferIndex] = new SimulationState
        {
            Position = rb.position,
            Velocity = rb.linearVelocity,
            State = currentState
        };
    }

    private void ApplyStateLogic(PlayerNetworkInputData input)
    {
        if (currentState == PlayerState.Blocking)
        {
            if (!input.IsBlockPressed)
            {
                if (championData.blockAbility != null) championData.blockAbility.EndAbility(this, IsServer);
                currentState = PlayerState.Normal;
            }
        }
        else if (currentState == PlayerState.Firing)
        {
            if (!input.IsProjectilePressed)
            {
                if (championData.projectileAbility != null) championData.projectileAbility.EndAbility(this, IsServer);
                currentState = PlayerState.Normal;
            }
            else
            {
                if (championData.projectileAbility != null) championData.projectileAbility.ProcessHold(this, IsServer);
            }
        }

        if (currentState == PlayerState.Normal)
        {
            if (input.IsBlockPressed)
                TryUseAbility(championData.blockAbility, PlayerState.Blocking);

            else if (input.IsDashPressed)
                TryUseAbility(championData.dashAbility, PlayerState.Dashing);

            else if (input.IsMeleePressed)
                TryUseAbility(championData.meleeAttack, PlayerState.Attacking);

            else if (input.IsProjectilePressed)
                TryUseAbility(championData.projectileAbility, PlayerState.Firing);

            else if (input.IsPrimaryAbilityPressed)
                TryUseAbility(championData.primaryAbility, PlayerState.UsingPrimaryAbility);

            else if (input.IsSignatureAbilityPressed)
                TryUseAbility(championData.signatureAbility, PlayerState.UsingSignatureAbility);
        }
    }
    
    private void ApplyMovementPhysics(PlayerNetworkInputData input)
    {
        Vector2 targetVelocity = Vector2.zero;

        switch (currentState)
        {
            case PlayerState.Normal:
            case PlayerState.Attacking: 
                targetVelocity = input.Movement * championData.moveSpeed;
                break;

            case PlayerState.Blocking:
                targetVelocity = input.Movement * (championData.moveSpeed * championData.blockMoveMultiplier);
                break;

            case PlayerState.Firing:
                targetVelocity = input.Movement * (championData.moveSpeed * championData.fireMoveMultiplier);
                break;

            case PlayerState.Dashing:
                break; 

            case PlayerState.UsingPrimaryAbility:
            case PlayerState.UsingSignatureAbility:
            case PlayerState.Stunned:
                targetVelocity = Vector2.zero;
                break;
        }

        rb.linearVelocity = targetVelocity;
    }


    [ServerRpc]
    private void ProcessInputServerRpc(PlayerNetworkInputData input)
    {
        _currentTick = input.Tick;
        ProcessPlayerSimulation(input);

        StatePayload statePayload = new StatePayload
        {
            Tick = input.Tick,
            Position = rb.position,
            Velocity = rb.linearVelocity,
            State = currentState
        };

        ReconcileClientRpc(statePayload);
    }

    [ClientRpc]
    private void ReconcileClientRpc(StatePayload serverState)
    {
        if (!IsOwner) return;

        int bufferIndex = serverState.Tick % BUFFER_SIZE;
        SimulationState predictedState = _stateBuffer[bufferIndex];

        float positionError = Vector2.Distance(serverState.Position, predictedState.Position);
        
        if (positionError > POSITION_TOLERANCE)
        {
            Debug.LogWarning($"Reconciling! Error: {positionError}");

            rb.position = serverState.Position;
            rb.linearVelocity = serverState.Velocity;
            currentState = serverState.State;

            int tickToReprocess = serverState.Tick + 1;
            while (tickToReprocess < _currentTick)
            {
                int replayIndex = tickToReprocess % BUFFER_SIZE;
                PlayerNetworkInputData replayInput = _inputBuffer[replayIndex];

                ApplyStateLogic(replayInput);
                ApplyMovementPhysics(replayInput);

                rb.position += rb.linearVelocity * TICK_RATE;

                _stateBuffer[replayIndex] = new SimulationState
                {
                    Position = rb.position,
                    Velocity = rb.linearVelocity,
                    State = currentState
                };

                tickToReprocess++;
            }
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
        SetAbilityCooldown(ability);
    }

    public bool IsAbilityOnCooldown(AbilityBase ability)
    {
        if (cooldowns.ContainsKey(ability))
        {
            return _currentTick < cooldowns[ability];
        }
        return false;
    }

    public void SetAbilityCooldown(AbilityBase ability)
    {
        int cooldownTicks = Mathf.CeilToInt(ability.cooldown / TICK_RATE);
        cooldowns[ability] = _currentTick + cooldownTicks;
    }
    private bool CanTransitionTo(PlayerState newState)
    {
        if (newState == PlayerState.Normal) return true;

        switch (currentState)
        {
            case PlayerState.Normal:
                return true;
            case PlayerState.Blocking:
                return newState == PlayerState.Blocking;
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

