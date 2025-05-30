using UnityEngine;
using System.Collections.Generic;

public class ParticlePulse : MonoBehaviour
{
    [Header("ParticlePulse Settings")]
    [SerializeField] private int particleCount = 200;
    [SerializeField] private float pulseHeight = 10f;
    [SerializeField] private float baseRadius = 1f;
    [SerializeField] private float topRadius = 3f;
    [SerializeField] private float swirlingSpeed = 2f;
    [SerializeField] private float verticalSpeed = 1f;
    
    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float particleSize = 0.1f;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private Material particleMaterial;
    
    [Header("Animation")]
    [SerializeField] private float noiseStrength = 0.5f;
    [SerializeField] private float noiseScale = 0.1f;
    
    private List<PulseParticle> particles = new List<PulseParticle>();
    private Transform playerTransform;
    
    [System.Serializable]
    private class PulseParticle
    {
        public GameObject gameObject;
        public float angle;
        public float height;
        public float speed;
        public Vector3 noiseOffset;
        public float verticalOffset;
    }
    
    void Start()
    {
        // Try to find the player - you can modify this to match your player tag/name
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            player = GameObject.Find("Player");
        
        if (player != null)
            playerTransform = player.transform;
        else
            playerTransform = transform; // Use this object as center if no player found
        
        CreateParticles();
    }
    
    void CreateParticles()
    {
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particleObj = CreateParticleObject();
            
            PulseParticle particle = new PulseParticle
            {
                gameObject = particleObj,
                angle = Random.Range(0f, 360f),
                height = Random.Range(0f, pulseHeight),
                speed = Random.Range(swirlingSpeed * 0.8f, swirlingSpeed * 1.2f),
                noiseOffset = new Vector3(
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f)
                ),
                verticalOffset = Random.Range(0f, Mathf.PI * 2f)
            };
            
            particles.Add(particle);
        }
    }
    
    GameObject CreateParticleObject()
    {
        GameObject particle;
        
        if (particlePrefab != null)
        {
            particle = Instantiate(particlePrefab);
        }
        else
        {
            // Create a simple sphere particle if no prefab is provided
            particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.localScale = Vector3.one * particleSize;
            
            // Remove collider to improve performance
            Collider col = particle.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);
            
            // Apply material and color
            Renderer renderer = particle.GetComponent<Renderer>();
            if (particleMaterial != null)
                renderer.material = particleMaterial;
            else
                renderer.material.color = particleColor;
        }
        
        particle.transform.parent = transform;
        return particle;
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        
        foreach (var particle in particles)
        {
            UpdateParticlePosition(particle, playerPos);
        }
    }
    
    void UpdateParticlePosition(PulseParticle particle, Vector3 centerPos)
    {
        // Update angle for swirling motion
        particle.angle += particle.speed * Time.deltaTime;
        if (particle.angle > 360f)
            particle.angle -= 360f;
        
        // Update height with vertical movement
        particle.height += verticalSpeed * Time.deltaTime;
        if (particle.height > pulseHeight)
        {
            particle.height = 0f;
            particle.angle = Random.Range(0f, 360f); // Randomize angle when particle resets
        }
        
        // Calculate radius based on height (wider at top)
        float heightRatio = particle.height / pulseHeight;
        float currentRadius = Mathf.Lerp(baseRadius, topRadius, heightRatio);
        
        // Add some noise for more organic movement
        Vector3 noiseInput = particle.noiseOffset + Time.time * noiseScale * Vector3.one;
        float noiseX = Mathf.PerlinNoise(noiseInput.x, noiseInput.y) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(noiseInput.z, noiseInput.x) - 0.5f;
        Vector3 noiseVector = new Vector3(noiseX, 0, noiseZ) * noiseStrength;
        
        // Calculate spiral position
        float angleRad = particle.angle * Mathf.Deg2Rad;
        Vector3 spiralPos = new Vector3(
            Mathf.Cos(angleRad) * currentRadius,
            particle.height,
            Mathf.Sin(angleRad) * currentRadius
        );
        
        // Add vertical bobbing motion
        float verticalBob = Mathf.Sin(Time.time * 2f + particle.verticalOffset) * 0.2f;
        spiralPos.y += verticalBob;
        
        // Final position with noise and center offset
        particle.gameObject.transform.position = centerPos + spiralPos + noiseVector;
        
        // Optional: Rotate particles to face movement direction
        Vector3 movementDir = (spiralPos - particle.gameObject.transform.position).normalized;
        if (movementDir != Vector3.zero)
        {
            particle.gameObject.transform.rotation = Quaternion.LookRotation(movementDir);
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize the pulse shape in the scene view
        if (playerTransform != null)
        {
            Vector3 center = playerTransform.position;
            
            // Draw base circle
            Gizmos.color = Color.yellow;
            DrawCircle(center, baseRadius);
            
            // Draw top circle
            Gizmos.color = Color.red;
            DrawCircle(center + Vector3.up * pulseHeight, topRadius);
            
            // Draw connecting lines
            Gizmos.color = Color.white;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 basePoint = center + new Vector3(Mathf.Cos(angle) * baseRadius, 0, Mathf.Sin(angle) * baseRadius);
                Vector3 topPoint = center + new Vector3(Mathf.Cos(angle) * topRadius, pulseHeight, Mathf.Sin(angle) * topRadius);
                Gizmos.DrawLine(basePoint, topPoint);
            }
        }
    }
    
    void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            
            Gizmos.DrawLine(point1, point2);
        }
    }
    
    void OnDestroy()
    {
        // Clean up particles when the script is destroyed
        foreach (var particle in particles)
        {
            if (particle.gameObject != null)
                DestroyImmediate(particle.gameObject);
        }
        particles.Clear();
    }
}