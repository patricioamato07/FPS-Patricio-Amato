using UnityEngine;

// Plantilla de datos: un asset por tipo de enemigo (Grunt, Heavy, etc.).
// NUNCA se modifica durante el juego; cada EnemyHealth copia su valor en Awake.
[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Min(1f)] public float maxHealth = 100f;
}
