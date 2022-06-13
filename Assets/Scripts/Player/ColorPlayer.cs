using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ColorPlayer : NetworkBehaviour
{


    SpriteRenderer spriteRenderer;
    [SerializeField] Color[] colours;
    int colorId;
    NetworkVariable<int> idx = new NetworkVariable<int>();
    [HideInInspector] public GameObject uiManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (IsOwner) {
            uiManager = GameObject.FindGameObjectWithTag("UIManager");
            colorId = uiManager.GetComponent<UIManager>().colorSelected;
            NotifyColorToServerRpc(colorId);
        }
    }

    [ServerRpc]
    void NotifyColorToServerRpc(int option) {
        spriteRenderer.color = colours[option];
        idx.Value = option;

        // Notifica el color de todos los clientes al jugador
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            int id = client.PlayerObject.GetComponent<ColorPlayer>().idx.Value;
            client.PlayerObject.GetComponent<ColorPlayer>().NotifyColorToClientRpc(id);
        }
    }

    [ClientRpc]
    void NotifyColorToClientRpc(int option) {
        spriteRenderer.color = colours[option];
    }
}
