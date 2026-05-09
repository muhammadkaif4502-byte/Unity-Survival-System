using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DeerSystem : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 18f;  // **MUCH FASTER - realistic deer speed**
    public float stuckSpeed = 12f;   // **Still fast when stuck/confused**

    [Header("Animation Tuning")]
    public float animSpeedMultiplier = 0.3f;

    [Header("Deer Stats")]
    public float health = 50f;
    public float detectionRadius = 15f;  // **Increased - deer notice you from farther**
    public float fleeDistance = 25f;      // **Runs farther away**

    [Header("Axe Settings")]
    public float axeDamage = 25f;
    public float axeRange = 3f;

    [Header("Terrain Settings")]
    public float maxTerrainSlope = 30f;
    public float maxHeightDifference = 3f;

    [Header("Stuck Behavior")]
    public float stuckCheckTime = 0.3f;  // Check if stuck every 0.3s
    public float minMoveDistance = 0.5f; // Must move this far to not be stuck
    public float stuckPanicTime = 1.5f;  // How long to struggle when stuck

    [Header("References")]
    public Transform player;
    public Animator anim;

    private NavMeshAgent agent;
    private bool isDead = false;
    private float nextFleeTime = 0f;
    private Vector3 lastValidPosition;

    // **Stuck detection variables**
    private Vector3 lastStuckCheckPosition;
    private float lastStuckCheckTime;
    private bool isStuck = false;
    private float stuckStartTime;
    private Vector3 stuckPanicDirection;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.radius = 0.8f;
        agent.stoppingDistance = 0.5f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 50;
        agent.autoBraking = true;

        // **CRITICAL: High acceleration and angular speed for quick escapes**
        agent.acceleration = 20f;      // Extremely quick acceleration
        agent.angularSpeed = 360f;     // Can turn instantly

        SnapToNavMesh();
        lastValidPosition = transform.position;
        lastStuckCheckPosition = transform.position;
        lastStuckCheckTime = Time.time;

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        if (!agent.isOnNavMesh)
        {
            SnapToNavMesh();
            return;
        }

        if (player == null) return;

        // **Check if deer is stuck (not moving much)**
        CheckIfStuck();

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // **Deer flees when player is nearby**
        if (distanceToPlayer < detectionRadius)
        {
            if (isStuck)
            {
                // **When stuck and panicking, still move fast**
                agent.speed = stuckSpeed;
                HandleStuckBehavior();
            }
            else
            {
                // **Normal fleeing - SUPER FAST**
                agent.speed = sprintSpeed;

                if (Time.time > nextFleeTime)
                {
                    FleeFromPlayer();
                    nextFleeTime = Time.time + 0.3f;  // Update path more frequently
                }
            }
        }
        else
        {
            // **Calm walking when player is far**
            agent.speed = walkSpeed;
            isStuck = false;  // Reset stuck state when calm

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.ResetPath();
            }
        }

        UpdateAnimation();

        if (Input.GetMouseButtonDown(0)) CheckForAxeHit();
    }

    // **NEW: Detect if deer is stuck (can't move forward)**
    void CheckIfStuck()
    {
        if (Time.time - lastStuckCheckTime < stuckCheckTime)
            return;

        float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);

        // **If deer has a path but hasn't moved much = STUCK**
        if (agent.hasPath && agent.remainingDistance > 2f && distanceMoved < minMoveDistance)
        {
            if (!isStuck)
            {
                // **Just got stuck - start panicking**
                isStuck = true;
                stuckStartTime = Time.time;
                stuckPanicDirection = Random.insideUnitSphere;
                stuckPanicDirection.y = 0;
                stuckPanicDirection.Normalize();
            }
        }
        else
        {
            isStuck = false;
        }

        lastStuckCheckPosition = transform.position;
        lastStuckCheckTime = Time.time;
    }

    // **NEW: When stuck, deer frantically searches for escape routes**
    void HandleStuckBehavior()
    {
        float timeSinceStuck = Time.time - stuckStartTime;

        // **Panic for a bit, then try new escape route**
        if (timeSinceStuck > stuckPanicTime)
        {
            // **Try aggressive escape in random directions**
            TryEmergencyEscape();
            stuckStartTime = Time.time;  // Reset panic timer
            return;
        }

        // **While panicking, try to move in panic direction**
        Vector3 panicTarget = transform.position + stuckPanicDirection * 5f;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(panicTarget, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // **NEW: Emergency escape when truly stuck**
    void TryEmergencyEscape()
    {
        // **Try 12 directions rapidly to find ANY escape route**
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 testPos = transform.position + direction * 8f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPos, out hit, 10f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                    isStuck = false;
                    return;
                }
            }
        }

        // **If still no path found, try going backwards**
        Vector3 backwardPos = transform.position - transform.forward * 5f;
        NavMeshHit backHit;
        if (NavMesh.SamplePosition(backwardPos, out backHit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(backHit.position);
        }
    }

    bool IsOnSteepSlope()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5f))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            return slope > maxTerrainSlope;
        }
        return false;
    }

    bool IsTerrainTooSteep(Vector3 targetPos)
    {
        float heightDiff = Mathf.Abs(targetPos.y - transform.position.y);
        return heightDiff > maxHeightDifference;
    }

    void SnapToNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            if (!agent.enabled) agent.enabled = true;
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        float moveSpeed = agent.velocity.magnitude;

        if (moveSpeed < 0.1f)
            anim.speed = 0f;
        else
            anim.speed = moveSpeed * animSpeedMultiplier;
    }

    void FleeFromPlayer()
    {
        // **IMPROVED: More aggressive fleeing, prioritize speed over perfect pathing**
        Vector3 bestFleePos = Vector3.zero;
        float bestScore = -1f;

        // Try 10 different angles for better coverage
        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f; // Every 36 degrees
            Vector3 directionAway = (transform.position - player.position).normalized;
            Vector3 rotatedDir = Quaternion.Euler(0, angle, 0) * directionAway;
            Vector3 testDestination = transform.position + rotatedDir * fleeDistance;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(testDestination, out navHit, fleeDistance * 0.7f, NavMesh.AllAreas))
            {
                if (IsTerrainTooSteep(navHit.position))
                    continue;

                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    bool pathIsFlat = true;
                    for (int j = 0; j < path.corners.Length - 1; j++)
                    {
                        float cornerHeightDiff = Mathf.Abs(path.corners[j + 1].y - path.corners[j].y);
                        if (cornerHeightDiff > maxHeightDifference * 0.5f)
                        {
                            pathIsFlat = false;
                            break;
                        }
                    }

                    if (!pathIsFlat)
                        continue;

                    // **Heavily prioritize distance from player**
                    float distanceFromPlayer = Vector3.Distance(player.position, navHit.position);
                    float heightPenalty = Mathf.Abs(navHit.position.y - transform.position.y);
                    float score = distanceFromPlayer * 2f - heightPenalty;  // Distance is 2x more important

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestFleePos = navHit.position;
                    }
                }
            }
        }

        if (bestScore > 0)
        {
            agent.SetDestination(bestFleePos);
        }
        else
        {
            // **FALLBACK: Just run in ANY direction away from player**
            Vector3 awayDir = (transform.position - player.position).normalized;
            Vector3 fallbackPos = transform.position + awayDir * 10f;

            NavMeshHit fallbackHit;
            if (NavMesh.SamplePosition(fallbackPos, out fallbackHit, 15f, NavMesh.AllAreas))
            {
                agent.SetDestination(fallbackHit.position);
            }
        }
    }

    void CheckForAxeHit()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        Vector3 dirToDeer = (transform.position - player.position).normalized;

        if (dist <= axeRange && Vector3.Dot(player.forward, dirToDeer) > 0.6f)
        {
            TakeDamage(axeDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;

        if (health <= 0)
            Die();
        else
        {
            // **Immediately flee when hit - emergency escape**
            nextFleeTime = 0f;
            isStuck = false;  // Reset stuck state
            agent.speed = sprintSpeed;  // Full speed panic
            FleeFromPlayer();
        }
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;

        if (anim != null) anim.speed = 0f;

        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 90f);

        Destroy(gameObject, 5f);
    }

    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = isStuck ? Color.red : Color.green;
            Vector3 prev = transform.position;
            foreach (Vector3 corner in agent.path.corners)
            {
                Gizmos.DrawLine(prev, corner);
                Gizmos.DrawSphere(corner, 0.3f);
                prev = corner;
            }
        }

        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Show stuck status
        if (Application.isPlaying && isStuck)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f);
            Gizmos.DrawRay(transform.position, stuckPanicDirection * 3f);
        }
    }
}