/*******************************************************
Product - Sound Shapes
Publisher - TelePresent Games
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
        public static void UpdateOcclusion(Vector3 target, Vector3 audioPosition, AudioSource source, AudioZone zone)
        {
            if (zone.occlusion2DMode)
                UpdateOcclusion2D(target, audioPosition, source, zone);
            else
                UpdateOcclusion3D(target, audioPosition, source, zone);
        }

        private static void UpdateOcclusion3D(Vector3 target, Vector3 audioPosition, AudioSource source, AudioZone zone)
        {
            Vector3 centre = audioPosition;
            List<Vector3> offsets = GetOcclusionOffsets3D(target, centre, zone.occlusionSampleRadius, zone.occlusionResolution);

            int occludedCount = 0;
            int total = offsets.Count;

            foreach (Vector3 offset in offsets)
            {
                Vector3 p0, p1;
                if (offset == Vector3.zero)
                {
                    p0 = centre;
                    p1 = target;
                }
                else
                {
                    p0 = target + offset;
                    p1 = centre + offset;
                }

                Vector3 dir = p1 - p0;
                if (!Physics.Raycast(p0, dir.normalized, out var hit, dir.magnitude, zone.occlusionLayer))
                    continue;

                if (zone.mode == AudioZone.ZoneMode.Mesh &&
                    zone.meshFilters != null &&
                    zone.meshFilters.Exists(mf => mf && hit.collider.gameObject == mf.gameObject))
                    continue;

                occludedCount++;
            }

            ApplyOcclusionResult(source, zone, occludedCount, total);
        }

        private static void UpdateOcclusion2D(Vector3 target3, Vector3 audioPosition3, AudioSource source, AudioZone zone)
        {
            Vector2 source2 = new Vector2(audioPosition3.x, audioPosition3.y);
            Vector2 target2 = new Vector2(target3.x, target3.y);
            float distance = Vector2.Distance(source2, target2);

            List<Vector2> dirs = GetOcclusionDirections2D(source2, target2, zone.occlusion2DSpreadDegrees, zone.occlusionResolution);

            int occludedCount = 0;
            int total = dirs.Count;

            foreach (Vector2 d in dirs)
            {
                RaycastHit2D hit = Physics2D.Raycast(source2, d, distance, zone.occlusionLayer);
                if (hit.collider != null)
                    occludedCount++;
            }

            ApplyOcclusionResult(source, zone, occludedCount, total);
        }

        private static void ApplyOcclusionResult(AudioSource source, AudioZone zone, int occludedSamples, int totalSamples)
        {
            zone.occlusionRatio = totalSamples > 0 ? (float)occludedSamples / totalSamples : 0f;
            float tgtVol = Mathf.Lerp(1f, zone.occlusionVolumeMultiplier, zone.occlusionRatio);

            if (source)
                source.volume = Mathf.Lerp(source.volume, tgtVol, Time.deltaTime * 10f);

            AudioLowPassFilter lp = source ? source.GetComponent<AudioLowPassFilter>() : null;
            if (lp)
            {
                float tgtCutoff = Mathf.Lerp(zone.defaultLowPassCutoff, zone.occlusionLowPassCutoff, zone.occlusionRatio);
                lp.cutoffFrequency = Mathf.Lerp(lp.cutoffFrequency, tgtCutoff, Time.deltaTime * 10f);
            }
        }

        public static List<Vector3> GetOcclusionOffsets(Vector3 target, Vector3 centre, float sampleRadius, int resolution)
        {
            return GetOcclusionOffsets3D(target, centre, sampleRadius, resolution);
        }

        private static List<Vector3> GetOcclusionOffsets3D(Vector3 target, Vector3 centre, float sampleRadius, int resolution)
        {
            Vector3 dir = (centre - target).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up);
            if (right == Vector3.zero) right = Vector3.right;
            right.Normalize();
            Vector3 forward = Vector3.Cross(right, dir).normalized;

            List<Vector3> offsets = new() { Vector3.zero };
            int res = Mathf.Max(1, resolution);
            float twoPi = 2f * Mathf.PI;

            for (int i = 0; i < res; i++)
            {
                float a = twoPi * i / res;
                Vector3 off = (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * sampleRadius;
                offsets.Add(off);
            }

            return offsets;
        }

        private static List<Vector2> GetOcclusionDirections2D(Vector2 source, Vector2 target, float spreadDeg, int resolution)
        {
            Vector2 baseDir = (target - source).normalized;
            float halfRad = 0.5f * spreadDeg * Mathf.Deg2Rad;
            int res = Mathf.Max(1, resolution);

            List<Vector2> dirs = new();
            if (res == 1)
            {
                dirs.Add(baseDir);
                return dirs;
            }

            for (int i = 0; i < res; i++)
            {
                float t = (float)i / (res - 1);
                float ang = Mathf.Lerp(-halfRad, halfRad, t);
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);
                Vector2 d = new Vector2(cos * baseDir.x - sin * baseDir.y, sin * baseDir.x + cos * baseDir.y);
                dirs.Add(d.normalized);
            }

            return dirs;
        }
    }
}
