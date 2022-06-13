using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class UIManager : MonoBehaviour
{

    #region Variables

    [SerializeField] NetworkManager networkManager;
    UnityTransport transport;
    readonly ushort port = 7777;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;

    // Lobby
    [SerializeField] private GameObject clientLobby;
    [SerializeField] private Button buttonReady;
    // Color Selector
    [SerializeField] private Button[] colorSelector;
    public int colorSelected; 
    [SerializeField] Text colorFeedback;
    // Sprite Selector
    [SerializeField] private Button[] spriteSelector;
    public int spriteSelected;
    [SerializeField] Text spriteFeedback;


    // Exit Button
    [SerializeField] private Button buttonExit;
    [SerializeField] private GameObject inGameRanking;


    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport;

        colorSelected = 8;  // Color base por defecto
        spriteSelected = 0; // Sprite base por defecto
    }

    private void Start()
    {
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttonReady.onClick.AddListener(() => SetReady());
        buttonExit.onClick.AddListener(() => GoMainMenu());
        ActivateMainMenu();
    }

    #endregion

    #region UI Related Methods

    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        //NetworkManager.Singleton.StartHost();
    }

    private void StartClient()
    {
        mainMenu.SetActive(false);
        clientLobby.SetActive(true);
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        mainMenu.SetActive(false);
    }

    private void SetReady() {
        var ip = inputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            transport.SetConnectionData(ip, port);
        }
        NetworkManager.Singleton.StartClient();

        clientLobby.SetActive(false);
        GetComponent<InGameUI>().enabled = true;
    }

    private void GoMainMenu() {
        inGameRanking.SetActive(false);
        mainMenu.SetActive(true);
    }


    // Funcion para el seleccionador de color
    public void SetColorIndex(int o) {
        colorSelected = o;

        switch (o) {
            case 0:
                colorFeedback.color = Color.yellow;
                colorFeedback.text = "Color Selected: Yellow.";
                break;
            case 1:
                colorFeedback.color = new Vector4(1.0f, 0.8419356f, 0.0f, 1.0f);
                colorFeedback.text = "Color Selected: Orange.";
                break;
            case 2:
                colorFeedback.color = Color.red;
                colorFeedback.text = "Color Selected: Red.";
                break;
            case 3:
                colorFeedback.color = new Vector4(1.0f, 0.0f, 0.6968298f, 1.0f);
                colorFeedback.text = "Color Selected: Pink.";
                break;
            case 4:
                colorFeedback.color = new Vector4(0.5141811f, 0.0f, 1.0f, 1.0f);
                colorFeedback.text = "Color Selected: Purple.";
                break;
            case 5:
                colorFeedback.color = Color.blue;
                colorFeedback.text = "Color Selected: Blue.";
                break;
            case 6:
                colorFeedback.color = Color.cyan;
                colorFeedback.text = "Color Selected: Cyan.";
                break;
            case 7:
                colorFeedback.color = Color.green;
                colorFeedback.text = "Color Selected: Green.";
                break;
            case 8:
                colorFeedback.color = Color.white;
                colorFeedback.text = "Color Selected: Default.";
                break;
            default:
                break;
        }
    }

    // Funcion para el seleccionador de sprites
    public void SetSpriteIndex(int o) {
        spriteSelected = o;

        switch (o)
        {
            case 0:
                spriteFeedback.text = "Player Selected: Steve.";
                break;
            case 1:
                spriteFeedback.text = "Player Selected: Razor.";
                break;
            case 2:
                spriteFeedback.text = "Player Selected: Danna.";
                break;
            case 3:
                spriteFeedback.text = "Player Selected: Lama.";
                break;
            case 4:
                spriteFeedback.text = "Player Selected: Spike.";
                break;
            case 5:
                spriteFeedback.text = "Player Selected: Wenn.";
                break;
            case 6:
                spriteFeedback.text = "Player Selected: Drone.";
                break;
            case 7:
                spriteFeedback.text = "Player Selected: Miner.";
                break;
            default:
                break;
        }
    }

    #endregion
}
