using UnityEngine;

/// <summary>Adversaire contrôlé par l'ordinateur : poursuit, esquive latéralement et tire.</summary>
[RequireComponent(typeof(CharacterController))]
public class BotAI : MonoBehaviour
{
    public Transform target;
    public Transform eye;
    public bool Active;

    [Header("Mouvement")]
    public float moveSpeed = 6f;
    public float acceleration = 40f;
    public float turnSpeed = 360f;      // degrés / seconde
    public float gravity = -28f;
    public float preferredMin = 6f;
    public float preferredMax = 14f;

    [Header("Difficulté")]
    public float reactionTime = 0.4f;   // délai avant de tirer après avoir vu le joueur
    public float aimError = 3f;         // erreur de visée en degrés
    public float fireDelayScale = 1.5f; // >1 = tire moins vite que l'arme ne le permet

    CharacterController cc;
    WeaponController weapon;
    Health targetHealth;
    Vector3 horizVel;
    float vertVel;
    float strafeDir = 1f;
    float nextStrafeChange;
    float seenSince = -1f;
    float nextShot;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        weapon = GetComponent<WeaponController>();
    }

    public void Teleport(Vector3 position, float yaw)
    {
        cc.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cc.enabled = true;
        horizVel = Vector3.zero;
        vertVel = 0f;
        seenSince = -1f;
    }

    void Update()
    {
        if (targetHealth == null && target != null)
            targetHealth = target.GetComponent<Health>();

        Vector3 wishDir = Vector3.zero;

        if (Active && target != null && targetHealth != null && !targetHealth.IsDead)
            wishDir = Think();

        Vector3 wishVel = wishDir.sqrMagnitude > 0.001f ? wishDir.normalized * moveSpeed : Vector3.zero;
        horizVel = Vector3.MoveTowards(horizVel, wishVel, acceleration * Time.deltaTime);

        if (cc.isGrounded && vertVel < 0f) vertVel = -2f;
        vertVel += gravity * Time.deltaTime;

        cc.Move((horizVel + Vector3.up * vertVel) * Time.deltaTime);
    }

    Vector3 Think()
    {
        Vector3 aimPoint = target.position + Vector3.up * 1.2f;
        Vector3 toTarget = aimPoint - eye.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = flat.magnitude;
        if (dist < 0.01f) return Vector3.zero;
        Vector3 fwd = flat / dist;

        // se tourner vers le joueur
        Quaternion want = Quaternion.LookRotation(fwd);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);

        bool los = HasLineOfSight(aimPoint);
        if (los) { if (seenSince < 0f) seenSince = Time.time; }
        else seenSince = -1f;

        Vector3 move;
        if (los)
        {
            if (Time.time >= nextStrafeChange)
            {
                strafeDir = Random.value < 0.5f ? -1f : 1f;
                nextStrafeChange = Time.time + Random.Range(0.6f, 1.8f);
                if (Random.value < 0.25f) TryJump();
            }

            float forward = dist > preferredMax ? 1f : (dist < preferredMin ? -1f : 0f);
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            move = fwd * forward + right * strafeDir * 0.8f;

            bool aligned = Vector3.Angle(transform.forward, fwd) < 8f;
            if (aligned && Time.time - seenSince >= reactionTime && Time.time >= nextShot)
            {
                if (weapon.Fire(eye.position, toTarget.normalized, aimError))
                    nextShot = Time.time + (1f / weapon.Current.fireRate) * fireDelayScale;
            }
        }
        else
        {
            move = fwd; // on fonce vers le joueur
        }

        return Avoid(move);
    }

    bool HasLineOfSight(Vector3 point)
    {
        if (!Physics.Linecast(eye.position, point, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            return true;
        return hit.collider.GetComponentInParent<Health>() == targetHealth;
    }

    bool Blocked(Vector3 origin, Vector3 dir)
    {
        return Physics.SphereCast(origin, 0.35f, dir, out RaycastHit h, 1.5f, ~0, QueryTriggerInteraction.Ignore)
               && h.collider.GetComponentInParent<Health>() == null;
    }

    Vector3 Avoid(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return dir;
        dir.Normalize();

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (!Blocked(origin, dir)) return dir;

        Vector3 left = Quaternion.Euler(0f, -60f, 0f) * dir;
        Vector3 right = Quaternion.Euler(0f, 60f, 0f) * dir;
        bool leftFree = !Blocked(origin, left);
        bool rightFree = !Blocked(origin, right);

        if (leftFree && rightFree) return Random.value < 0.5f ? left : right;
        if (leftFree) return left;
        if (rightFree) return right;
        return -dir;
    }

    void TryJump()
    {
        if (cc.isGrounded)
            vertVel = Mathf.Sqrt(1.4f * -2f * gravity);
    }
}
