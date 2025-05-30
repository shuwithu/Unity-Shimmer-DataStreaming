using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.Util;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.Diagnostics;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Meta.XR.ImmersiveDebugger;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Script to switch between Mixed Reality (Passthrough) and VR modes
/// </summary>
public class MRVRModeSwitcher : MonoBehaviour
{
    [Header("Mode Settings")]
    [SerializeField] private bool startInMRMode = true;
    [SerializeField] private float transitionDuration = 1.0f;
    
    [Header("VR Environment")]
    [SerializeField] private GameObject vrEnvironmentParent;
    [SerializeField] private Material vrSkyboxMaterial;
    [SerializeField] private Light vrDirectionalLight;
    [SerializeField] private bool useInvertedSphere = false;
    [SerializeField] private MeshRenderer sphereRenderer;
    
    [Header("MR/Passthrough Settings")]
    [SerializeField] private OVRPassthroughLayer passthroughLayer;
    [SerializeField] private Camera mainCamera;
    
    [Header("Input Controls")]
    [SerializeField] private OVRInput.Button switchButton = OVRInput.Button.One;
    [SerializeField] private KeyCode keyboardKey = KeyCode.M;
    
    [Header("Proximity Toggle")]
    [SerializeField] private bool useProximityToggle = false;
    [SerializeField] private Transform objectA;
    [SerializeField] private Transform objectB;
    [SerializeField] private float proximityThreshold = 0.5f;
    [SerializeField] private float proximityCheckInterval = 0.1f;
    [SerializeField] private bool toggleOnEnter = true; // Toggle when entering proximity
    [SerializeField] private bool toggleOnExit = false; // Toggle when exiting proximity
    
    [Header("Audio")]
    [SerializeField] private AudioSource transitionAudioSource;
    [SerializeField] private AudioClip switchToMRSound;
    [SerializeField] private AudioClip switchToVRSound;
    
    [Header("Events")]
    public UnityEvent OnModeChanged;
    public UnityEvent OnSwitchToMR;
    public UnityEvent OnSwitchToVR;
    
    // Private variables
    private bool isInMRMode;
    private bool isTransitioning = false;
    private Material originalSkyboxMaterial;
    private Color originalCameraColor;
    private CameraClearFlags originalClearFlags;
    
    // Proximity variables
    private bool wasWithinProximity = false;
    private float lastProximityCheck = 0f;
    private Coroutine proximityCheckCoroutine;
    
    // Properties
    public bool IsInMRMode => isInMRMode;
    public bool IsTransitioning => isTransitioning;
    
    private void Awake()
    {
        InitializeComponents();
        StoreCameraDefaults();
    }
    
    private void Start()
    {
        // Set initial mode
        if (startInMRMode)
        {
            SetMRMode(false);
        }
        else
        {
            SetVRMode(false);
        }
        
        // Start proximity checking if enabled
        if (useProximityToggle && objectA != null && objectB != null)
        {
            proximityCheckCoroutine = StartCoroutine(ProximityCheckCoroutine());
        }
    }
    
    private void Update()
    {
        HandleInput();
        
        // Handle proximity toggle if not using coroutine method
        if (useProximityToggle && proximityCheckCoroutine == null)
        {
            HandleProximityToggle();
        }
    }
    
    private void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            
        if (passthroughLayer == null)
            passthroughLayer = FindObjectOfType<OVRPassthroughLayer>();
            
