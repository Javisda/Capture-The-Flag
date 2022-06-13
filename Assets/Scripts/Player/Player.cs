using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using System;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    #region Variables

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> Life;
    public NetworkVariable<int> Kills;
    public NetworkVariable<int> Deaths;
    public NetworkVariable<int> Timer;

    private int maxHealth = 6;
    [HideInInspector] public GameObject uiManager;
    [HideInInspector] public Text NameHolder;
    private Spawner spawner;

    public ulong NetId { get; set; }

    #endregion

    #region Unity Event Functions


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Network Variables Initialization
            State = new NetworkVariable<PlayerState>();
            Life = new NetworkVariable<int>();
            Kills = new NetworkVariable<int>();
            Deaths = new NetworkVariable<int>();
            Timer = new NetworkVariable<int>();
            Timer.Value = 90;
            Life.Value = maxHealth;
            Kills.Value = 0;
            Deaths.Value = 0;
            spawner = GetComponent<Spawner>();
        }

        if (IsOwner)
        {
            uiManager = GameObject.FindGameObjectWithTag("UIManager");
            StartPlayer();
        }
    }

    private void Start()
    {
        NetId = NetworkObjectId;
        Debug.Log("NET id del jugador: " + NetId);

        // Set Name Overlay
        NameHolder = gameObject.GetComponentInChildren<Text>();

        // Ponemos de forma local el nombre del inputField del menu, alojado en uiManager, al jugador, y se lo notificamos al servidor.
        if (IsOwner) { 
            NameHolder.text = uiManager.GetComponent<InGameUI>().playerName.text;
            NotifyNameServerRpc(NameHolder.text);
        }
    }

    private void OnEnable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
    }



    private void OnDisable()
    {
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
    }

    #endregion

    #region Config Methods

    public void StartPlayer()
    {
        if (IsLocalPlayer)
        {
            // Health Init
            maxHealth = 6;

            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();
        }
    }

    void ConfigurePlayer()
    {
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
    }

    void ConfigureCamera()
    {
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    void ConfigureControls()
    {
        GetComponent<InputHandler>().enabled = true;
    }

    private void FixedUpdate()
    {
        if (IsServer) {
            //UpdatePositionClientRpc(transform.position); // Mecanismo para actualizar la posicion en caso de quitar el NetworkTransform

            NameHolder.transform.position = transform.position + (Vector3.up * 0.3f); // Actualiza la transformada del componente que almacena el nombre para que siga al jugador
            UpdatePositionNameClientRpc(NameHolder.transform.position);
            NotifyTimerClientRpc(Timer.Value);
        }
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        State.Value = state;
    }

    // Esta funcion está preparada para quitar el networkTransform. Mientras no se use es porque el Network Transform está activado
    [ClientRpc]
    void UpdatePositionClientRpc(Vector3 position) {
        transform.position = position;
    }

    [ClientRpc]
    void UpdatePositionNameClientRpc(Vector3 position)
    {
        NameHolder.transform.position = position;
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        State.Value = current;
    }

    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.tag == "Bullet") // En caso de que impacte una bala sobre el jugador, recogemos el id del jugador del cual pertenece la bala, y comprobamos...
            {
                ulong ownerOfTheBulletId = collision.gameObject.GetComponent<NetworkObject>().OwnerClientId;
                ulong shootedPlayerId = this.GetComponent<NetworkObject>().NetworkObjectId;
                Debug.Log("Jugador con ID " + shootedPlayerId + " alcanzado por la bala del jugador con ID: " + ownerOfTheBulletId);
                Life.Value -= 1;

                // Send bullet out of bounds of the map
                collision.gameObject.transform.position = new Vector3(-20.0f, .0f, .0f);
                

                // En caso de muerte
                if (Life.Value == 0) {
                    // Anotamos la kill al otro jugador y actualizamos su HUD
                    GetNetworkObject(ownerOfTheBulletId).GetComponent<Player>().Kills.Value += 1;
                    int kills = GetNetworkObject(ownerOfTheBulletId).GetComponent<Player>().Kills.Value;
                    GetNetworkObject(ownerOfTheBulletId).GetComponent<Player>().UpdateKillsHUDClientRpc(kills);

                    // Notifica la kill a todos los clientes, para que aparezca en el feed de bajas/muertes
                    string nameMata = GetNetworkObject(ownerOfTheBulletId).GetComponent<Player>().NameHolder.text;
                    string nameMuere = NameHolder.text;
                    foreach(NetworkClient nc in NetworkManager.Singleton.ConnectedClientsList) { 
                        nc.PlayerObject.GetComponent<Player>().UpdateFeedClientRpc(nameMata, nameMuere);
                    }


                    // Desactivamos el hook por si muere con el activado, para que no reaparezca con el gancho puesto
                    this.gameObject.GetComponent<GrapplingHook>().GetComponent<LineRenderer>().enabled = false;
                    this.gameObject.GetComponent<GrapplingHook>().GetComponent<DistanceJoint2D>().enabled = false;
                    DeactivateHookOnClientRpc();

                    // Llamada al gestor de spawn para que seleccione una posicion de reaparición válida
                    spawner.NewSpawnPoint();
                    Life.Value = 6;
                    Deaths.Value += 1;
                    // Update HUD
                    UpdateDeathsHUDClientRpc(Deaths.Value);

                    // Actualiza la corona
                    this.gameObject.GetComponent<GiveCrown>().UpdateCrown();
                }

                // Update HUD
                UpdateLifeHUDClientRpc(Life.Value);
            }
        }
    }


    #region Update UIs
    [ClientRpc]
    void UpdateLifeHUDClientRpc(int currentLife){
        if (IsOwner) { 
            uiManager.GetComponent<InGameUI>().UpdateLifeUI(currentLife);
        }
    }
    [ClientRpc]
    void UpdateDeathsHUDClientRpc(int deaths)
    {
        if (IsOwner)
        {
            uiManager.GetComponent<InGameUI>().UpdateDeathsUI(deaths);
        }
    }

    [ClientRpc]
    void UpdateKillsHUDClientRpc(int currentKills)
    {
        if (IsOwner)
        {
            uiManager.GetComponent<InGameUI>().UpdateKillsUI(currentKills);
        }
    }

    [ClientRpc]
    void UpdateFeedClientRpc(string mata, string muere)
    {
        if (IsOwner) { 
            uiManager.GetComponent<Feed>().AddFeed(mata + " has killed " + muere);
        }
    }

    #endregion


    [ServerRpc] // El jugador envia su nombre al servidor
    void NotifyNameServerRpc(string n) {
        // Pone el nameHolder en el lado del servidor
        NameHolder.text = n;
        

        // FUNCIONA!! QUÉ ALEGRIA. Lo que me ha costado...sacar los nombres...
        foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList) {
            string name = client.PlayerObject.GetComponent<Player>().NameHolder.text;
            client.PlayerObject.GetComponent<Player>().NotifyClientNameClientRpc(name);
        }
        
    }

    [ClientRpc]
    void NotifyClientNameClientRpc(string n)
    {
        if (!IsOwner)
        {
            NameHolder.text = n;
        }
    }

    [ClientRpc]
    void DeactivateHookOnClientRpc()
    {
        if (IsOwner) { 
            this.gameObject.GetComponent<GrapplingHook>().GetComponent<DistanceJoint2D>().enabled = false;
        }
        this.gameObject.GetComponent<GrapplingHook>().GetComponent<LineRenderer>().enabled = false;
    }


    [ClientRpc]
    void NotifyTimerClientRpc(int time) {
        if (IsOwner) {
            uiManager.GetComponent<InGameUI>().UpdateTimer(time);
        }
    }

    [ClientRpc]
    public void NotifyLadderboardClientRpc(string s) {
        if (IsOwner) { 
            uiManager.GetComponent<InGameUI>().AddToLadderBoard(s);
        }
    }

    [ClientRpc]
    public void NotifyShowLadderboardClientRpc()
    {
        if (IsOwner)
        {
            uiManager.GetComponent<InGameUI>().ShowLadderboard();
        }
    }

}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}




