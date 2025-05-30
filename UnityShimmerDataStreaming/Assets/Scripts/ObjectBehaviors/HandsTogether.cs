using UnityEngine;
using System.Collections.Generic;

public class HandsTogether : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject objectA;
    [SerializeField] private GameObject objectB;
    [SerializeField] private GameObject objectToActivate;

    [Header("Proximity Settings")]
    [SerializeField] private float activationDistance = 0.2f;

    [Header("Audio Settings")]
    [SerializeField] private float lerpSpeed = 2f;

    private AudioSource audioA;
    private AudioSource audioB;

    private class AudioInfo
    {
        public AudioSource source;
        public float originalVolume;
    }

    private List<AudioInfo> otherAudioSources = new List<AudioInfo>();
    private bool isInProximity = false;

    void Start()
    {
        if (objectA != null)
            audioA = objectA.GetComponent<AudioSource>();

        if (objectB != null)
            audioB = objectB.GetComponent<AudioSource>();

        // Get all AudioSources in the scene
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource src in allSources)
        {
            // Skip nulls and make sure we only exclude audioA and audioB
            if (src != null && src != audioA && src != audioB)
            {
                otherAudioSources.Add(new AudioInfo
                {
                    source = src,
                    originalVolume = src.volume
                });
            }
        }
    }

    void Update()
    {
        if (objectA == null || objectB == null || objectToActivate == null)
            return;

        float distance = Vector3.Distance(objectA.transform.position, objectB.transform.position);
        bool nowInProximity = distance < activationDistance;

        objectToActivate.SetActive(nowInProximity);

        // Start/stop audio if proximity state changed
        if (nowInProximity && !isInProximity)
        {
            isInProximity = true;
            PlayIfNotPlaying(audioA);
            PlayIfNotPlaying(audioB);
        }
        else if (!nowInProximity && isInProximity)
        {
            isInProximity = false;
            StopIfPlaying(audioA);
            StopIfPlaying(audioB);
        }

        // Always update volumes
        UpdateAudioVolumes(nowInProximity);
    }

    private void UpdateAudioVolumes(bool inProximity)
    {
        float targetVolumeAB = inProximity ? 1f : 0f;

        // Lerp volumes for A and B
        if (audioA != null)
            audioA.volume = Mathf.Lerp(audioA.volume, targetVolumeAB, Time.deltaTime * lerpSpeed);

        if (audioB != null)
            audioB.volume = Mathf.Lerp(audioB.volume, targetVolumeAB, Time.deltaTime * lerpSpeed);

        // Lerp all other audio back to original volumes
        foreach (AudioInfo info in otherAudioSources)
        {
            if (info.source != null)
            {
                float target = inProximity ? 0f : info.originalVolume;
                info.source.volume = Mathf.Lerp(info.source.volume, target, Time.deltaTime * lerpSpeed);
            }
        }
    }

    private void PlayIfNotPlaying(AudioSource source)
    {
        if (source != null && !source.isPlaying)
            source.Play();
    }

    private void StopIfPlaying(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }
}
