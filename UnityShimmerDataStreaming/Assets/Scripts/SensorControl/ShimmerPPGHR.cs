using System;
using System.Collections.Generic;
using UnityEngine;
using ShimmerAPI;
using ShimmerLibrary;
using System.Linq;


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
        private Queue<int> hrHistory = new Queue<int>();
        private int maxHRWindowSize = 10;
        private int defaultHR = 65;

        // --- For Direct Method ---
        private Filter LPF_PPG_Direct;
        private Filter HPF_PPG_Direct;
        private PPGToHRAlgorithm ppgToHRAlgorithmDirect;
        private int hrDirect = -1;
        [Header("Direct Method Settings")]
        [SerializeField, Tooltip("Number of heart beats to average for HR calculation (Direct method)")]
        private int NumberOfHeartBeatsToAverage = 1;
        [SerializeField, Tooltip("Training period (in seconds) for PPG data buffer (Direct method)")]
        private int TrainingPeriodPPG = 3;

        // --- For Buffered Method ---
        private Filter LPF_PPG_Buffered;
        private Filter HPF_PPG_Buffered;
        

        private void Awake()
        {
            // Initialize Direct Method filters and algorithm.
            LPF_PPG_Direct = new Filter(Filter.LOW_PASS, shimmerDevice.SamplingRate, new double[] { 5.0 });
            HPF_PPG_Direct = new Filter(Filter.HIGH_PASS, shimmerDevice.SamplingRate, new double[] { 0.2 });
            ppgToHRAlgorithmDirect = new PPGToHRAlgorithm(shimmerDevice.SamplingRate, NumberOfHeartBeatsToAverage, TrainingPeriodPPG);

            // Initialize Buffered Method filters.
            LPF_PPG_Buffered = new Filter(Filter.LOW_PASS, shimmerDevice.SamplingRate, new double[] { 5.0 });
            HPF_PPG_Buffered = new Filter(Filter.HIGH_PASS, shimmerDevice.SamplingRate, new double[] { 0.2 });
        }

        private void Start()
        {
            if (shimmerDevice != null)
                shimmerDevice.OnStateChanged.AddListener(OnShimmerStateChanged);
        }

        private void OnShimmerStateChanged(ShimmerDevice device, ShimmerDevice.State state)
        {
            if (state == ShimmerDevice.State.Streaming)
            {
                Debug.Log("[ShimmerPPGHR] Streaming confirmed, now subscribing to data.");
                shimmerDevice.OnDataRecieved.AddListener(OnDataRecieved);
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
        
        private void OnDataRecieved(ShimmerDevice device, ObjectCluster objectCluster)
        {
            // Retrieve PPG and timestamp data
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

            // Compute HR using Direct Method
            double filteredLP_direct = LPF_PPG_Direct.filterData(dataPPG.Data);
            double filteredHP_direct = HPF_PPG_Direct.filterData(filteredLP_direct);
            int computedHR = (int)Math.Round(ppgToHRAlgorithmDirect.ppgToHrConversion(filteredHP_direct, dataTS.Data));

            if (computedHR <= 0)
            {
                computedHR = defaultHR;
            }

            // Maintain rolling window
            hrHistory.Enqueue(computedHR);
            if (hrHistory.Count > maxHRWindowSize)
            {
                hrHistory.Dequeue();
            }

            hrDirect = (int)Math.Round(hrHistory.Average());

            Debug.Log($"[Direct Method - Smoothed] HR: {hrDirect} BPM (Raw: {computedHR})");
        }
        
        public int GetHRDirect()
        {
            return hrDirect;
        }
        
    }
}
