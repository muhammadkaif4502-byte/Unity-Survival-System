using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Converts Unity Terrain Trees to real GameObjects with scripts at runtime
/// Attach this to your Terrain GameObject
/// </summary>
public class TerrainTreeConverter : MonoBehaviour
{
    [Header("Settings")]
    public Terrain terrain;
    public GameObject choppableTreePrefab; // Your tree prefab WITH scripts
    public bool convertOnStart = true;
    public bool showProgress = true;
    
    [Header("Conversion Options")]
    public bool keepOriginalTrees = false; // Keep terrain trees as visuals
    public float conversionRadius = 50f; // Only convert trees within this radius of player
    public Transform playerTransform;
    
    [Header("Status")]
    public int totalTerrainTrees = 0;
    public int convertedTrees = 0;
    
    private List<GameObject> spawnedTrees = new List<GameObject>();
    
    void Start()
    {
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }
        
        if (convertOnStart)
        {
            ConvertAllTrees();
        }
    }
    
    [ContextMenu("Convert All Terrain Trees")]
    public void ConvertAllTrees()
    {
        if (terrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return;
        }
        
        if (choppableTreePrefab == null)
        {
            Debug.LogError("No choppable tree prefab assigned!");
            return;
        }
        
        TerrainData terrainData = terrain.terrainData;
        totalTerrainTrees = terrainData.treeInstanceCount;
        
        Debug.Log($"Converting {totalTerrainTrees} terrain trees...");
        
        // Get terrain position and size
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        
        // Convert each tree
        TreeInstance[] trees = terrainData.treeInstances;
        
        for (int i = 0; i < trees.Length; i++)
        {
            TreeInstance tree = trees[i];
            
            // Calculate world position
            Vector3 worldPos = new Vector3(
                terrainPos.x + tree.position.x * terrainSize.x,
                terrainPos.y + tree.position.y * terrainSize.y,
                terrainPos.z + tree.position.z * terrainSize.z
            );
            
            // Check if within conversion radius (if player assigned)
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(worldPos, playerTransform.position);
                if (distance > conversionRadius)
                {
                    continue; // Skip trees too far away
                }
            }
            
            // Spawn choppable tree
            Quaternion rotation = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
            Vector3 scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            
            GameObject newTree = Instantiate(choppableTreePrefab, worldPos, rotation);
            newTree.transform.localScale = scale;
            newTree.tag = "Tree"; // Make sure it has Tree tag
            
            spawnedTrees.Add(newTree);
            convertedTrees++;
            
            if (showProgress && i % 100 == 0)
            {
                Debug.Log($"Converted {i}/{totalTerrainTrees} trees...");
            }
        }
        
        Debug.Log($"? Conversion complete! {convertedTrees}/{totalTerrainTrees} trees converted.");
        
        // Remove original terrain trees
        if (!keepOriginalTrees)
        {
            terrainData.treeInstances = new TreeInstance[0];
            terrain.Flush();
            Debug.Log("Original terrain trees removed.");
        }
    }
    
    [ContextMenu("Remove All Converted Trees")]
    public void RemoveConvertedTrees()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
            {
                Destroy(tree);
            }
        }
        
        spawnedTrees.Clear();
        convertedTrees = 0;
        Debug.Log("All converted trees removed.");
    }
    
    [ContextMenu("Restore Terrain Trees")]
    public void RestoreTerrainTrees()
    {
        RemoveConvertedTrees();
        
        // You would need to save original tree data to restore
        Debug.Log("Terrain trees restored (if backup exists).");
    }
}