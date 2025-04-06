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
    public class SoundShapes_BVHNode
    {
        public Bounds Bounds; // Unified bounds for 2D or 3D objects
        public List<GameObject> Objects;
        public SoundShapes_BVHNode LeftChild;
        public SoundShapes_BVHNode RightChild;

        public SoundShapes_BVHNode(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Objects = new List<GameObject> { gameObject };

                if (gameObject.TryGetComponent<Collider2D>(out Collider2D collider2D))
                {
                    Bounds = collider2D.bounds;
                }
                else
                {
                    Bounds = CalculateAccurateBounds(gameObject);
                }
            }
            else
            {
                Objects = new List<GameObject>();
                Bounds = new Bounds(Vector3.zero, Vector3.zero);
            }
        }

        /// <summary>
        /// Calculates a more accurate world-space bound for a 3D object.
        /// </summary>
        private Bounds CalculateAccurateBounds(GameObject obj)
        {
            if (obj == null)
                return new Bounds(Vector3.zero, Vector3.zero);

            if (obj.TryGetComponent<MeshFilter>(out MeshFilter meshFilter) &&
                meshFilter.sharedMesh != null &&
                obj.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer) &&
                meshRenderer.enabled)
            {
                Mesh mesh = meshFilter.sharedMesh;
                // Start with the first vertex and encapsulate all others.
                Bounds meshBounds = new Bounds(mesh.vertices[0], Vector3.zero);
                foreach (Vector3 vertex in mesh.vertices)
                    meshBounds.Encapsulate(vertex);

                // Transform the local bounds to world space.
                Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;
                Vector3 worldMin = localToWorld.MultiplyPoint3x4(meshBounds.min);
                Vector3 worldMax = localToWorld.MultiplyPoint3x4(meshBounds.max);
                return new Bounds((worldMin + worldMax) * 0.5f, worldMax - worldMin);
            }
            else if (obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMeshRenderer) &&
                     skinnedMeshRenderer.sharedMesh != null &&
                     skinnedMeshRenderer.enabled)
            {
                return skinnedMeshRenderer.bounds;
            }
            else
            {
                return new Bounds(obj.transform.position, Vector3.zero);
            }
        }

        public bool IsLeaf => LeftChild == null && RightChild == null;

        /// <summary>
        /// Performs a raycast against all objects in this node and returns sorted hit points.
        /// </summary>
        public bool TryRaycast(Ray ray, out List<(Vector3 hitPoint, GameObject obj)> hitPointsWithObjects)
        {
            hitPointsWithObjects = new List<(Vector3, GameObject)>();

            // Early exit if the ray doesn't intersect this node's bounds.
            if (!Bounds.IntersectRay(ray))
                return false;

            foreach (GameObject obj in Objects)
            {
                if (obj == null)
                    continue;

                // 2D Collider case.
                if (obj.TryGetComponent<Collider2D>(out Collider2D collider2D))
                {
                    Ray2D ray2D = new Ray2D(ray.origin, ray.direction);
                    RaycastHit2D hit2D = Physics2D.Raycast(ray2D.origin, ray2D.direction);
                    if (hit2D.collider != null && hit2D.collider == collider2D)
                    {
                        hitPointsWithObjects.Add((hit2D.point, obj));
                    }
                }
                // MeshFilter case.
                else if (obj.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    if (mesh != null)
                    {
                        Transform t = meshFilter.transform;
                        Matrix4x4 worldToLocal = t.worldToLocalMatrix;
                        Ray localRay = new Ray(worldToLocal.MultiplyPoint(ray.origin), worldToLocal.MultiplyVector(ray.direction));
                        if (RayIntersectsMesh(localRay, mesh, out List<Vector3> localHits))
                        {
                            foreach (Vector3 localHit in localHits)
                            {
                                Vector3 worldHit = t.TransformPoint(localHit);
                                hitPointsWithObjects.Add((worldHit, obj));
                            }
                        }
                    }
                }
                // SkinnedMeshRenderer case.
                else if (obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMeshRenderer))
                {
                    Mesh mesh = skinnedMeshRenderer.sharedMesh;
                    if (mesh != null)
                    {
                        Transform t = skinnedMeshRenderer.transform;
                        Matrix4x4 worldToLocal = t.worldToLocalMatrix;
                        Ray localRay = new Ray(worldToLocal.MultiplyPoint(ray.origin), worldToLocal.MultiplyVector(ray.direction));
                        if (RayIntersectsMesh(localRay, mesh, out List<Vector3> localHits))
                        {
                            foreach (Vector3 localHit in localHits)
                            {
                                Vector3 worldHit = t.TransformPoint(localHit);
                                hitPointsWithObjects.Add((worldHit, obj));
                            }
                        }
                    }
                }
            }

            // Order hits by distance from the ray's origin.
            hitPointsWithObjects = hitPointsWithObjects
                .OrderBy(hit => Vector3.SqrMagnitude(ray.origin - hit.hitPoint))
                .ToList();

            return hitPointsWithObjects.Count > 0;
        }

        /// <summary>
        /// Clears this node and its children.
        /// </summary>
        public void Clear()
        {
            Objects.Clear();
            LeftChild?.Clear();
            RightChild?.Clear();
            LeftChild = null;
            RightChild = null;
        }

        /// <summary>
        /// Determines intersection points between a ray and a mesh.
        /// </summary>
        public static bool RayIntersectsMesh(Ray ray, Mesh mesh, out List<Vector3> hitPoints)
        {
            hitPoints = new List<Vector3>();
            if (mesh == null)
                return false;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0, count = triangles.Length; i < count; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                if (RayIntersectsTriangle(ray, v0, v1, v2, out Vector3 intersection))
                {
                    hitPoints.Add(intersection);
                }
            }
            return hitPoints.Count > 0;
        }

        /// <summary>
        /// Implements the Möller–Trumbore algorithm to test ray-triangle intersection.
        /// </summary>
        private static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 intersectionPoint)
        {
            intersectionPoint = Vector3.zero;
            const float epsilon = 1e-8f;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);
            if (Mathf.Abs(a) < epsilon)
                return false;

            float f = 1f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f)
                return false;

            float t = f * Vector3.Dot(edge2, q);
            if (t > epsilon)
            {
                intersectionPoint = ray.origin + ray.direction * t;
                return true;
            }
            return false;
        }
    }
}