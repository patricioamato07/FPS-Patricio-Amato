using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class RespawnManager : MonoBehaviour
{
    public enum GameState { Playing, Dead, Finished }

    [Header("Jugador")]
    [Tooltip("Si está vacío, se busca el PlayerHealth de la escena.")]
    [SerializeField] private PlayerHealth player;
    [Tooltip("Punto de reaparición. Si está vacío, se usa la posición inicial del jugador.")]
    [SerializeField] private Transform respawnPoint;
    [Tooltip("Scripts extra a bloquear mientras se muestra el panel (p. ej. el que gira la cámara con el mouse).")]
    [SerializeField] private Behaviour[] extraScriptsToLock;

    [Header("Reinicio")]
    [Tooltip("true: R recarga la escena (reaparecen enemigos, armas y munición). false: solo reaparece el jugador en el punto de respawn.")]
    [SerializeField] private bool reloadSceneOnRestart = true;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;

    [Header("Textos")]
    [SerializeField] private string deathTitle = "HAS MUERTO";
    [SerializeField] private string victoryTitle = "¡CIRCUITO COMPLETADO!";
    [SerializeField] private string restartHint = "Presiona R para reiniciar";
    [SerializeField] private Color deathColor = new Color(0.85f, 0.1f, 0.1f);
    [SerializeField] private Color victoryColor = new Color(0.2f, 0.85f, 0.3f);

    public GameState State { get; private set; } = GameState.Playing;

    private PlayerMovement movement;
    private PlayerShooting shooting;
    private Rigidbody playerBody;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogError("RespawnManager: no se encontró un PlayerHealth en la escena.", this);
            return;
        }

        movement = player.GetComponentInChildren<PlayerMovement>();
        shooting = player.GetComponentInChildren<PlayerShooting>();
        playerBody = player.GetComponent<Rigidbody>();

        startPosition = player.transform.position;
        startRotation = player.transform.rotation;
    }

    private void OnEnable()
    {
        if (player != null) player.OnDeath += ShowDeath;
    }

    private void OnDisable()
    {
        if (player != null) player.OnDeath -= ShowDeath;
    }

    private void Update()
    {
        if (State == GameState.Playing) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            Restart();
        }
    }

    public void ShowDeath()
    {
        Show(GameState.Dead, deathTitle, deathColor);
    }

    public void ShowVictory()
    {
        Show(GameState.Finished, victoryTitle, victoryColor);
    }

    public void Restart()
    {
        if (!reloadSceneOnRestart)
        {
            Respawn();
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex < 0)
        {
            Debug.LogError($"RespawnManager: la escena '{scene.name}' no está en Build Settings, no se puede recargar. " +
                           "Agrégala en File > Build Profiles (Build Settings) > Add Open Scenes. Mientras tanto se reaparece en el punto de respawn.", this);
            Respawn();
            return;
        }

        SceneManager.LoadScene(scene.buildIndex);
    }

    public void Respawn()
    {
        if (player == null) return;

        Vector3 position = respawnPoint != null ? respawnPoint.position : startPosition;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : startRotation;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        player.transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();

        player.ResetHealth();
        SetControlsLocked(false);

        if (panel != null) panel.SetActive(false);
        State = GameState.Playing;
    }

    public void SetRespawnPoint(Transform point)
    {
        respawnPoint = point;
    }

    private void Show(GameState newState, string title, Color color)
    {
        if (player == null || State != GameState.Playing) return;

        State = newState;
        SetControlsLocked(true);

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = color;
        }
        if (hintText != null) hintText.text = restartHint;

        if (panel != null)
        {
            panel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RespawnManager: no hay panel de UI asignado.", this);
        }
    }

    private void SetControlsLocked(bool locked)
    {
        if (movement != null) movement.enabled = !locked;
        if (shooting != null) shooting.enabled = !locked;

        if (extraScriptsToLock != null)
        {
            foreach (Behaviour b in extraScriptsToLock)
            {
                if (b != null) b.enabled = !locked;
            }
        }

        if (locked && playerBody != null)
        {
            playerBody.linearVelocity = new Vector3(0f, playerBody.linearVelocity.y, 0f);
        }
    }
}
