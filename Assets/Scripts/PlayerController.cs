using UnityEngine;

/// <summary>Contrôleur FPS : vue souris, déplacements rapides, double saut, dash, armes.</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Déplacement")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 10.5f;
    public float jumpHeight = 1.6f;
    public float gravity = -28f;
    public float groundAccel = 60f;
    public float airAccel = 20f;
    public int maxJumps = 2;
    public float dashSpeed = 24f;
    public float dashTime = 0.15f;
    public float dashCooldown = 1.5f;

    [Header("Vue")]
    public Camera cam;
    public float mouseSensitivity = 2f;
    public float baseFov = 80f;
    public float recoilRecovery = 6f;

    [HideInInspector] public bool canMove;
    [HideInInspector] public bool canFire;

    CharacterController cc;
    WeaponController weapon;
    float yaw, pitch, recoil;
    Vector3 horizVel;
    float vertVel;
    int jumpsLeft;
    float dashEnd, nextDash;
    Vector3 dashDir;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        weapon = GetComponent<WeaponController>();
    }

    void Start()
    {
        LockCursor(true);
    }

    public void Teleport(Vector3 position, float newYaw)
    {
        cc.enabled = false;
        transform.position = position;
        cc.enabled = true;
        yaw = newYaw;
        pitch = 0f;
        recoil = 0f;
        horizVel = Vector3.zero;
        vertVel = 0f;
        dashEnd = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    public void AddRecoil(float degrees)
    {
        recoil += degrees;
    }

    void Update()
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        HandleCursor();

        if (canMove && locked) Look();
        Move(locked);
        if (canFire && locked && weapon != null) HandleWeapons();
        UpdateCamera(locked);
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
    }

    static void LockCursor(bool on)
    {
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }

    void Look()
    {
        float sens = mouseSensitivity * (cam.fieldOfView / baseFov); // sensibilité réduite en zoom
        yaw += Input.GetAxis("Mouse X") * sens;
        pitch -= Input.GetAxis("Mouse Y") * sens;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void Move(bool locked)
    {
        bool active = canMove && locked;
        Vector2 input = active
            ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f);

        bool sprint = Input.GetKey(KeyCode.LeftShift) && input.y > 0.1f;
        float speed = sprint ? sprintSpeed : walkSpeed;
        Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed;

        bool grounded = cc.isGrounded;
        horizVel = Vector3.MoveTowards(horizVel, wish, (grounded ? groundAccel : airAccel) * Time.deltaTime);

        if (grounded)
        {
            if (vertVel < 0f) vertVel = -2f;
            jumpsLeft = maxJumps;
        }

        if (active)
        {
            if (Input.GetButtonDown("Jump") && jumpsLeft > 0)
            {
                vertVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsLeft--;
            }

            if (Input.GetKeyDown(KeyCode.E) && Time.time >= nextDash)
            {
                dashDir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                dashEnd = Time.time + dashTime;
                nextDash = Time.time + dashCooldown;
                horizVel = dashDir * walkSpeed;
                vertVel = Mathf.Max(vertVel, 0f);
            }
        }

        vertVel += gravity * Time.deltaTime;
        bool dashing = Time.time < dashEnd;
        Vector3 h = dashing ? dashDir * dashSpeed : horizVel;
        Vector3 v = dashing ? Vector3.zero : Vector3.up * vertVel;
        cc.Move((h + v) * Time.deltaTime);
    }

    void HandleWeapons()
    {
        int n = weapon.weapons.Length;

        for (int i = 0; i < n && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) weapon.Equip(i);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.01f) weapon.Equip((weapon.CurrentIndex + 1) % n);
        else if (scroll < -0.01f) weapon.Equip((weapon.CurrentIndex - 1 + n) % n);

        if (Input.GetKeyDown(KeyCode.R)) weapon.StartReload();

        bool wantFire = weapon.Current.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
        if (wantFire && weapon.Fire(cam.transform.position, cam.transform.forward))
            AddRecoil(weapon.Current.recoil);
    }

    void UpdateCamera(bool locked)
    {
        recoil = Mathf.MoveTowards(recoil, 0f, (recoilRecovery + recoil * 3f) * Time.deltaTime);
        cam.transform.localRotation = Quaternion.Euler(pitch - recoil, 0f, 0f);

        float targetFov = baseFov;
        if (canFire && locked && weapon != null && weapon.Current.zoomFov > 0f && Input.GetMouseButton(1))
            targetFov = weapon.Current.zoomFov;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 15f * Time.deltaTime);
    }
}
