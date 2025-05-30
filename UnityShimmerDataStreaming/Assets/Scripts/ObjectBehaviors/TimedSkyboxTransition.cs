using System.Collections;
using UnityEngine;

/// <summary>
/// Shows a colored skybox for 5 seconds then fades to MR view
/// </summary>
public class TimedSkyboxTransition : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float skyboxDuration = 5f;
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("Skybox")]
    [SerializeField] private Color skyboxColor = Color.blue;
    [SerializeField] private Material skyboxMaterial;
    
    [Header("MR Components")]
    [SerializeField] private OVRPassthroughLayer passthroughLayer;
    [SerializeField] private Camera mainCamera;
    
    [Header("Activation")]
    [SerializeField] private GameObject objectToActivate;
    
    private Material originalSkybox;
    private bool hasTransitioned = false;
    
    private void Awake()
    {
        // Auto-find components
        if (mainCamera == null)
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            
        if (passthroughLayer == null)
            passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
    }
    
    private void Start()
    {
        // Store original skybox
        originalSkybox = RenderSettings.skybox;
        
        // Set up initial skybox view
        SetupSkyboxView();
        
        // Start the transition sequence
        StartCoroutine(SkyboxToMRSequence());
    }
    
    /// <summary>
    /// Set up the colored skybox view
    /// </summary>
    private void SetupSkyboxView()
    {
        // Disable passthrough initially
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = false;
            passthroughLayer.textureOpacity = 0f;
        }
        
        // Configure camera for skybox
        if (mainCamera != null)
        {
            if (skyboxMaterial != null)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                // Use solid color if no skybox material
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = skyboxColor;
            }
        }
        
        Debug.Log("Skybox view active - transitioning to MR in " + skyboxDuration + " seconds");
    }
    
    /// <summary>
    /// Main sequence: Show skybox for 5 seconds, then fade to MR
    /// </summary>
    private IEnumerator SkyboxToMRSequence()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(skyboxDuration);
        
        // Fade to MR
        yield return StartCoroutine(FadeToMR());
        
        hasTransitioned = true;
        Debug.Log("Transition to MR complete");
    }
    
    /// <summary>
    /// Fade from skybox to MR view
    /// </summary>
    private IEnumerator FadeToMR()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            // Fade passthrough opacity
            if (passthroughLayer != null)
            {
                if (!passthroughLayer.enabled && t > 0.1f)
                {
                    passthroughLayer.enabled = true;
                }
                passthroughLayer.textureOpacity = t;
            }
            
            // Fade camera background if using solid color
            if (mainCamera != null && mainCamera.clearFlags == CameraClearFlags.SolidColor)
            {
                Color currentColor = mainCamera.backgroundColor;
                currentColor.a = 1f - t;
                mainCamera.backgroundColor = currentColor;
            }
            
            yield return null;
        }
        
        // Ensure MR is fully active
        SetupMRView();
    }
    
    /// <summary>
    /// Set up the final MR view
    /// </summary>
    private void SetupMRView()
    {
        // Enable passthrough fully
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = true;
            passthroughLayer.textureOpacity = 1f;
        }
        
        // Configure camera for MR
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.clear;
        }
        
        // Clear skybox
        RenderSettings.skybox = null;
        
        // Activate the specified GameObject
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
            Debug.Log($"Activated GameObject: {objectToActivate.name}");
        }
    }
    
    /// <summary>
    /// Manually trigger the transition (for testing)
    /// </summary>
    [ContextMenu("Start Transition")]
    public void StartTransition()
    {
        if (!hasTransitioned)
        {
            StopAllCoroutines();
            StartCoroutine(FadeToMR());
        }
    }
    
    /// <summary>
    /// Reset to skybox view (for testing)
    /// </summary>
    [ContextMenu("Reset to Skybox")]
    public void ResetToSkybox()
    {
        hasTransitioned = false;
        StopAllCoroutines();
        SetupSkyboxView();
        StartCoroutine(SkyboxToMRSequence());
    }
    
    private void OnValidate()
    {
        skyboxDuration = Mathf.Max(0.1f, skyboxDuration);
        fadeDuration = Mathf.Max(0.1f, fadeDuration);
    }
}