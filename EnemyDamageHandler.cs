using UnityEngine;
using EnemyAI;

/// <summary>
/// Attach this to Enemy GameObjects to handle damage from player attacks
/// Fixed version - hit effects now follow the enemy!
/// </summary>
public class EnemyDamageHandler : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage multiplier for different attack types")]
    public float chopDamageMultiplier = 1.0f;
    public float heavyDamageMultiplier = 2.0f;
    public float comboDamageMultiplier = 1.5f;
    
    [Header("Visual Feedback")]
    public bool showDamageFlash = true;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip[] painSounds;
    public float painSoundChance = 0.7f;
    public bool playBothHitAndPain = true; // Play both sounds together
    
    [Header("Hit Effects")]
    public GameObject hitEffect;
    public bool attachEffectToEnemy = true;
    public bool useContactPoint = true;
    public float effectDuration = 2f;
    public Vector3 effectOffset = new Vector3(0, 1.5f, 0);
    public float spreadRadius = 0.5f;
    public bool effectFollowsRotation = true; // New option
    
    [Header("Particle Settings")]
    public int particleCount = 50;
    public float particleSpeed = 5f;
    public float particleLifetime = 2f;
    
    [Header("Death Effects")]
    public GameObject deathEffect;
    public bool playDeathEffect = true;
    public Vector3 deathEffectOffset = new Vector3(0, 1f, 0);
    public float deathEffectDuration = 3f;
    public bool attachDeathEffectToEnemy = false; // Usually false - stays in world
    
    private EnemyFollow enemyFollow;
    private Renderer enemyRenderer;
    private Color originalColor;
    private AudioSource audioSource;
    private bool isDead = false;

    void Start()
    {
        enemyFollow = GetComponent<EnemyFollow>();
        if (enemyFollow == null)
        {
            Debug.LogError("EnemyDamageHandler: No EnemyFollow component found on " + gameObject.name);
        }
        
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_Color"))
        {
            originalColor = enemyRenderer.material.color;
        }
        
        // Setup AudioSource properly
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource settings
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 20f;
        audioSource.volume = 1.0f;
        audioSource.priority = 128;
    }

    // Main method with all parameters (new version)
    public void TakeDamageFromAxe(int baseDamage, AttackType attackType, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead || enemyFollow == null) return;
        
        int finalDamage = CalculateDamage(baseDamage, attackType);
        
        enemyFollow.TakeDamage(finalDamage);
        
        ShowDamageEffects(finalDamage, hitPoint, hitNormal);
        
        Debug.Log($"{gameObject.name} took {finalDamage} damage from {attackType} attack!");
    }
    
    // Backward compatible method (old version - 2 parameters)
    public void TakeDamageFromAxe(int baseDamage, AttackType attackType)
    {
        // Call the full version with default values
        Vector3 defaultHitPoint = transform.position + effectOffset;
        Vector3 defaultHitNormal = Vector3.up;
        TakeDamageFromAxe(baseDamage, attackType, defaultHitPoint, defaultHitNormal);
    }
    
    private int CalculateDamage(int baseDamage, AttackType attackType)
    {
        float multiplier = 1.0f;
        
        switch (attackType)
        {
            case AttackType.Chop:
                multiplier = chopDamageMultiplier;
                break;
            case AttackType.Heavy:
                multiplier = heavyDamageMultiplier;
                break;
            case AttackType.Combo:
                multiplier = comboDamageMultiplier;
                break;
        }
        
        return Mathf.RoundToInt(baseDamage * multiplier);
    }
    
    private void ShowDamageEffects(int damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (showDamageFlash && enemyRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        // IMPROVED: Play both hit and pain sounds simultaneously
        bool playedPainSound = false;
        
        // Play pain sound (voice/scream)
        if (painSounds != null && painSounds.Length > 0 && Random.value < painSoundChance)
        {
            AudioClip painClip = painSounds[Random.Range(0, painSounds.Length)];
            if (painClip != null)
            {
                // Use PlayClipAtPoint so it doesn't override other sounds
                AudioSource.PlayClipAtPoint(painClip, transform.position, 1.0f);
                playedPainSound = true;
                Debug.Log($"Playing pain sound: {painClip.name}");
            }
        }
        
        // ALWAYS play hit sound (impact sound) using PlayClipAtPoint
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.8f);
            Debug.Log($"Playing hit sound: {hitSound.name}");
        }
        
        if (hitEffect != null)
        {
            SpawnHitEffect(hitPoint, hitNormal);
        }
    }
    
    private void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Determine spawn position
        Vector3 spawnPosition = hitPoint != Vector3.zero ? hitPoint : (transform.position + effectOffset);
        
        // Create effect at position
        GameObject effect = Instantiate(hitEffect, spawnPosition, Quaternion.identity);
        
        if (attachEffectToEnemy)
        {
            // Find the best transform to attach to (spine/chest bone or animator root)
            Transform attachPoint = FindBestAttachPoint(spawnPosition);
            
            if (attachPoint != null)
            {
                // Parent and preserve world position
                effect.transform.SetParent(attachPoint, true);
                
                Debug.Log($"Blood effect attached to: {attachPoint.name}");
                Debug.Log($"Blood effect LOCAL position: {effect.transform.localPosition}");
            }
            else
            {
                Debug.LogWarning("No suitable attach point found!");
            }
        }
        
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main;
            main.startLifetime = particleLifetime;
            main.startSpeed = particleSpeed;
            main.maxParticles = particleCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = spreadRadius;
            
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0f, particleCount) 
            });
            
            particles.Play();
        }
        
        Destroy(effect, effectDuration);
    }
    
    private Transform FindBestAttachPoint(Vector3 hitPosition)
    {
        // Try to find spine/chest bones in animator
        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            // Try chest bone
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (chest != null) return chest;
            
            // Try spine bone
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);
            if (spine != null) return spine;
            
            // Try upper chest
            Transform upperChest = anim.GetBoneTransform(HumanBodyBones.UpperChest);
            if (upperChest != null) return upperChest;
        }
        
        // Fallback: Find by name (common bone names)
        Transform[] allTransforms = GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            string lowerName = t.name.ToLower();
            if (lowerName.Contains("spine") || lowerName.Contains("chest") || 
                lowerName.Contains("torso") || lowerName.Contains("body"))
            {
                Debug.Log($"Found bone by name: {t.name}");
                return t;
            }
        }
        
        // Last resort: use the mesh renderer
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null) return rend.transform;
        
        // Final fallback: use root
        return transform.root;
    }
    
    private Renderer GetClosestRenderer(Vector3 position)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Renderer closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Renderer rend in renderers)
        {
            float distance = Vector3.Distance(position, rend.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = rend;
            }
        }
        
        return closest;
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (enemyRenderer == null) yield break;
        
        enemyRenderer.material.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        enemyRenderer.material.color = originalColor;
    }
    
    public void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"=== DEATH EFFECT TRIGGERED for {gameObject.name} ===");
        
        // Play death sound at position (survives after object destruction)
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 1.0f);
            Debug.Log($"Playing death sound: {deathSound.name} at position {transform.position}");
        }
        else
        {
            Debug.LogWarning("Death sound clip not assigned!");
        }
        
        // Play death effect
        if (playDeathEffect && deathEffect != null)
        {
            Vector3 spawnPosition = transform.position + deathEffectOffset;
            GameObject effect = Instantiate(deathEffect, spawnPosition, Quaternion.identity);
            
            if (attachDeathEffectToEnemy)
            {
                effect.transform.SetParent(transform);
                Debug.Log("Death effect attached to enemy");
            }
            else
            {
                Debug.Log("Death effect spawned in world space");
            }
            
            Destroy(effect, deathEffectDuration);
        }
        else if (deathEffect == null)
        {
            Debug.LogWarning("No death effect assigned!");
        }
    }
}

public enum AttackType
{
    Chop,
    Heavy,
    Combo
}