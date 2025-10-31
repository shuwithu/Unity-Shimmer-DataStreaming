using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequentialActivator : MonoBehaviour
{
    public GameObject[] objectsToActivate;
    private int currentIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (currentIndex < objectsToActivate.Length)
        {
            objectsToActivate[currentIndex].SetActive(true);
            currentIndex++;
        }
    }
}
