using UnityEngine;

public class StartSequence : MonoBehaviour
{
    [Tooltip("The name of the trigger parameter in the Animator")]
    public string triggerParameterName = "PlayAnimation";
    
    [Tooltip("Optional: Only trigger animation when colliding with objects with this tag")]
    public string requiredTag = "";

    // Trigger animation on the other object when entering a trigger collision
    private void OnTriggerEnter(Collider other)
    {
        TryTriggerAnimation(other.gameObject);
    }

    // Trigger animation on the other object when entering a regular collision
    private void OnCollisionEnter(Collision collision)
    {
        TryTriggerAnimation(collision.gameObject);
    }

    // Helper function to check conditions and trigger the animation
    private void TryTriggerAnimation(GameObject otherObject)
    {
        // Check tag requirement (if any)
        if (!string.IsNullOrEmpty(requiredTag) && !otherObject.CompareTag(requiredTag))
        {
            return; // Skip if tag doesn't match
        }

        // Try to get the Animator from the collided object
        Animator otherAnimator = otherObject.GetComponent<Animator>();
        
        if (otherAnimator != null)
        {
            otherAnimator.SetTrigger(triggerParameterName);
        }
        else
        {
            Debug.LogWarning("No Animator found on the collided object: " + otherObject.name, this);
        }
    }
}