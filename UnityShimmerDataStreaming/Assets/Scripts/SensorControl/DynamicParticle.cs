using UnityEngine;

public class DynamicParticle : MonoBehaviour
{
    [Header("Particle System")]
    public ParticleSystem particleSystem;

    [Header("Sensor Values")]
    [SerializeField] private float _heartRate = 60f;
    [SerializeField] private float _gsrValue = 0.5f;
    [SerializeField] private float _ppgValue = 0f;
    [SerializeField] private float _temperature = 25f;
    
    public float HeartRate
    {
        get => _heartRate;
        set => _heartRate = Mathf.Clamp(value, minHR, maxHR);
    }
    
    public float GSRValue
    {
        get => _gsrValue;
        set => _gsrValue = Mathf.Clamp(value, minGSR, maxGSR);
    }
    
    public float Temperature
    {
        get => _temperature;
        set => _temperature = Mathf.Clamp(value, minTemperature, maxTemperature);
    }
    
    public float PPGValue
    {
        get => _ppgValue;
        set => _ppgValue = value;
    }

    [Header("Heart Rate Mapping")]
    [Tooltip("Minimum expected heart rate (BPM)")]
    public float minHR = 40f;
    [Tooltip("Maximum expected heart rate (BPM)")]
    public float maxHR = 180f;
    [Tooltip("Base speed when at minimum heart rate")]
    public float minSpeed = 1f;
    [Tooltip("Maximum speed when at peak heart rate")]
    public float maxSpeed = 10f;

    [Header("GSR Mapping")]
    [Tooltip("Minimum expected GSR value")]
    public float minGSR = 0.1f;
    [Tooltip("Maximum expected GSR value")]
    public float maxGSR = 10f;
    [Tooltip("Base size at minimum GSR")]
    public float minSize = 0.1f;
    [Tooltip("Maximum size at peak GSR")]
    public float maxSize = 3f;

    [Header("Temperature Mapping")]
    [Tooltip("Minimum expected temperature value")]
    public float minTemperature = 20f;
    [Tooltip("Maximum expected temperature value")]                  
    public float maxTemperature = 40f;
    public Gradient colorGradient;

    [Header("Debug (Read Only)")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float currentSize;
    [SerializeField] private Color currentColor;

    void Start()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                Debug.LogError("No ParticleSystem found!", this);
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (particleSystem == null) return;

        UpdateParticleSpeed();
        UpdateParticleSize();
        UpdateParticleColor();
    }

    private void UpdateParticleSpeed()
    {
        // Use the private field _heartRate instead of heartRate
        float normalizedHR = Mathf.InverseLerp(minHR, maxHR, _heartRate);
        currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedHR);
        
        var main = particleSystem.main;
        main.startSpeed = currentSpeed;
    }

    private void UpdateParticleSize()
    {
        // Use the private field _gsrValue instead of gsrValue
        float normalizedGSR = Mathf.InverseLerp(minGSR, maxGSR, _gsrValue);
        currentSize = Mathf.Lerp(minSize, maxSize, normalizedGSR);
        
        var main = particleSystem.main;
        main.startSize = currentSize;
    }

    private void UpdateParticleColor()
    {
        // Use the private field _temperature instead of temperature
        float normalizedTemp = Mathf.InverseLerp(minTemperature, maxTemperature, _temperature);
        currentColor = colorGradient.Evaluate(normalizedTemp);
        
        var main = particleSystem.main;
        main.startColor = currentColor;
    }

    // Updated setters to use the private fields
    public void SetHeartRate(float value) => _heartRate = Mathf.Clamp(value, minHR, maxHR);
    public void SetGSRValue(float value) => _gsrValue = Mathf.Clamp(value, minGSR, maxGSR);
    public void SetTemperature(float value) => _temperature = Mathf.Clamp(value, minTemperature, maxTemperature);
    public void SetPPGValue(float value) => _ppgValue = value;
}