using UnityEngine;

public class HoverTowardsTarget : MonoBehaviour
{
    public GameObject targetObject; // Assign Object A in the inspector
    public float moveSpeed = 2f; // Speed when moving towards target
    public float hoverDistance = 0.5f; // How far to hover up and down
    public float hoverSpeed = 1f; // Speed of hover pulsing
    public float arrivalDistance = 0.1f; // How close we need to be to consider "arrived"

    private Vector3 originalPosition; // Original position before hovering starts
    private bool isHovering = false;
    private float hoverTimer = 0f;

    void Update()
    {
        if (targetObject != null && targetObject.activeInHierarchy)
        {
            if (!isHovering)
            {
                // Move towards target
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    targetObject.transform.position, 
                    moveSpeed * Time.deltaTime
                );

                // Check if we've arrived
                float distance = Vector3.Distance(transform.position, targetObject.transform.position);
                if (distance <= arrivalDistance)
                {
                    isHovering = true;
                    originalPosition = transform.position;
                }
            }
            else
            {
                // Hover pulsing effect
                hoverTimer += Time.deltaTime * hoverSpeed;
                
                // Calculate vertical offset using sine wave for smooth up/down motion
                float verticalOffset = Mathf.Sin(hoverTimer) * hoverDistance;
                
                // Apply the hover effect while maintaining original x/z position
                transform.position = originalPosition + new Vector3(0, verticalOffset, 0);
            }
        }
        else
        {
            // Target is not active - reset hovering state
            isHovering = false;
            hoverTimer = 0f;
        }
    }
}