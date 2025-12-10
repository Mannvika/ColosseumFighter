using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public NetworkList<LobbyPlayerData> players;
    [SerializeField]
    private int minPlayersToStart = 2;
    [SerializeField]
    private string gameSceneName = "GameScene";

    void Awake()
    {
        players = new NetworkList<LobbyPlayerData>();
    }

    void Start()
    {
        players.OnListChanged += HandleLobbyPlayersStateChanged;
    }

    public override void OnDestroy()
    {
        players.OnListChanged -= HandleLobbyPlayersStateChanged;
        players.Dispose();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;            
        }
    }

    private void OnClientConnected(ulong clientID)
    {
        if(!IsServer) return;

        LobbyPlayerData player = new LobbyPlayerData
        {
            clientID = clientID,
            isReady = false,
            playerName = $"Player {players.Count + 1}"
        };

        Debug.Log("Adding player to lobby:" + player.playerName);
        players.Add(player);
    }

    private void OnClientDisconnected(ulong clientID)
    {
        if (!IsServer) return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientID == clientID)
            {
                players.RemoveAt(i);
                break;
            }
        }
    }

    public void ToggleReady()
    {
        if(IsClient)
        {
            ToggleReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientID = serverRpcParams.Receive.SenderClientId;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientID == clientID)
            {
               LobbyPlayerData player = players[i];
               player.isReady = !player.isReady;

               players[i] = player;

               CheckIfGameCanStart();
               break;
            }
        }
    }

    private void CheckIfGameCanStart()
    {
        if(players.Count < minPlayersToStart) return;
        foreach(var player in players)
        {
            if(!player.isReady) return;
        }
        StartGame();
    }

    private void StartGame()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void HandleLobbyPlayersStateChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        UpdateLobbyUI(); 
    }

    private void UpdateLobbyUI()
    {
        Debug.Log("Lobby Updated!");
        foreach(var player in players)
        {
            Debug.Log($"Player {player.clientID}: {(player.isReady ? "READY" : "NOT READY")}");
        }
    }
}
