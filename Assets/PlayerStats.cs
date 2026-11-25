using UnityEngine;

// Plantilla de datos del jugador. PlayerHealth copia estos valores en Awake;
// el asset no guarda la vida actual.
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Scriptable Objects/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    [Min(1f)] public float maxHealth = 100f;
}