        if (vrDirectionalLight == null)
            vrDirectionalLight = FindObjectOfType<Light>();
    }
    
    private void StoreCameraDefaults()
    {
        if (mainCamera != null)
        {
            originalSkyboxMaterial = RenderSettings.skybox;
            originalCameraColor = mainCamera.backgroundColor;
            originalClearFlags = mainCamera.clearFlags;
        }
    }
    
    private void HandleInput()
    {
        if (isTransitioning) return;
        
        // Skip manual input if proximity toggle is active
        if (useProximityToggle) return;
        
        bool switchPressed = false;
        
        // Check VR controller input
        if (OVRInput.GetDown(switchButton))
            switchPressed = true;
            
        // Check keyboard input for editor testing
        if (Input.GetKeyDown(keyboardKey))
            switchPressed = true;
            
        if (switchPressed)
        {
            ToggleMode();
        }
    }
    
    /// <summary>
    /// Toggle between MR and VR modes
    /// </summary>
    public void ToggleMode()
    {
        if (isTransitioning) return;
        
        if (isInMRMode)
        {
            SwitchToVR();
        }
        else
        {
            SwitchToMR();
        }
    }
    
    /// <summary>
    /// Switch to Mixed Reality mode
    /// </summary>
    public void SwitchToMR()
    {
        if (isInMRMode || isTransitioning) return;
        
        StartCoroutine(TransitionToMR());
    }
    
    /// <summary>
    /// Switch to VR mode
    /// </summary>
    public void SwitchToVR()
    {
        if (!isInMRMode || isTransitioning) return;
        
        StartCoroutine(TransitionToVR());
    }
    
    private IEnumerator TransitionToMR()
    {
        isTransitioning = true;
        
        // Play transition sound
        PlayTransitionSound(switchToMRSound);
        
        // Fade transition
        yield return StartCoroutine(FadeTransition(true));
        
        // Apply MR settings
        SetMRMode(true);
        
        // Fade back in
        yield return StartCoroutine(FadeTransition(false));
        
        isTransitioning = false;
        OnSwitchToMR?.Invoke();
        OnModeChanged?.Invoke();
        
        Debug.Log("Switched to Mixed Reality mode");
    }
    
    private IEnumerator TransitionToVR()
    {
        isTransitioning = true;
        
        // Play transition sound
        PlayTransitionSound(switchToVRSound);
        
        // Fade transition
        yield return StartCoroutine(FadeTransition(true));
        
        // Apply VR settings
        SetVRMode(true);
        
        // Fade back in
        yield return StartCoroutine(FadeTransition(false));
        
        isTransitioning = false;
        OnSwitchToVR?.Invoke();
        OnModeChanged?.Invoke();
        
        Debug.Log("Switched to VR mode");
    }
    
    /// <summary>
    /// Coroutine-based proximity checking for better performance
    /// </summary>
    private IEnumerator ProximityCheckCoroutine()
    {
        while (useProximityToggle && objectA != null && objectB != null)
        {
            CheckProximityAndToggle();
            yield return new WaitForSeconds(proximityCheckInterval);
        }
    }
    
    /// <summary>
    /// Handle proximity toggle in Update method (alternative to coroutine)
    /// </summary>
    private void HandleProximityToggle()
    {
        if (objectA == null || objectB == null) return;
        
        // Throttle proximity checks
        if (Time.time - lastProximityCheck < proximityCheckInterval)
            return;
            
        lastProximityCheck = Time.time;
        CheckProximityAndToggle();
    }
    
    /// <summary>
    /// Check proximity between objects and toggle mode if conditions are met
    /// </summary>
    private void CheckProximityAndToggle()
    {
        if (isTransitioning || objectA == null || objectB == null) return;
        
        float distance = Vector3.Distance(objectA.position, objectB.position);
        bool isWithinProximity = distance <= proximityThreshold;
        
        // Check for proximity state change
        if (isWithinProximity != wasWithinProximity)
        {
            if (isWithinProximity && toggleOnEnter)
            {
                Debug.Log($"Objects entered proximity ({distance:F2}m) - Toggling mode");
                ToggleMode();
            }
            else if (!isWithinProximity && toggleOnExit)
            {
                Debug.Log($"Objects exited proximity ({distance:F2}m) - Toggling mode");
                ToggleMode();
            }
            
            wasWithinProximity = isWithinProximity;
        }
    }
    
    /// <summary>
    /// Get current distance between proximity objects
    /// </summary>
    public float GetProximityDistance()
    {
        if (objectA == null || objectB == null) return float.MaxValue;
        return Vector3.Distance(objectA.position, objectB.position);
    }
    
    /// <summary>
    /// Check if objects are currently within proximity threshold
    /// </summary>
    public bool AreObjectsInProximity()
    {
        return GetProximityDistance() <= proximityThreshold;
    }
    
    /// <summary>
    /// Enable or disable proximity toggle system
    /// </summary>
    public void SetProximityToggle(bool enabled)
    {
        useProximityToggle = enabled;
        
        if (enabled && objectA != null && objectB != null && proximityCheckCoroutine == null)
        {
            proximityCheckCoroutine = StartCoroutine(ProximityCheckCoroutine());
        }
        else if (!enabled && proximityCheckCoroutine != null)
        {
            StopCoroutine(proximityCheckCoroutine);
            proximityCheckCoroutine = null;
        }
    }
    
    private void SetMRMode(bool animated)
    {
        isInMRMode = true;
        
        // Enable passthrough
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = true;
            passthroughLayer.textureOpacity = 1.0f;
        }
        
        // Configure camera for MR
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.clear;
        }
        
        // Handle inverted sphere specifically
        if (useInvertedSphere && sphereRenderer != null)
        {
            sphereRenderer.enabled = false;
        }
        else if (vrEnvironmentParent != null)
        {
            vrEnvironmentParent.SetActive(false);
        }
            
        // Disable VR lighting
        if (vrDirectionalLight != null)
            vrDirectionalLight.enabled = false;
            
        // Set transparent skybox for MR
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
    }
    
    private void SetVRMode(bool animated)
    {
        isInMRMode = false;
        
        // Disable passthrough
        if (passthroughLayer != null)
        {
            passthroughLayer.enabled = false;
            passthroughLayer.textureOpacity = 0.0f;
        }
        
        // Configure camera for VR
        if (mainCamera != null)
        {
            // For inverted sphere, use SolidColor, for skybox use Skybox
            if (useInvertedSphere)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
            }
            else
            {
                mainCamera.clearFlags = vrSkyboxMaterial != null ? CameraClearFlags.Skybox : originalClearFlags;
                mainCamera.backgroundColor = originalCameraColor;
            }
        }
        
        // Handle inverted sphere specifically
        if (useInvertedSphere && sphereRenderer != null)
        {
            sphereRenderer.enabled = true;
            // Ensure proper material and culling settings
            ConfigureInvertedSphere();
        }
        else if (vrEnvironmentParent != null)
        {
            vrEnvironmentParent.SetActive(true);
        }
            
        // Enable VR lighting
        if (vrDirectionalLight != null)
            vrDirectionalLight.enabled = true;
            
        // Set VR skybox (only if not using inverted sphere)
        if (!useInvertedSphere)
        {
            RenderSettings.skybox = vrSkyboxMaterial ?? originalSkyboxMaterial;
        }
        else
        {
            RenderSettings.skybox = null;
        }
        
        RenderSettings.fog = true;
    }
    
    private IEnumerator FadeTransition(bool fadeOut)
    {
        if (mainCamera == null) yield break;
        
        float duration = transitionDuration * 0.5f; // Half duration for each fade
        float elapsed = 0f;
        
        Color startColor = fadeOut ? Color.clear : Color.black;
        Color endColor = fadeOut ? Color.black : Color.clear;
        
        // Create fade overlay if it doesn't exist
        GameObject fadeOverlay = CreateFadeOverlay();
        Canvas canvas = fadeOverlay.GetComponent<Canvas>();
        UnityEngine.UI.Image fadeImage = fadeOverlay.GetComponentInChildren<UnityEngine.UI.Image>();
        
        canvas.enabled = true;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        fadeImage.color = endColor;
        
        if (!fadeOut)
        {
            canvas.enabled = false;
        }
    }
    
    private GameObject CreateFadeOverlay()
    {
        GameObject existingOverlay = GameObject.Find("MRVRFadeOverlay");
        if (existingOverlay != null)
            return existingOverlay;
            
        GameObject overlay = new GameObject("MRVRFadeOverlay");
        Canvas canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvas.enabled = false;
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(overlay.transform);
        
        UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.clear;
        
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        DontDestroyOnLoad(overlay);
        return overlay;
    }
    
    private void PlayTransitionSound(AudioClip clip)
    {
        if (transitionAudioSource != null && clip != null)
        {
            transitionAudioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Force set to MR mode without transition
    /// </summary>
    public void ForceSetMRMode()
    {
        if (isTransitioning) return;
        SetMRMode(false);
        OnSwitchToMR?.Invoke();
        OnModeChanged?.Invoke();
    }
    
    /// <summary>
    /// Configure the inverted sphere for proper rendering
    /// </summary>
    private void ConfigureInvertedSphere()
    {
        if (sphereRenderer == null) return;
        
        // Ensure the material has proper settings for inverted sphere
        Material sphereMaterial = sphereRenderer.material;
        if (sphereMaterial != null)
        {
            // Set proper culling mode for inverted normals
            sphereMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            
            // Ensure the material is set to render from inside
            if (sphereMaterial.HasProperty("_MainTex"))
            {
                // Flip texture coordinates if needed
                sphereMaterial.SetTextureScale("_MainTex", new Vector2(-1, 1));
            }
        }
        
        // Ensure the sphere is large enough and properly positioned
        Transform sphereTransform = sphereRenderer.transform;
        if (mainCamera != null)
        {
            // Position sphere at camera location
            sphereTransform.position = mainCamera.transform.position;
            
            // Ensure sphere is large enough
            float sphereScale = Mathf.Max(100f, sphereTransform.localScale.x);
            sphereTransform.localScale = Vector3.one * sphereScale;
        }
    }
    
    /// <summary>
    /// Create or fix an inverted sphere mesh for 360 environments
    /// </summary>
    public void CreateInvertedSphere()
    {
        if (sphereRenderer != null) return;
        
        // Create sphere GameObject
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "InvertedSphere_VREnvironment";
        sphere.transform.localScale = Vector3.one * 100f;
        
        // Position at camera
        if (mainCamera != null)
        {
            sphere.transform.position = mainCamera.transform.position;
        }
        
        // Get components
        sphereRenderer = sphere.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = sphere.GetComponent<MeshFilter>();
        
        // Invert the normals of the mesh
        Mesh mesh = meshFilter.mesh;
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;
        
        // Flip triangles to face inward
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int temp = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = temp;
        }
        mesh.triangles = triangles;
        
        // Create material for 360 content
        Material sphereMaterial = new Material(Shader.Find("Unlit/Texture"));
        sphereMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        sphereRenderer.material = sphereMaterial;
        
        // Remove collider as it's not needed
        Collider sphereCollider = sphere.GetComponent<Collider>();
        if (sphereCollider != null)
        {
            DestroyImmediate(sphereCollider);
        }
        
        Debug.Log("Created inverted sphere for VR environment");
    }
    
    /// <summary>
    /// Force set to VR mode without transition
    /// </summary>
    public void ForceSetVRMode()
    {
        if (isTransitioning) return;
        SetVRMode(false);
        OnSwitchToVR?.Invoke();
        OnModeChanged?.Invoke();
    }
    
    private void OnValidate()
    {
        // Ensure transition duration is reasonable
        transitionDuration = Mathf.Clamp(transitionDuration, 0.1f, 5.0f);
        
        // Ensure proximity threshold is positive
        proximityThreshold = Mathf.Max(0.01f, proximityThreshold);
        
        // Ensure proximity check interval is reasonable
        proximityCheckInterval = Mathf.Clamp(proximityCheckInterval, 0.01f, 1.0f);
    }
    
    private void OnDestroy()
    {
        // Stop proximity checking
        if (proximityCheckCoroutine != null)
        {
            StopCoroutine(proximityCheckCoroutine);
        }
        
        // Clean up fade overlay if it exists
        GameObject fadeOverlay = GameObject.Find("MRVRFadeOverlay");
        if (fadeOverlay != null)
        {
            DestroyImmediate(fadeOverlay);
        }
    }
}