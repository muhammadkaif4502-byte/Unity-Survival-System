using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TreeData class - stores tree information
public class TreeData : MonoBehaviour
{
    [Header("Tree Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Tree Properties")]
    public int woodDrops = 3;
    public bool hasFallen = false;
    public bool isBeingChopped = false;

    void Start()
    {
        currentHealth = maxHealth;
        hasFallen = false;
        isBeingChopped = false;
    }

    public void TakeDamage(int damage)
    {
        if (isBeingChopped) return;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
    }

    public bool IsDestroyed()
    {
        return currentHealth <= 0;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / (float)maxHealth;
    }
}

// Main Attack class
public class Attack : MonoBehaviour
{
    [Header("Attack Settings")]
    public bool hasAxe = false;
    public float attackRange = 5.0f;
    public int chopDamage = 20;
    public int heavyDamage = 40;
    public int comboDamage = 30;

    [Header("Wood Drops")]
    public GameObject woodLogPrefab;
    public int woodLogsPerTree = 3;
    public float logHeight = 1.0f;
    public float groundOffset = 0.05f;
    public float bounceHeight = 2.0f;
    public float bounceSpread = 0.5f;

    [Header("Axe Recoil Animation")]
    public Transform axeTransform;
    public float recoilDistance = 0.2f;
    public float recoilAngle = 15f;

    [Header("Log Collection")]
    public float collectionRange = 2.5f;
    public LayerMask logLayerMask = -1;
    public KeyCode collectKey = KeyCode.F;

    [Header("Enemy Combat")]
    public float enemyAttackRange = 2.5f;
    public bool canAttackEnemies = true;

