using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ShimmerAPI;
using ShimmerLibrary;

namespace ShimmeringUnity
{
    /// <summary>
    /// Logs HR (both Direct and Buffered methods), raw PPG, GSR, and Temperature data into a CSV file during streaming,
    /// along with Unix Timestamp and LocalTime.
    /// HR values are pulled directly from the ShimmerPPGHR script.
    /// </summary>
    public class ShimmerDataLoggerToExcel : MonoBehaviour
    {
        [Header("Shimmer Device Reference")]
        [SerializeField] private ShimmerDevice shimmerDevice;
        
        [Header("PPG HR Reference")]
        [Tooltip("Reference to the ShimmerPPGHR script that computes HR.")]
        [SerializeField] private ShimmerPPGHR shimmerPPGHR;

        [Header("Logging Options")]
        [SerializeField, Tooltip("Log computed Heart Rate data.")]
        private bool logHR = true;
        [SerializeField, Tooltip("Log raw PPG sensor value.")]
        private bool logPPG = true;
        [SerializeField, Tooltip("Log GSR (skin conductance) sensor value.")]
        private bool logGSR = true;
        [SerializeField, Tooltip("Log Temperature sensor value.")]
        private bool logTemperature = true;

        [Header("File Output Settings")]
        [SerializeField, Tooltip("File name for the CSV log file.")]
        private string fileName;
        private string filePath;
        private StreamWriter streamWriter;
        
        private void Awake()
        {
            // Build the file name with the current date and time (yyyyMMdd_HHmm)
            fileName = "ShimmerData_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
            // Save in the DataOutput folder at the project root
            string projectRoot = Path.GetFullPath(Application.dataPath + "/..");
            string dataOutputFolder = Path.Combine(projectRoot, "DataOutput");
            Directory.CreateDirectory(dataOutputFolder);
            filePath = Path.Combine(dataOutputFolder, fileName);
            
            try
            {
                // Overwrite if file exists.
                streamWriter = new StreamWriter(filePath, false);
                
                // Updated CSV header: Two HR columns ("HR_Direct" and "HR_Buffered")
                List<string> headers = new List<string> { "RawTimestamp", "LocalTime" };
                if (logHR)
                {
                    headers.Add("HR_Direct");
                    headers.Add("HR_Buffered");
                }
                if (logPPG) headers.Add("PPG");
                if (logGSR) headers.Add("GSR");
                if (logTemperature) headers.Add("Temperature");
                
                streamWriter.WriteLine(string.Join(",", headers));
                streamWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error opening file {filePath}: {ex.Message}");
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
            
            if (streamWriter != null)
            {
                streamWriter.Flush();
                streamWriter.Close();
                streamWriter = null;
            }
        }
        
        /// <summary>
        /// Listener for new data from the Shimmer device.
        /// Extracts timestamps, raw PPG, HR from both methods (directly from ShimmerPPGHR), GSR, and Temperature,
        /// and writes a CSV line with these values.
        /// </summary>
        private void OnDataRecieved(ShimmerDevice device, ObjectCluster objectCluster)
        {
            // 1) Get system timestamp data.
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.SYSTEM_TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            double rawTimestampVal = dataTS != null ? dataTS.Data : 0.0;
            string rawTimestampString = rawTimestampVal.ToString("F0"); // no decimals
            
            DateTime localTime = DateTime.Now;
            try
            {
                long ms = (long)Math.Round(rawTimestampVal);
                localTime = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
            }
            catch
            {
                localTime = DateTime.Now;
            }
            string localTimeString = localTime.ToString("HH:mm:ss.fff");
            
            double ppgValue = double.NaN;
            int hrDirect = -1;
            int hrBuffered = -1;
            double gsrValue = double.NaN;
            double temperatureValue = double.NaN;
            
            // 2) Retrieve raw PPG value.
            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            if (dataPPG != null)
            {
                ppgValue = dataPPG.Data;
            }
            
            // 3) Retrieve HR values directly from the ShimmerPPGHR script.
            if (logHR && shimmerPPGHR != null)
            {
                hrDirect = shimmerPPGHR.GetHRDirect();
                hrBuffered = shimmerPPGHR.GetHRBuffered();
            }
            
            // 4) Retrieve GSR data.
            if (logGSR)
            {
                SensorData dataGSR = objectCluster.GetData(
                    ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE],
                    ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
                );
                if (dataGSR != null)
                {
                    gsrValue = dataGSR.Data;
                }
            }
            
            // 5) Retrieve Temperature data.
            if (logTemperature)
            {
                SensorData dataTemp = objectCluster.GetData(
                    ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.TEMPERATURE],
                    ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
                );
                if (dataTemp != null)
                {
                    temperatureValue = dataTemp.Data;
                }
            }
            
            // Debug.Log($"Real-time Sensor Values -> PPG: {ppgValue}, " +
            //           $"HR Direct: {hrDirect} BPM, " +
            //           $"HR Buffered: {hrBuffered} BPM, " +
            //           $"GSR: {gsrValue}, Temp: {temperatureValue}");

            // 6) Build the CSV row.
            List<string> dataLine = new List<string>
            {
                rawTimestampString,
                localTimeString
            };
            if (logHR)
            {
                dataLine.Add(hrDirect.ToString());
                dataLine.Add(hrBuffered.ToString());
            }
            if (logPPG) dataLine.Add(ppgValue.ToString());
            if (logGSR) dataLine.Add(gsrValue.ToString());
            if (logTemperature) dataLine.Add(temperatureValue.ToString());
            
            string csvLine = string.Join(",", dataLine);
            try
            {
                streamWriter.WriteLine(csvLine);
                streamWriter.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing to file: {ex.Message}");
            }
        }
    }
}
