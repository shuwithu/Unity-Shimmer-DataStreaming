using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class forceMovement : MonoBehaviour
{
    [Header("Force Settings")]
    [Tooltip("The amount of force to apply constantly")]
    public float forceAmount = 10f;

    [Tooltip("The direction of the force (normalized automatically)")]
    public Vector3 forceDirection = Vector3.forward;

    [Header("Options")]
    [Tooltip("Should the force be applied in world space or local space?")]
    public bool useLocalSpace = false;

    [Tooltip("Should we ignore the object's mass?")]
    public bool ignoreMass = false;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Normalize the direction vector to ensure consistent force magnitude
        forceDirection = forceDirection.normalized;
    }

    void FixedUpdate()
    {
        // Calculate the final force vector
        Vector3 finalForce = forceDirection * forceAmount;

        // Transform to local space if needed
        if (useLocalSpace)
        {
            finalForce = transform.TransformDirection(finalForce);
        }

        // Apply force either as acceleration or regular force
        if (ignoreMass)
        {
            rb.AddForce(finalForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(finalForce, ForceMode.Force);
        }
    }
}