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
using System.Linq;

namespace TelePresent.SoundShapes
{
public static class AudioZoneGeometry
{
    public static bool IsPointInsideZone(Vector3 worldPos, List<Vector3> points, Transform transform)
    {
        if (points == null || points.Count < 3)
            return false;

        List<Vector2> poly = new List<Vector2>(points.Count);
        foreach (Vector3 localPt in points)
        {
            Vector3 wp = transform.TransformPoint(localPt);
            poly.Add(new Vector2(wp.x, wp.z));
        }
        Vector2 p2D = new Vector2(worldPos.x, worldPos.z);
        return IsPointInsidePolygon(p2D, poly);
    }

    /// <summary>
    /// Determines if a 2D point is inside a polygon defined by a list of points.
    /// </summary>
    private static bool IsPointInsidePolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int count = polygon.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>
    /// Returns a constrained position for the target. For closed zones, it returns a position with the average zone height.
    /// Otherwise, it returns the closest point on the perimeter.
    /// </summary>
    public static Vector3 GetConstrainedPosition(Vector3 target, List<Vector3> points, bool closedLoop, Transform transform)
    {
        if (!closedLoop || points.Count < 3)
            return GetClosestPointOnPerimeter(target, points, closedLoop, transform);

        List<Vector3> worldPoints = new List<Vector3>(points.Count);
        foreach (Vector3 localPt in points)
            worldPoints.Add(transform.TransformPoint(localPt));

        float avgHeight = worldPoints.Average(p => p.y);
        return new Vector3(target.x, avgHeight, target.z);
    }

