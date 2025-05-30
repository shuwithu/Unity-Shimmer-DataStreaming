using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndingParticles : MonoBehaviour
{
    [Header("Tornado Settings")]
    [SerializeField] private int maxParticleCount = 100;
    [SerializeField] private float tornadoRadius = 3f;
    [SerializeField] private float tornadoHeight = 5f;
    
    [Header("Formation Settings")]
    [SerializeField] private float particleSpawnRate = 0.1f; // Time between each particle spawn
    [SerializeField] private float initialSwirlingSpeed = 30f; // Degrees per second
    [SerializeField] private float maxSwirlingSpeed = 180f; // Maximum speed when all particles are active
    [SerializeField] private float speedIncreaseRate = 2f; // How much speed increases per particle
    
    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float particleSize = 0.1f;
    [SerializeField] private Color particleColor = Color.white;
    [SerializeField] private Material particleMaterial;
    
    [Header("Animation")]
    [SerializeField] private float verticalBobStrength = 0.3f;
    [SerializeField] private float formationHeight = 0.5f; // Height above ground where particles form
    
    private List<TornadoParticle> activeParticles = new List<TornadoParticle>();
    private Transform playerTransform;
    private bool isForming = false;
    private float currentSwirlingSpeed;
    
    [System.Serializable]
    private class TornadoParticle
    {
        public GameObject gameObject;
        public float angle;
        public float targetRadius;
        public float currentRadius;
        public float height;
        public float verticalOffset;
        public float formationTime;
        public bool fullyFormed;
    }
    
    void Start()
    {
        // Find the player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            player = GameObject.Find("Player");
        
        if (player != null)
            playerTransform = player.transform;
        else
            playerTransform = transform;
        
        currentSwirlingSpeed = initialSwirlingSpeed;
    }
    
    public void StartTornadoFormation()
    {
        if (!isForming)
        {
            isForming = true;
            StartCoroutine(FormTornado());
        }
    }
    
    public void StopTornado()
    {
        isForming = false;
        StopAllCoroutines();
        StartCoroutine(DissolveTornado());
    }
    
    IEnumerator FormTornado()
    {
        for (int i = 0; i < maxParticleCount && isForming; i++)
        {
            CreateNewParticle(i);
            
            // Increase swirling speed as more particles are added
            currentSwirlingSpeed = Mathf.Min(
                initialSwirlingSpeed + (i * speedIncreaseRate), 
                maxSwirlingSpeed
            );
            
            yield return new WaitForSeconds(particleSpawnRate);
        }
    }
    
    void CreateNewParticle(int index)
    {
        GameObject particleObj = CreateParticleObject();
        
        // Random starting angle to distribute particles evenly
        float startAngle = (index * (360f / maxParticleCount)) + Random.Range(-30f, 30f);
        
        TornadoParticle particle = new TornadoParticle
        {
            gameObject = particleObj,
            angle = startAngle,
            targetRadius = tornadoRadius,
            currentRadius = 0f, // Start at center
            height = Random.Range(formationHeight, tornadoHeight),
            verticalOffset = Random.Range(0f, Mathf.PI * 2f),
            formationTime = 0f,
            fullyFormed = false
        };
        
        activeParticles.Add(particle);
        
        // Start particle at player position
        if (playerTransform != null)
        {
            particleObj.transform.position = playerTransform.position + Vector3.up * particle.height;
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
            particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.localScale = Vector3.one * particleSize;
            
            // Remove collider
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
        if (playerTransform == null || activeParticles.Count == 0) return;
        
        Vector3 playerPos = playerTransform.position;
        
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            UpdateParticlePosition(activeParticles[i], playerPos);
        }
    }
    
    void UpdateParticlePosition(TornadoParticle particle, Vector3 centerPos)
    {
        // Update formation progress
        if (!particle.fullyFormed)
        {
            particle.formationTime += Time.deltaTime;
            float formationProgress = Mathf.Clamp01(particle.formationTime / 2f); // 2 seconds to fully form
            particle.currentRadius = Mathf.Lerp(0f, particle.targetRadius, formationProgress);
            
            if (formationProgress >= 1f)
                particle.fullyFormed = true;
        }
        
        // Update swirling angle
        particle.angle += currentSwirlingSpeed * Time.deltaTime;
        if (particle.angle > 360f)
            particle.angle -= 360f;
        
        // Calculate position
        float angleRad = particle.angle * Mathf.Deg2Rad;
        
        // Add vertical bobbing
        float verticalBob = Mathf.Sin(Time.time * 3f + particle.verticalOffset) * verticalBobStrength;
        
        Vector3 spiralPos = new Vector3(
            Mathf.Cos(angleRad) * particle.currentRadius,
            particle.height + verticalBob,
            Mathf.Sin(angleRad) * particle.currentRadius
        );
        
        particle.gameObject.transform.position = centerPos + spiralPos;
        
        // Make particles face their movement direction for better visual effect
        if (particle.currentRadius > 0.1f)
        {
            Vector3 tangentDirection = new Vector3(-Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
            particle.gameObject.transform.rotation = Quaternion.LookRotation(tangentDirection);
        }
    }
    
    IEnumerator DissolveTornado()
    {
        // Reverse the formation process
        while (activeParticles.Count > 0)
        {
            if (activeParticles.Count > 0)
            {
                int lastIndex = activeParticles.Count - 1;
                GameObject particleToRemove = activeParticles[lastIndex].gameObject;
                activeParticles.RemoveAt(lastIndex);
                
                if (particleToRemove != null)
                    DestroyImmediate(particleToRemove);
                
                // Decrease speed as particles are removed
                currentSwirlingSpeed = Mathf.Max(
                    initialSwirlingSpeed,
                    currentSwirlingSpeed - speedIncreaseRate
                );
            }
            
            yield return new WaitForSeconds(particleSpawnRate * 0.5f); // Dissolve faster than formation
        }
        
        currentSwirlingSpeed = initialSwirlingSpeed;
    }
    
    // Public methods to control the effect
    public void TriggerTornado()
    {
        if (activeParticles.Count == 0)
            StartTornadoFormation();
        else
            StopTornado();
    }
    
    public bool IsActive()
    {
        return activeParticles.Count > 0;
    }
    
    public int GetActiveParticleCount()
    {
        return activeParticles.Count;
    }
    
    public float GetCurrentSpeed()
    {
        return currentSwirlingSpeed;
    }
    
    void OnDrawGizmos()
    {
        if (playerTransform != null)
        {
            Vector3 center = playerTransform.position;
            
            // Draw the tornado radius at formation height
            Gizmos.color = Color.yellow;
            DrawCircle(center + Vector3.up * formationHeight, tornadoRadius);
            
            // Draw the top circle
            Gizmos.color = Color.red;
            DrawCircle(center + Vector3.up * tornadoHeight, tornadoRadius);
            
            // Draw height indicator
            Gizmos.color = Color.white;
            Gizmos.DrawLine(center + Vector3.up * formationHeight, center + Vector3.up * tornadoHeight);
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
        foreach (var particle in activeParticles)
        {
            if (particle.gameObject != null)
                DestroyImmediate(particle.gameObject);
        }
        activeParticles.Clear();
    }
}