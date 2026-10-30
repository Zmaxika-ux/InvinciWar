using UnityEngine;

/// <summary>
/// Construit tout le jeu au lancement (arène, joueur, bot, manche, HUD).
/// Aucune configuration de scène nécessaire : ouvrez une scène vide et appuyez sur Play.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindFirstObjectByType<MatchManager>() != null) return;

        Transform[] spawns = BuildArena();
        PlayerController player = BuildPlayer();
        BotAI bot = BuildBot(player.transform);

        var match = new GameObject("MatchManager").AddComponent<MatchManager>();
        match.player = player;
        match.bot = bot;
        match.spawnPoints = spawns;

        var hud = new GameObject("HUD").AddComponent<HUD>();
        hud.match = match;
    }

    // ---------- arène ----------

    static Transform[] BuildArena()
    {
        var root = new GameObject("Arena").transform;

        // lumière
        if (Object.FindFirstObjectByType<Light>() == null)
        {
            var l = new GameObject("Sun").AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            l.shadows = LightShadows.Soft;
        }

        Color floor = new Color(0.20f, 0.22f, 0.27f);
        Color wall = new Color(0.30f, 0.33f, 0.40f);
        Color orange = new Color(0.95f, 0.55f, 0.15f);
        Color teal = new Color(0.15f, 0.70f, 0.70f);

        // sol et murs d'enceinte (60 x 60)
        Box(root, new Vector3(0, -0.5f, 0), new Vector3(62, 1, 62), floor);
        Box(root, new Vector3(0, 3, 30.5f), new Vector3(62, 6, 1), wall);
        Box(root, new Vector3(0, 3, -30.5f), new Vector3(62, 6, 1), wall);
        Box(root, new Vector3(30.5f, 3, 0), new Vector3(1, 6, 62), wall);
        Box(root, new Vector3(-30.5f, 3, 0), new Vector3(1, 6, 62), wall);

        // plateforme centrale + rampes
        Box(root, new Vector3(0, 0.75f, 0), new Vector3(10, 1.5f, 10), orange);
        Box(root, new Vector3(0, 0.7f, -8f), new Vector3(5, 0.3f, 6.2f), teal, Quaternion.Euler(-14f, 0, 0));
        Box(root, new Vector3(0, 0.7f, 8f), new Vector3(5, 0.3f, 6.2f), teal, Quaternion.Euler(14f, 0, 0));

        // piliers et murs de couverture
        Box(root, new Vector3(-15, 1.25f, -10), new Vector3(3, 2.5f, 3), wall);
        Box(root, new Vector3(15, 1.25f, 10), new Vector3(3, 2.5f, 3), wall);
        Box(root, new Vector3(12, 1, -12), new Vector3(1.2f, 2, 8), orange);
        Box(root, new Vector3(-12, 1, 12), new Vector3(1.2f, 2, 8), orange);
        Box(root, new Vector3(20, 1, 0), new Vector3(6, 2, 1.2f), teal);
        Box(root, new Vector3(-20, 1, 0), new Vector3(6, 2, 1.2f), teal);

        // couvertures basses
        foreach (var p in new[] { new Vector2(6, 16), new Vector2(-6, 16), new Vector2(6, -16), new Vector2(-6, -16) })
            Box(root, new Vector3(p.x, 0.6f, p.y), new Vector3(4, 1.2f, 1), wall);

        // points d'apparition
        var spawnRoot = new GameObject("Spawns").transform;
        spawnRoot.SetParent(root);
        Vector3[] spawnPos =
        {
            new Vector3(-25, 0.05f, -25), new Vector3(25, 0.05f, 25),
            new Vector3(-25, 0.05f, 25),  new Vector3(25, 0.05f, -25),
            new Vector3(0, 0.05f, -25),   new Vector3(0, 0.05f, 25),
        };
        var spawns = new Transform[spawnPos.Length];
        for (int i = 0; i < spawnPos.Length; i++)
        {
            var s = new GameObject("Spawn" + i).transform;
            s.SetParent(spawnRoot);
            s.position = spawnPos[i];
            spawns[i] = s;
        }
        return spawns;
    }

    static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color color, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Block";
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.rotation = rot ?? Quaternion.identity;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().material.color = color;
        return go;
    }

    static void Paint(GameObject go, Color c)
    {
        go.GetComponent<Renderer>().material.color = c;
    }

    // ---------- joueur ----------

    static PlayerController BuildPlayer()
    {
        // remplace la caméra par défaut de la scène
        if (Camera.main != null) Object.DestroyImmediate(Camera.main.gameObject);

        var go = new GameObject("Player");
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.4f;
        cc.slopeLimit = 50f;

        go.AddComponent<Health>();
        var weapon = go.AddComponent<WeaponController>();
        var pc = go.AddComponent<PlayerController>();

        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(go.transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.62f, 0);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = pc.baseFov;
        camGo.AddComponent<AudioListener>();
        pc.cam = cam;

        // arme visible à l'écran
        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "Viewmodel";
        Object.Destroy(gun.GetComponent<Collider>());
        gun.transform.SetParent(camGo.transform, false);
        gun.transform.localPosition = new Vector3(0.28f, -0.25f, 0.55f);
        gun.transform.localScale = new Vector3(0.09f, 0.12f, 0.6f);
        Paint(gun, new Color(0.12f, 0.12f, 0.14f));

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun.transform, false);
        muzzle.localPosition = new Vector3(0, 0, 0.5f);
        weapon.muzzle = muzzle;

        return pc;
    }

    // ---------- bot ----------

    static BotAI BuildBot(Transform playerTransform)
    {
        var go = new GameObject("Bot");
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.4f;
        cc.slopeLimit = 50f;

        go.AddComponent<Health>();
        var weapon = go.AddComponent<WeaponController>();

        // corps
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(go.transform, false);
        body.transform.localPosition = new Vector3(0, 0.9f, 0);
        body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
        Paint(body, new Color(0.85f, 0.2f, 0.2f));

        // tête
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(go.transform, false);
        head.transform.localPosition = new Vector3(0, 1.62f, 0);
        head.transform.localScale = Vector3.one * 0.32f;
        Paint(head, new Color(1f, 0.75f, 0.35f));

        // arme
        var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(gun.GetComponent<Collider>());
        gun.transform.SetParent(go.transform, false);
        gun.transform.localPosition = new Vector3(0.35f, 1.15f, 0.45f);
        gun.transform.localScale = new Vector3(0.1f, 0.12f, 0.7f);
        Paint(gun, new Color(0.1f, 0.1f, 0.1f));

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(gun.transform, false);
        muzzle.localPosition = new Vector3(0, 0, 0.5f);
        weapon.muzzle = muzzle;

        var eye = new GameObject("Eye").transform;
        eye.SetParent(go.transform, false);
        eye.localPosition = new Vector3(0, 1.55f, 0);

        var bot = go.AddComponent<BotAI>();
        bot.target = playerTransform;
        bot.eye = eye;
        return bot;
    }
}
