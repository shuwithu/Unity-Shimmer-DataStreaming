using UnityEngine;

public class DestroyAndActivate : MonoBehaviour
{
    public GameObject objectToActivate;
    public bool shouldDestroy = false;

    void Update()
    {
        if (shouldDestroy)
        {
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }

            Destroy(gameObject);
        }
    }

    // Optional: Can be triggered by Animation Event
    public void SetShouldDestroyTrue()
    {
        shouldDestroy = true;
    }

    public void SetShouldDestroyFalse()
    {
        shouldDestroy = false;
    }
}
