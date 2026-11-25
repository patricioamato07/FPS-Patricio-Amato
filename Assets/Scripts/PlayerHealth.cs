using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [Tooltip("Solo se usa si no hay Stats asignado.")]
    [SerializeField] private float fallbackMaxHealth = 100f;

    [Header("Estado actual (editable en Play para probar)")]
    [SerializeField] private float currentHealth;

    [Header("Estado de muerte")]
    [Tooltip("Cámara que se congela al morir. Si está vacía, usa la cámara hija o Camera.main.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Opcional: scripts a desactivar al morir (p. ej. el que gira la cámara con el mouse). La cámara se congela igual.")]
    [SerializeField] private Behaviour[] scriptsToDisableOnDeath;
    [Tooltip("Recarga el nivel actual al presionar R estando muerto. Desmárcalo si usas RespawnManager.")]
    [SerializeField] private bool restartLevelOnR = true;

    public event Action<float, float> OnHealthChanged; // (actual, máxima)
    public event Action OnDeath;

    public float MaxHealth => stats != null ? stats.maxHealth : fallbackMaxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead { get; private set; }

    private Quaternion frozenRootRotation;
    private Quaternion frozenCameraRotation;

    private void Awake()
    {
        currentHealth = MaxHealth;
    }

    private void Update()
    {
        if (!IsDead || !restartLevelOnR) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

    private void LateUpdate()
    {
        if (!IsDead) return;

        transform.rotation = frozenRootRotation;
        if (playerCamera != null)
        {
            playerCamera.transform.rotation = frozenCameraRotation;
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0f)
        {
            IsDead = true;
            EnterDeathState();
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void ResetHealth()
    {
        IsDead = false;
        SetScriptsEnabled(true);

        currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void RestartLevel()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex < 0)
        {
            Debug.LogError($"PlayerHealth: la escena '{scene.name}' no está en Build Settings, no se puede reiniciar. " +
                           "Agrégala en File > Build Profiles (Build Settings) > Add Open Scenes.", this);
            return;
        }

        SceneManager.LoadScene(scene.buildIndex);
    }

    private void EnterDeathState()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        frozenRootRotation = transform.rotation;
        if (playerCamera != null) frozenCameraRotation = playerCamera.transform.rotation;

        SetScriptsEnabled(false);
    }

    private void SetScriptsEnabled(bool value)
    {
        if (scriptsToDisableOnDeath == null) return;

        foreach (Behaviour b in scriptsToDisableOnDeath)
        {
            if (b != null) b.enabled = value;
        }
    }
}
