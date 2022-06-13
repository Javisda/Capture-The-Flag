using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class SpriteSelector : NetworkBehaviour
{
    SpriteRenderer spriteRenderer;
    [SerializeField] Sprite[] sprites;
    int spriteId;
    NetworkVariable<int> spriteIdx = new NetworkVariable<int>();
    [HideInInspector] public GameObject uiManager;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (IsOwner)
        {
            uiManager = GameObject.FindGameObjectWithTag("UIManager");
            spriteId = uiManager.GetComponent<UIManager>().spriteSelected; // Recoge el id del sprite seleccionado. En el menú, cada botón de sprite tiene un id asociado
            NotifySpriteToServerRpc(spriteId); // Le decimos al servidor el id del sprite que hemos escogido
        }
    }

    [ServerRpc]
    void NotifySpriteToServerRpc(int option)
    {
        spriteRenderer.sprite = sprites[option];
        spriteIdx.Value = option;

        // Notifica el sprite de todos los clientes al jugador
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            int id = client.PlayerObject.GetComponent<SpriteSelector>().spriteIdx.Value;
            client.PlayerObject.GetComponent<SpriteSelector>().NotifySpriteToClientRpc(id);
        }
    }

    [ClientRpc]
    void NotifySpriteToClientRpc(int option)
    {
        spriteRenderer.sprite = sprites[option];
    }
}
