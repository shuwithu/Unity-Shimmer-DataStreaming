using UnityEngine;
using System.Collections;

public class TreeTouchedIX : MonoBehaviour
{
    [Header("Settings")]
    public GameObject effectPrefab;
    public float xzMoveTime = 1f;
    public float yMoveTime = 1f;
    public float fadeTime = 1f;
    public float spawnOffset = 0.1f;
    public float ySpeed = 2f;

    private void OnTriggerEnter(Collider other) // Changed back to trigger
    {
        Vector3 spawnPos = other.ClosestPoint(transform.position);
        Vector3 dir = (spawnPos - transform.position).normalized;
        spawnPos += dir * spawnOffset;

        StartCoroutine(MovementRoutine(
            Instantiate(effectPrefab, spawnPos, Quaternion.identity),
            spawnPos,
            other.bounds.center
        ));
    }

    IEnumerator MovementRoutine(GameObject effect, Vector3 startPos, Vector3 center)
    {
        Renderer rend = effect.GetComponent<Renderer>();
        Material mat = rend.material;
        Color initialColor = mat.color;

        // Phase 1: Move to center (XZ only)
        Vector3 xzTarget = new Vector3(center.x, startPos.y, center.z);
        float xzTimer = 0;

        while (xzTimer < xzMoveTime)
        {
            xzTimer += Time.deltaTime;
            effect.transform.position = Vector3.Lerp(startPos, xzTarget, xzTimer / xzMoveTime);
            yield return null;
        }

        // Phase 2: Move up and fade
        float yTimer = 0;
        while (yTimer < yMoveTime)
        {
            yTimer += Time.deltaTime;

            // Move up
            effect.transform.position += Vector3.up * ySpeed * Time.deltaTime;

            // Fade
            if (yTimer > yMoveTime - fadeTime)
            {
                float fadeProgress = (yTimer - (yMoveTime - fadeTime)) / fadeTime;
                mat.color = new Color(
                    initialColor.r,
                    initialColor.g,
                    initialColor.b,
                    1 - fadeProgress
                );
            }

            yield return null;
        }

        Destroy(effect);
    }
}