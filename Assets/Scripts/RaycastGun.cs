using UnityEngine;

public class RaycastGun : Gun
{
    [Header("Raycast")]
    public float damage = 25f;
    public float range = 100f;
    public LayerMask hitMask = ~0;
    public GameObject impactPrefab;
    [Tooltip("Cámara desde la que se apunta. Si está vacía, usa la cámara padre o Camera.main.")]
    public Camera aimCamera;
    [Tooltip("Muestra en consola qué golpea cada disparo. Desactívalo cuando todo funcione.")]
    public bool debugHits = true;

    [Header("Gizmo de trayectoria (solo Editor)")]
    public bool drawTrajectory = true;
    public Color trajectoryColor = Color.red;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private Camera cachedCamera;

    protected override void Fire()
    {
        if (cachedCamera == null) cachedCamera = FindAimCamera();
        if (cachedCamera == null)
        {
            Debug.LogWarning($"{name}: no hay cámara. Pon el tag MainCamera a la cámara del jugador.", this);
            return;
        }

        Ray ray = new Ray(cachedCamera.transform.position, cachedCamera.transform.forward);

        if (!TryGetClosestHit(ray, out RaycastHit hit))
        {
            if (debugHits) Debug.Log($"{name}: el rayo no golpeó nada (rango {range}, máscara {hitMask.value}).", this);
            return;
        }

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        EnemyHealth target = enemy == null ? hit.collider.GetComponentInParent<EnemyHealth>() : null;
        bool canDamage = enemy != null || target != null;

        if (debugHits)
        {
            Debug.Log($"{name}: golpeó '{hit.collider.name}' a {hit.distance:F1} m. " +
                      (canDamage ? $"Daño {damage}." : "NO tiene Enemy ni EnemyHealth, no hay daño."), hit.collider);
        }

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        else if (target != null)
        {
            target.TakeDamage(damage);
        }

        if (impactPrefab != null)
        {
            GameObject fx = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(fx, 2f);
        }
    }

    private bool TryGetClosestHit(Ray ray, out RaycastHit hit)
    {
        int count = Physics.RaycastNonAlloc(ray, hitBuffer, range, hitMask, QueryTriggerInteraction.Ignore);

        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (ownerRoot != null && h.collider.transform.IsChildOf(ownerRoot)) continue;

            if (h.distance < closestDistance)
            {
                closestDistance = h.distance;
                closestIndex = i;
            }
        }

        hit = closestIndex >= 0 ? hitBuffer[closestIndex] : default;
        return closestIndex >= 0;
    }

    private Camera FindAimCamera()
    {
        if (aimCamera != null) return aimCamera;

        Camera parentCamera = GetComponentInParent<Camera>();
        return parentCamera != null ? parentCamera : Camera.main;
    }

    private void OnDrawGizmos()
    {
        if (!drawTrajectory) return;

        Camera cam = cachedCamera != null ? cachedCamera : FindAimCamera();
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 end = ray.origin + ray.direction * range;

        bool hasHit = TryGetClosestHit(ray, out RaycastHit hit);
        if (hasHit) end = hit.point;

        Vector3 start = BulletSpawnPoint != null ? BulletSpawnPoint.position : ray.origin;

        Gizmos.color = trajectoryColor;
        Gizmos.DrawLine(start, end);
        if (hasHit) Gizmos.DrawWireSphere(end, 0.15f);
    }
}
