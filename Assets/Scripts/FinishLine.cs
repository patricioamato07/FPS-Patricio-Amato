using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLine : MonoBehaviour
{
    [Tooltip("Si está vacío, se busca el RespawnManager de la escena.")]
    [SerializeField] private RespawnManager respawnManager;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryFinish(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryFinish(collision.collider);
    }

    private void TryFinish(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;

        if (respawnManager == null) respawnManager = FindFirstObjectByType<RespawnManager>();
        if (respawnManager == null)
        {
            Debug.LogWarning("FinishLine: no hay RespawnManager en la escena.", this);
            return;
        }

        respawnManager.ShowVictory();
    }
}
