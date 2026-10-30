using System;
using UnityEngine;

/// <summary>Points de vie d'un combattant (joueur ou bot).</summary>
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    /// <summary>Hauteur (au-dessus des pieds) à partir de laquelle un tir compte comme headshot.</summary>
    public float headHeight = 1.5f;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    /// <summary>(victime, attaquant, dégâts)</summary>
    public event Action<Health, Health, float> Damaged;

    /// <summary>(victime, tueur)</summary>
    public event Action<Health, Health> Died;

    void Awake()
    {
        Current = maxHealth;
    }

    public void ResetHealth()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount, Health attacker)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(this, attacker, amount);

        if (Current <= 0f)
            Died?.Invoke(this, attacker);
    }
}
