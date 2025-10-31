using UnityEngine;

public class voxCtrl : MonoBehaviour
{
    [Header("Voxel Settings")]
    public float voxelSize = 0.1f;
    public Color voxelColor = Color.gray;
    public float voxelBlend = 0.5f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private Collider volumeCollider;
    private Material originalMaterial;
    private Material voxelMaterial;

    void Start()
    {
        // Get the collider (preferably a MeshCollider for precise boundaries)
        volumeCollider = GetComponent<Collider>();
        if (volumeCollider == null)
        {
            Debug.LogError("No collider found. Please add a collider to the GameObject.");
            return;
        }

        // Create voxel material
        voxelMaterial = new Material(Shader.Find("Custom/VolumeVoxelizationShader"));
    }

    void OnTriggerEnter(Collider other)
    {
        Renderer otherRenderer = other.GetComponent<Renderer>();
        if (otherRenderer != null)
        {
            // Store original material
            originalMaterial = otherRenderer.material;

            // Create voxel material instance
            Material voxelizedMaterial = new Material(voxelMaterial);

            // Set volume parameters
            Bounds volumeBounds = volumeCollider.bounds;
            voxelizedMaterial.SetVector("_VolumeCenter", volumeBounds.center);
            voxelizedMaterial.SetVector("_VolumeExtents", volumeBounds.extents);
            voxelizedMaterial.SetFloat("_VoxelSize", voxelSize);
            voxelizedMaterial.SetColor("_VoxelColor", voxelColor);
            voxelizedMaterial.SetFloat("_VoxelBlend", voxelBlend);

            // Apply voxel material
            otherRenderer.material = voxelizedMaterial;
        }
    }

    void OnTriggerExit(Collider other)
    {
        Renderer otherRenderer = other.GetComponent<Renderer>();
        if (otherRenderer != null && originalMaterial != null)
        {
            // Restore original material
            otherRenderer.material = originalMaterial;
        }
    }

    // Visualize volume in scene view
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider boxCollider)
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (col is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
            }
            else if (col is MeshCollider meshCollider)
            {
                Gizmos.DrawWireMesh(meshCollider.sharedMesh);
            }
        }
    }
}