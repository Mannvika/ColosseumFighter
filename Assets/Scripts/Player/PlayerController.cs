using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
public enum PlayerState
{
    Normal,
    Attacking,
    Dashing,
    Blocking,
    Firing,
    Stunned
}

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerResources))]
public class PlayerController : NetworkBehaviour
{
    [Header("Data")]
    public Champion championData;
    
    [Header("Sub-Systems")]
    public PlayerMovement Movement;
    public PlayerResources Resources;
    public PlayerVisuals Visuals;
    public StatSystem Stats = new StatSystem();
    private PlayerInputHandler _inputHandler;
    public PlayerAbilitySystem AbilitySystem;

    [Header("State")]
    public PlayerState currentState = PlayerState.Normal;
    public AbilityBase currentActiveAbility; // Currently executing ability
    public Vector2 CurrentInputMovement { get; private set; }


    [Header("Reconciliation")]
    public float POSITION_TOLERANCE = 0.1f;
    public float FAST_MOVEMENT_MULTIPLIER = 3f;

    // Simulation Buffers
    private const int BUFFER_SIZE = 1024;
    private PlayerNetworkInputData[] _inputBuffer = new PlayerNetworkInputData[BUFFER_SIZE];
    private SimulationState[] _stateBuffer = new SimulationState[BUFFER_SIZE];

    public bool isPredicting {get; private set; }
    public bool isRollingBack {get; private set; }

