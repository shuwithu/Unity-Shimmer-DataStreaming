using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class GrabInteractionEvents : MonoBehaviour
{
    [Header("Grab References")]
    [SerializeField] private InteractableUnityEventWrapper interactable;

    [Header("On Grab Start")]
    public GameObject[] objectsToEnableOnGrab;
    public GameObject[] objectsToDisableOnGrab;

    [Header("While Holding")]
    public GameObject[] objectsToStayEnabled;
    public GameObject[] objectsToStayDisabled;

    [Header("After Duration While Holding")]
    public float delayDuration = 2f;
    public GameObject[] objectsToEnableAfterDelay;
    public GameObject[] objectsToDisableAfterDelay;

    [Header("Animation")]
    public Animator[] animatorsToTriggerWhileHeld;
    public string whileHeldTriggerName = "WhileHeld";
    public Animator[] animatorsToTriggerOnRelease;
    public string onReleaseTriggerName = "OnRelease";

    private bool isGrabbed = false;
    private Coroutine holdingCoroutine;

    private void OnEnable()
    {
        interactable.WhenSelect.AddListener(OnGrabStart);
        interactable.WhenUnselect.AddListener(OnGrabEnd);
    }

    private void OnDisable()
    {
        interactable.WhenSelect.RemoveListener(OnGrabStart);
        interactable.WhenUnselect.RemoveListener(OnGrabEnd);
    }

    private void OnGrabStart()
    {
        isGrabbed = true;

        SetActiveForObjects(objectsToEnableOnGrab, true);
        SetActiveForObjects(objectsToDisableOnGrab, false);

        holdingCoroutine = StartCoroutine(HoldingRoutine());

        foreach (var anim in animatorsToTriggerWhileHeld)
        {
            if (anim != null)
                anim.SetTrigger(whileHeldTriggerName);
        }
    }

    private void OnGrabEnd()
    {
        isGrabbed = false;

        if (holdingCoroutine != null)
            StopCoroutine(holdingCoroutine);

        foreach (var anim in animatorsToTriggerOnRelease)
        {
            if (anim != null)
                anim.SetTrigger(onReleaseTriggerName);
        }
    }

    private IEnumerator HoldingRoutine()
    {
        SetActiveForObjects(objectsToStayEnabled, true);
        SetActiveForObjects(objectsToStayDisabled, false);

        yield return new WaitForSeconds(delayDuration);

        if (isGrabbed)
        {
            SetActiveForObjects(objectsToEnableAfterDelay, true);
            SetActiveForObjects(objectsToDisableAfterDelay, false);
        }
    }

    private void SetActiveForObjects(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        foreach (var obj in objs)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
