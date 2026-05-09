using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player Health System - handles damage, death, and respawn
/// Attach this to your Player GameObject
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    
    [Header("UI References")]
    public Slider healthBarSlider;
    public Text healthText;
    public Image healthBarFillImage;
    public GameObject deathScreen; // Optional death screen UI
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.yellow;
    public Color criticalColor = Color.red;
    
    [Header("Damage Feedback")]
    public bool enableDamageFlash = true;
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.2f;
    public bool enableScreenShake = true;
    public float screenShakeDuration = 0.2f;
    public float screenShakeIntensity = 0.3f;
    
    [Header("Audio")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip healSound;
    
    [Header("Death Settings")]
    public bool enableRagdoll = false;
    public float respawnDelay = 3f;
    public Vector3 respawnPosition = Vector3.zero;
    public bool autoRespawn = false; // Disabled - will destroy player instead
    public bool freezeOnDeath = true;
    public bool disableInputOnDeath = true;
    public bool destroyPlayerOnDeath = true; // Destroy player instead of respawn
    public float destroyDelay = 2f; // Delay before destroying (for death animation)
    
    [Header("Invincibility")]
    public float invincibilityAfterDamage = 0.5f;
    
    // Private variables
    private bool isDead = false;
    private bool isInvincible = false;
    private AudioSource audioSource;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Renderer[] playerRenderers;
    private Color[] originalColors;
    private Attack attackScript;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.localPosition;
        }
        
        playerRenderers = GetComponentsInChildren<Renderer>();
        if (playerRenderers.Length > 0)
        {
            originalColors = new Color[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = playerRenderers[i].material.color;
                }
            }
        }
        
        attackScript = GetComponent<Attack>();
        
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        
        UpdateHealthUI();
    }
    
    /// <summary>
    /// Called when player takes damage from enemy
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        UpdateHealthUI();
        
        if (enableDamageFlash)
        {
            StartCoroutine(DamageFlashEffect());
        }
        
        if (enableScreenShake && mainCamera != null)
        {
            StartCoroutine(ScreenShake());
        }
        
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
        
        if (invincibilityAfterDamage > 0)
        {
            StartCoroutine(InvincibilityFrames());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;
        
        int oldHealth = currentHealth;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        int actualHeal = currentHealth - oldHealth;
        Debug.Log($"Player healed {actualHeal} HP! Health: {currentHealth}/{maxHealth}");
        
        UpdateHealthUI();
        
        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log("========================================");
        Debug.Log(">>>       GAME OVER       <<<");
        Debug.Log(">>>    PLAYER DIED!       <<<");
        Debug.Log("========================================");
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        if (attackScript != null)
        {
            attackScript.enabled = false;
        }
        
        if (freezeOnDeath)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        
        if (disableInputOnDeath)
        {
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != this && script.GetType() != typeof(PlayerHealth))
                {
                    script.enabled = false;
                }
            }
        }
        
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }
        
        if (enableRagdoll)
        {
            EnableRagdoll();
        }
        
        // Destroy player or respawn
        if (destroyPlayerOnDeath)
        {
            StartCoroutine(DestroyPlayerAfterDelay());
        }
        else if (autoRespawn)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }
    
    IEnumerator DestroyPlayerAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        
        Debug.Log(">>> PLAYER DESTROYED - GAME OVER <<<");
        
        Destroy(gameObject);
    }
    
    void EnableRagdoll()
    {
        // Enable ragdoll physics
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;
        }
        
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
        }
    }
    
    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }
    
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        if (respawnPosition != Vector3.zero)
        {
            transform.position = respawnPosition;
        }
        
        if (attackScript != null)
        {
            attackScript.enabled = true;
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = true;
        }
        
        if (enableRagdoll)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
            }
        }
        
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        
        UpdateHealthUI();
        
        Debug.Log("Player respawned!");
    }
    
    void UpdateHealthUI()
    {
        // Update health bar slider
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
        
        // Update health bar color
        if (healthBarFillImage != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            
            if (healthPercent > 0.6f)
            {
                healthBarFillImage.color = healthyColor;
            }
            else if (healthPercent > 0.3f)
            {
                healthBarFillImage.color = damagedColor;
            }
            else
            {
                healthBarFillImage.color = criticalColor;
            }
        }
    }
    
    IEnumerator DamageFlashEffect()
    {
        // Flash red
        foreach (Renderer rend in playerRenderers)
        {
            if (rend != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = damageFlashColor;
            }
        }
        
        yield return new WaitForSeconds(damageFlashDuration);
        
        // Return to original colors
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null && playerRenderers[i].material.HasProperty("_Color"))
            {
                playerRenderers[i].material.color = originalColors[i];
            }
        }
    }
    
    IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < screenShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * screenShakeIntensity;
            float y = Random.Range(-1f, 1f) * screenShakeIntensity;
            
            mainCamera.transform.localPosition = originalCameraPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalCameraPosition;
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityAfterDamage);
        isInvincible = false;
    }
    
    // Public getters
    public bool IsDead() => isDead;
    public bool IsInvincible() => isInvincible;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    
    // Debug method
    [ContextMenu("Take 20 Damage")]
    void DebugTakeDamage()
    {
        TakeDamage(20);
    }
    
    [ContextMenu("Heal Full")]
    void DebugHealFull()
    {
        Heal(maxHealth);
    }
}