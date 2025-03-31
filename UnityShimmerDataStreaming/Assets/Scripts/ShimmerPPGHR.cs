using System;
using System.Collections.Generic;
using UnityEngine;
using ShimmerAPI;
using ShimmerLibrary;

namespace ShimmeringUnity
{
    /// <summary>
    /// ShimmerPPGHR calculates heart rate (BPM) from the PPG signal using two methods:
    /// 1) A "Direct" method that uses filtered data from each sample.
    /// 2) A buffered peak detection algorithm.
    /// Both HR values are computed and exposed via public getters.
    /// It subscribes to the Shimmer device’s data and can update a real-time line graph via a LineRenderer.
    /// </summary>
    public class ShimmerPPGHR : MonoBehaviour
    {
        [SerializeField]
        private ShimmerDevice shimmerDevice;

        // --- For Direct Method ---
        private Filter LPF_PPG_Direct;
        private Filter HPF_PPG_Direct;
        private PPGToHRAlgorithm ppgToHRAlgorithmDirect;
        private int hrDirect = -1;
        [Header("Direct Method Settings")]
        [SerializeField, Tooltip("Number of heart beats to average for HR calculation (Direct method)")]
        private int NumberOfHeartBeatsToAverage = 1;
        [SerializeField, Tooltip("Training period (in seconds) for PPG data buffer (Direct method)")]
        private int TrainingPeriodPPG = 10; // 10-second buffer

        // --- For Buffered Method ---
        private Filter LPF_PPG_Buffered;
        private Filter HPF_PPG_Buffered;
        private List<double> ppgBuffer = new List<double>();
        private List<double> timeBuffer = new List<double>();
        private int hrBuffered = -1;
        [Header("Buffered Method Settings")]
        [SerializeField, Tooltip("Number of samples to accumulate for buffered HR computation")]
        private int requiredBufferSize = 30;
        
        // --- Real-time Graph Settings (Optional) ---
        [Header("Real-Time Graph Settings (Optional)")]
        [SerializeField, Tooltip("LineRenderer used to draw the heart rate graph")]
        private LineRenderer heartRateLineRenderer;
        [SerializeField, Tooltip("Maximum number of data points on the graph")]
        private int maxPoints = 100;
        [SerializeField, Tooltip("Horizontal spacing between each data point")]
        private float xScale = 0.1f;
        [SerializeField, Tooltip("Vertical scale factor for the heart rate values")]
        private float yScale = 0.05f;
        private List<Vector3> hrPoints = new List<Vector3>();
        private float currentTime = 0f;

        private void Awake()
        {
            // Initialize Direct Method filters and algorithm.
            LPF_PPG_Direct = new Filter(Filter.LOW_PASS, shimmerDevice.SamplingRate, new double[] { 5.0 });
            HPF_PPG_Direct = new Filter(Filter.HIGH_PASS, shimmerDevice.SamplingRate, new double[] { 0.2 });
            ppgToHRAlgorithmDirect = new PPGToHRAlgorithm(shimmerDevice.SamplingRate, NumberOfHeartBeatsToAverage, TrainingPeriodPPG);

            // Initialize Buffered Method filters.
            LPF_PPG_Buffered = new Filter(Filter.LOW_PASS, shimmerDevice.SamplingRate, new double[] { 5.0 });
            HPF_PPG_Buffered = new Filter(Filter.HIGH_PASS, shimmerDevice.SamplingRate, new double[] { 0.2 });

            // Configure the LineRenderer if assigned.
            if (heartRateLineRenderer != null)
            {
                heartRateLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                heartRateLineRenderer.startWidth = 0.02f;
                heartRateLineRenderer.endWidth = 0.02f;
                heartRateLineRenderer.startColor = Color.green;
                heartRateLineRenderer.endColor = Color.green;
                heartRateLineRenderer.useWorldSpace = true;
            }
        }

        private void OnEnable()
        {
            if (shimmerDevice != null)
                shimmerDevice.OnDataRecieved.AddListener(OnDataRecieved);
        }

