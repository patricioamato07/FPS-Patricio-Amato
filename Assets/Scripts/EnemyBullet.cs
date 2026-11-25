using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;
    public float damage = 10f;
    [Tooltip("Tu Bullet.cs actual se mueve hacia -forward. Déjalo marcado si el prefab está hecho igual.")]
    public bool reverseDirection = true;

    private Rigidbody rb;
    private bool consumed;

    private void Awake()
    {

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.isTrigger = true;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 direction = reverseDirection ? -transform.forward : transform.forward;
        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        if (other.isTrigger) return;
        if (other.GetComponentInParent<EnemyHealth>() != null) return;
        if (other.GetComponentInParent<EnemyBullet>() != null) return;
        if (other.GetComponentInParent<Bullet>() != null) return;

        consumed = true;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
