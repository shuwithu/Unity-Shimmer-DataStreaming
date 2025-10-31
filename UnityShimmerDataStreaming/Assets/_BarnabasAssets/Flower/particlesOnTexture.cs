using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class particlesOnTexture : MonoBehaviour
{
    public MeshRenderer targetMeshRenderer; // The mesh with the texture mask
    public string texturePropertyName = "_MainTex"; // Name of the texture property in shader
    public float emissionRate = 100f;
    public float threshold = 0.9f; // How white must the pixel be to emit (0-1)

    private ParticleSystem particleSystem;
    private Texture2D maskTexture;
    private Texture2D readableTexture;
    private Color[] pixelData;
    private int textureWidth;
    private int textureHeight;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();

        if (targetMeshRenderer == null)
        {
            Debug.LogError("Target Mesh Renderer not assigned!");
            return;
        }

        // Get the texture from the mesh's material
        maskTexture = targetMeshRenderer.material.GetTexture(texturePropertyName) as Texture2D;

        if (maskTexture == null)
        {
            Debug.LogError("No texture found on the target mesh!");
            return;
        }

        // Create a readable copy of the texture
        CreateReadableTexture();
    }

    void CreateReadableTexture()
    {
        // Create a temporary RenderTexture
        RenderTexture tmp = RenderTexture.GetTemporary(
            maskTexture.width,
            maskTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        // Blit the pixels to the RenderTexture
        Graphics.Blit(maskTexture, tmp);

        // Backup the currently active RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;

        // Create a new readable Texture2D
        readableTexture = new Texture2D(maskTexture.width, maskTexture.height);

        // Read the pixels from the RenderTexture
        readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readableTexture.Apply();

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        // Get pixel data
        pixelData = readableTexture.GetPixels();
        textureWidth = readableTexture.width;
        textureHeight = readableTexture.height;
    }

    void Update()
    {
        if (targetMeshRenderer == null || readableTexture == null) return;

        // Emit particles based on emission rate
        int particlesToEmit = (int)(emissionRate * Time.deltaTime);

        for (int i = 0; i < particlesToEmit; i++)
        {
            // Get random UV coordinates
            Vector2 uv = new Vector2(Random.value, Random.value);

            // Convert UV to pixel coordinates
            int x = Mathf.FloorToInt(uv.x * textureWidth);
            int y = Mathf.FloorToInt(uv.y * textureHeight);

            // Clamp coordinates to texture dimensions
            x = Mathf.Clamp(x, 0, textureWidth - 1);
            y = Mathf.Clamp(y, 0, textureHeight - 1);

            // Get pixel index
            int pixelIndex = y * textureWidth + x;

            if (pixelIndex >= 0 && pixelIndex < pixelData.Length)
            {
                // Check if pixel is white enough
                Color pixel = pixelData[pixelIndex];
                float grayscale = pixel.grayscale;

                if (grayscale >= threshold)
                {
                    // Emit particle at this position on the mesh
                    EmitParticleAtUV(uv);
                }
            }
        }
    }

    void EmitParticleAtUV(Vector2 uv)
    {
        // Get the mesh filter
        MeshFilter meshFilter = targetMeshRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.mesh == null) return;

        // Get the mesh data
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        // Find the triangle that contains this UV coordinate
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector2 uv1 = uvs[triangles[i]];
            Vector2 uv2 = uvs[triangles[i + 1]];
            Vector2 uv3 = uvs[triangles[i + 2]];

            if (PointInTriangle(uv, uv1, uv2, uv3))
            {
                // Calculate barycentric coordinates
                Vector3 barycentric = Barycentric(uv, uv1, uv2, uv3);

                // Get the corresponding vertices
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                // Calculate world position
                Vector3 worldPos = targetMeshRenderer.transform.TransformPoint(
                    v1 * barycentric.x + v2 * barycentric.y + v3 * barycentric.z);

                // Emit particle
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = worldPos;
                particleSystem.Emit(emitParams, 1);

                break;
            }
        }
    }

    // Check if a point is inside a triangle
    bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    // Calculate barycentric coordinates
    Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);

        float denom = d00 * d11 - d01 * d01;
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }
}