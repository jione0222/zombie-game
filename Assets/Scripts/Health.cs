using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float> OnDamaged;
    public event Action OnDeath;

    void Awake()
    {
        Current = maxHealth;
    }

    public void Init(float max)
    {
        maxHealth = max;
        Current = max;
        IsDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount);
        if (Current <= 0f)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(maxHealth, Current + amount);
    }
}
