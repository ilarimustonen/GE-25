using UnityEngine;
using Bitgem.VFX.StylisedWater;

namespace Bitgem.VFX.StylisedWater
{
    public class WaterVolumeHelper : MonoBehaviour
    {
        private static WaterVolumeHelper instance = null;

        public WaterVolumeBase WaterVolume = null;

        [Header("Manual Override")]
        [Tooltip("If true, uses the manual base height instead of WaterVolume.GetHeight()")]
        public bool useManualBaseHeight = false;

        [Tooltip("The base Y position of the water surface (without waves)")]
        public float manualBaseHeight = 49.5f;

        public static WaterVolumeHelper Instance { get { return instance; } }

        private void Awake()
        {
            instance = this;
        }

        public float? GetHeight(Vector3 _position)
        {
            // Ensure a water volume
            if (!WaterVolume)
            {
                return null;
            }

            // Ensure a material
            var renderer = WaterVolume.gameObject.GetComponent<MeshRenderer>();
            if (!renderer || !renderer.sharedMaterial)
            {
                return null;
            }

            // Get base water height
            float baseHeight;
            if (useManualBaseHeight)
            {
                baseHeight = manualBaseHeight;
            }
            else
            {
                var waterHeight = WaterVolume.GetHeight(_position);
                if (!waterHeight.HasValue)
                {
                    return null;
                }
                baseHeight = waterHeight.Value;
            }

            // Convert world position to object space to check the step condition
            Vector3 objectSpacePos = WaterVolume.transform.InverseTransformPoint(_position);

            // Replicate the step function: step(0.5, objectY)
            float stepResult = objectSpacePos.y >= 0.5f ? 1f : 0f;

            // Get wave parameters
            var _WaveFrequency = renderer.sharedMaterial.GetFloat("_WaveFrequency");
            var _WaveScale = renderer.sharedMaterial.GetFloat("_WaveScale");
            var _WaveSpeed = renderer.sharedMaterial.GetFloat("_WaveSpeed");

            // Calculate wave offset using world space X and Z (exact shader logic)
            var time = Time.time * _WaveSpeed;
            var waveOffset = (Mathf.Sin(_position.x * _WaveFrequency + time) +
                             Mathf.Cos(_position.z * _WaveFrequency + time)) * _WaveScale;

            // Apply the step function to the wave offset
            waveOffset *= stepResult;

            // Final height = base height + wave offset
            return baseHeight + waveOffset;
        }
    }
}