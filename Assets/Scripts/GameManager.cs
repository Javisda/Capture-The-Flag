using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject playerPrefab;
    private static GameManager _instance;


    private const int maxPlayers = 8;
    Vector3[] SpawnPoints = new Vector3[8];
    bool matchInitialized;
    bool matchEnded;
    public NetworkVariable<int> numPlayers;
    public NetworkVariable<float> matchTimer;

    // LadderBoard
    private string[] names = new string[maxPlayers];
    private int[] kills = new int[maxPlayers];
    private int[] deaths = new int[maxPlayers];

    private void Start()
    {
        // Singleton
        if (_instance != null) return;
            _instance = this;

        // Spawn Points init
        SpawnPoints[0] = new Vector3(0, 0, 0);
        SpawnPoints[1] = new Vector3(2.5f, 3.5f, 0);
        SpawnPoints[2] = new Vector3(-7.0f, -2.0f, 0);
        SpawnPoints[3] = new Vector3(-1.0f, 3.0f, 0);
        SpawnPoints[4] = new Vector3(-6.0f, 6.0f, 0);
        SpawnPoints[5] = new Vector3(6.5f, 7.0f, 0);
        SpawnPoints[6] = new Vector3(9.0f, -0.6f, 0);
        SpawnPoints[7] = new Vector3(1.05f, -4.0f, 0);
        matchInitialized = false;
        matchEnded = false;

        // Net Variables
        numPlayers = new NetworkVariable<int>();
        matchTimer = new NetworkVariable<float>();
    }
    private void OnEnable()
    {
        networkManager.OnServerStarted += OnServerReady;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        numPlayers.OnValueChanged += UpdateNumPlayers;
    }

    private void OnDisable()
    {
        networkManager.OnClientDisconnectCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    // Esto se ejecuta en el servidor
    private void OnServerReady()
    {
        print("Server ready");
    }

    // Esto se ejecuta en servidor y cliente que se conecta
    private void OnClientConnected(ulong clientId)
    {
        if (networkManager.IsServer) {
            if (numPlayers.Value > maxPlayers - 1) // Si tenemos el máximo de jugadores, desconecta al nuevo
            {
                OnClientDisconnected(clientId);
                return; 
            }
            else if (matchInitialized) { // Si el juego ya ha sido inicializado, spawnea al jugador entrante directamente, sin hacerle esperar
                GameObject player = Instantiate(playerPrefab, SpawnPoints[numPlayers.Value], Quaternion.identity);
                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            }

            // Num players connected
            print("NumPlayers: " + (numPlayers.Value + 1));
            numPlayers.Value += 1;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        networkManager.DisconnectClient(clientId);
    }

    private void UpdateNumPlayers(int previousValue, int newValue)
    {
        numPlayers.Value = newValue;
    }


    private void FixedUpdate()
    {
        // Inicializacion de la partida
        if (networkManager.IsServer && !matchInitialized) {
            if (networkManager.ConnectedClientsList.Count > 1) { // Si ya hay esperando al menos 2 jugadores, los instanciamos y comienza la partida. Hasta entonces, el jugador tendrá que esperar
                InitMatch();
            }
        }

        // Actualizacion del temporizador
        if (networkManager.IsServer && matchInitialized && !matchEnded) {
            matchTimer.Value -= Time.deltaTime;

            if (networkManager.ConnectedClientsList.Count <= 1) { // Si la partida se queda con un jugador o ninguno, se termina en 3 segundos. No funciona por el momento ya que el jugador no se desconecta completamente al cerrar el .exe
                matchTimer.Value = 3.0f;  
            }

            // La partida termina si llega a 0 el temporizador
            if (matchTimer.Value < 0)
            {
                Debug.Log("PARTIDA TERMINADA");
                EndMatch();
                matchEnded = true;
            }
            else { 
                NotifyTimeToClients(matchTimer.Value);
            }
        }

    }

    private void InitMatch() {
        for (int i = 0; i < networkManager.ConnectedClientsList.Count - 1; i++)
        {
            GameObject player = Instantiate(playerPrefab, SpawnPoints[i], Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(networkManager.ConnectedClientsList[i].ClientId, true);
        }
        matchInitialized = true;

        // Inicializamos el temporizador
        matchTimer.Value = 90;
    }

    private void EndMatch() {

        CalculateLaderboard();

        // Almacenamos cadenas de texto preparadas para pasarselas a los clientes
        string[] ladderboard = new string[networkManager.ConnectedClientsList.Count];
        for (int i = 0; i < ladderboard.Length; i++) {
            ladderboard[i] = names[i] + ". Kills: " + kills[i] + ". Deaths: " + deaths[i];
        }

        // Pass al clients ladderboard Data and make them show it
        foreach (NetworkClient c in networkManager.ConnectedClientsList)
        {
            for (int i = 0; i < ladderboard.Length; i++)
            {
                c.PlayerObject.GetComponent<Player>().NotifyLadderboardClientRpc(ladderboard[i]); 
            }

            // Show ladderboard
            c.PlayerObject.GetComponent<Player>().NotifyShowLadderboardClientRpc();
        }

        // Despawn All Players
        foreach (NetworkClient c in networkManager.ConnectedClientsList)
        {
            c.PlayerObject.GetComponent<NetworkObject>().Despawn();
        }

        // Desconecta jugadores a los 5 segundos
        StartCoroutine(DisconnectPlayers(5));
    }



    void NotifyTimeToClients(float time) {
        foreach (NetworkClient c in networkManager.ConnectedClientsList) {
            c.PlayerObject.GetComponent<Player>().Timer.Value = (int)time;
        }
    }


    private void CalculateLaderboard() {

        // Funcion que ordena el array de nombres, kills y muertes en función de las kills y, en caso de tener las mismas kills, en funcion del numero de muertes

        names = new string[networkManager.ConnectedClientsList.Count];
        kills = new int[networkManager.ConnectedClientsList.Count];
        deaths = new int[networkManager.ConnectedClientsList.Count];

        // Almacenamos
        for (int i = 0; i < networkManager.ConnectedClientsList.Count; i++)
        {
            names[i] = networkManager.ConnectedClientsList[i].PlayerObject.GetComponent<Player>().NameHolder.text;
            kills[i] = networkManager.ConnectedClientsList[i].PlayerObject.GetComponent<Player>().Kills.Value;
            deaths[i] = networkManager.ConnectedClientsList[i].PlayerObject.GetComponent<Player>().Deaths.Value;
        }

        // Ordenamos
        int tempKill = 0;
        int tempDeath = 0;
        string tempName = "";
        for (int i = 0; i <= kills.Length - 1; i++) {
            for (int j = i + 1; j < kills.Length; j++) {
                if ((kills[i] > kills[j]) || (kills[i] == kills[j] && deaths[i] < deaths[j])) {
                    ////////////////////
                    tempKill = kills[i];
                    tempDeath = deaths[i];
                    tempName = names[i];
                    ////////////////////
                    kills[i] = kills[j];
                    deaths[i] = deaths[j];
                    names[i] = names[j];
                    ////////////////////
                    kills[j] = tempKill;
                    deaths[j] = tempDeath;
                    names[j] = tempName;
                    ////////////////////
                }
            }
        }
    }



    IEnumerator DisconnectPlayers(int secs)
    {
        yield return new WaitForSeconds(secs);
        foreach (NetworkClient c in networkManager.ConnectedClientsList)
        {
            OnClientDisconnected(c.ClientId);
        }
    }

}
