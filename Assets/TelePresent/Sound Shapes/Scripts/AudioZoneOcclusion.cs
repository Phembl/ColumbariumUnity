/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace TelePresent.SoundShapes
{
    public static class AudioZoneOcclusion
    {
        /// <summary>
        /// Updates the occlusion effect on the given AudioSource based on the ratio of occluded samples.
        /// </summary>
        public static void UpdateOcclusion(Vector3 target, Vector3 audioPosition, AudioSource source, AudioZone zone)
        {
            Vector3 center = audioPosition;
            List<Vector3> offsets = GetOcclusionOffsets(target, center, zone.occlusionSampleRadius, zone.occlusionResolution);
            int occludedCount = 0;
            int totalSamples = offsets.Count;

            foreach (Vector3 offset in offsets)
            {
                Vector3 sampleOrigin, sampleTarget;
                if (offset == Vector3.zero)
                {
                    sampleOrigin = center;
                    sampleTarget = target;
                }
                else
                {
                    sampleOrigin = target + offset;
                    sampleTarget = center + offset;
                }

                Vector3 sampleDir = sampleTarget - sampleOrigin;
                float sampleDistance = sampleDir.magnitude;
                if (Physics.Raycast(sampleOrigin, sampleDir.normalized, out RaycastHit hit, sampleDistance, zone.occlusionLayer))
                {
                    if (zone.mode == AudioZone.ZoneMode.Mesh &&
                        zone.meshFilters != null && zone.meshFilters.Count > 0 &&
                        zone.meshFilters.Exists(mf => mf != null && hit.collider.gameObject == mf.gameObject))
                    {
                        continue;
                    }
                    occludedCount++;
                }
            }

            float occlusionRatio = (float)occludedCount / totalSamples;
            float targetVolume = Mathf.Lerp(1f, zone.occlusionVolumeMultiplier, occlusionRatio);
            source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 10f);

            AudioLowPassFilter lp = source.GetComponent<AudioLowPassFilter>();
            if (lp != null)
            {
                float targetCutoff = Mathf.Lerp(zone.defaultLowPassCutoff, zone.occlusionLowPassCutoff, occlusionRatio);
                lp.cutoffFrequency = Mathf.Lerp(lp.cutoffFrequency, targetCutoff, Time.deltaTime * 10f);
            }
        }

        /// <summary>
        /// Computes sample offsets arranged in a circle (plus the center point) for occlusion raycasting.
        /// </summary>
        public static List<Vector3> GetOcclusionOffsets(Vector3 target, Vector3 center, float sampleRadius, int occlusionResolution)
        {
            Vector3 direction = (center - target).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up);
            if (right == Vector3.zero)
                right = Vector3.right;
            right.Normalize();
            Vector3 forward = Vector3.Cross(right, direction).normalized;

            List<Vector3> offsets = new List<Vector3> { Vector3.zero };
            int resolution = Mathf.Max(1, occlusionResolution);
            float twoPi = 2 * Mathf.PI;
            for (int i = 0; i < resolution; i++)
            {
                float angle = twoPi * i / resolution;
                Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * sampleRadius;
                offsets.Add(offset);
            }
            return offsets;
        }
    }
}