    /// <summary>
    /// Finds the closest point on the zone's perimeter to the target position.
    /// </summary>
    public static Vector3 GetClosestPointOnPerimeter(Vector3 target, List<Vector3> points, bool closedLoop, Transform transform)
    {
        if (points == null || points.Count < 2)
        {
            if (points.Count == 1)
                return transform.TransformPoint(points[0]);
            else
                return transform.position;
        }

        Vector3 closestPoint = Vector3.zero;
        float minDistanceSqr = Mathf.Infinity;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p1 = transform.TransformPoint(points[i]);
            Vector3 p2 = transform.TransformPoint(points[i + 1]);
            Vector3 proj = ProjectPointOnLineSegment(p1, p2, target);
            float distSqr = (target - proj).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestPoint = proj;
            }
        }
        if (closedLoop && points.Count > 2)
        {
            Vector3 p1 = transform.TransformPoint(points[points.Count - 1]);
            Vector3 p2 = transform.TransformPoint(points[0]);
            Vector3 proj = ProjectPointOnLineSegment(p1, p2, target);
            float distSqr = (target - proj).sqrMagnitude;
            if (distSqr < minDistanceSqr)
                closestPoint = proj;
        }
        return closestPoint;
    }

  

        /// <summary>
        /// Projects a point onto a line segment defined by points a and b.
        /// </summary>
        public static Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        return a + Mathf.Clamp01(t) * ab;
    }

    /// <summary>
    /// Returns the closest point on a mesh to the target position, using cached mesh data if available.
    /// </summary>
    public static Vector3 GetClosestPointOnMesh(Vector3 target, MeshFilter meshFilter, AudioZone zone, SoundShapes_CachedMeshData cachedData = null)
    {
        Vector3[] vertices;
        int[] triangles;

        if (cachedData != null)
        {
            // Use the pre-cached data.
            vertices = cachedData.vertices;
            triangles = cachedData.triangles;
        }
        else
        {
            // Fallback: if the mesh is not readable.
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
                return meshFilter.transform.position;
            GenerateMeshData(zone);
            return meshFilter.transform.position;
        }

        // Transform vertices to world space.
        Vector3[] worldVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            worldVertices[i] = meshFilter.transform.TransformPoint(vertices[i]);

        Vector3 closest = Vector3.zero;
        float minDistSqr = Mathf.Infinity;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = worldVertices[triangles[i]];
            Vector3 b = worldVertices[triangles[i + 1]];
            Vector3 c = worldVertices[triangles[i + 2]];
            Vector3 proj = ClosestPointOnTriangle(a, b, c, target);
            float distSqr = (target - proj).sqrMagnitude;
            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closest = proj;
            }
        }
        return closest;
    }

    /// <summary>
    /// Caches mesh data for each MeshFilter in the provided AudioZone.
    /// </summary>
    public static void GenerateMeshData(AudioZone audioZone)
    {
        audioZone.cachedMeshDataList.Clear();

        if (audioZone.meshFilters != null)
        {
            foreach (MeshFilter mf in audioZone.meshFilters)
            {
                if (mf != null && mf.sharedMesh != null)
                {
                    Mesh mesh = mf.sharedMesh;
                    SoundShapes_CachedMeshData cmd = new SoundShapes_CachedMeshData
                    {
                        meshReference = mesh,
                        // Access vertices and triangles (allowed in the editor).
                        vertices = mesh.vertices,
                        triangles = mesh.triangles
                    };

                    audioZone.cachedMeshDataList.Add(cmd);
                }
            }
        }
    }

    /// <summary>
    /// Iterates through mesh filters to find the closest point on any mesh to the target position.
    /// </summary>
    public static Vector3 GetClosestPointOnMeshes(Vector3 target, List<MeshFilter> meshFilters, AudioZone zone, List<SoundShapes_CachedMeshData> cachedMeshes = null)
    {
        float bestSqrDist = Mathf.Infinity;
        Vector3 bestPoint = Vector3.zero;
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null)
                continue;

            SoundShapes_CachedMeshData cachedData = null;
            if (cachedMeshes != null)
                cachedData = cachedMeshes.Find(x => x.meshReference == mf.sharedMesh);

            Vector3 candidate = GetClosestPointOnMesh(target, mf, zone, cachedData);
            float sqrDist = (target - candidate).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                bestPoint = candidate;
            }
        }
        return bestPoint;
    }

    /// <summary>
    /// Returns the closest point on a triangle to point p.
    /// </summary>
    public static Vector3 ClosestPointOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 normal = Vector3.Cross(ab, ac).normalized;
        float dist = Vector3.Dot(p - a, normal);
        Vector3 proj = p - dist * normal;

        Vector3 ap = proj - a;
        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        float d3 = Vector3.Dot(ab, ab);
        float d4 = Vector3.Dot(ab, ac);
        float d5 = Vector3.Dot(ac, ac);
        float denom = d3 * d5 - d4 * d4;
        if (denom == 0)
            return a;

        float v = (d1 * d5 - d2 * d4) / denom;
        float w = (d2 * d3 - d1 * d4) / denom;
        float u = 1 - v - w;

        if (u >= 0 && v >= 0 && w >= 0)
            return proj;
        else
        {
            Vector3 closest = ProjectPointOnLineSegment(a, b, p);
            float minDist = (p - closest).magnitude;
            Vector3 temp = ProjectPointOnLineSegment(b, c, p);
            float distTemp = (p - temp).magnitude;
            if (distTemp < minDist)
            {
                minDist = distTemp;
                closest = temp;
            }
            temp = ProjectPointOnLineSegment(c, a, p);
            distTemp = (p - temp).magnitude;
            if (distTemp < minDist)
                closest = temp;
            return closest;
        }
    }

    /// <summary>
    /// Generates an offset perimeter based on the audio zone's configuration.
    /// </summary>
    public static List<Vector3> GetOffsetPerimeter(AudioZone audioZone)
    {
        List<Vector3> perimeter = new List<Vector3>();
        float triggerDistance = (audioZone.triggerDistanceOverride > 0f)
            ? audioZone.triggerDistanceOverride
            : (audioZone.audioSource != null ? audioZone.audioSource.maxDistance : 0f);

        if (audioZone.flipTriggerDistance)
            triggerDistance = -triggerDistance;

        if (audioZone.mode == AudioZone.ZoneMode.Shape && audioZone.closedShape && audioZone.points.Count >= 3)
        {
            List<Vector3> worldPoints = new List<Vector3>(audioZone.points.Count);
            foreach (Vector3 localPt in audioZone.points)
                worldPoints.Add(audioZone.cachedTransform.TransformPoint(localPt));

            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 prev = worldPoints[(i - 1 + worldPoints.Count) % worldPoints.Count];
                Vector3 current = worldPoints[i];
                Vector3 next = worldPoints[(i + 1) % worldPoints.Count];

                Vector3 edge1 = (current - prev).normalized;
                Vector3 edge2 = (next - current).normalized;
                Vector3 avgDir = (edge1 + edge2).normalized;
                Vector3 normal = new Vector3(-avgDir.z, 0, avgDir.x);
                perimeter.Add(current + normal * triggerDistance);
            }
        }
        else if (audioZone.mode == AudioZone.ZoneMode.Shape && audioZone.points.Count >= 2)
        {
            List<Vector3> worldPoints = new List<Vector3>(audioZone.points.Count);
            foreach (Vector3 localPt in audioZone.points)
                worldPoints.Add(audioZone.cachedTransform.TransformPoint(localPt));

            List<Vector3> offsetUpper = new List<Vector3>();
            List<Vector3> offsetLower = new List<Vector3>();
            for (int i = 0; i < worldPoints.Count; i++)
            {
                Vector3 tangent;
                if (i == 0)
                    tangent = (worldPoints[1] - worldPoints[0]).normalized;
                else if (i == worldPoints.Count - 1)
                    tangent = (worldPoints[i] - worldPoints[i - 1]).normalized;
                else
                    tangent = ((worldPoints[i + 1] - worldPoints[i]) + (worldPoints[i] - worldPoints[i - 1])).normalized;
                Vector3 normal = new Vector3(-tangent.z, 0, tangent.x);
                offsetUpper.Add(worldPoints[i] + normal * triggerDistance);
                offsetLower.Add(worldPoints[i] - normal * triggerDistance);
            }
            perimeter.AddRange(offsetUpper);
            offsetLower.Reverse();
            perimeter.AddRange(offsetLower);
        }
        return perimeter;
    }




        // Helper structures for line segment calculations.
        private struct PointOnLine
    {
        public Vector3 position;
        public float distance;
        public float angle;
    }

    private struct LineSegment
    {
        public Vector3 start;
        public Vector3 end;
    }
}
}