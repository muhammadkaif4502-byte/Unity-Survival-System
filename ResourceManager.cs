using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    // Global access from any script
    public static ResourceManager Instance;

    [Header("Resources")]
    public int woodCount = 0;

    [Header("UI References")]
    public TextMeshProUGUI woodCountText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateWoodUI();
    }

    // Call this when player collects wood
    public void AddWood(int amount)
    {
        woodCount += amount;
        UpdateWoodUI();
        Debug.Log("Total Wood: " + woodCount);
    }

    // Call this when crafting uses wood
    public bool UseWood(int amount)
    {
        if (woodCount >= amount)
        {
            woodCount -= amount;
            UpdateWoodUI();
            Debug.Log("Used " + amount + " wood. Remaining: " + woodCount);
            return true;
        }
        Debug.Log("Not enough wood! Need: " + amount + " Have: " + woodCount);
        return false;
    }

    // Check if player has enough wood
    public bool HasEnoughWood(int amount)
    {
        return woodCount >= amount;
    }

    // Updates the UI text
    void UpdateWoodUI()
    {
        if (woodCountText != null)
            woodCountText.text = "x " + woodCount;
    }
}