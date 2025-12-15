using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);

    [Header("Setup")]
    public Transform[] spawnPoints;

    // Track players by ID
    private Dictionary<ulong, PlayerController> players = new Dictionary<ulong, PlayerController>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        currentState.OnValueChanged += OnStateChanged;

        // Force initial UI/Input state for late joiners
        if(IsClient)
        {
            OnStateChanged(GameState.WaitingForPlayers, currentState.Value);
        }

        if(IsServer)
        {
            StartCoroutine(GameLoopRoutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        currentState.OnValueChanged -= OnStateChanged;
    }

    private IEnumerator GameLoopRoutine()
    {
        // Wait for connection stability / players to load
        yield return new WaitForSeconds(1f);

        // 1. Setup Phase
        InitializePlayers();
        currentState.Value = GameState.Countdown;
        
        // 2. Countdown Phase
        yield return new WaitForSeconds(3f);

        // 3. Fight Phase
        currentState.Value = GameState.Active;
    }

    void InitializePlayers()
    {
        players.Clear();
        var foundPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        int index = 0;

        foreach(var p in foundPlayers)
        {
            // Register player in dictionary
            if(!players.ContainsKey(p.OwnerClientId))
            {
                players.Add(p.OwnerClientId, p);
            }

            // Position player at spawn point
            if(spawnPoints != null && index < spawnPoints.Length)
            {
                p.transform.position = spawnPoints[index].position;
            }

            // Reset Visuals and Input via the Facade methods we added to PlayerController
            p.TogglePlayerSpawnState(true);
            p.SetInputActive(false);

            // Reset Health (Server Only)
            if(IsServer)
            {
                 var healthScript = p.GetComponent<Health>();
                 if(healthScript != null) 
                 {
                     healthScript.currentHealth.Value = p.championData.maxHealth;
                 }
                 
                 // Optional: Reset other resources like Charge if needed
                 if(p.Resources != null)
                 {
                     p.Resources.ResetSignatureCharge();
                 }
            }

            index++;
        }
    }

    public void OnPlayerDied(ulong deadID)
    {
        if(!IsServer || currentState.Value != GameState.Active) return;

        int aliveCount = 0;
        ulong winnerId = 0;

        foreach(var p in players)
        {
            // We ignore the player who just died
            if(p.Key != deadID)
            {
                // Check if this other player is actually alive
                var healthScript = p.Value.GetComponent<Health>();
                if (healthScript != null && healthScript.currentHealth.Value > 0)
                {
                    aliveCount++;
                    winnerId = p.Key;
                }
            }
        }

        // If 1 or fewer players remain alive, end the round
        if(aliveCount <= 1)
        {
            EndGame(winnerId);
        }
    }

    private void EndGame(ulong winnerId)
    {
        currentState.Value = GameState.GameOver;
        Debug.Log("Winner: " + winnerId.ToString());
        
        // Optional: Start a coroutine here to restart the game loop after a delay
        // StartCoroutine(RestartGameRoutine());
    }

    private void OnStateChanged(GameState oldState, GameState newState)
    {
        // Handle Global Updates (for all players locally)
        if (newState == GameState.Countdown || newState == GameState.Active)
        {
            var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach(var p in allPlayers)
            {
                // Ensure everyone is visible
                p.TogglePlayerSpawnState(true);
                
                // If counting down, lock everyone's input
                if (newState == GameState.Countdown) 
                {
                    p.SetInputActive(false);
                }
            }
        }

        // Handle Local Player Input Locking
        if(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
            if (localPlayer == null) return;

            if(newState == GameState.Active)
            {
                localPlayer.SetInputActive(true);
            }
            else if (newState == GameState.GameOver || newState == GameState.Countdown)
            {
                localPlayer.SetInputActive(false);
            }
        }
    }
}

public enum GameState
{
    WaitingForPlayers,
    Countdown,
    Active,
    GameOver
}