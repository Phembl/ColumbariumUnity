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
    public class SoundShapesBvh
    {
        private SoundShapesBvhNode _root;

        public SoundShapesBvh(SoundShapesBvhNode[] nodes)
        {
            if (nodes == null || nodes.Length == 0)
            {
                _root = null;
                return;
            }
            _root = BuildBvh(nodes);
        }

        private SoundShapesBvhNode BuildBvh(SoundShapesBvhNode[] nodes)
        {
            if (nodes.Length == 1)
                return nodes[0];

            UnityEngine.Bounds totalBounds = CalculateTotalBounds(nodes);
            int axis = FindLargestAxis(totalBounds.size);

            // Sort nodes along the chosen axis.
            System.Array.Sort(nodes,
                (a, b) => a.NodeBounds.center[axis].CompareTo(b.NodeBounds.center[axis]));

            int mid = nodes.Length / 2;
            SoundShapesBvhNode left  = BuildBvh(nodes.Take(mid).ToArray());
            SoundShapesBvhNode right = BuildBvh(nodes.Skip(mid).ToArray());

            // Parent node with combined bounds.
            SoundShapesBvhNode parent = new SoundShapesBvhNode(null)
            {
                LeftChild  = left,
                RightChild = right,
                NodeBounds = CalculateTotalBounds(new[] { left, right })
            };

            return parent;
        }

        private UnityEngine.Bounds CalculateTotalBounds(SoundShapesBvhNode[] nodes)
        {
            UnityEngine.Bounds total =
                new UnityEngine.Bounds(nodes[0].NodeBounds.center, nodes[0].NodeBounds.size);

            foreach (SoundShapesBvhNode n in nodes)
                total.Encapsulate(n.NodeBounds);

            return total;
        }

        private int FindLargestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return 0; // X
            if (size.y >= size.z)                    return 1; // Y
            return 2;                                           // Z
        }

        /// Returns all BVH nodes whose bounds intersect the ray,
        public List<SoundShapesBvhNode> Traverse(Ray ray)
        {
            var potential = new List<(SoundShapesBvhNode node, float dist)>();
            TraverseRecursive(_root, ray, potential);

            return potential
                .OrderBy(h => h.dist)
                .Select(h => h.node)
                .ToList();
        }

        public void Clear()
        {
            _root?.Clear();
            _root = null;
        }

        private void TraverseRecursive(SoundShapesBvhNode node,
                                       Ray ray,
                                       List<(SoundShapesBvhNode node, float dist)> hits)
        {
            if (node == null) return;

            if (node.NodeBounds.IntersectRay(ray, out float dist))
            {
                if (node.IsLeaf)
                {
                    hits.Add((node, dist));
                }
                else
                {
                    TraverseRecursive(node.LeftChild,  ray, hits);
                    TraverseRecursive(node.RightChild, ray, hits);
                }
            }
        }
    }
}
