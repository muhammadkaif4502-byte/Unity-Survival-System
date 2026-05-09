using UnityEngine;

/// <summary>
/// Attach this to your Player to diagnose combat issues
/// Press 'D' key to run diagnostics
/// </summary>
public class CombatDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public KeyCode debugKey = KeyCode.D;
    public bool showRealTimeInfo = true;
    public float gizmoDrawDistance = 5f;
    
    private Attack attackScript;
    private PlayerPickup pickupScript;
    
    void Start()
    {
        attackScript = GetComponent<Attack>();
        pickupScript = GetComponent<PlayerPickup>();
        
        Debug.Log("=== COMBAT DEBUGGER STARTED ===");
        Debug.Log("Press '" + debugKey + "' key to run full diagnostics");
        Debug.Log("================================");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            RunFullDiagnostics();
        }
        
        if (showRealTimeInfo && Input.GetMouseButtonDown(0))
        {
            Debug.Log("--- LEFT CLICK DETECTED ---");
            CheckAttackStatus();
        }
    }
    
    void RunFullDiagnostics()
    {
        Debug.Log("\n========== FULL COMBAT DIAGNOSTICS ==========");
        
        // 1. Check Attack Script
        Debug.Log("\n--- 1. ATTACK SCRIPT CHECK ---");
        if (attackScript == null)
        {
            Debug.LogError("PROBLEM: No Attack script found on player!");
        }
        else
        {
            Debug.Log("Attack script: FOUND");
            Debug.Log("  Has Axe: " + attackScript.hasAxe);
            Debug.Log("  Attack Range: " + attackScript.attackRange);
            Debug.Log("  Chop Damage: " + attackScript.chopDamage);
            Debug.Log("  Can Attack Enemies: " + attackScript.canAttackEnemies);
            Debug.Log("  Axe Transform: " + (attackScript.axeTransform != null ? "Assigned" : "NULL"));
            
            if (!attackScript.hasAxe)
            {
                Debug.LogWarning("WARNING: hasAxe is FALSE - player cannot attack!");
            }
        }
        
        // 2. Check Pickup Script
        Debug.Log("\n--- 2. PICKUP SCRIPT CHECK ---");
        if (pickupScript == null)
        {
            Debug.LogWarning("No PlayerPickup script found");
        }
        else
        {
            Debug.Log("PlayerPickup script: FOUND");
        }
        
        // 3. Check Player Tag
        Debug.Log("\n--- 3. PLAYER TAG CHECK ---");
        if (gameObject.CompareTag("Player"))
        {
            Debug.Log("Player tag: CORRECT");
        }
        else
        {
            Debug.LogError("PROBLEM: GameObject tag is '" + gameObject.tag + "' but should be 'Player'");
        }
        
        // 4. Check for Enemies
        Debug.Log("\n--- 4. ENEMY DETECTION CHECK ---");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            Debug.LogWarning("WARNING: No enemies found with 'Enemy' tag in scene!");
        }
        else
        {
            Debug.Log("Found " + enemies.Length + " enemy/enemies:");
            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                Debug.Log("  - " + enemy.name + " (distance: " + distance.ToString("F2") + "m)");
                
                // Check enemy components
                EnemyAI.EnemyFollow enemyFollow = enemy.GetComponent<EnemyAI.EnemyFollow>();
                EnemyDamageHandler damageHandler = enemy.GetComponent<EnemyDamageHandler>();
                Collider enemyCollider = enemy.GetComponent<Collider>();
                
                Debug.Log("    - EnemyFollow: " + (enemyFollow != null ? "YES" : "MISSING"));
                Debug.Log("    - EnemyDamageHandler: " + (damageHandler != null ? "YES" : "MISSING"));
                Debug.Log("    - Collider: " + (enemyCollider != null ? "YES" : "MISSING"));
                
                if (enemyCollider != null)
                {
                    Debug.Log("    - Collider isTrigger: " + enemyCollider.isTrigger + " (should be FALSE)");
                }
            }
        }
        
        // 5. Check Axe in Scene
        Debug.Log("\n--- 5. AXE PICKUP CHECK ---");
        GameObject[] pickups = GameObject.FindGameObjectsWithTag("Pickup");
        if (pickups.Length == 0)
        {
            Debug.LogWarning("No objects with 'Pickup' tag found");
        }
        else
        {
            Debug.Log("Found " + pickups.Length + " pickup(s):");
            foreach (GameObject pickup in pickups)
            {
                Debug.Log("  - " + pickup.name);
                AxePickup axePickup = pickup.GetComponent<AxePickup>();
                if (axePickup != null)
                {
                    Debug.Log("    - AxePickup script: YES");
                    Debug.Log("    - RightHandSlot assigned: " + (axePickup.rightHandSlot != null ? "YES" : "NO"));
                }
            }
        }
        
        // 6. Check Animator
        Debug.Log("\n--- 6. ANIMATOR CHECK ---");
        Animator anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("PROBLEM: No Animator found on player!");
        }
        else
        {
            Debug.Log("Animator: FOUND");
            Debug.Log("  Has 'Attack' trigger: " + HasParameter(anim, "Attack"));
            Debug.Log("  Has 'HeavyAttack' trigger: " + HasParameter(anim, "HeavyAttack"));
            Debug.Log("  Has 'ComboAttack' trigger: " + HasParameter(anim, "ComboAttack"));
        }
        
        Debug.Log("\n========== DIAGNOSTICS COMPLETE ==========\n");
    }
    
    void CheckAttackStatus()
    {
        if (attackScript == null)
        {
            Debug.LogError("Cannot attack - No Attack script!");
            return;
        }
        
        if (!attackScript.hasAxe)
        {
            Debug.LogWarning("Cannot attack - hasAxe is FALSE! Pick up the axe first.");
            return;
        }
        
        // Check for enemies in range
        Vector3 attackPos = transform.position + transform.forward * attackScript.attackRange;
        Collider[] hits = Physics.OverlapSphere(attackPos, attackScript.attackRange * 0.8f);
        
        Debug.Log("Attack check at position: " + attackPos);
        Debug.Log("Detected " + hits.Length + " colliders:");
        
        bool foundEnemy = false;
        foreach (Collider hit in hits)
        {
            Debug.Log("  - " + hit.name + " (Tag: " + hit.tag + ")");
            if (hit.CompareTag("Enemy"))
            {
                foundEnemy = true;
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                Debug.Log("    --> ENEMY FOUND! Distance: " + distance.ToString("F2"));
            }
        }
        
        if (!foundEnemy)
        {
            Debug.LogWarning("No enemies in attack range!");
        }
    }
    
    bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    // Visual debug in Scene view
    void OnDrawGizmos()
    {
        if (attackScript == null) return;
        
        // Draw attack range
        Gizmos.color = attackScript.hasAxe ? Color.red : Color.gray;
        Vector3 attackPos = transform.position + transform.forward * attackScript.attackRange;
        Gizmos.DrawWireSphere(attackPos, attackScript.attackRange * 0.8f);
        Gizmos.DrawLine(transform.position, attackPos);
        
        // Draw line to nearest enemy
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length > 0)
        {
            GameObject nearest = enemies[0];
            float nearestDist = Vector3.Distance(transform.position, nearest.transform.position);
            
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }
            
            Gizmos.color = nearestDist <= attackScript.attackRange ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up, nearest.transform.position + Vector3.up);
        }
    }
}