using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    public int woodCount = 0;
    public int maxWoodCount = 999; // Maximum wood the player can carry
    
    [Header("UI References (Optional)")]
    public Text woodCountText; // Drag your UI Text component here
    public Image woodIcon; // Drag your wood icon image here
    
    [Header("Audio (Optional)")]
    public AudioClip collectSound; // Sound to play when collecting wood
    private AudioSource audioSource;

    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize UI
        UpdateUI();
    }
    
    public bool AddWood(int amount)
    {
        // Check if we can add the wood (don't exceed max capacity)
        if (woodCount + amount > maxWoodCount)
        {
            int maxCanAdd = maxWoodCount - woodCount;
            if (maxCanAdd <= 0)
            {
                Debug.Log("Inventory full! Cannot collect more wood.");
                return false; // Inventory full
            }
            amount = maxCanAdd;
            Debug.Log("Inventory nearly full! Only collected " + amount + " wood.");
        }
        
        woodCount += amount;
        Debug.Log("Added " + amount + " wood. Total wood: " + woodCount + "/" + maxWoodCount);
        
        // Play collection sound
        PlayCollectSound();
        
        // Update UI
        UpdateUI();
        
        return true; // Successfully added wood
    }
    
    public bool RemoveWood(int amount)
    {
        if (woodCount >= amount)
        {
            woodCount -= amount;
            Debug.Log("Removed " + amount + " wood. Total wood: " + woodCount + "/" + maxWoodCount);
            UpdateUI();
            return true; // Successfully removed wood
        }
        else
        {
            Debug.Log("Not enough wood! Have: " + woodCount + ", Need: " + amount);
            return false; // Not enough wood
        }
    }
    
    public int GetWoodCount()
    {
        return woodCount;
    }
    
    public bool HasWood(int amount)
    {
        return woodCount >= amount;
    }
    
    public bool IsInventoryFull()
    {
        return woodCount >= maxWoodCount;
    }
    
    public float GetInventoryPercentage()
    {
        return (float)woodCount / maxWoodCount;
    }
    
    void PlayCollectSound()
    {
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }
    
    void UpdateUI()
    {
        // Update wood count text
        if (woodCountText != null)
        {
            woodCountText.text = woodCount + "/" + maxWoodCount;
            
            // Change color if inventory is getting full
            if (GetInventoryPercentage() >= 0.9f)
            {
                woodCountText.color = Color.red; // Nearly full
            }
            else if (GetInventoryPercentage() >= 0.7f)
            {
                woodCountText.color = Color.yellow; // Getting full
            }
            else
            {
                woodCountText.color = Color.white; // Normal
            }
        }
        
        // You can add more UI updates here
        // Example: Update inventory bar fill amount
        // if (inventoryBar != null)
        // {
        //     inventoryBar.fillAmount = GetInventoryPercentage();
        // }
    }
    
    // Method for debugging - call this to see inventory status
    [ContextMenu("Debug Inventory")]
    void DebugInventory()
    {
        Debug.Log("=== INVENTORY STATUS ===");
        Debug.Log("Wood: " + woodCount + "/" + maxWoodCount);
        Debug.Log("Percentage Full: " + (GetInventoryPercentage() * 100f).ToString("F1") + "%");
        Debug.Log("Is Full: " + IsInventoryFull());
        Debug.Log("======================");
    }
    
    // Optional: Save/Load functionality (requires using System.IO)
    /*
    public void SaveInventory()
    {
        PlayerPrefs.SetInt("WoodCount", woodCount);
        PlayerPrefs.Save();
        Debug.Log("Inventory saved!");
    }
    
    public void LoadInventory()
    {
        woodCount = PlayerPrefs.GetInt("WoodCount", 0);
        UpdateUI();
        Debug.Log("Inventory loaded! Wood: " + woodCount);
    }
    */
}