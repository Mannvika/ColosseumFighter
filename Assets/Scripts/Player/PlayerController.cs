using UnityEngine;
using Unity.Netcode;
using System.Collections;
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

    public event System.Action OnChampionDataLoaded;
    
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

    public NetworkVariable<PlayerState> netState = new NetworkVariable<PlayerState>(PlayerState.Normal);

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
        _rb = GetComponent<Rigidbody2D>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        Movement = GetComponent<PlayerMovement>();
        Resources = GetComponent<PlayerResources>();
        Visuals = GetComponent<PlayerVisuals>();
        
        Movement.Initialize(this, _rb);
        Resources.Initialize(this);
        AbilitySystem = new PlayerAbilitySystem(this);
        
        championId.OnValueChanged += OnChampionChanged;
        isPlayerActive.OnValueChanged += OnActiveStateChanged;

        if (GameManager.instance != null)
        {
            LoadChampionData(championId.Value);
            InitializeHealth();
        }
        else
        {
            Debug.LogWarning($"[PlayerController] GameManager not ready for {name} (NetId: {NetworkObjectId}). Queuing initialization...");
            StartCoroutine(WaitForGameManagerAndInit());
        }

        Time.fixedDeltaTime = TICK_RATE;
        initialized = true;

        if(isPlayerActive.Value) TogglePlayerSpawnState(true);
        else TogglePlayerSpawnState(false);
    }

    private IEnumerator WaitForGameManagerAndInit()
    {
        while (GameManager.instance == null)
        {
            yield return null;
        }

        Debug.Log($"[PlayerController] GameManager found! Delayed initialization for {name} (NetId: {NetworkObjectId})");
        LoadChampionData(championId.Value);
        InitializeHealth();
    }

    private void InitializeHealth()
    {
        Health health = GetComponent<Health>();
        if (IsServer && health != null && championData != null)
        {
            health.currentHealth.Value = championData.maxHealth;
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
        // Debug 1: Confirm the method is actually called on the Client
        Debug.Log($"[NetId: {NetworkObjectId} IsServer:{IsServer}] LoadChampionData called with index: {index}");

        if (GameManager.instance == null)
        {
            // TRAP 1: Singleton is missing
            Debug.LogError($"[NetId: {NetworkObjectId}] FAILED: GameManager.instance is NULL!");
            return;
        }
        
        if (GameManager.instance.allChampions == null)
        {
            // TRAP 2: Array is null
            Debug.LogError($"[NetId: {NetworkObjectId}] FAILED: GameManager.allChampions array is NULL!");
            return;
        }

        if (index < 0 || index >= GameManager.instance.allChampions.Length)
        {
            // TRAP 3: Index is invalid (e.g., -1 if initialization failed)
            Debug.LogError($"[NetId: {NetworkObjectId}] FAILED: Index {index} is out of bounds. Array Length: {GameManager.instance.allChampions.Length}");
            return;
        }

        championData = GameManager.instance.allChampions[index];
        
        // Debug 2: Confirm local data assignment
        Debug.Log($"[NetId: {NetworkObjectId}] SUCCESS: Set championData to {championData.name}");

        if(IsServer && Resources != null && championData != null)
        {
            Resources.BlockCharge.Value = championData.blockAbility.maxCharge;
        }

        // Debug 3: Confirm event invocation
        if (OnChampionDataLoaded != null)
        {
            Debug.Log($"[NetId: {NetworkObjectId}] Invoking OnChampionDataLoaded for {OnChampionDataLoaded.GetInvocationList().Length} listeners.");
        }
        else
        {
            Debug.LogWarning($"[NetId: {NetworkObjectId}] Invoking OnChampionDataLoaded but NO LISTENERS are subscribed yet.");
        }

        OnChampionDataLoaded?.Invoke();
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

        if(IsServer)
        {
            netState.Value = currentState;
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

