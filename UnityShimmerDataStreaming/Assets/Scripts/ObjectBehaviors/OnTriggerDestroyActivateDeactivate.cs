using UnityEngine;

public class OnTriggerDestroyActivateDeactivate : MonoBehaviour
{
    [Tooltip("Objects to activate when this object is destroyed")]
    public GameObject[] objectsToActivate;

    [Tooltip("Objects to deactivate when this object is destroyed")]
    public GameObject[] objectsToDeactivate;

    void OnTriggerEnter(Collider other)
    {
        // Only trigger if the colliding object has the "player" tag
        if (!other.CompareTag("Player"))
            return;

        // Activate all target objects
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Deactivate all target objects
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Destroy this GameObject
        Destroy(gameObject);
    }
}
