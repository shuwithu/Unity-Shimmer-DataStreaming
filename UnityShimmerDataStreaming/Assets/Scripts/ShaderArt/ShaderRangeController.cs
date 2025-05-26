using UnityEngine;
using ShimmeringUnity;

public class ShaderRangeController : MonoBehaviour
{
    [Header("Sensor Input")]
    public SensorValueUpdater sensorUpdater;

    [Header("Sensor Value Range")]
    public Vector2 temperatureRange = new Vector2(32f, 38f);

    [Header("Target Material (Required)")]
    public Material targetMaterial;

    [Header("Shader Property")]
    public string shaderProperty = "_LerpFactor";  // Ensure your shader uses this exact property name

    [Header("Debug Settings")]
    [Range(0f, 1f)] public float previewValue = 0f;
    public bool previewOnly = false;

    void Update()
    {
        if (targetMaterial == null) return;

        float lerpValue = previewOnly ? previewValue : GetNormalizedTemperature();
        targetMaterial.SetFloat(shaderProperty, lerpValue);
    }

    private float GetNormalizedTemperature()
    {
        if (sensorUpdater == null) return 0f;

        float temp = sensorUpdater.LatestTemperature;
        float norm = Mathf.Clamp01(Mathf.InverseLerp(temperatureRange.x, temperatureRange.y, temp));
        float boosted = Mathf.Pow(norm, 0.2f); // or try 1.5f or 3f
        return boosted;
        //return Mathf.Clamp01(Mathf.InverseLerp(temperatureRange.x, temperatureRange.y, temp));
    }
}