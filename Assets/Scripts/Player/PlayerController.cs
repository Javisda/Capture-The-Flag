using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

// TODO: Implementar animacion isWalking

// Estos componentes son automaticamente añadidos al gameObject que tenga el script. Sirve para evitar errores de inicialización.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : NetworkBehaviour
{

    #region Variables

    readonly float speed = 3.4f;
    readonly float jumpHeight = 6.5f;
    readonly float gravity = 1.5f;
    readonly int maxJumps = 2;

    LayerMask _layer;
    int _jumpsLeft;

    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ContactFilter2D.html
    ContactFilter2D filter;
    InputHandler handler;
    Player player;
    Rigidbody2D rb;
    new CapsuleCollider2D collider;
    Animator anim;
    SpriteRenderer spriteRenderer;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> FlipSprite;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        FlipSprite = new NetworkVariable<bool>();
    }

    private void OnEnable()
    {
        handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.AddListener(PerformJumpServerRpc);
        handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;
    }

    private void OnDisable()
    {
        handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.RemoveListener(PerformJumpServerRpc);
        handler.OnMoveFixedUpdate.RemoveListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
    }

    void Start()
    {
        // Configure Rigidbody2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Detecta la colisión con cualquier geometría estática.
        rb.gravityScale = gravity;

        // Configure LayerMask
        _layer = LayerMask.GetMask("Obstacles");

        // Configure ContactFilter2D
        filter.minNormalAngle = 45;
        filter.maxNormalAngle = 135;
        filter.useNormalAngle = true;
        filter.layerMask = _layer;
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        UpdateAnimatorStateServerRpc(input);
        UpdateSpriteOrientation(input);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateAnimatorStateServerRpc(Vector2 input)
    {
        if (IsGrounded)
        {
            anim.SetBool("isJumping", false);

            if (input.x > -0.5f && input.x < 0.5f)
            {
                anim.SetBool("isWalking", false);
                anim.SetBool("isGrounded", true);
            }
            else { // En caso de mover las teclas de movimiento
                anim.SetBool("isGrounded", false);
                anim.SetBool("isWalking", true);
            }
        }
        else
        {
            anim.SetBool("isGrounded", false);
            anim.SetBool("isWalking", false);
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void PerformJumpServerRpc()
    {
        if (player.State.Value == PlayerState.Grounded)
        {
            _jumpsLeft = maxJumps;
        }
        else if (_jumpsLeft == 0)
        {
            return;
        }
        player.State.Value = PlayerState.Jumping;


        anim.SetBool("isJumping", true);
        rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
        _jumpsLeft--;

        Debug.Log(_jumpsLeft);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        if (IsGrounded)
        {
            player.State.Value = PlayerState.Grounded;
        }

        if ((player.State.Value != PlayerState.Hooked))
        {
            rb.velocity = new Vector2(input.x * speed, rb.velocity.y);
        }
    }

    #endregion

    #endregion

    #region Methods

    void UpdateSpriteOrientation(Vector2 input)
    {
        if (input.x < 0)
        {
            FlipSprite.Value = false;
        }
        else if (input.x > 0)
        {
            FlipSprite.Value = true;
        }
    }

    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        spriteRenderer.flipX = current;
    }

    // Sabremos que está en el suelo 100% cuando la velocidad del jugador en el eje y sea 0
    bool IsGrounded => collider.IsTouching(filter) && rb.velocity.y == 0;

    #endregion

}
