using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Si está vacío, se busca el PlayerHealth de la escena.")]
    [SerializeField] private PlayerHealth player;
    [Tooltip("Imagen con Image Type = Filled (Horizontal).")]
    [SerializeField] private Image fillImage;
    [Tooltip("Opcional: texto con la vida (por ejemplo 75 / 100).")]
    [SerializeField] private TMP_Text valueText;

    [Header("Apariencia")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.85f, 0.3f);
    [SerializeField] private Color lowColor = new Color(0.85f, 0.1f, 0.1f);
    [Tooltip("Velocidad de la animación de la barra. 0 = cambio instantáneo.")]
    [SerializeField] private float smoothSpeed = 3f;

    private float targetFill = 1f;
    private float shownFill = 1f;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (player != null) player.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (player != null) player.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("HealthBarUI: no se encontró un PlayerHealth en la escena.", this);
            return;
        }
        HandleHealthChanged(player.CurrentHealth, player.MaxHealth);
        shownFill = targetFill;
        Apply();
    }

    private void Update()
    {
        if (fillImage == null) return;

        shownFill = smoothSpeed <= 0f
            ? targetFill
            : Mathf.MoveTowards(shownFill, targetFill, smoothSpeed * Time.deltaTime);

        Apply();
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (valueText != null)
        {
            valueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void Apply()
    {
        if (fillImage == null) return;

        fillImage.fillAmount = shownFill;
        fillImage.color = Color.Lerp(lowColor, fullColor, shownFill);
    }
}