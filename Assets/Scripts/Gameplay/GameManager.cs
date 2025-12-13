using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    WaitingForPlayers,
    Countdown,
    Active,  
    GameOver
}

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);

    [Header("Setup")]
    public Transform[] spawnPoints;

    private Dictionary<ulong, PlayerController> players = new Dictionary<ulong, PlayerController>();


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        currentState.OnValueChanged += OnStateChanged;

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
        yield return new WaitForSeconds(1f);

        InitalizePlayers();

        currentState.Value = GameState.Countdown;

        yield return new WaitForSeconds(3f);

        currentState.Value = GameState.Active;

        currentState.Value = GameState.GameOver;
    }

    void InitalizePlayers()
    {
        var foundPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        int index = 0;
        foreach(var p in foundPlayers)
        {
            if(!players.ContainsKey(p.OwnerClientId))
            {
                players.Add(p.OwnerClientId, p);
            }

            if(index < spawnPoints.Length)
            {
                p.transform.position = spawnPoints[index].position;
            }

            p.TogglePlayerSpawnState(true);

            index++;
        }
    }

    public void OnPlayerDied(ulong deadID)
    {
        if(!IsServer || currentState.Value != GameState.Active)
        {
            return;
        }

        int aliveCount = 0;
        ulong winnerId = 0;
        foreach(var p in players)
        {
            if(p.Key != deadID)
            {
                aliveCount++;
                winnerId = p.Key;
            }
        }

        if(aliveCount <= 1)
        {
            EndGame(winnerId);
        }
    }

    private void EndGame(ulong winnerId)
    {
        currentState.Value = GameState.GameOver;

        Debug.Log("Winner: " + winnerId.ToString());
    }

    private void OnStateChanged(GameState oldState, GameState newState)
    {
        if(newState == GameState.Active)
        {
            if(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                localPlayer.TogglePlayerSpawnState(true);
            }
        }

        else if(newState == GameState.GameOver)
        {
            if(NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                localPlayer.TogglePlayerSpawnState(false);
            }
        }
    }
}
