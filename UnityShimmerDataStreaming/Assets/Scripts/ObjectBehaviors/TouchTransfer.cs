using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TouchTransfer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float ascendSpeed = 1.5f;
    public float centerThreshold = 0.5f;
    public float fadeSpeed = 1.0f;
    public float destroyDelay = 3.0f;

    [Header("References")]
    public Renderer objectRenderer;
    public Animator targetAnimator;
    public string animationTriggerName = "Play";
    public AudioSource contactAudio;

    [Header("Contact Prefab")]
    public GameObject contactPrefab;
    public Vector3 prefabOffset = Vector3.up * 0.2f;
    public float prefabSpawnDelay = 2.0f;

    private bool isMoving = false;
    private bool isAscending = false;
    private Vector3 targetCenter;
    private float ascendStartTime;
    private Material[] materials;
    private float originalAlpha = 1.0f;

    private void Start()
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }

        materials = objectRenderer.materials;
        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_Color"))
            {
                originalAlpha = mat.color.a;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isMoving || !other.CompareTag("tree")) return;
        
        // Get center of collided object
        Vector3 center = Vector3.zero;
        MeshFilter meshFilter = other.GetComponent<MeshFilter>();
        MeshCollider meshCollider = other.GetComponent<MeshCollider>();
        if (meshFilter != null)
            center = other.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
        else if (meshCollider != null)
            center = other.transform.TransformPoint(meshCollider.sharedMesh.bounds.center);
        else
            return;

        // Detach from parent
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;
        transform.parent = null;
        transform.SetPositionAndRotation(worldPos, worldRot);

        // Destroy first child if exists
        if (transform.childCount > 0)
        {
            Destroy(transform.GetChild(0).gameObject);
        }

        // Trigger animation
        if (targetAnimator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            targetAnimator.SetTrigger(animationTriggerName);
        }

        // Play audio
        if (contactAudio != null)
        {
            contactAudio.Play();
        }
        
        // activate a new leafcluster
        GameObject leavesParent = GameObject.Find("Leaves");

        if (leavesParent != null)
        {
            foreach (Transform child in leavesParent.transform)
            {
                if (child.CompareTag("LeaveCluster") && !child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                    break; // Activate only one per trigger
                }
            }
        }

        // Delay prefab creation
        if (contactPrefab != null)
        {
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            StartCoroutine(SpawnContactPrefabAfterDelay(contactPoint));
        }

        targetCenter = center;
        isMoving = true;
    }

    private IEnumerator SpawnContactPrefabAfterDelay(Vector3 contactPoint)
    {
        yield return new WaitForSeconds(prefabSpawnDelay);
        GameObject instance = Instantiate(contactPrefab, contactPoint + prefabOffset, Quaternion.identity);
        Destroy(instance, 15f);
    }

    private void Update()
    {
        if (!isMoving) return;

        Vector3 posXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetXZ = new Vector3(targetCenter.x, 0, targetCenter.z);
        float distance = Vector3.Distance(posXZ, targetXZ);

        if (distance <= centerThreshold && !isAscending)
        {
            isAscending = true;
            ascendStartTime = Time.time;
        }

        Vector3 newPos = transform.position;

        if (!isAscending && distance > 0.01f)
        {
            Vector3 dir = (targetXZ - posXZ).normalized;
            newPos.x += dir.x * moveSpeed * Time.deltaTime;
            newPos.z += dir.z * moveSpeed * Time.deltaTime;
        }

        if (isAscending)
        {
            newPos.y += ascendSpeed * Time.deltaTime;

            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = Mathf.Max(0, color.a - fadeSpeed * Time.deltaTime);
                    mat.color = color;

                    if (color.a < 1.0f && mat.renderQueue < 3000)
                    {
                        mat.SetFloat("_Mode", 2);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                    }
                }
            }

            if (Time.time >= ascendStartTime + destroyDelay)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = newPos;
    }
}