        private void OnDisable()
        {
            if (shimmerDevice != null)
                shimmerDevice.OnDataRecieved.RemoveListener(OnDataRecieved);
        }

        private void Update()
        {
            // Update the real-time graph if a LineRenderer is assigned.
            if (heartRateLineRenderer != null)
            {
                heartRateLineRenderer.positionCount = hrPoints.Count;
                heartRateLineRenderer.SetPositions(hrPoints.ToArray());
            }
        }

        private void OnDataRecieved(ShimmerDevice device, ObjectCluster objectCluster)
        {
            // Retrieve PPG and timestamp data.
            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.SYSTEM_TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );

            if (dataPPG == null || dataTS == null)
            {
                Debug.Log("PPG or Timestamp data is NULL.");
                return;
            }

            Debug.Log($"Raw PPG Value: {dataPPG.Data}");

            // --- Compute HR using Direct Method ---
            double filteredLP_direct = LPF_PPG_Direct.filterData(dataPPG.Data);
            double filteredHP_direct = HPF_PPG_Direct.filterData(filteredLP_direct);
            hrDirect = (int)Math.Round(ppgToHRAlgorithmDirect.ppgToHrConversion(filteredHP_direct, dataTS.Data));
            Debug.Log($"[Direct Method] Computed Heart Rate: {hrDirect} BPM");

            // --- Compute HR using Buffered Method ---
            double filteredLP_buff = LPF_PPG_Buffered.filterData(dataPPG.Data);
            double filteredHP_buff = HPF_PPG_Buffered.filterData(filteredLP_buff);
            double currentTimestamp = dataTS.Data;
            ppgBuffer.Add(filteredHP_buff);
            timeBuffer.Add(currentTimestamp);
            if (ppgBuffer.Count > requiredBufferSize)
            {
                ppgBuffer.RemoveAt(0);
                timeBuffer.RemoveAt(0);
            }
            if (ppgBuffer.Count >= requiredBufferSize)
            {
                hrBuffered = ComputeHeartRateFromBuffer();
                Debug.Log($"[Buffered Method] Computed Heart Rate: {hrBuffered} BPM");
            }
        }

        /// <summary>
        /// Computes heart rate (BPM) from the buffered PPG data using peak detection.
        /// A peak is defined as a sample that is higher than its immediate neighbors.
        /// Only intervals between 0.3 and 2.0 seconds are considered plausible.
        /// </summary>
        /// <returns>The computed heart rate in BPM, or -1 if insufficient peaks are detected.</returns>
        private int ComputeHeartRateFromBuffer()
        {
            List<double> peakTimes = new List<double>();

            for (int i = 1; i < ppgBuffer.Count - 1; i++)
            {
                if (ppgBuffer[i] > ppgBuffer[i - 1] && ppgBuffer[i] > ppgBuffer[i + 1])
                {
                    peakTimes.Add(timeBuffer[i]);
                }
            }

            if (peakTimes.Count < 2)
                return -1;

            List<double> intervals = new List<double>();
            for (int i = 1; i < peakTimes.Count; i++)
            {
                double interval = peakTimes[i] - peakTimes[i - 1];
                if (interval > 0.3 && interval < 2.0)
                    intervals.Add(interval);
            }

            if (intervals.Count == 0)
                return -1;

            double avgInterval = 0.0;
            foreach (double interval in intervals)
            {
                avgInterval += interval;
            }
            avgInterval /= intervals.Count;

            Debug.Log($"[Buffered Method] Average RR interval: {avgInterval} seconds");

            if (avgInterval <= 0)
                return -1;

            int hr = (int)Math.Round(60.0 / avgInterval);
            Debug.Log($"[Buffered Method] Computed Heart Rate: {hr} BPM");
            return hr;
        }
        
        // Public getters to access HR values.
        public int GetHRDirect()
        {
            return hrDirect;
        }
        
        public int GetHRBuffered()
        {
            return hrBuffered;
        }
    }
}
