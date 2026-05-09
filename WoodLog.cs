using System.Collections;
using UnityEngine;

public class WoodLog : MonoBehaviour
{
    [Header("Collection Settings")]
    public int woodValue = 1; // How much wood this log gives
    public float collectionDelay = 0.5f; // Time before log can be collected after spawn
    public bool autoCollectOnTouch = true; // Automatically collect when player touches

    [Header("Visual Effects")]
    public float floatHeight = 0.1f; // Gentle floating animation
    public float floatSpeed = 2f;
    public GameObject collectEffect; // Optional particle effect on collection

    [Header("References - Auto-Assigned")]
    [Tooltip("Leave empty - will be auto-assigned when spawned")]
    public Transform playerTransform;
    [Tooltip("Leave empty - will be auto-assigned when spawned")]
    public Attack attackScript;

    private bool canBeCollected = false;
    private bool isCollected = false;
    private Vector3 originalPosition;
    private Rigidbody rb;
    private float spawnTime;
    private Collider logCollider;

    void Start()
    {
        spawnTime = Time.time;
        originalPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        logCollider = GetComponent<Collider>();

        // Ensure we have required components
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("Added Rigidbody to " + gameObject.name);
        }

        if (logCollider == null)
        {
            logCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to " + gameObject.name);
        }

        // Make sure collider is set as trigger for collection
        logCollider.isTrigger = true;

        // Start collection timer
        StartCoroutine(EnableCollection());

        // Auto-find player references if not assigned
        if (playerTransform == null || attackScript == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                attackScript = player.GetComponent<Attack>();
                Debug.Log("Wood log found player: " + player.name);
            }
            else
            {
                Debug.LogWarning("Wood log could not find player with 'Player' tag!");
            }
        }

        Debug.Log("Wood log " + gameObject.name + " initialized. Collection in " + collectionDelay + " seconds.");
    }

    void Update()
    {
        if (!isCollected && canBeCollected && rb != null && rb.velocity.magnitude < 0.5f)
        {
            // Gentle floating animation when log is still
            Vector3 floatOffset = new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * floatHeight, 0);
            if (originalPosition == Vector3.zero)
                originalPosition = transform.position;

            transform.position = originalPosition + floatOffset;
        }
        else if (rb != null && rb.velocity.magnitude >= 0.5f)
        {
            // Update original position while moving
            originalPosition = transform.position;
        }
    }

    IEnumerator EnableCollection()
    {
        yield return new WaitForSeconds(collectionDelay);
        canBeCollected = true;
        Debug.Log("Wood log " + gameObject.name + " is now ready for collection!");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Wood log trigger hit by: " + other.name + " with tag: " + other.tag);

        if (autoCollectOnTouch && other.CompareTag("Player"))
        {
            Debug.Log("Player touched wood log!");

            if (attackScript != null)
            {
                attackScript.OnLogTouched(this);
            }
            else
            {
                // Direct collection if no attack script
                CollectLog();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Wood log collision with: " + collision.gameObject.name + " with tag: " + collision.gameObject.tag);

        if (autoCollectOnTouch && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player collided with wood log!");

            if (attackScript != null)
            {
                attackScript.OnLogTouched(this);
            }
            else
            {
                // Direct collection if no attack script
                CollectLog();
            }
        }
    }

    public bool CanBeCollected()
    {
        return canBeCollected && !isCollected;
    }

    public void CollectLog()
    {
        if (!CanBeCollected()) return;

        isCollected = true;

        // UPDATED - Now uses ResourceManager instead of PlayerInventory
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddWood(woodValue);
        }
        else
        {
            Debug.LogWarning("ResourceManager not found in scene! Make sure ResourceManager GameObject exists.");
        }

        // Spawn collection effect if available
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Animate collection
        StartCoroutine(CollectionAnimation());
    }

    IEnumerator CollectionAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position + Vector3.up * 1.5f : startPos + Vector3.up * 2f;
        Vector3 startScale = transform.localScale;

        float animationTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < animationTime)
        {
            float t = elapsed / animationTime;
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);

            transform.position = Vector3.Lerp(startPos, targetPos, curve);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}