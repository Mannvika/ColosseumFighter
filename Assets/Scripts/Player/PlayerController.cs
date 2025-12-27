using UnityEngine;
using Unity.Netcode;
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
    public NetworkVariable<bool> isPlayerActive = new NetworkVariable<bool>(false);
    
    [Header("Data")]
    public Champion championData;
    public NetworkVariable<int> championId = new NetworkVariable<int>(0);
    
    [Header("Sub-Systems")]
    public PlayerMovement Movement;
    public PlayerResources Resources;
    public PlayerVisuals Visuals;
    public StatSystem Stats = new StatSystem();
    private PlayerInputHandler _inputHandler;
    public PlayerAbilitySystem AbilitySystem;

    [Header("State")]
    public PlayerState currentState = PlayerState.Normal;
    public AbilityBase currentActiveAbility;
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

    public int CurrentTick { get; private set; } = 0;
    private float _timer;
    private const float TICK_RATE = 1f / 60f;
    
    private Rigidbody2D _rb;

    private void Awake()
    {
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        // 1. ALWAYS initialize local components (Fixes Teleport crash)
        _rb = GetComponent<Rigidbody2D>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Resources = GetComponent<PlayerResources>();
        Visuals = GetComponent<PlayerVisuals>();
        
        Movement.Initialize(this, _rb);
        Resources.Initialize(this);
        AbilitySystem = new PlayerAbilitySystem(this);
        
        // 2. Setup Network Variables
        championId.OnValueChanged += OnChampionChanged;
        isPlayerActive.OnValueChanged += OnActiveStateChanged;

        // 3. Try loading data safely (Fixes Data crash)
        // If GameManager exists (Game Scene), load now. 
        // If not (Lobby Scene), we wait for the GameManager to poke us later.
        if (GameManager.instance != null)
        {
            LoadChampionData(championId.Value);
            
            // Server-side health init
            Health health = GetComponent<Health>();
            if (IsServer && health != null && championData != null)
            {
                health.currentHealth.Value = championData.maxHealth;
            }
        }
        else
        {
            // Safety: Disable script until we are officially active
            enabled = false; 
        }

        // 4. Time Setup
        Time.fixedDeltaTime = TICK_RATE;
        initialized = true;

        // 5. Initial State Check
        // If we join late and player is already active, this ensures we are visible
        if(isPlayerActive.Value)
        {
            TogglePlayerSpawnState(true);
        }
        else
        {
            TogglePlayerSpawnState(false);
        }
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector2 newPosition)
    {
        transform.position = newPosition;
        _rb.position = newPosition;
        _rb.linearVelocity = Vector2.zero;

        for(int i=0; i < _stateBuffer.Length; i++)
        {
            _stateBuffer[i].Position = newPosition;
        }
        
        CurrentInputMovement = Vector2.zero;
    }

    public override void OnNetworkDespawn()
    {
        championId.OnValueChanged -= OnChampionChanged;
        isPlayerActive.OnValueChanged -= OnActiveStateChanged;
    }

    private void OnActiveStateChanged(bool oldVal, bool newVal)
    {
        TogglePlayerSpawnState(newVal);

        enabled = newVal;
    }

    public void InitializeChampion(int index)
    {
        if(!IsServer) return;
        championId.Value = index;
    }

    private void OnChampionChanged(int oldChampion, int newChampion)
    {
        LoadChampionData(newChampion);
    }

    public void LoadChampionData(int index)
    {
        if (GameManager.instance == null) return;
        
        if (index >= 0 && index < GameManager.instance.allChampions.Length)
        {
            championData = GameManager.instance.allChampions[index];
            
            // Re-initialize resources that depend on Champion Data
            if(Resources != null && championData != null)
            {
                Resources.BlockCharge.Value = championData.blockAbility.maxCharge;
            }
        }
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
        if(!initialized || championData == null) return;

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
    }

    private void ProcessPlayerSimulation(PlayerNetworkInputData input)
    {
        if(!initialized) return;

        int bufferIndex = input.Tick % BUFFER_SIZE;
        _inputBuffer[bufferIndex] = input;

        CurrentInputMovement = input.Movement;

        Movement.RotateTowards(input.MousePosition);

        AbilitySystem.ProcessInput(input);
        float moveMult = 1f;
        bool stopMove = false;
        
        if (currentActiveAbility != null)
        {
            moveMult = currentActiveAbility.moveSpeedMultiplier;
            stopMove = currentActiveAbility.stopMovementOnActivate;
        }
        
        Movement.TickPhysics(input, moveMult, stopMove);

        Stats.Tick();

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
        
        float tolerance = POSITION_TOLERANCE;
        if (currentState == PlayerState.Dashing || serverState.State == PlayerState.Dashing)
            tolerance *= FAST_MOVEMENT_MULTIPLIER;

        bool stateMismatch = predictedState.State != serverState.State;

        if (positionError > tolerance || stateMismatch)
        {
            _rb.position = serverState.Position;
            _rb.linearVelocity = serverState.Velocity;
            currentState = serverState.State;
            Stats.SetState(serverState.Stats);

            int tickToReprocess = serverState.Tick + 1;

            isPredicting = true;
            isRollingBack = true;
            while (tickToReprocess < CurrentTick)
            {
                int replayIndex = tickToReprocess % BUFFER_SIZE;
                PlayerNetworkInputData replayInput = _inputBuffer[replayIndex];

                AbilitySystem.ProcessInput(replayInput);

                float moveMult = currentActiveAbility != null ? currentActiveAbility.moveSpeedMultiplier : 1f;
                bool stopMove = currentActiveAbility != null && currentActiveAbility.stopMovementOnActivate;
                Movement.TickPhysics(replayInput, moveMult, stopMove);

                Stats.Tick(); 

                _stateBuffer[replayIndex] = new SimulationState
                {
                    Position = transform.position,
                    Velocity = _rb.linearVelocity,
                    State = currentState,
                    Stats = Stats.GetState()
                };

                tickToReprocess++;
            }
            isPredicting = false;
            isRollingBack = false;
        }
    }

    public void SetAbilityCooldown(AbilityBase ability) => AbilitySystem.SetCooldown(ability);
    public bool IsAbilityOnCooldown(AbilityBase ability) => AbilitySystem.IsOnCooldown(ability);

    public void SetDashDirection(Vector2 dir) => Movement.StartDash(dir);
    
    public void OnDamageDealt(float damage) => Resources.AddSignatureCharge(damage);

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
        if(_inputHandler != null)
        {
            _inputHandler.enabled = active;
            if(!active) _inputHandler.ResetInputs();
        }
    }

    public void TogglePlayerSpawnState(bool isSpawned)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if(sr != null) sr.enabled = isSpawned;

        var col = GetComponent<Collider2D>();
        if(col != null) col.enabled = isSpawned;

        if(IsOwner)
        {
            SetInputActive(isSpawned);
        }

        if(isSpawned)
        {
            currentState = PlayerState.Normal;
            Stats.Reset();
        }
    }
}

