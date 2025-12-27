using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);

    [Header("Data Registry")]
    public Champion[] allChampions;

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
        // Wait until all connected players have a physical representation in the scene
        yield return new WaitUntil(() => 
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length >= NetworkManager.Singleton.ConnectedClientsIds.Count
        );
        
        // Small buffer for stability
        yield return new WaitForSeconds(0.5f);

        InitializePlayers();
        currentState.Value = GameState.Countdown;
        
        yield return new WaitForSeconds(3f);

        currentState.Value = GameState.Active;
    }

    // In GameManager.cs

    void InitializePlayers()
    {
        players.Clear();
        var connectedIds = NetworkManager.Singleton.ConnectedClientsIds;
        int index = 0;

        foreach (ulong id in connectedIds)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var client))
            {
                if (client.PlayerObject == null) continue;

                var p = client.PlayerObject.GetComponent<PlayerController>();
                if (p == null) continue;

                // 1. Force Load Champion Data (Fixes the NullRef on maxHealth)
                if (GameSessionData.CharacterSelections.ContainsKey(id))
                {
                    int champIndex = GameSessionData.CharacterSelections[id];
                    
                    // Set the ID network variable
                    p.InitializeChampion(champIndex); 
                    
                    // CRITICAL: Manually load the data locally on the Server immediately
                    // This ensures 'p.championData' is not null for the next lines
                    p.LoadChampionData(champIndex); 
                }

                // 2. Add to Dictionary
                if (!players.ContainsKey(id)) players.Add(id, p);

                // 3. Teleport
                if (spawnPoints != null && index < spawnPoints.Length)
                {
                    // Now safe because _rb is initialized in PlayerController.OnNetworkSpawn
                    p.TeleportClientRpc(spawnPoints[index].position);
                }

                // 4. Activate
                p.isPlayerActive.Value = true;

                // 5. Reset State (Server Only)
                if (IsServer)
                {
                    // Now safe because we forced LoadChampionData above
                    if (p.championData != null)
                    {
                        var healthScript = p.GetComponent<Health>();
                        if (healthScript != null)
                            healthScript.currentHealth.Value = p.championData.maxHealth;
                    }
                    
                    if (p.Resources != null) p.Resources.ResetSignatureCharge();
                }

                index++;
            }
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
        Debug.Log($"Game State Changed: {newState}"); // Debug logging

        if (newState == GameState.Countdown || newState == GameState.Active)
        {
            var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                p.TogglePlayerSpawnState(true);
                
                // Only lock/unlock if we are the OWNER of this player
                if (p.IsOwner)
                {
                    bool shouldEnableInput = (newState == GameState.Active);
                    p.SetInputActive(shouldEnableInput);
                    Debug.Log($"Setting Input Active: {shouldEnableInput}");
                }
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