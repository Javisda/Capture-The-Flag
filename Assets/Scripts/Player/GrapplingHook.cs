using UnityEngine;
using Unity.Netcode;

public class GrapplingHook : NetworkBehaviour
{
    #region Variables

    InputHandler handler;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/DistanceJoint2D.html
    DistanceJoint2D rope;
    // // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LineRenderer.html
    LineRenderer ropeRenderer;
    Transform playerTransform;
    [SerializeField] Material material;
    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/LayerMask.html
    LayerMask layerTerrain;
    LayerMask layerPlayer; // Layer de los jugadores, para poder hacer el rayCasting
    ulong idPlayerOnHit; // id del jugador al que estamos enganchados, para poder actualizar el otro extremo del gancho a su posicion. Si no hacemos esto, el gancho se engancha, efectivamente, pero el extremo se quedaría estático
    bool hitOnPlayer; // Esta variable controla que si nos hemos enganchado a un jugador y nos desenganchamos, al volvernos a enganchar a algo no enganche al jugador anterior directamente, ya que sin este control el gancho se lanzaría automáticamente al jugador enganchado con anterioridad
    Player player;

    readonly float climbSpeed = 2f;
    readonly float swingForce = 80f;

    Rigidbody2D rb;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<float> ropeDistance;

    #endregion


    #region Unity Event Functions

    void Awake()
    {
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();

        //Configure Rope Renderer
        ropeRenderer = gameObject.AddComponent<LineRenderer>();
        ropeRenderer.startWidth = .05f;
        ropeRenderer.endWidth = .05f;
        ropeRenderer.material = material;
        ropeRenderer.sortingOrder = 3;
        ropeRenderer.enabled = false;

        // Configure Rope
        rope = gameObject.AddComponent<DistanceJoint2D>();
        rope.enableCollision = true;
        rope.enabled = false;

        playerTransform = transform;
        layerTerrain = LayerMask.GetMask("Obstacles");
        layerPlayer = LayerMask.GetMask("Player");

        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        ropeDistance = new NetworkVariable<float>();
    }

    private void OnEnable()
    {
        handler.OnHookRender.AddListener(UpdateHookServerRpc);
        handler.OnMoveFixedUpdate.AddListener(SwingRopeServerRpc);
        handler.OnJump.AddListener(JumpPerformedServerRpc);
        handler.OnHook.AddListener(LaunchHookServerRpc);

        ropeDistance.OnValueChanged += OnRopeDistanceValueChanged;
    }

    private void OnDisable()
    {
        handler.OnHookRender.RemoveListener(UpdateHookServerRpc);
        handler.OnMoveFixedUpdate.RemoveListener(SwingRopeServerRpc);
        handler.OnJump.RemoveListener(JumpPerformedServerRpc);
        handler.OnHook.RemoveListener(LaunchHookServerRpc);

        ropeDistance.OnValueChanged -= OnRopeDistanceValueChanged;
    }

    #endregion

