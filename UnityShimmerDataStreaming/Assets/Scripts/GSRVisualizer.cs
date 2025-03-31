using System.Collections.Generic;
using UnityEngine;
using ShimmerAPI;
using System.Linq;

namespace ShimmeringUnity
{
    /// <summary>
    /// Visualizes the skin conductance (GSR) signal in real-time using a LineRenderer.
    /// Ensure that the ShimmerDevice has the GSR sensor enabled and that its OnDataRecieved event is firing.
    /// The GSR values are normalized based on the specified minimum and maximum values.
    /// </summary>
    public class GSRVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("Reference to the Shimmer device.")]
        private ShimmerDevice shimmerDevice;

        [Header("Line Graph Settings")]
        [SerializeField, Tooltip("LineRenderer used to draw the GSR graph.")]
        private LineRenderer gsrLineRenderer;

        [SerializeField, Tooltip("Maximum number of data points on the graph.")]
        private int maxPoints = 100;
        [SerializeField, Tooltip("Horizontal spacing between each data point.")]
        private float xScale = 0.1f;
        [SerializeField, Tooltip("Vertical scale factor for the normalized GSR values.")]
        private float yScale = 1.0f;  // Adjust as needed to set the graph's vertical scale

        [Header("GSR Normalization Settings")]
        [SerializeField, Tooltip("Minimum expected GSR value for normalization.")]
        private float minGSR = 0f;
        [SerializeField, Tooltip("Maximum expected GSR value for normalization.")]
        private float maxGSR = 100f;  // Adjust this based on your sensor's output range

        // List of points for the graph
        private List<Vector3> gsrPoints = new List<Vector3>();
        private float currentTime = 0f;

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
            // Update the LineRenderer with the current points
            if (gsrLineRenderer != null)
            {
                gsrLineRenderer.positionCount = gsrPoints.Count;
                gsrLineRenderer.SetPositions(gsrPoints.ToArray());
            }
        }

        private void OnDataRecieved(ShimmerDevice device, ObjectCluster objectCluster)
        {
            // Retrieve the GSR conductance data from the sensor.
            SensorData dataGSR = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.GSR_CONDUCTANCE],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );

            if (dataGSR == null)
            {
                Debug.Log("GSR Data is NULL.");
                return;
            }

            Debug.Log($"Raw GSR Value: {dataGSR.Data}");
            AddGSRPoint((float)dataGSR.Data);
        }

        /// <summary>
        /// Adds a new GSR data point to the graph.
        /// The raw GSR value is normalized based on minGSR and maxGSR.
        /// </summary>
        /// <param name="gsrValue">The raw GSR value to graph.</param>
        private void AddGSRPoint(float gsrValue)
        {
            // Increment current time (x-axis position)
            currentTime += xScale;
            // Normalize the GSR value so that values between minGSR and maxGSR map to 0-1
            float normalizedGSR = Mathf.InverseLerp(minGSR, maxGSR, gsrValue);
            // Create a new point (x = currentTime, y = normalized value scaled by yScale)
            Vector3 newPoint = new Vector3(currentTime, normalizedGSR * yScale, 0f);
            gsrPoints.Add(newPoint);

            // If too many points, remove the oldest and shift the graph to create a scrolling effect
            if (gsrPoints.Count > maxPoints)
            {
                gsrPoints.RemoveAt(0);
                for (int i = 0; i < gsrPoints.Count; i++)
                {
                    gsrPoints[i] = new Vector3(gsrPoints[i].x - xScale, gsrPoints[i].y, gsrPoints[i].z);
                }
                currentTime -= xScale;
            }
        }
    }
}
