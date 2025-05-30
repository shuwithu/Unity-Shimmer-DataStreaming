using TMPro;
using UnityEngine;

public class ArboristNumberDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;

    void Start()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        if (textMesh == null)
        {
            Debug.LogError("[ArboristNumberDisplay] TextMeshPro not assigned or found.");
            return;
        }

        // Try accessing session count
        var gameData = DataPersistenceManager.instance?.GetGameData();
        if (gameData != null)
        {
            int sessionId = gameData.sessionCount;
            textMesh.text = sessionId.ToString();
        }
        else
        {
            textMesh.text = "1";
            Debug.LogError("[ArboristNumberDisplay] GameData not found.");
        }
    }
}