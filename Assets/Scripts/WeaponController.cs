using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Caractéristiques d'une arme.</summary>
[Serializable]
public class WeaponData
{
    public string name;
    public float damage;          // dégâts par projectile
    public float fireRate;        // tirs par seconde
    public int magSize;           // taille du chargeur
    public float reloadTime;      // secondes
    public float spread;          // dispersion en degrés
    public int pellets = 1;       // projectiles par tir (fusil à pompe)
    public bool automatic;
    public float recoil;          // recul vertical en degrés
    public float range = 200f;
    public float zoomFov = 0f;    // 0 = pas de zoom

    public static WeaponData[] CreateDefaults()
    {
        return new[]
        {
            new WeaponData { name = "Pistolet",        damage = 24f, fireRate = 6f,   magSize = 12, reloadTime = 1.2f, spread = 0.4f, automatic = false, recoil = 1.2f },
            new WeaponData { name = "Fusil d'assaut",  damage = 13f, fireRate = 11f,  magSize = 30, reloadTime = 1.8f, spread = 1.1f, automatic = true,  recoil = 0.8f },
            new WeaponData { name = "Fusil à pompe",   damage = 11f, fireRate = 1.1f, magSize = 6,  reloadTime = 2.2f, spread = 4.5f, pellets = 8, automatic = false, recoil = 4f, range = 60f },
            new WeaponData { name = "Sniper",          damage = 85f, fireRate = 1.0f, magSize = 5,  reloadTime = 2.4f, spread = 0.05f, automatic = false, recoil = 5f, zoomFov = 20f },
        };
    }
}

/// <summary>Gère l'inventaire, les munitions, le rechargement et le tir (hitscan).</summary>
public class WeaponController : MonoBehaviour
{
    public Transform muzzle;                 // point de départ visuel du tracer
    public LayerMask hitMask = ~0;
    public float headshotMultiplier = 2f;
    public WeaponData[] weapons;

    public int CurrentIndex { get; private set; }
    public WeaponData Current => weapons[CurrentIndex];
    public int Ammo => ammo[CurrentIndex];
    public bool IsReloading => reloading;
    public float ReloadProgress => reloading ? 1f - Mathf.Clamp01((reloadEnd - Time.time) / Current.reloadTime) : 1f;

    /// <summary>(victime touchée, headshot ?)</summary>
    public event Action<Health, bool> HitConfirmed;

    int[] ammo;
    float nextShotTime;
    float reloadEnd;
    bool reloading;
    Health owner;

    static Material tracerMat, impactMat, bloodMat;

    void Awake()
    {
        owner = GetComponent<Health>();
        if (weapons == null || weapons.Length == 0)
            weapons = WeaponData.CreateDefaults();
        ammo = new int[weapons.Length];
        RefillAll();
    }

    void Update()
    {
        if (reloading && Time.time >= reloadEnd)
        {
            ammo[CurrentIndex] = Current.magSize;
            reloading = false;
        }
    }

    public void RefillAll()
    {
        for (int i = 0; i < weapons.Length; i++)
            ammo[i] = weapons[i].magSize;
        reloading = false;
    }

    public void Equip(int index)
    {
        if (index < 0 || index >= weapons.Length || index == CurrentIndex) return;
        CurrentIndex = index;
        reloading = false;
        nextShotTime = Time.time + 0.25f;
    }

    public void StartReload()
    {
        if (reloading || Ammo >= Current.magSize) return;
        reloading = true;
        reloadEnd = Time.time + Current.reloadTime;
    }

    /// <summary>Tente de tirer. Retourne true si un tir est parti.</summary>
    public bool Fire(Vector3 origin, Vector3 direction, float extraSpread = 0f)
    {
        if (reloading || Time.time < nextShotTime) return false;
        if (Ammo <= 0) { StartReload(); return false; }
        if (owner == null) owner = GetComponent<Health>();

        var w = Current;
        ammo[CurrentIndex]--;
        nextShotTime = Time.time + 1f / w.fireRate;

        for (int i = 0; i < w.pellets; i++)
            FireRay(origin, direction, w.spread + extraSpread, w);

        if (ammo[CurrentIndex] <= 0) StartReload();
        return true;
    }

    void FireRay(Vector3 origin, Vector3 dir, float spreadDeg, WeaponData w)
    {
        dir = ApplySpread(dir, spreadDeg);
        Vector3 end = origin + dir * w.range;

        var hits = Physics.RaycastAll(origin, dir, w.range, hitMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            var victim = hit.collider.GetComponentInParent<Health>();
            if (victim != null && victim == owner) continue; // on ignore son propre corps

            end = hit.point;
            if (victim != null)
            {
                if (!victim.IsDead)
                {
                    bool head = hit.point.y - victim.transform.position.y > victim.headHeight;
                    float dmg = w.damage * (head ? headshotMultiplier : 1f);
                    victim.TakeDamage(dmg, owner);
                    HitConfirmed?.Invoke(victim, head);
                }
                SpawnImpact(hit.point, true);
            }
            else
            {
                SpawnImpact(hit.point, false);
            }
            break;
        }

        SpawnTracer(muzzle != null ? muzzle.position : origin, end);
    }

    static Vector3 ApplySpread(Vector3 dir, float spreadDeg)
    {
        if (spreadDeg <= 0.001f) return dir;
        Vector3 right = Vector3.Cross(Vector3.up, dir);
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(dir, right);
        Vector2 r = Random.insideUnitCircle * Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
        return (dir + right * r.x + up * r.y).normalized;
    }

    // ---------- effets visuels ----------

    static GameObject MakePrimitive(PrimitiveType type, ref Material cache, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        var rend = go.GetComponent<Renderer>();
        if (cache == null)
        {
            cache = new Material(rend.sharedMaterial) { color = color };
        }
        rend.sharedMaterial = cache;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static void SpawnTracer(Vector3 a, Vector3 b)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.2f) return;

        var t = MakePrimitive(PrimitiveType.Cube, ref tracerMat, new Color(1f, 0.9f, 0.5f));
        t.transform.position = a + d * 0.5f;
        t.transform.rotation = Quaternion.LookRotation(d);
        t.transform.localScale = new Vector3(0.015f, 0.015f, len);
        Destroy(t, 0.04f);
    }

    static void SpawnImpact(Vector3 point, bool body)
    {
        var go = body
            ? MakePrimitive(PrimitiveType.Sphere, ref bloodMat, new Color(0.9f, 0.1f, 0.1f))
            : MakePrimitive(PrimitiveType.Sphere, ref impactMat, new Color(0.8f, 0.8f, 0.75f));
        go.transform.position = point;
        go.transform.localScale = Vector3.one * (body ? 0.18f : 0.12f);
        Destroy(go, 0.15f);
    }
}
