using UnityEngine;
using Oculus.Interaction.HandGrab;  // Use HandGrab namespace

public class SimpleGrabTrigger : MonoBehaviour
{
    public HandGrabInteractable handGrabInteractable;  // Changed to HandGrabInteractable
    public GameObject[] objectsToActivate;
    public GameObject[] objectsToDeactivate;

    private bool wasGrabbed = false;

    private void Awake()
    {
        if (handGrabInteractable == null)
        {
            handGrabInteractable = GetComponent<HandGrabInteractable>();
        }
    }

    private void Update()
    {
        // Check if this interactable currently has any selecting interactors grabbing it
        bool isGrabbed = handGrabInteractable.SelectingInteractors.Count > 0;

        if (!wasGrabbed && isGrabbed)
        {
            // Grab just started
            TriggerObjects();
        }

        wasGrabbed = isGrabbed;
    }

    private void TriggerObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
