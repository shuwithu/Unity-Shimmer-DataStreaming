using System.Collections.Generic;
using UnityEngine;

public class SessionResetManager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSessionData();
        }
    }

    private void ResetSessionData()
    {
        Debug.Log("[SessionResetManager] Resetting session data...");

        // Access current GameData directly
        GameData gameData = DataPersistenceManager.instance.gameData;

        if (gameData == null)
        {
            Debug.LogError("[SessionResetManager] No GameData found to reset.");
            return;
        }

        // Reset session count and clear leaf data
        gameData.sessionCount = 1;
        gameData.leafsData = new List<LeafData>();

        // Deactivate all LeafSaveData objects in scene
        LeafSaveData[] allLeafs = GameObject.FindObjectsOfType<LeafSaveData>();
        foreach (LeafSaveData leaf in allLeafs)
        {
            leaf.gameObject.SetActive(false);
            gameData.leafsData.Add(new LeafData(leaf.id, false));
        }

        // Save new state
        DataPersistenceManager.instance.SaveGame();
        Debug.Log("[SessionResetManager] Reset complete.");
    }
}