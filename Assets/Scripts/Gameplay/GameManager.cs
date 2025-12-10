using UnityEngine;
using Unity.Netcode;
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
    public NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);
    private Dictionary<ulong, PlayerController> activePlayers = new Dictionary<ulong, PlayerController>();
}
