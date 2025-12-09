using System.Collections.Generic;
using UnityEngine;
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

    [Header("Reconciliation")]
    public float POSITION_TOLERANCE;

    // Buffers for input and state history
    private const int BUFFER_SIZE = 1024;
    private PlayerNetworkInputData[] _inputBuffer = new PlayerNetworkInputData[BUFFER_SIZE];

    // Struct to hold simulation state
    private struct SimulationState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public PlayerState State;
    }

    // State history buffer
    private SimulationState[] _stateBuffer = new SimulationState[BUFFER_SIZE];

    // Simulation tick tracking
    private int _currentTick = 0;
    private float _timer;
    private const float TICK_RATE = 1f / 60f;
    private int _stateStartTick;

    private Vector2 _dashDirection;
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

        // Set fixed delta time for consistent tick rate
        Time.fixedDeltaTime = TICK_RATE;
    }

    void FixedUpdate()
    {
        // Run simulation ticks based on fixed delta time
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
            // Update signature ability charge over time
            float chargeAmount = championData.signatureAbility.chargePerSecond * Time.deltaTime;
            currentSignatureCharge.Value = Mathf.Min(currentSignatureCharge.Value + chargeAmount, championData.signatureAbility.maxCharge);
        }
    }

    private void RunSimulationTick()
    {
        if (IsClient && IsOwner)
        {
            // Gather input and process simulation
            PlayerNetworkInputData input = _inputHandler.CurrentInput;
            input.Tick = _currentTick;

            int bufferIndex = _currentTick % BUFFER_SIZE;
            _inputBuffer[bufferIndex] = input;

            ProcessPlayerSimulation(input);
            ProcessInputServerRpc(input);

            _inputHandler.ResetInputs();

            _currentTick++;
        }
        else if (IsServer && !IsOwner)
        {
            // wait 
        }
    }

    private void ProcessPlayerSimulation(PlayerNetworkInputData input)
    {
        // Store input in buffer
        int bufferIndex = input.Tick % BUFFER_SIZE;
        _inputBuffer[bufferIndex] = input;

        // Apply movement and state logic
        RotatePlayerTowardsMouse(input.MousePosition);
        ApplyStateLogic(input);
        ApplyMovementPhysics(input);

        // Update simulation state buffer
        _stateBuffer[bufferIndex] = new SimulationState
        {
            Position = rb.position,
            Velocity = rb.linearVelocity,
            State = currentState
        };
    }

    private void ApplyStateLogic(PlayerNetworkInputData input)
    {
        CurrentMovementDirection = input.Movement;

        // Handle state-specific logic
        if(currentState == PlayerState.Dashing)
        {
            float durationInSecs = championData.dashAbility.dashDuration;
            int durationInTicks = Mathf.CeilToInt(durationInSecs / TICK_RATE);
            if (_currentTick >= _stateStartTick + durationInTicks)
            {
                championData.dashAbility.EndAbility(this, IsServer);
            }
            return;
        }

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
        // Determine target velocity based on state and input
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
                targetVelocity = _dashDirection * championData.dashAbility.dashSpeed;
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
        // Update server-side simulation
        _currentTick = input.Tick;
        ProcessPlayerSimulation(input);

        // Reconcile with client
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

        // Reconcile with server

        // Get predicted state from buffer
        int bufferIndex = serverState.Tick % BUFFER_SIZE;
        SimulationState predictedState = _stateBuffer[bufferIndex];

        // Check for position error
        float positionError = Vector2.Distance(serverState.Position, predictedState.Position);
        float effectiveTolerance = POSITION_TOLERANCE;
        if(currentState == PlayerState.Dashing || serverState.State == PlayerState.Dashing)
        {
            effectiveTolerance *= 2f;
        }

        if (positionError > effectiveTolerance)
        {
            Debug.LogWarning($"Reconciling! Error: {positionError}");

            // Correct position and velocity
            rb.position = serverState.Position;
            rb.linearVelocity = serverState.Velocity;
            currentState = serverState.State;

            // Reprocess inputs from the corrected tick
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
        if(currentState == PlayerState.Dashing || currentState == PlayerState.Stunned) return;

        if(Vector2.Distance(mousePos, rb.position) < 0.3f) return;

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

        _stateStartTick = _currentTick;

        ability.Activate(this, IsServer);
        SetAbilityCooldown(ability);
    }

    public void SetDashDirection(Vector2 direction)
    {
        _dashDirection = direction;
    }

    public bool IsAbilityOnCooldown(AbilityBase ability)
    {
        if (cooldowns.ContainsKey(ability))
        {
            int thresholdTick = _currentTick;
            if (IsServer) thresholdTick += 1;
            return thresholdTick < cooldowns[ability];
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

