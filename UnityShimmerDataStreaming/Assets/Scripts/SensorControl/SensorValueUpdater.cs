using UnityEngine;
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
        [Header("Optional Particle Receiver")]
        public DynamicParticle dynamicParticle;

        [Header("Debug Logging Settings")]
        [SerializeField] private bool enableLiveLogging = true;
        [SerializeField] private float logInterval = 1.0f;
        private float logTimer = 0f;

        private float latestGSR = 0f;
        private float latestTemperature = 0f;
        private float latestPPG = 0f;
        private float hrDirect = 0f;
        // private float hrBuffered = 0f;

        public float HRDirect => hrDirect;
        // public float HRBuffered => hrBuffered;
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

            // Timestamp (for packet validation)
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.SYSTEM_TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            /*
            if (dataTS != null)
            {
                Debug.Log($"[Data] Timestamp: {dataTS.Data}");
            }
            */

            // PPG
            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestPPG = dataPPG != null ? (float)dataPPG.Data : float.NaN;

            // HR from ShimmerPPGHR
            if (shimmerPPGHR != null)
            {
                hrDirect = shimmerPPGHR.GetHRDirect();
            }

            // GSR
            SensorData dataGSR = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestGSR = dataGSR != null ? (float)dataGSR.Data : float.NaN;

            // Temperature
            SensorData dataTemp = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.TEMPERATURE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestTemperature = dataTemp != null ? (float)dataTemp.Data : float.NaN;
        }

        void Update()
        {
            if (dynamicParticle != null)
            {
                dynamicParticle.HeartRate = hrDirect;
                dynamicParticle.GSRValue = latestGSR;
                dynamicParticle.Temperature = latestTemperature;
                dynamicParticle.PPGValue = latestPPG;
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
