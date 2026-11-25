using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private EnemyStats stats;
    [Tooltip("Solo se usa si no hay Stats asignado.")]
    [SerializeField] private float maxHealth = 100f;

    public event Action<EnemyHealth> OnDeath;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = stats != null ? stats.maxHealth : maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