    #region Netcode RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateHookServerRpc(Vector2 input)
    {
        if (player.State.Value == PlayerState.Hooked)
        {
            ClimbRope(input.y);
            UpdateRopeClientRpc();

        }
        else if (player.State.Value == PlayerState.Grounded || player.State.Value == PlayerState.Jumping)
        {
            hitOnPlayer = false;
            RemoveRopeClientRpc();
            rope.enabled = false;
            ropeRenderer.enabled = false;
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void JumpPerformedServerRpc()
    {
        RemoveRopeClientRpc();
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void LaunchHookServerRpc(Vector2 input)
    {

        // Primero hacemos el raycasting al terreno
        hitOnPlayer = false;

        var hitTerrain = Physics2D.Raycast(playerTransform.position, input - (Vector2)playerTransform.position, Mathf.Infinity, layerTerrain);

        if (hitTerrain.collider)
        {
            var anchor = hitTerrain.centroid;
            rope.connectedAnchor = anchor;
            ropeRenderer.SetPosition(1, anchor);
            UpdateAnchorClientRpc(hitTerrain.centroid);
            player.State.Value = PlayerState.Hooked;


            // Mostramos el gancho en el servidor
            ropeRenderer.SetPosition(1, anchor);
            ropeRenderer.SetPosition(0, playerTransform.position);
            // Y lo activamos
            rope.enabled = true;
            ropeRenderer.enabled = true;
        }


        // En caso de haber colision con el terreno, se sobreescribe el resultado en caso de haber con el jugador también
        // Colision de gancho con jugadores.
        Vector3 direction = input - (Vector2)playerTransform.position;
        var hitPlayer = Physics2D.Raycast(playerTransform.position + (direction.normalized * 0.6f), direction, Mathf.Infinity, layerPlayer);

        if (hitPlayer.collider)
        {
            hitOnPlayer = true;

            idPlayerOnHit = hitPlayer.collider.gameObject.GetComponent<NetworkObject>().OwnerClientId;

            var anchor2 = hitPlayer.collider.gameObject.transform.position;
            rope.connectedAnchor = anchor2;
            ropeRenderer.SetPosition(1, anchor2);
            UpdateAnchorClientRpc(anchor2);
            player.State.Value = PlayerState.Hooked;


            // Mostramos el gancho en el servidor
            ropeRenderer.SetPosition(1, anchor2);
            ropeRenderer.SetPosition(0, playerTransform.position);
            // Y lo activamos
            rope.enabled = true;
            ropeRenderer.enabled = true;

        }


    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void SwingRopeServerRpc(Vector2 input)
    {
        if (player.State.Value == PlayerState.Hooked)
        {
            // Player 2 hook direction
            var direction = (rope.connectedAnchor - (Vector2)playerTransform.position).normalized;

            // Perpendicular direction
            var forceDirection = new Vector2(input.x * direction.y, direction.x);

            var force = forceDirection * swingForce;
            rb.AddForce(force, ForceMode2D.Force); // Añade fuerza al rigidBody usando su masa


            // Actualizamos el gancho en el servidor
            ropeRenderer.SetPosition(0, playerTransform.position);

            // Actualizamos el gancho con el jugador cuyo id es el del jugador impactado -> idPlayerOnHit.
            if (hitOnPlayer) { 
                foreach (NetworkClient c in NetworkManager.Singleton.ConnectedClientsList) {
                    if (c.ClientId == idPlayerOnHit) {
                        ropeRenderer.SetPosition(1, c.PlayerObject.transform.position);
                        UpdateAnchorClientRpc(c.PlayerObject.transform.position);

                        // En caso de que muera el jugador al que estamos enganchados, quitamos su estado de enganchado. No funciona por el momento
                        if (c.PlayerObject.GetComponent<Player>().Life.Value <= 0) {
                            player.State.Value = PlayerState.Jumping;
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region ClientRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateAnchorClientRpc(Vector2 anchor)
    {
        rope.connectedAnchor = anchor;
        ShowRopeClientRpc();
        ropeRenderer.SetPosition(1, anchor);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void UpdateRopeClientRpc()
    {
        ropeRenderer.SetPosition(0, playerTransform.position); // 0 es un punto extremo de la recta a renderizar. Si se pone 1 será el otro extremo
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void ShowRopeClientRpc()
    {
        rope.enabled = true;
        ropeRenderer.enabled = true;
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/clientrpc
    [ClientRpc]
    void RemoveRopeClientRpc()
    {
        rope.enabled = false;
        ropeRenderer.enabled = false;
    }

    #endregion

    #endregion

    #region Methods

    void ClimbRope(float input)
    {
        ropeDistance.Value = (input) * climbSpeed * Time.deltaTime;
    }

    void OnRopeDistanceValueChanged(float previous, float current)
    {
        rope.distance -= current;
    }

    #endregion
}
