using UnityEngine;
using System.Collections;
using ShimmeringUnity;
using ShimmerAPI;
using ShimmerLibrary;

namespace ShimmeringUnity
{
    public class MusicShimmerController : MonoBehaviour
    {
      
        [Header("Shimmer Device Reference")]
        [SerializeField] private ShimmerDevice shimmerDevice;
        
        [Header("PPG HR Reference")]
        [Tooltip("Optional - Will try to GetComponent if not assigned")]
        [SerializeField] private ShimmerPPGHR shimmerPPGHR;
        private float hrDirect = 0f;
        private float latestGSR = 0f;
        private float latestTemperature = 0f;
        private float latestPPG = 0f;

        public float BPMSignal = 50; // replace with shimmer input
        FMOD.Studio.PARAMETER_ID BPM; // ID handle for update (supposed to help performance)

        public FMODUnity.EventReference music; // filepath reference for event
        FMOD.Studio.EventInstance musicEv; // instance of event

        
        void Awake()
        {
            if (shimmerPPGHR == null)
            {
                shimmerPPGHR = GetComponent<ShimmerPPGHR>();
                if (shimmerPPGHR == null)
                {
                    Debug.LogWarning("ShimmerPPGHR component not found.", this);
                }
            }
        }

        void OnEnable()
        {
            if (shimmerDevice != null)
            {
                shimmerDevice.OnDataRecieved.AddListener(OnDataRecieved);
                Debug.Log("Successfully subscribed to ShimmerDevice events", this);
            }
            else
            {
                Debug.LogError("ShimmerDevice reference not assigned!", this);
            }
        }

        void OnDisable()
        {
            if (shimmerDevice != null)
            {
                shimmerDevice.OnDataRecieved.RemoveListener(OnDataRecieved);
            }
        }

        private void OnDataRecieved(ShimmerDevice device, ObjectCluster objectCluster)
        {
            if (objectCluster == null)
            {
                Debug.LogWarning("Received null ObjectCluster", this);
                return;
            }

            // Debug timestamp to verify data is coming through
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.SYSTEM_TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            /*
            if (dataTS != null)
            {
                Debug.Log($"Received data with timestamp: {dataTS.Data}");
            }
            */

            // Process PPG
            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestPPG = dataPPG != null ? (float)dataPPG.Data : float.NaN;

            // Process HR
            if (shimmerPPGHR != null)
            {
                hrDirect = shimmerPPGHR.GetHRDirect();
            }
            
            Debug.Log($"Sensor Musci HR: {hrDirect}");
        }

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
            BPMSignal = hrDirect;
            //Debug.Log($"Updated BPMSignal: {hrDirect}");
            musicEv.setParameterByID(BPM, hrDirect); // update event BPM parameter
        }
    }
}