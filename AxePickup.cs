using UnityEngine;

public class AxePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform rightHandSlot; // Assign RightHandSlot here in Inspector
    
    [Header("Visual Feedback (Optional)")]
    public GameObject pickupEffect; // Particle effect when picked up
    public AudioClip pickupSound; // Sound when picked up
    
    private bool isPickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return; // Prevent multiple pickups
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player touched the axe - attempting pickup...");
            
            // Get the Attack script from the player
            Attack playerAttack = other.GetComponent<Attack>();
            
            if (playerAttack == null)
            {
                Debug.LogError("Player doesn't have Attack script! Cannot equip axe.");
                return;
            }
            
            if (rightHandSlot == null)
            {
                Debug.LogError("Right Hand Slot not assigned! Please assign it in the Inspector.");
                return;
            }
            
            // Mark as picked up
            isPickedUp = true;
            
            // Move axe to hand
            transform.position = rightHandSlot.position;
            transform.rotation = rightHandSlot.rotation;
            transform.parent = rightHandSlot;
            
            // Disable collider so it won't trigger again
            Collider axeCollider = GetComponent<Collider>();
            if (axeCollider != null)
            {
                axeCollider.enabled = false;
            }
            
            // IMPORTANT: Tell the Attack script that axe is equipped
            playerAttack.EquipAxe();
            
            // Set the axe transform reference in Attack script for recoil animation
            if (playerAttack.axeTransform == null)
            {
                playerAttack.axeTransform = transform;
                Debug.Log("Axe transform assigned to Attack script for recoil animation");
            }
            
            // Visual/Audio feedback
            PlayPickupEffects();
            
            Debug.Log("Axe successfully equipped! Player can now attack with Left/Right/Middle Click");
        }
    }
    
    void PlayPickupEffects()
    {
        // Play pickup sound
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Spawn pickup effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    // Optional: Visual indicator that axe can be picked up
    void Update()
    {
        if (!isPickedUp)
        {
            // Gentle rotation animation
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);
            
            // Gentle float animation
            float newY = transform.position.y + Mathf.Sin(Time.time * 2f) * 0.002f;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}