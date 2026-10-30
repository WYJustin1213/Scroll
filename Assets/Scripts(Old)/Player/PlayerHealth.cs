using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 100;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void RestoreToFull()
    {
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player died.", this);
        Died?.Invoke();
    }
}
