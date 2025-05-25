using UnityEditor;
using UnityEngine;

public class AssignLeafSaveData : EditorWindow
{
    [MenuItem("Tools/Assign LeafSaveData to LeafCluster")]
    public static void AssignLeafComponents()
    {
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("LeaveCluster");
        System.Array.Sort(allObjects, (a, b) => string.CompareOrdinal(a.name, b.name));

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject leaf = allObjects[i];

            // Add component if not already present
            LeafSaveData leafData = leaf.GetComponent<LeafSaveData>();
            if (leafData == null)
                leafData = leaf.AddComponent<LeafSaveData>();

            // Assign id based on order
            leafData.id = $"L{i + 1}";

            EditorUtility.SetDirty(leaf); // Mark as dirty for saving
        }

        Debug.Log($"[AssignLeafSaveData] Assigned LeafSaveData to {allObjects.Length} objects.");
    }
}