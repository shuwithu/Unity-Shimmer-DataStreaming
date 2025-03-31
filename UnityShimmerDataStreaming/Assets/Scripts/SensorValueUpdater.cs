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
        [Tooltip("Optional - Will try to GetComponent if not assigned")]
        [SerializeField] private ShimmerPPGHR shimmerPPGHR;
        
        public DynamicParticle dynamicParticle;

        private float latestGSR = 0f;
        private float latestTemperature = 0f;
        private float latestPPG = 0f;
        private float hrDirect = 0f;
        private float hrBuffered = 0f;

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
            if (dataTS != null)
            {
                Debug.Log($"Received data with timestamp: {dataTS.Data}");
            }

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
                hrBuffered = shimmerPPGHR.GetHRBuffered();
            }

            // Process GSR
            SensorData dataGSR = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestGSR = dataGSR != null ? (float)dataGSR.Data : float.NaN;

            // Process Temperature
            SensorData dataTemp = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.TEMPERATURE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            latestTemperature = dataTemp != null ? (float)dataTemp.Data : float.NaN;

            Debug.Log($"SensorUpdate - PPG: {latestPPG}, HR: {hrDirect}, GSR: {latestGSR}, Temp: {latestTemperature}");
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
        }
    }
}