    private struct SimulationState
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public PlayerState State;
        public StatState Stats;
    }

    private bool initialized = false;

    // Tick Logic
    public int CurrentTick { get; private set; } = 0;
    private float _timer;
    private const float TICK_RATE = 1f / 60f;
    
    private Rigidbody2D _rb;

    private void Awake()
    {
        //enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        _rb = GetComponent<Rigidbody2D>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Resources = GetComponent<PlayerResources>();
        Visuals = GetComponent<PlayerVisuals>();
        
        Movement.Initialize(this, _rb);
        Resources.Initialize(this);
        AbilitySystem = new PlayerAbilitySystem(this);

        Health health = GetComponent<Health>();
        if (IsServer && health != null)
        {
            health.currentHealth.Value = championData.maxHealth;
        }
        Time.fixedDeltaTime = TICK_RATE;

        //enabled = true;

        initialized = true;
    }

    void FixedUpdate()
    {
        if(!initialized) return;
        _timer += Time.fixedDeltaTime;
        while (_timer >= TICK_RATE)
        {
            _timer -= TICK_RATE;
            RunSimulationTick();
        }
    }

    private void RunSimulationTick()
    {
        if (IsClient && IsOwner)
        {
            PlayerNetworkInputData input = _inputHandler.CurrentInput;
            input.Tick = CurrentTick;

            isPredicting = true;
            ProcessPlayerSimulation(input);
            isPredicting = false;
            ProcessInputServerRpc(input);

            _inputHandler.ResetInputs();
            CurrentTick++;
        }
        // Server handles its own ticks via RPC processing usually, 
        // but if Server is also a player (Host), separate logic is needed.
        // For pure Server (Dedicated), it waits for RPC.
    }

    // Core Logic Loop
    private void ProcessPlayerSimulation(PlayerNetworkInputData input)
    {
        if(!initialized) return;

        int bufferIndex = input.Tick % BUFFER_SIZE;
        _inputBuffer[bufferIndex] = input;

        CurrentInputMovement = input.Movement;

        // 1. Process Visuals (Rotation)
        Movement.RotateTowards(input.MousePosition);

        // 2. Process Logic (Abilities & State Transitions)
        // Delegated to Ability System
        AbilitySystem.ProcessInput(input);

        // 3. Process Physics
        // Calculate multipliers based on Active Ability
        float moveMult = 1f;
        bool stopMove = false;
        
        if (currentActiveAbility != null)
        {
            moveMult = currentActiveAbility.moveSpeedMultiplier;
            stopMove = currentActiveAbility.stopMovementOnActivate;
        }
        
        Movement.TickPhysics(input, moveMult, stopMove);

        // 4. Update Tick Timer (Optional, if abilities use ticks internally)
        Stats.Tick();

        // 5. Save State for Rollback
        _stateBuffer[bufferIndex] = new SimulationState
        {
            Position = _rb.position,
            Velocity = _rb.linearVelocity,
            State = currentState,
            Stats = Stats.GetState()
        };
    }

    [ServerRpc]
    private void ProcessInputServerRpc(PlayerNetworkInputData input)
    {
        isPredicting = false;
        CurrentTick = input.Tick;
        ProcessPlayerSimulation(input);

        StatePayload statePayload = new StatePayload
        {
            Tick = input.Tick,
            Position = _rb.position,
            Velocity = _rb.linearVelocity,
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
        
        // Dynamic Tolerance based on State
        float tolerance = POSITION_TOLERANCE;
        if (currentState == PlayerState.Dashing || serverState.State == PlayerState.Dashing)
            tolerance *= FAST_MOVEMENT_MULTIPLIER;

        bool stateMismatch = predictedState.State != serverState.State;

        if (positionError > tolerance || stateMismatch)
        {
            // Debug.LogWarning($"Reconciling Tick {serverState.Tick}! Error: {positionError}");

            // Snap to authoritative state
            _rb.position = serverState.Position;
            _rb.linearVelocity = serverState.Velocity;
            currentState = serverState.State;
            Stats.SetState(serverState.Stats); // Usually you want server stats here? 
                                                  // Ideally Stats should be in StatePayload too if they affect physics significantly.



            // Replay Inputs
            int tickToReprocess = serverState.Tick + 1;

            isPredicting = true;
            isRollingBack = true;
            while (tickToReprocess < CurrentTick)
            {
                int replayIndex = tickToReprocess % BUFFER_SIZE;
                PlayerNetworkInputData replayInput = _inputBuffer[replayIndex];

                // A. Process Logic
                AbilitySystem.ProcessInput(replayInput);

                // B. Process Physics
                float moveMult = currentActiveAbility != null ? currentActiveAbility.moveSpeedMultiplier : 1f;
                bool stopMove = currentActiveAbility != null && currentActiveAbility.stopMovementOnActivate;
                Movement.TickPhysics(replayInput, moveMult, stopMove);

                // C. Process Stats (Tick down cooldowns/buffs)
                Stats.Tick(); 

                // D. Update Buffer with corrected future
                _stateBuffer[replayIndex] = new SimulationState
                {
                    Position = transform.position,
                    Velocity = _rb.linearVelocity,
                    State = currentState,
                    Stats = Stats.GetState() // Save the corrected stat state
                };

                tickToReprocess++;
            }
            isPredicting = false;
            isRollingBack = false;
        }
    }

    // --- Helper Methods for External Systems (Abilities) ---

    // Called by Abilities to set cooldowns
    public void SetAbilityCooldown(AbilityBase ability) => AbilitySystem.SetCooldown(ability);
    public bool IsAbilityOnCooldown(AbilityBase ability) => AbilitySystem.IsOnCooldown(ability);

    // Called by PlayerMovement
    public void SetDashDirection(Vector2 dir) => Movement.StartDash(dir);
    
    // Called by Abilities
    public void OnDamageDealt(float damage) => Resources.AddSignatureCharge(damage);

    // Forces a state exit (e.g. Shield running out of energy)
    public void ForceEndState(PlayerState stateToEnd)
    {
        if (currentState == stateToEnd)
        {
            if (stateToEnd == PlayerState.Blocking) championData.blockAbility.OnEnd(this, IsServer);
            currentState = PlayerState.Normal;
        }
    }
    public void SetInputActive(bool active)
    {
        // Bridge to the Input Component
        if(_inputHandler != null)
        {
            _inputHandler.enabled = active;
            if(!active) _inputHandler.ResetInputs();
        }
    }

    public void TogglePlayerSpawnState(bool isSpawned)
    {
        // toggle visuals
        var sr = GetComponentInChildren<SpriteRenderer>();
        if(sr != null) sr.enabled = isSpawned;

        // toggle physics body
        var col = GetComponent<Collider2D>();
        if(col != null) col.enabled = isSpawned;

        // toggle input
        SetInputActive(isSpawned);

        // Optional: Reset internal state
        if(isSpawned)
        {
            currentState = PlayerState.Normal;
            Stats.Reset(); // If you want to reset buffs on respawn
            // Health reset would go here too
        }
    }
}

