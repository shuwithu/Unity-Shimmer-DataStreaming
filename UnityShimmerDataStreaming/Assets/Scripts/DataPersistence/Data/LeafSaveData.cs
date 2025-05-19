using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafSaveData : MonoBehaviour, IDataPersistence
{
    public string id;

    private void Awake()
    {
        if (string.IsNullOrEmpty(id))
            id = gameObject.name;
    }

    public void LoadData(GameData data)
    {
        if (data.leafsData == null)
        {
            Debug.LogWarning($"[LeafSaveData] leafsData is null when loading {id}");
            return;
        }

        LeafData leaf = data.leafsData.Find(c => c.id == id);
        if (leaf != null)
        {
            gameObject.SetActive(leaf.isActive);
        }
        else
        {
            Debug.Log($"[LeafSaveData] No data found for {id}, leaving active state as-is.");
        }
    }

    public void SaveData(GameData data)
    {
        LeafData leafData = new LeafData(id, gameObject.activeSelf);

        int index = data.leafsData.FindIndex(c => c.id == id);
        if (index >= 0)
            data.leafsData[index] = leafData;
        else
            data.leafsData.Add(leafData);
    }
}
