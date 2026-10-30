using UnityEngine;

public enum MatchState { Countdown, Live, RoundOver, MatchOver }
public enum BotDifficulty { Facile, Normal, Difficile }

/// <summary>Gère le déroulement du duel : manches, score, compte à rebours, réapparitions.</summary>
public class MatchManager : MonoBehaviour
{
    public PlayerController player;
    public BotAI bot;
    public Transform[] spawnPoints;

    public int roundsToWin = 5;
    public float roundTime = 90f;
    public float countdownTime = 3f;
    public float roundOverTime = 3f;
    public BotDifficulty difficulty = BotDifficulty.Normal;

    public MatchState State { get; private set; }
    public float Timer { get; private set; }
    public int PlayerScore { get; private set; }
    public int BotScore { get; private set; }
    public int Round { get; private set; }
    public string RoundMessage { get; private set; } = "";

    Health pHealth, bHealth;
    WeaponController pWeapon, bWeapon;

    void Start()
    {
        pHealth = player.GetComponent<Health>();
        bHealth = bot.GetComponent<Health>();
        pWeapon = player.GetComponent<WeaponController>();
        bWeapon = bot.GetComponent<WeaponController>();

        pHealth.Died += OnDied;
        bHealth.Died += OnDied;

        StartRound();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) difficulty = BotDifficulty.Facile;
        if (Input.GetKeyDown(KeyCode.F2)) difficulty = BotDifficulty.Normal;
        if (Input.GetKeyDown(KeyCode.F3)) difficulty = BotDifficulty.Difficile;

        Timer -= Time.deltaTime;

        switch (State)
        {
            case MatchState.Countdown:
                if (Timer <= 0f)
                {
                    State = MatchState.Live;
                    Timer = roundTime;
                    player.canMove = player.canFire = true;
                    bot.Active = true;
                }
                break;

            case MatchState.Live:
                if (Timer <= 0f) EndRoundOnTimeout();
                break;

            case MatchState.RoundOver:
                if (Timer <= 0f)
                {
                    if (PlayerScore >= roundsToWin || BotScore >= roundsToWin) EndMatch();
                    else StartRound();
                }
                break;

            case MatchState.MatchOver:
                if (Input.GetKeyDown(KeyCode.R))
                {
                    PlayerScore = BotScore = Round = 0;
                    StartRound();
                }
                break;
        }
    }

    void StartRound()
    {
        Round++;
        pHealth.ResetHealth();
        bHealth.ResetHealth();
        pWeapon.RefillAll();
        bWeapon.RefillAll();
        bWeapon.Equip(Random.Range(0, bWeapon.weapons.Length)); // le bot change d'arme à chaque manche
        ApplyDifficulty();

        int pi = Random.Range(0, spawnPoints.Length);
        int bi = FarthestFrom(pi);
        Vector3 pp = spawnPoints[pi].position;
        Vector3 bp = spawnPoints[bi].position;
        player.Teleport(pp, YawTo(pp, bp));
        bot.Teleport(bp, YawTo(bp, pp));

        player.canMove = player.canFire = false;
        bot.Active = false;

        State = MatchState.Countdown;
        Timer = countdownTime;
        RoundMessage = "";
    }

    void OnDied(Health victim, Health killer)
    {
        if (State != MatchState.Live) return;

        if (victim == pHealth) { BotScore++; EndRound("Manche perdue"); }
        else { PlayerScore++; EndRound("Manche gagnée !"); }
    }

    void EndRoundOnTimeout()
    {
        if (pHealth.Current > bHealth.Current) { PlayerScore++; EndRound("Temps écoulé - manche gagnée !"); }
        else if (pHealth.Current < bHealth.Current) { BotScore++; EndRound("Temps écoulé - manche perdue"); }
        else EndRound("Temps écoulé - égalité");
    }

    void EndRound(string message)
    {
        State = MatchState.RoundOver;
        Timer = roundOverTime;
        RoundMessage = message;
        bot.Active = false;
        player.canFire = false;
    }

    void EndMatch()
    {
        State = MatchState.MatchOver;
        RoundMessage = PlayerScore > BotScore ? "VICTOIRE !" : "DÉFAITE";
        player.canMove = true;
        player.canFire = false;
        bot.Active = false;
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case BotDifficulty.Facile:    bot.aimError = 6f;   bot.reactionTime = 0.7f; bot.fireDelayScale = 2.2f; bot.moveSpeed = 5f; break;
            case BotDifficulty.Normal:    bot.aimError = 3f;   bot.reactionTime = 0.4f; bot.fireDelayScale = 1.5f; bot.moveSpeed = 6f; break;
            case BotDifficulty.Difficile: bot.aimError = 1.4f; bot.reactionTime = 0.2f; bot.fireDelayScale = 1.1f; bot.moveSpeed = 7f; break;
        }
    }

    int FarthestFrom(int index)
    {
        int best = index;
        float bestDist = -1f;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (i == index) continue;
            float d = (spawnPoints[i].position - spawnPoints[index].position).sqrMagnitude;
            if (d > bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    static float YawTo(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.y = 0f;
        return d.sqrMagnitude < 0.001f ? 0f : Quaternion.LookRotation(d).eulerAngles.y;
    }
}
