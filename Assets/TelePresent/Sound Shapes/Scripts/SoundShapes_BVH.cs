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
    public class SoundShapes_BVH
    {
        private SoundShapes_BVHNode root;

        public SoundShapes_BVH(SoundShapes_BVHNode[] nodes)
        {
            root = BuildBVH(nodes);
        }

        private SoundShapes_BVHNode BuildBVH(SoundShapes_BVHNode[] nodes)
        {
            if (nodes.Length == 1)
                return nodes[0];

            Bounds totalBounds = CalculateTotalBounds(nodes);
            int axis = FindLargestAxis(totalBounds.size);

            // Sort nodes along the selected axis.
            System.Array.Sort(nodes, (a, b) => a.Bounds.center[axis].CompareTo(b.Bounds.center[axis]));

            int mid = nodes.Length / 2;
            SoundShapes_BVHNode left = BuildBVH(nodes.Take(mid).ToArray());
            SoundShapes_BVHNode right = BuildBVH(nodes.Skip(mid).ToArray());

            // Create a parent node with combined bounds.
            SoundShapes_BVHNode parent = new SoundShapes_BVHNode(null)
            {
                LeftChild = left,
                RightChild = right,
                Bounds = CalculateTotalBounds(new SoundShapes_BVHNode[] { left, right })
            };

            return parent;
        }

        private Bounds CalculateTotalBounds(SoundShapes_BVHNode[] nodes)
        {
            Bounds totalBounds = new Bounds(nodes[0].Bounds.center, nodes[0].Bounds.size);
            foreach (SoundShapes_BVHNode node in nodes)
                totalBounds.Encapsulate(node.Bounds);
            return totalBounds;
        }

        private int FindLargestAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
                return 0; // X-axis
            if (size.y >= size.z)
                return 1; // Y-axis
            return 2; // Z-axis
        }

        /// <summary>
        /// Traverses the BVH and returns nodes whose bounding boxes intersect the ray,
        /// sorted by the ray’s origin distance.
        /// </summary>
        public List<SoundShapes_BVHNode> Traverse(Ray ray)
        {
            List<(SoundShapes_BVHNode node, float distance)> potentialHits = new List<(SoundShapes_BVHNode, float)>();
            TraverseRecursive(root, ray, potentialHits);
            return potentialHits.OrderBy(hit => hit.distance).Select(hit => hit.node).ToList();
        }

        public void Clear()
        {
            root?.Clear();
            root = null;
        }

        private void TraverseRecursive(SoundShapes_BVHNode node, Ray ray, List<(SoundShapes_BVHNode node, float distance)> potentialHits)
        {
            if (node == null)
                return;

            if (node.Bounds.IntersectRay(ray, out float distance))
            {
                if (node.IsLeaf)
                {
                    potentialHits.Add((node, distance));
                }
                else
                {
                    TraverseRecursive(node.LeftChild, ray, potentialHits);
                    TraverseRecursive(node.RightChild, ray, potentialHits);
                }
            }
        }
    }
}