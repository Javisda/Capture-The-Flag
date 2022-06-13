using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GiveCrown : NetworkBehaviour
{
    [SerializeField] SpriteRenderer crownRenderer;
    NetworkVariable<bool> hasCrown;

    private void Awake()
    {
         hasCrown = new NetworkVariable<bool>();
         hasCrown.Value = false;
    }

    private void OnEnable()
    {
        hasCrown.OnValueChanged += UpdateCrownViewer;
    }

    private void OnDisable()
    {
        hasCrown.OnValueChanged -= UpdateCrownViewer;
    }

    public void UpdateCrown() {
        
        if (IsServer) {
            ulong playerId = 0;
            int numKills = 0;
            // Se la quita a todos y comprueba qué jugador tiene más Kills por el momento
            foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList) {
                c.PlayerObject.GetComponent<GiveCrown>().hasCrown.Value = false;
                if (numKills < c.PlayerObject.GetComponent<Player>().Kills.Value) {
                    numKills = c.PlayerObject.GetComponent<Player>().Kills.Value;
                    playerId = c.ClientId;
                }
            }

            // Le damos la corona a quien corresponde
            foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (c.ClientId == playerId) {
                    c.PlayerObject.GetComponent<GiveCrown>().hasCrown.Value = true;
                    break;
                }
            }
        }
    }

    private void UpdateCrownViewer(bool previous, bool current) {
        crownRenderer.enabled = current;
    }
}
