using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public Transform rightHandSlot; // assign the RightHandSlot here
    private GameObject heldAxe = null;
    private Attack attackController; // Reference to Attack class
    
    private void Start()
    {
        // Get the Attack component from the same GameObject
        attackController = GetComponent<Attack>();
        
        if (attackController == null)
        {
            Debug.LogError("Attack script not found! Make sure it's attached to the same GameObject.");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is an axe
        if (other.CompareTag("Pickup") && heldAxe == null)
        {
            heldAxe = other.gameObject;
            
            // Move axe to hand
            heldAxe.transform.position = rightHandSlot.position;
            heldAxe.transform.rotation = rightHandSlot.rotation;
            heldAxe.transform.parent = rightHandSlot;
            
            // Disable its collider so it doesn't trigger again
            Collider axeCollider = heldAxe.GetComponent<Collider>();
            if (axeCollider != null)
            {
                axeCollider.enabled = false;
            }
            
            // Enable attack functionality
            if (attackController != null)
            {
                attackController.EquipAxe();
            }
            
            Debug.Log("Axe picked up and equipped for combat!");
        }
    }
    
    // Optional: Method to drop the axe
    public void DropAxe()
    {
        if (heldAxe != null)
        {
            // Unparent the axe
            heldAxe.transform.parent = null;
            
            // Re-enable collider
            Collider axeCollider = heldAxe.GetComponent<Collider>();
            if (axeCollider != null)
            {
                axeCollider.enabled = true;
            }
            
            // Disable attack functionality
            if (attackController != null)
            {
                attackController.UnequipAxe();
            }
            
            heldAxe = null;
            Debug.Log("Axe dropped!");
        }
    }
}