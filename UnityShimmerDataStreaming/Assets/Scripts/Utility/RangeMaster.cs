using UnityEngine;

[ExecuteInEditMode]
public class MaterialMasterRangeController : MonoBehaviour
{
    [Header("Material References")]
    public Material[] materials;

    [Header("Master Range Controls")]
    [Range(0, 1)]
    public float[] masterRangeValues;

    private void OnValidate()
    {
        // Automatically sync array sizes in editor
        if (materials != null && (masterRangeValues == null || masterRangeValues.Length != materials.Length))
        {
            float[] newValues = new float[materials.Length];

            // Preserve existing values if possible
            if (masterRangeValues != null)
            {
                for (int i = 0; i < Mathf.Min(materials.Length, masterRangeValues.Length); i++)
                {
                    newValues[i] = masterRangeValues[i];
                }
            }

            // Initialize new values to 0.5 if array was expanded
            for (int i = masterRangeValues != null ? masterRangeValues.Length : 0; i < materials.Length; i++)
            {
                newValues[i] = 0.5f;
            }

            masterRangeValues = newValues;
        }
    }

    private void Update()
    {
        if (materials == null || masterRangeValues == null) return;

        // Update shader properties
        for (int i = 0; i < Mathf.Min(materials.Length, masterRangeValues.Length); i++)
        {
            if (materials[i] != null)
            {
                materials[i].SetFloat("_MasterRange", masterRangeValues[i]);
            }
        }
    }

    // Editor-only visualization
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (materials == null) return;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null)
            {
                string label = $"{materials[i].name}: {masterRangeValues[i]:F2}";
                UnityEditor.Handles.Label(transform.position + Vector3.up * (i * 0.3f), label, style);
            }
        }
    }
#endif
}