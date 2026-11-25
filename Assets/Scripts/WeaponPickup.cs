using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Tooltip("Prefab del arma EMPUÑADA (con el componente Gun en la raíz), no el modelo del suelo.")]
    public Gun gunPrefab;

    public float pickupRadius = 1.5f;
    [Tooltip("Evita que el arma se recoja sola justo al soltarla.")]
    public float pickupDelay = 1f;
    public LayerMask playerMask = ~0;

    private static readonly Collider[] buffer = new Collider[16];

    private int ammo = -1; // -1 = Full Magazine
    private float enabledAt;

    public void SetAmmo(int value)
    {
        ammo = value;
    }

    void Awake()
    {
        enabledAt = Time.time + pickupDelay;
    }

    void Update()
    {
        if (Time.time < enabledAt) return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, pickupRadius, buffer, playerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            PlayerShooting shooter = buffer[i].GetComponentInParent<PlayerShooting>();
            if (shooter != null && shooter.TryPickUp(gunPrefab, ammo))
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
