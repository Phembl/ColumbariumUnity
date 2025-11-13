/*******************************************************
Product   - Sound Shapes
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
    public class SoundShapesBvhNode
    {
        public UnityEngine.Bounds NodeBounds;

        private readonly List<GameObject> _objects;
        public SoundShapesBvhNode LeftChild;
        public SoundShapesBvhNode RightChild;

        public SoundShapesBvhNode(GameObject gameObject)
        {
            if (gameObject)
            {
                _objects = new List<GameObject> { gameObject };

                NodeBounds = gameObject.TryGetComponent(out Collider2D collider2D)
                    ? collider2D.bounds
                    : CalculateAccurateBounds(gameObject);
            }
            else
            {
                _objects = new List<GameObject>();
                NodeBounds = new UnityEngine.Bounds(Vector3.zero, Vector3.zero);
            }
        }

        private static UnityEngine.Bounds CalculateAccurateBounds(GameObject obj)
        {
            if (!obj)
                return new UnityEngine.Bounds(Vector3.zero, Vector3.zero);

            if (obj.TryGetComponent(out MeshFilter meshFilter) &&
                meshFilter.sharedMesh &&
                obj.TryGetComponent(out MeshRenderer meshRenderer) &&
                meshRenderer.enabled)
            {
                Mesh mesh = meshFilter.sharedMesh;

                // Start with the first vertex and encapsulate all others.
                UnityEngine.Bounds meshBounds =
                    new UnityEngine.Bounds(mesh.vertices[0], Vector3.zero);

                foreach (Vector3 v in mesh.vertices)
                    meshBounds.Encapsulate(v);

                // Transform the local bounds to world space.
                Matrix4x4 l2W = obj.transform.localToWorldMatrix;
                Vector3 worldMin = l2W.MultiplyPoint3x4(meshBounds.min);
                Vector3 worldMax = l2W.MultiplyPoint3x4(meshBounds.max);

                return new UnityEngine.Bounds((worldMin + worldMax) * 0.5f,
                    worldMax - worldMin);
            }
            else if (obj.TryGetComponent(out SkinnedMeshRenderer skin) &&
                     skin.sharedMesh &&
                     skin.enabled)
            {
                return skin.bounds;
            }
            else
            {
                return new UnityEngine.Bounds(obj.transform.position, Vector3.zero);
            }
        }

        public bool IsLeaf => LeftChild == null && RightChild == null;

        public bool TryRaycast(Ray ray,
            out List<(Vector3 hitPoint, GameObject obj)> hits)
        {
            hits = new List<(Vector3, GameObject)>();

            // Early-out if the ray misses this node’s bounds.
            if (!NodeBounds.IntersectRay(ray))
                return false;

            foreach (GameObject obj in _objects)
            {
                if (!obj) continue;

                // 2D collider
                if (obj.TryGetComponent(out Collider2D col2D))
                {
                    Ray2D r2d = new Ray2D(ray.origin, ray.direction);
                    RaycastHit2D h = Physics2D.Raycast(r2d.origin, r2d.direction);
                    if (h.collider == col2D)
                        hits.Add((h.point, obj));
                }
                // MeshFilter
                else if (obj.TryGetComponent(out MeshFilter mf))
                {
                    Mesh mesh = mf.sharedMesh;
                    if (mesh)
                    {
                        Transform t = mf.transform;
                        Matrix4x4 w2L = t.worldToLocalMatrix;
                        Ray localRay = new Ray(w2L.MultiplyPoint(ray.origin),
                            w2L.MultiplyVector(ray.direction));

                        if (!RayIntersectsMesh(localRay, mesh, out List<Vector3> localPts)) continue;
                        foreach (Vector3 p in localPts)
                            hits.Add((t.TransformPoint(p), obj));
                    }
                }
                // SkinnedMeshRenderer
                else if (obj.TryGetComponent(out SkinnedMeshRenderer skin))
                {
                    Mesh mesh = skin.sharedMesh;
                    if (mesh)
                    {
                        Transform t = skin.transform;
                        Matrix4x4 w2L = t.worldToLocalMatrix;
                        Ray localRay = new Ray(w2L.MultiplyPoint(ray.origin),
                            w2L.MultiplyVector(ray.direction));

                        if (RayIntersectsMesh(localRay, mesh, out List<Vector3> localPts))
                        {
                            foreach (Vector3 p in localPts)
                                hits.Add((t.TransformPoint(p), obj));
                        }
                    }
                }
            }

            // Sort by distance
            hits = hits.OrderBy(h => (ray.origin - h.hitPoint).sqrMagnitude).ToList();
            return hits.Count > 0;
        }


        public void Clear()
        {
            _objects.Clear();
            LeftChild?.Clear();
            RightChild?.Clear();
            LeftChild = null;
            RightChild = null;
        }


        private static bool RayIntersectsMesh(Ray ray,
            Mesh mesh,
            out List<Vector3> hitPoints)
        {
            hitPoints = new List<Vector3>();
            if (!mesh) return false;

            Vector3[] v = mesh.vertices;
            int[] tri = mesh.triangles;

            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 v0 = v[tri[i]];
                Vector3 v1 = v[tri[i + 1]];
                Vector3 v2 = v[tri[i + 2]];

                if (RayIntersectsTriangle(ray, v0, v1, v2, out Vector3 p))
                    hitPoints.Add(p);
            }

            return hitPoints.Count > 0;
        }


        private static bool RayIntersectsTriangle(Ray ray,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            out Vector3 hit)
        {
            hit = Vector3.zero;
            const float eps = 1e-8f;

            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, e2);
            float a = Vector3.Dot(e1, h);

            if (Mathf.Abs(a) < eps) return false;

            float f = 1f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0f || u + v > 1f) return false;

            float t = f * Vector3.Dot(e2, q);
            if (t > eps)
            {
                hit = ray.origin + ray.direction * t;
                return true;
            }

            return false;
        }
    }
}