using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public NetworkList<LobbyPlayerData> players;
    [SerializeField]
    private int minPlayersToStart = 2;
    [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("UI References")]
    [SerializeField] private Button char1Btn;     // Button for Champion 0
    [SerializeField] private Button char2Btn;     // Button for Champion 1
    [SerializeField] private Button startGameBtn; // Only visible to Host

    void Awake()
    {
        players = new NetworkList<LobbyPlayerData>();
    }

    void Start()
    {
        char1Btn.onClick.AddListener(() => SelectChampion(0));
        char2Btn.onClick.AddListener(() => SelectChampion(1));
        startGameBtn.onClick.AddListener(StartGame);
        startGameBtn.gameObject.SetActive(false);
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

            OnClientConnected(NetworkManager.Singleton.LocalClientId);        
        }
    }

    private void OnClientConnected(ulong clientID)
    {
        if(!IsServer) return;

        LobbyPlayerData player = new LobbyPlayerData
        {
            clientID = clientID,
            isReady = false,
            championId = -1,
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

    public void SelectChampion(int championId)
    {
        SelectChampionServerRpc(championId);
    }

    [ServerRpc(RequireOwnership = false)]    
    private void SelectChampionServerRpc(int championId, ServerRpcParams serverRpcParams = default)
    {
        ulong senderId = serverRpcParams.Receive.SenderClientId;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientID == senderId)
            {
               LobbyPlayerData player = players[i];
               player.championId = championId;
               player.isReady = true;
               players[i] = player;
               break;
            }
        }
    }

    private void StartGame()
    {
        bool allReady = true;
        foreach(var p in players) { if(!p.isReady) allReady = false; }
        if(!allReady || players.Count < minPlayersToStart) return;

        GameSessionData.Clear();

        foreach (var player in players)
        {
            GameSessionData.CharacterSelections[player.clientID] = player.championId;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void HandleLobbyPlayersStateChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        UpdateLobbyUI(); 
    }

    private void UpdateLobbyUI()
    {
        bool allReady = players.Count >= minPlayersToStart;

        foreach(var p in players)
        {
            if (!p.isReady) allReady = false;
            
            Debug.Log($"Player {p.clientID} selected Champ {p.championId} is {(p.isReady ? "READY" : "WAITING")}");
        }

        startGameBtn.gameObject.SetActive(IsServer && allReady);
    }
}
