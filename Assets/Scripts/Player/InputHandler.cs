using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Netcode;

public class InputHandler : NetworkBehaviour
{

    private WeaponAim weaponAim;
    [SerializeField] GameObject bulletPrefab;

    #region Variables

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/index.html
    [SerializeField] InputAction _move;
    [SerializeField] InputAction _jump;
    [SerializeField] InputAction _hook;
    [SerializeField] InputAction _fire;
    [SerializeField] InputAction _mousePosition;

    // https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html
    public UnityEvent<Vector2> OnMove;
    public UnityEvent<Vector2> OnMoveFixedUpdate;
    public UnityEvent<Vector2> OnMousePosition;
    public UnityEvent<Vector2> OnHook;
    public UnityEvent<Vector2> OnHookRender;
    public UnityEvent OnJump;
    public UnityEvent OnFire;

    Player player;

    Vector2 CachedMoveInput { get; set; }

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Left", "<Keyboard>/a")
            .With("Down", "<Keyboard>/s")
            .With("Right", "<Keyboard>/d");

        _jump.AddBinding("<Keyboard>/space");
        _hook.AddBinding("<Mouse>/middleButton");
        _fire.AddBinding("<Mouse>/leftButton");
        _mousePosition.AddBinding("<Mouse>/position");

        player = GetComponent<Player>();
        weaponAim = GetComponent<WeaponAim>();
    }

    private void OnEnable()
    {
        _move.Enable();
        _jump.Enable();
        _hook.Enable();
        _fire.Enable();
        _mousePosition.Enable();
    }

    private void OnDisable()
    {
        _move.Disable();
        _jump.Disable();
        _hook.Disable();
        _fire.Disable();
        _mousePosition.Disable();
    }

    private void Update()
    {
        if (IsOwner)
        {
            CachedMoveInput = _move.ReadValue<Vector2>();
            var mousePosition = _mousePosition.ReadValue<Vector2>();

            var hookPerformed = _hook.WasPerformedThisFrame();
            var jumpPerformed = _jump.WasPerformedThisFrame();

            Move(CachedMoveInput);
            MousePosition(mousePosition);

            // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
            var screenPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            if (hookPerformed) { Hook(screenPoint); }

            if (jumpPerformed) { Jump(); }
            if (_fire.WasPerformedThisFrame()) { Fire(); }

            HookRender(CachedMoveInput);
        }
    }

    private void FixedUpdate()
    {
        MoveFixedUpdate(CachedMoveInput);
    }

    #endregion

    #region InputSystem Related Methods

    void Move(Vector2 input)
    {
        OnMove?.Invoke(input);
    }

    void MoveFixedUpdate(Vector2 input)
    {
        OnMoveFixedUpdate?.Invoke(input);
    }

    void Jump()
    {
        OnJump?.Invoke();
    }

    void Hook(Vector2 input)
    {
        OnHook?.Invoke(input);
    }

    void HookRender(Vector2 input)
    {
        OnHookRender?.Invoke(input);
    }

    void Fire()
    {
        if (IsOwner) {
            Transform crossHair = weaponAim.crossHair;
            Vector3 shootDir = crossHair.transform.position - transform.position;
            shootDir.Normalize();

            // Instanciamos la bala en el servidor
            FireServerRpc(new Vector2(shootDir.x, shootDir.y), weaponAim.crossHair.transform.position, this.gameObject.GetComponent<Player>().NetId);
        }
    }

    [ServerRpc]
    void FireServerRpc(Vector2 shootDir, Vector3 posToShoot, ulong clientId) {
        GameObject bullet = Instantiate(bulletPrefab, posToShoot, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        bullet.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        Debug.Log("Bala disparada por jugador con ID: " + clientId);
        bullet.GetComponent<Bullet>().shootDir = shootDir;
        Destroy(bullet, 3.0f);
    }

    void MousePosition(Vector2 input)
    {
        OnMousePosition?.Invoke(input);
    }

    #endregion

}
