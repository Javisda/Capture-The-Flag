using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Spawner : NetworkBehaviour
{
    private Vector3[] SpawnPoints = new Vector3[8];
    void Start()
    {
        if (IsServer) { 
            SpawnPoints[0] = new Vector3(0, 0, 0);
            SpawnPoints[1] = new Vector3(2.5f, 3.5f, 0);
            SpawnPoints[2] = new Vector3(-7.0f, -2.0f, 0);
            SpawnPoints[3] = new Vector3(-1.0f, 3.0f, 0);
            SpawnPoints[4] = new Vector3(-6.0f, 6.0f, 0);
            SpawnPoints[5] = new Vector3(6.5f, 7.0f, 0);
            SpawnPoints[6] = new Vector3(9.0f, -0.6f, 0);
            SpawnPoints[7] = new Vector3(1.05f, -4.0f, 0);
        }
    }

    // Llamada únicamente desde el servidor
    public void NewSpawnPoint() {
        Vector3 pos = GetPosition();
        float segundos = 3.0f;

        // Hacemos una cuenta atrás de 3 segundos
        StartCoroutine(WaitBeforeSpawn(pos, segundos));
    }

    private Vector3 GetPosition() {

        // Genera aleatoriamente una posicion de SpawnPoint y comprueba si puede reaparecer en ella. Esto dependerá de si hay algún jugador a menos de 2 de distancia de dicho punto

        bool posFound;
        Vector3 pos;
        while (true) {
            pos = SpawnPoints[Random.Range(1, 7)];
            posFound = true;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                float distance = Vector3.Distance(pos, client.PlayerObject.transform.position);
                if (distance <= 2)
                {
                    posFound = false; // Posición no válida
                    break;
                }
            }

            // En caso de que haya encontrado la posicion, la devolvemos
            if (posFound)
            {
                return pos;
            }
        }


        // Lo pongo porque el programa lo pide, pero no se va a ejecutar nunca ya que siempre va a haber una posicion disponible.
        return Vector3.zero;
    }


    IEnumerator WaitBeforeSpawn(Vector3 newPos, float time) {

        ActivateDeactivatePlayerClientRpc(false);
        ActivateDeactivateOnServer(false);

        yield return new WaitForSeconds(time);

        ActivateDeactivatePlayerClientRpc(true);
        ActivateDeactivateOnServer(true);
        this.gameObject.transform.position = newPos;
    }

    [ClientRpc]
    void ActivateDeactivatePlayerClientRpc(bool option) {
        if (IsOwner) {
            this.gameObject.GetComponent<Player>().uiManager.GetComponent<InGameUI>().SetRespawningText(!option);
            this.gameObject.GetComponent<Player>().enabled = option;
            this.gameObject.GetComponent<InputHandler>().enabled = option;
            this.gameObject.GetComponent<WeaponAim>().crossHairRenderer.enabled = option;
        }

        // Estos elementos se ejecutan tanto para el owner como para los demás jugadores
        this.gameObject.GetComponentInChildren<Canvas>().enabled = option;
        this.gameObject.GetComponent<SpriteRenderer>().enabled = option;
        this.gameObject.GetComponent<WeaponAim>().weaponRenderer.enabled = option;
    }
    void ActivateDeactivateOnServer(bool option)
    {
        this.gameObject.GetComponent<Player>().enabled = option;
        this.gameObject.GetComponent<InputHandler>().enabled = option;
        this.gameObject.GetComponent<SpriteRenderer>().enabled = option;
        this.gameObject.GetComponentInChildren<Canvas>().enabled = option;
        this.gameObject.GetComponent<WeaponAim>().weaponRenderer.enabled = option;
        this.gameObject.GetComponent<CapsuleCollider2D>().enabled = option;
        this.gameObject.GetComponent<Rigidbody2D>().gravityScale = option ? 1 : Mathf.Epsilon;
    }
}