    private Animator animator;
    private bool isAttacking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found on " + gameObject.name);
        }
    }

    void Update()
    {
        if (hasAxe && !isAttacking)
        {
            if (Input.GetMouseButtonDown(0)) StartChopAttack();
            if (Input.GetMouseButtonDown(1)) StartHeavyAttack();
            if (Input.GetMouseButtonDown(2)) StartComboAttack();
        }

        if (Input.GetKeyDown(collectKey)) CollectNearbyLogs();
    }

    void StartChopAttack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
        StartCoroutine(ChopHitCheck(0.2f, AttackType.Chop));
        StartCoroutine(ResetAttackState(0.8f));
    }

    void StartHeavyAttack()
    {
        isAttacking = true;
        animator.SetTrigger("HeavyAttack");
        StartCoroutine(ChopHitCheck(0.3f, AttackType.Heavy));
        StartCoroutine(ResetAttackState(1.0f));
    }

    void StartComboAttack()
    {
        isAttacking = true;
        animator.SetTrigger("ComboAttack");
        StartCoroutine(ChopHitCheck(0.25f, AttackType.Combo));
        StartCoroutine(ResetAttackState(1.2f));
    }

    IEnumerator ChopHitCheck(float delay, AttackType attackType)
    {
        yield return new WaitForSeconds(delay);
        
        // Method 1: Forward sphere cast
        Vector3 attackPos = transform.position + transform.forward * attackRange;
        float detectionRadius = attackRange; // Full range detection
        
        Collider[] hits = Physics.OverlapSphere(attackPos, detectionRadius);
        
        // Method 2: Also check around player position
        Collider[] hitsAroundPlayer = Physics.OverlapSphere(transform.position, attackRange * 1.5f);
        
        // Combine both detection methods
        List<Collider> allHits = new List<Collider>();
        allHits.AddRange(hits);
        foreach (Collider hit in hitsAroundPlayer)
        {
            if (!allHits.Contains(hit))
                allHits.Add(hit);
        }

        bool hitSomething = false;

        Debug.Log($"Attack detection: Found {allHits.Count} total colliders");

        foreach (Collider hit in allHits)
        {
            // Skip if hitting self
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            Debug.Log($"Checking: {hit.name} (Tag: {hit.tag}, Distance: {distance:F2}m)");
            
            // Check for enemies - IMPROVED DETECTION
            // Check the collider itself, its parent, and root for enemy components
            GameObject targetObject = hit.gameObject;
            GameObject rootObject = hit.transform.root.gameObject;
            GameObject parentObject = hit.transform.parent != null ? hit.transform.parent.gameObject : null;
            
            // Method 1: Check tag on collider
            if (canAttackEnemies && hit.CompareTag("Enemy"))
            {
                Debug.Log($"ENEMY HIT (by collider tag): {hit.name} at distance {distance:F2}m");
                StartCoroutine(AnimateAxeRecoil());
                DamageEnemy(targetObject, attackType);
                hitSomething = true;
                break;
            }
            
            // Method 2: Check for EnemyFollow component on hit object
            if (canAttackEnemies && hit.GetComponent<EnemyAI.EnemyFollow>() != null)
            {
                Debug.Log($"ENEMY HIT (EnemyFollow on collider): {hit.name}");
                StartCoroutine(AnimateAxeRecoil());
                DamageEnemy(targetObject, attackType);
                hitSomething = true;
                break;
            }
            
            // Method 3: Check parent for EnemyFollow component
            if (canAttackEnemies && parentObject != null)
            {
                if (parentObject.CompareTag("Enemy") || parentObject.GetComponent<EnemyAI.EnemyFollow>() != null)
                {
                    Debug.Log($"ENEMY HIT (found on parent): {parentObject.name}");
                    StartCoroutine(AnimateAxeRecoil());
                    DamageEnemy(parentObject, attackType);
                    hitSomething = true;
                    break;
                }
            }
            
            // Method 4: Check root object for EnemyFollow
            if (canAttackEnemies && rootObject != null && rootObject != targetObject)
            {
                if (rootObject.CompareTag("Enemy") || rootObject.GetComponent<EnemyAI.EnemyFollow>() != null)
                {
                    Debug.Log($"ENEMY HIT (found on root): {rootObject.name}");
                    StartCoroutine(AnimateAxeRecoil());
                    DamageEnemy(rootObject, attackType);
                    hitSomething = true;
                    break;
                }
            }
            
            // Check for trees
            if (hit.CompareTag("Tree"))
            {
                Debug.Log($"TREE HIT: {hit.name}");
                StartCoroutine(AnimateAxeRecoil());
                DamageTree(hit.gameObject);
                hitSomething = true;
                break;
            }
        }

        if (!hitSomething)
        {
            Debug.LogWarning($"Attack MISSED! No valid targets found. Detected {allHits.Count} colliders but none were enemies or trees.");
        }
    }

    void DamageTree(GameObject tree)
    {
        TreeData treeData = tree.GetComponent<TreeData>();
        if (treeData == null)
        {
            treeData = tree.AddComponent<TreeData>();
            treeData.maxHealth = 100;
            treeData.currentHealth = 100;
            treeData.woodDrops = 3;
        }

        if (treeData.isBeingChopped) return;

        treeData.currentHealth -= chopDamage;

        if (!treeData.hasFallen)
        {
            StartCoroutine(ShakeTree(tree.transform));
        }

        if (treeData.currentHealth <= 0)
        {
            if (!treeData.hasFallen)
            {
                StartCoroutine(TreeFallAndDestroy(tree, treeData));
            }
            else
            {
                DestroyFallenTree(tree, treeData);
            }
        }
    }

    void DamageEnemy(GameObject enemy, AttackType attackType)
    {
        EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
        
        if (damageHandler != null)
        {
            int damage = GetDamageForAttackType(attackType);
            
            // Get hit point using raycast for accurate position
            Vector3 hitPoint = enemy.transform.position + Vector3.up * 1.5f; // Default to chest height
            Vector3 hitNormal = -transform.forward;
            
            // Try to get exact hit point with raycast
            RaycastHit hit;
            Vector3 rayStart = transform.position + Vector3.up;
            if (Physics.Raycast(rayStart, transform.forward, out hit, attackRange * 2f))
            {
                if (hit.collider != null && (hit.collider.gameObject == enemy || hit.collider.transform.IsChildOf(enemy.transform)))
                {
                    hitPoint = hit.point;
                    hitNormal = hit.normal;
                }
            }
            
            // Call with all 4 parameters
            damageHandler.TakeDamageFromAxe(damage, attackType, hitPoint, hitNormal);
            
            Debug.Log($"Hit {enemy.name} with {attackType} attack for {damage} base damage at {hitPoint}!");
        }
        else
        {
            Debug.LogWarning($"Enemy {enemy.name} hit but has no EnemyDamageHandler component!");
            
            EnemyAI.EnemyFollow enemyFollow = enemy.GetComponent<EnemyAI.EnemyFollow>();
            if (enemyFollow != null)
            {
                int damage = GetDamageForAttackType(attackType);
                enemyFollow.TakeDamage(damage);
                Debug.Log($"Direct damage to {enemy.name}: {damage}");
            }
        }
    }

    int GetDamageForAttackType(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Chop:
                return chopDamage;
            case AttackType.Heavy:
                return heavyDamage;
            case AttackType.Combo:
                return comboDamage;
            default:
                return chopDamage;
        }
    }

    IEnumerator TreeFallAndDestroy(GameObject tree, TreeData treeData)
    {
        treeData.isBeingChopped = true;

        Transform treeTransform = tree.transform;
        Vector3 originalPos = treeTransform.position;
        Quaternion originalRot = treeTransform.rotation;

        Vector3 fallDir = (treeTransform.position - transform.position).normalized;
        fallDir.y = 0;

        Quaternion targetRot = Quaternion.LookRotation(fallDir) * Quaternion.Euler(90, 0, 0);
        Vector3 targetPos = originalPos + fallDir * 1.5f;
        targetPos.y = originalPos.y;

        float fallTime = 2f;
        float elapsed = 0f;

        while (elapsed < fallTime)
        {
            float t = elapsed / fallTime;
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);

            treeTransform.rotation = Quaternion.Lerp(originalRot, targetRot, curve);
            treeTransform.position = Vector3.Lerp(originalPos, targetPos, curve);

            elapsed += Time.deltaTime;
            yield return null;
        }

        treeTransform.rotation = targetRot;
        treeTransform.position = targetPos;

        StartCoroutine(SpawnLogsAfterDelay(treeTransform.position, 0.1f));
        Destroy(tree);
    }

    void DestroyFallenTree(GameObject tree, TreeData treeData)
    {
        StartCoroutine(SpawnLogsAfterDelay(tree.transform.position, 0.2f));
        Destroy(tree);
    }

    IEnumerator SpawnLogsAfterDelay(Vector3 treePosition, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (woodLogPrefab == null || woodLogsPerTree <= 0) yield break;

        Vector3 lineDir = transform.right.normalized;
        float spacing = 1.2f;
        Vector3 startPos = treePosition - lineDir * ((woodLogsPerTree - 1) * spacing / 2f);
        
        // Find ground level using raycast
        float groundLevel = GetGroundLevel(treePosition);

        for (int i = 0; i < woodLogsPerTree; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            
            Vector3 dropPos = startPos + lineDir * (i * spacing);
            
            // Use raycast to find exact ground position for this log
            float logGroundLevel = GetGroundLevel(dropPos);
            
            // Spawn above ground with offset
            dropPos.y = logGroundLevel + logHeight / 2f + groundOffset + 0.5f; // Extra 0.5 to ensure above ground

            Quaternion rot = Quaternion.LookRotation(lineDir) * Quaternion.Euler(0f, 90f, 0f);
            rot *= Quaternion.Euler(0f, Random.Range(-10f, 10f), 0f);

            GameObject log = Instantiate(woodLogPrefab, dropPos, rot);

            WoodLog woodLogScript = log.GetComponent<WoodLog>();
            if (woodLogScript == null)
            {
                woodLogScript = log.AddComponent<WoodLog>();
            }

            woodLogScript.playerTransform = transform;
            woodLogScript.attackScript = this;

            Rigidbody rb = log.GetComponent<Rigidbody>();
            if (rb == null) rb = log.AddComponent<Rigidbody>();

            rb.mass = 1f;
            rb.drag = 0.2f;
            rb.angularDrag = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 velocity = new Vector3(
                Random.Range(-bounceSpread, bounceSpread),
                bounceHeight,
                Random.Range(-bounceSpread, bounceSpread)
            );
            rb.velocity = velocity;

            Vector3 torque = new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(-8f, 8f),
                Random.Range(-3f, 3f)
            );
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }
    
    float GetGroundLevel(Vector3 position)
    {
        RaycastHit hit;
        
        // Cast ray downward from high position to find ground
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
        {
            // Found ground
            return hit.point.y;
        }
        
        // Fallback: use original position Y
        return position.y;
    }

    void CollectNearbyLogs()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, collectionRange, logLayerMask);
        int collectedCount = 0;
        
        foreach (Collider obj in nearby)
        {
            WoodLog log = obj.GetComponent<WoodLog>();
            if (log != null && log.CanBeCollected())
            {
                log.CollectLog();
                collectedCount++;
            }
        }
        
        if (collectedCount > 0)
        {
            Debug.Log($"Collected {collectedCount} wood logs!");
        }
    }

    IEnumerator AnimateAxeRecoil()
    {
        if (axeTransform == null) yield break;

        Vector3 originalPos = axeTransform.localPosition;
        Quaternion originalRot = axeTransform.localRotation;

        Vector3 recoilPos = originalPos + Vector3.back * recoilDistance;
        Quaternion recoilRot = originalRot * Quaternion.Euler(recoilAngle, 0f, 0f);

        float recoilTime = 0.15f;
        float elapsed = 0f;
        while (elapsed < recoilTime)
        {
            float t = elapsed / recoilTime;
            axeTransform.localPosition = Vector3.Lerp(originalPos, recoilPos, t);
            axeTransform.localRotation = Quaternion.Lerp(originalRot, recoilRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float returnTime = 0.25f;
        elapsed = 0f;
        while (elapsed < returnTime)
        {
            float t = elapsed / returnTime;
            axeTransform.localPosition = Vector3.Lerp(recoilPos, originalPos, t);
            axeTransform.localRotation = Quaternion.Lerp(recoilRot, originalRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        axeTransform.localPosition = originalPos;
        axeTransform.localRotation = originalRot;
    }

    IEnumerator ShakeTree(Transform treeTransform)
    {
        Vector3 originalPos = treeTransform.position;
        float shakeDuration = 0.3f;
        float shakeMagnitude = 0.1f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float z = originalPos.z + Random.Range(-shakeMagnitude, shakeMagnitude);
            treeTransform.position = new Vector3(x, originalPos.y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        treeTransform.position = originalPos;
    }

    IEnumerator ResetAttackState(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    public void EquipAxe()
    {
        hasAxe = true;
        Debug.Log("Axe equipped!");
    }

    public void UnequipAxe()
    {
        hasAxe = false;
        Debug.Log("Axe unequipped!");
    }

    // Called when player touches a wood log
    public void OnLogTouched(WoodLog log)
    {
        if (log != null && log.CanBeCollected())
        {
            log.CollectLog();
        }
    }

    // Utility method to check if player can currently attack
    public bool CanAttack()
    {
        return hasAxe && !isAttacking;
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw attack range sphere
        Gizmos.color = Color.red;
        Vector3 attackPos = transform.position + transform.forward * attackRange;
        Gizmos.DrawWireSphere(attackPos, attackRange * 0.8f);
        
        // Draw line to attack position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, attackPos);

        // Draw collection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}