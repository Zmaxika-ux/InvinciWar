using UnityEngine;

/// <summary>Interface du jeu dessinée avec OnGUI (aucun Canvas à configurer).</summary>
public class HUD : MonoBehaviour
{
    public MatchManager match;

    Texture2D white;
    GUIStyle style;
    float scale = 1f;
    float hitTimer, hurtTimer;
    bool lastHeadshot;
    WeaponController pw;
    Health ph, bh;

    void Start()
    {
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        pw = match.player.GetComponent<WeaponController>();
        ph = match.player.GetComponent<Health>();
        bh = match.bot.GetComponent<Health>();

        pw.HitConfirmed += (victim, headshot) => { hitTimer = 0.25f; lastHeadshot = headshot; };
        ph.Damaged += (victim, attacker, amount) => hurtTimer = 0.45f;
    }

    void Update()
    {
        hitTimer -= Time.deltaTime;
        hurtTimer -= Time.deltaTime;
    }

    void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    void Label(Rect r, string text, int size, TextAnchor anchor, Color c)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false };
        }
        style.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * scale));
        style.alignment = anchor;

        style.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(r.x + 2f * scale, r.y + 2f * scale, r.width, r.height), text, style);
        style.normal.textColor = c;
        GUI.Label(r, text, style);
    }

    void Bar(Rect r, float ratio, Color fill)
    {
        Fill(r, new Color(0f, 0f, 0f, 0.55f));
        Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(ratio), r.height), fill);
    }

    void OnGUI()
    {
        if (match == null || white == null) return;

        scale = Screen.height / 1080f;
        float W = Screen.width, H = Screen.height, cx = W * 0.5f, cy = H * 0.5f;

        // flash rouge quand on est touché
        if (hurtTimer > 0f)
            Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, Mathf.Clamp01(hurtTimer) * 0.45f));

        DrawCrosshair(cx, cy);
        DrawTop(W, cx);
        DrawPlayerStatus(W, H);
        DrawBanner(cx, H);

        if (Cursor.lockState != CursorLockMode.Locked)
            Label(new Rect(0, H * 0.62f, W, 60 * scale), "Cliquez pour reprendre le contrôle", 36, TextAnchor.MiddleCenter, Color.yellow);

        Label(new Rect(20 * scale, 12 * scale, W, 30 * scale),
            "WASD bouger | Espace saut (x2) | Maj sprint | E dash | Clic tirer | Clic droit zoom (sniper) | R recharger | 1-4 armes | F1/F2/F3 difficulté",
            18, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.55f));
    }

    void DrawCrosshair(float cx, float cy)
    {
        float gap = 6f * scale, len = 12f * scale, th = 2f * scale;
        Color c = new Color(1f, 1f, 1f, 0.9f);
        Fill(new Rect(cx - gap - len, cy - th / 2f, len, th), c);
        Fill(new Rect(cx + gap, cy - th / 2f, len, th), c);
        Fill(new Rect(cx - th / 2f, cy - gap - len, th, len), c);
        Fill(new Rect(cx - th / 2f, cy + gap, th, len), c);

        if (hitTimer > 0f)
        {
            Color hc = lastHeadshot ? new Color(1f, 0.25f, 0.2f) : Color.white;
            Matrix4x4 old = GUI.matrix;
            for (int i = 0; i < 2; i++)
            {
                GUI.matrix = old;
                GUIUtility.RotateAroundPivot(45f + 90f * i, new Vector2(cx, cy));
                Fill(new Rect(cx + 9f * scale, cy - th / 2f, 10f * scale, th), hc);
                Fill(new Rect(cx - 19f * scale, cy - th / 2f, 10f * scale, th), hc);
            }
            GUI.matrix = old;
        }
    }

    void DrawTop(float W, float cx)
    {
        string score = "VOUS  " + match.PlayerScore + "  -  " + match.BotScore + "  BOT";
        Label(new Rect(cx - 300 * scale, 40 * scale, 600 * scale, 60 * scale), score, 48, TextAnchor.MiddleCenter, Color.white);

        int t = Mathf.Max(0, Mathf.CeilToInt(match.State == MatchState.Live ? match.Timer : match.roundTime));
        string info = "Manche " + match.Round + "  |  " + (t / 60) + ":" + (t % 60).ToString("00") +
                      "  |  Premier à " + match.roundsToWin + "  |  Bot : " + match.difficulty;
        Label(new Rect(cx - 400 * scale, 98 * scale, 800 * scale, 30 * scale), info, 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));

        // vie de l'adversaire
        Rect bar = new Rect(cx - 200 * scale, 135 * scale, 400 * scale, 14 * scale);
        Bar(bar, bh.Current / bh.maxHealth, new Color(0.9f, 0.25f, 0.25f));
    }

    void DrawPlayerStatus(float W, float H)
    {
        // vie
        Rect hb = new Rect(40 * scale, H - 70 * scale, 380 * scale, 30 * scale);
        float ratio = ph.Current / ph.maxHealth;
        Bar(hb, ratio, Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.9f, 0.4f), ratio));
        Label(hb, "PV  " + Mathf.CeilToInt(ph.Current), 24, TextAnchor.MiddleCenter, Color.white);

        // munitions
        WeaponData w = pw.Current;
        string ammo = pw.IsReloading ? "RECHARGEMENT" : pw.Ammo + " / " + w.magSize;
        Label(new Rect(W - 520 * scale, H - 150 * scale, 480 * scale, 70 * scale), ammo, 56, TextAnchor.MiddleRight,
            (!pw.IsReloading && pw.Ammo <= Mathf.Max(1, w.magSize / 4)) ? new Color(1f, 0.4f, 0.3f) : Color.white);

        if (pw.IsReloading)
            Bar(new Rect(W - 340 * scale, H - 85 * scale, 300 * scale, 8 * scale), pw.ReloadProgress, Color.white);

        // liste des armes
        string list = "";
        for (int i = 0; i < pw.weapons.Length; i++)
            list += (i == pw.CurrentIndex ? "[" + (i + 1) + " " + pw.weapons[i].name + "]" : (i + 1) + " " + pw.weapons[i].name) + "   ";
        Label(new Rect(W - 900 * scale, H - 50 * scale, 860 * scale, 30 * scale), list, 20, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.8f));
    }

    void DrawBanner(float cx, float H)
    {
        string banner = null;
        Color color = Color.white;

        switch (match.State)
        {
            case MatchState.Countdown:
                banner = Mathf.Max(1, Mathf.CeilToInt(match.Timer)).ToString();
                break;
            case MatchState.Live:
                if (match.roundTime - match.Timer < 1f) { banner = "FIGHT !"; color = new Color(1f, 0.85f, 0.2f); }
                break;
            case MatchState.RoundOver:
                banner = match.RoundMessage;
                color = match.RoundMessage.Contains("gagnée") ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
                break;
            case MatchState.MatchOver:
                banner = match.RoundMessage + "\nAppuyez sur R pour rejouer";
                color = match.PlayerScore > match.BotScore ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
                break;
        }

        if (!string.IsNullOrEmpty(banner))
            Label(new Rect(cx - 600 * scale, H * 0.3f, 1200 * scale, 200 * scale), banner, 84, TextAnchor.MiddleCenter, color);
    }
}
