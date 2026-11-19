using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Renderer))]
public class Enemy : MonoBehaviour
{
    public int health = 100;

    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public GameObject weaponFlash;
    public float bloom;
    public float fireRate;

    public Vector3 bulletRotationOffset = new Vector3(0f, 180f, 0f);
    private float lastShotTime = 0f;

    public Material EnemyHit;

    private Rigidbody rb;
    private Renderer rend;
    private Material originalMaterial;

    //AI Settings. Will be moved to a Scriptable Object later for general use and Enemy Variant configuration. Probably have to make everything public.
    public int currentPointIndex = 0;
    public Vector3 currentTarget;
    public float positionThreshold = 1f;
    public float idleTime = 5f;
    public float attackDistance = 5f;
    public float maxVisionDistance = 20f;
    public float minChasingHealth = 30f;

    public LayerMask visionMask = ~0;

    public Transform[] patrolPoints;
    private float idleTimeCounter;
    private Transform playerTransform;
    private bool canSeePlayer;
    private Vector3 lastKnownPlayerPosition;

    private NavMeshAgent agent;
    public enum State { Idle, Patrolling, Chasing, Attacking }
    public State state = State.Idle;

    private float ArrivalThreshold => Mathf.Max(positionThreshold, agent.stoppingDistance + 0.5f);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        originalMaterial = rend.material;

        agent = GetComponent<NavMeshAgent>();
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();

        GameObject patrolPointParent = GameObject.FindWithTag("PatrolPoint");
        patrolPoints = patrolPointParent.GetComponentsInChildren<Transform>().Where(t => t != patrolPointParent.transform).ToArray();

        SetNearestPatrolPoint();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            health -= 10;

            if(health <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(Blink());
            }
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    IEnumerator Blink()
    {
        rend.material = EnemyHit;
        yield return new WaitForSeconds(0.1f);
        rend.material = originalMaterial;
    }

    private void Update()
    {
        LookForPlayer();
        SetLastKnownPlayerPosition();

        switch (state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Patrolling:
                Patrolling();
                break;
            case State.Attacking:
                Attacking();
                break;
            case State.Chasing:
                Chasing();
                break;
        }

        rb.linearVelocity = Vector3.zero;

        LookAtPlayer();
    }

    private void LookForPlayer()
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

        canSeePlayer = false;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, maxVisionDistance, visionMask, QueryTriggerInteraction.Ignore))
        {
            canSeePlayer = hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform);
        }

        if (canSeePlayer && state != State.Attacking && health >= minChasingHealth)
        {
            state = State.Chasing;
        }
    }

    private void Idle()
    {
        agent.ResetPath();

        idleTimeCounter -= Time.deltaTime;

        if (idleTimeCounter < 0)
        {
            state = State.Patrolling;
            idleTimeCounter = idleTime;
        }
    }

    private void Patrolling()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (FlatDistance(currentTarget, transform.position) < ArrivalThreshold)
        {
            float chance = Random.Range(0, 100);

            if (chance < 10)
            {
                state = State.Idle;
                return;
            }

            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            currentTarget = patrolPoints[currentPointIndex].position;
        }

        agent.SetDestination(currentTarget);
    }

    private void Attacking()
    {
        idleTimeCounter = idleTime;
        agent.ResetPath();

        Shoot();

        if (!canSeePlayer)
        {
            if (health < minChasingHealth)
            {
                GoToPatrol();
            }
            else
            {
                state = State.Chasing;
            }
        }
    }

    private void Chasing()
    {
        idleTimeCounter = idleTime;
        agent.SetDestination(lastKnownPlayerPosition);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (health < minChasingHealth)
        {
            GoToPatrol();
        }
        else if (canSeePlayer && distanceToPlayer <= attackDistance)
        {
            state = State.Attacking;
        }
        else if (!canSeePlayer && (distanceToPlayer > maxVisionDistance || ReachedLastKnownPosition()))
        {
            GoToPatrol();
        }
    }

    private bool ReachedLastKnownPosition()
    {
        if (FlatDistance(transform.position, lastKnownPlayerPosition) < ArrivalThreshold) return true;

        return !agent.pathPending && agent.remainingDistance <= ArrivalThreshold;
    }

    private void GoToPatrol()
    {
        agent.ResetPath();
        SetNearestPatrolPoint();
        state = State.Patrolling;
    }

    private void SetNearestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        int best = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = FlatDistance(patrolPoints[i].position, transform.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }

        currentPointIndex = best;
        currentTarget = patrolPoints[best].position;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void LookAtPlayer()
    {
        if (canSeePlayer)
        {
            transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
        }
    }

    private void SetLastKnownPlayerPosition()
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }
    }

    private void Shoot()
    {
        if (Time.time > lastShotTime + fireRate)
        {
            Vector3 directionToPlayer = (playerTransform.position - bulletSpawnPoint.position).normalized;

            Quaternion bulletRotation = Quaternion.LookRotation(directionToPlayer);

            float maxInaccuracy = 10f;
            float currentInaccuracy = bloom * maxInaccuracy;
            float randomYaw = Random.Range(-currentInaccuracy, currentInaccuracy);
            float randomPitch = Random.Range(-currentInaccuracy, currentInaccuracy);

            bulletRotation *= Quaternion.Euler(randomPitch, randomYaw, 0f);
            bulletRotation *= Quaternion.Euler(bulletRotationOffset);

            Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletRotation);
            Instantiate(weaponFlash, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            lastShotTime = Time.time;
        }
    }
}
