using UnityEngine;
using System.Collections;

public class MusicController : MonoBehaviour
{
    public float BPMSignal = 50; // replace with shimmer input
    FMOD.Studio.PARAMETER_ID BPM; // ID handle for update (supposed to help performance)

    public FMODUnity.EventReference music; // filepath reference for event
    FMOD.Studio.EventInstance musicEv; // instance of event

    void Start()
    {
        musicEv = FMODUnity.RuntimeManager.CreateInstance(music);
        musicEv.start(); // start playing on start

        // track musicEv in a description
        FMOD.Studio.EventDescription musicEvDescription;
        musicEv.getDescription(out musicEvDescription);

        // track params of musicEv
        FMOD.Studio.PARAMETER_DESCRIPTION BPMDescription;
        musicEvDescription.getParameterDescriptionByName("BPM", out BPMDescription);
        BPM = BPMDescription.id;
    }

    // Update is called once per frame
    void Update()
    {
        musicEv.setParameterByID(BPM, BPMSignal); // update event BPM parameter
    }
}
