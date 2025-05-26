using UnityEngine;
using System.Collections.Generic;
using ShimmeringUnity;
using ShimmerAPI;
using ShimmerLibrary;

namespace ShimmeringUnity
{
    public class SensorValueUpdater : MonoBehaviour
    {
        [Header("Shimmer Device Reference")]
        [SerializeField] private ShimmerDevice shimmerDevice;

        [Header("PPG HR Reference")]
        [SerializeField] private ShimmerPPGHR shimmerPPGHR;

        [Header("Debug Logging Settings")]
        [SerializeField] private bool enableLiveLogging = true;
        [SerializeField] private float logInterval = 1.0f;
        private float logTimer = 0f;

        private float latestGSR = 0f;
        private float latestTemperature = 0f;
        private float latestPPG = 0f;
        private float hrDirect = 0f;

        private List<DynamicParticle> dynamicParticles = new List<DynamicParticle>();

        public float HRDirect => hrDirect;
        public float LatestGSR => latestGSR;
        public float LatestPPG => latestPPG;
        public float LatestTemperature => latestTemperature;

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

            // Find all DynamicParticle components in the scene
            DynamicParticle[] foundParticles = GameObject.FindObjectsOfType<DynamicParticle>();
            dynamicParticles.AddRange(foundParticles);
            Debug.Log($"[SensorUpdater] Found {dynamicParticles.Count} DynamicParticle components.");
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

            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestPPG = dataPPG != null ? (float)dataPPG.Data : float.NaN;

            if (shimmerPPGHR != null)
            {
                hrDirect = shimmerPPGHR.GetHRDirect();
            }

            SensorData dataGSR = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestGSR = dataGSR != null ? (float)dataGSR.Data : float.NaN;

            SensorData dataTemp = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.TEMPERATURE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestTemperature = dataTemp != null ? (float)dataTemp.Data : float.NaN;
        }

        void Update()
        {
            foreach (var dp in dynamicParticles)
            {
                if (dp != null)
                {
                    dp.HeartRate = hrDirect;
                    dp.GSRValue = latestGSR;
                    dp.Temperature = latestTemperature;
                    dp.PPGValue = latestPPG;
                }
            }

            if (enableLiveLogging)
            {
                logTimer += Time.deltaTime;
                if (logTimer >= logInterval)
                {
                    Debug.Log($"[LiveSensorUpdate] HR: {hrDirect}, GSR: {latestGSR}, Temp: {latestTemperature}, PPG: {latestPPG}");
                    logTimer = 0f;
                }
            }
        }
    }
}
