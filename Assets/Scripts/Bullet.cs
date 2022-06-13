using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float bulletSpeed = 5f;
    [HideInInspector] public Vector2 shootDir;
    void Start()
    {
        // Nada mas ser instanciada, se le aplica la velocidad que le diga el jugador que la dispara
        this.GetComponent<Rigidbody2D>().velocity = shootDir * bulletSpeed;
        Destroy(this, 3.0f);
    }
   
    private void FixedUpdate()
    {
        if (IsServer) {
            UpdateBulletPosClientRpc(transform.position);
        }
    }

    [ClientRpc]
    void UpdateBulletPosClientRpc(Vector3 bulletPos) {
        transform.position = bulletPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Choque con lo que choque, la envia fuera del mapa. Se destruye al cabo de 3 segundos desde su aparición.
        if (IsServer)
        {
            transform.position = new Vector3(-20.0f, .0f, .0f);
        }
    }
}
