/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TelePresent.SoundShapes
{
public static class AudioZoneDrawingTools
{
    private static int s_selectedShapeIndex = -1;
    private static int s_selectedMultiEmitterIndex = -1;
    public static SoundShapes_BVH bvh;

    #region BVH Rebuild

    public static void RebuildBVH()
    {
        // Clear previous BVH data
        if (bvh != null)
        {
            bvh.Clear();
            bvh = null;
        }

        // Gather nodes from GameObjects that have a MeshFilter with a valid sharedMesh
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<SoundShapes_BVHNode> nodeList = new List<SoundShapes_BVHNode>();
        foreach (var go in allGameObjects)
        {
            if (go.TryGetComponent<MeshFilter>(out MeshFilter mf) && mf.sharedMesh != null)
                nodeList.Add(new SoundShapes_BVHNode(go));
        }
        // Build a new BVH from the gathered nodes
        bvh = new SoundShapes_BVH(nodeList.ToArray());
    }

    #endregion

    #region Main Overlay

    public static void DrawSceneOverlayUI(
        AudioZone zone,
        bool isShapeDrawing,
        bool isMultiEmitterDrawing,
        Plane drawingPlane,
        GUIStyle boxStyle,
        GUIStyle titleStyle,
        GUIStyle labelStyle
    )
    {
        bool inDrawingMode = (zone.mode == AudioZone.ZoneMode.Shape && isShapeDrawing) ||
                             (zone.mode == AudioZone.ZoneMode.MultiEmitter && isMultiEmitterDrawing);
        if (!inDrawingMode)
        {
            HandleExistingPoints(zone);
            return;
        }

        Handles.BeginGUI();
        const float WINDOW_WIDTH = 300f;
        // Increased window height to allow for the additional collider toggle controls
        const float WINDOW_HEIGHT = 180f;
        const float PADDING = 15f;

        // Set overlay background style
        boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f));
        boxStyle.padding = new RectOffset(10, 10, 10, 10);

        GUILayout.BeginArea(new Rect(PADDING + 35, PADDING, WINDOW_WIDTH, WINDOW_HEIGHT), boxStyle);
        GUILayout.Space(5);
        GUI.color = new Color(0.9f, 0.9f, 1f);

        GUILayout.Label(zone.mode == AudioZone.ZoneMode.Shape ? "SHAPE DRAWING MODE" : "MULTI-EMITTER DRAWING MODE", titleStyle);
        GUI.color = Color.white;
        GUILayout.Space(5);

        labelStyle.wordWrap = true;
        labelStyle.fontSize = 11;
        GUILayout.Label(
            "• Left-click: Add/insert point\n" +
            "• ESC or exit: Stop drawing\n" +
            "• Delete key: Remove selected point",
            labelStyle
        );
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        // Shape-mode toggles: Freehand and Closed Shape
        GUILayout.BeginHorizontal();
        Color prevColor = GUI.backgroundColor;
        if (zone.mode == AudioZone.ZoneMode.Shape)
        {
            GUI.backgroundColor = zone.freehandMode ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
            if (GUILayout.Button("Freehand", GUILayout.Height(24)))
            {
                zone.freehandMode = !zone.freehandMode;
                EditorUtility.SetDirty(zone);
            }
            GUI.backgroundColor = zone.closedShape ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
            if (GUILayout.Button("Closed Shape", GUILayout.Height(24)))
            {
                zone.closedShape = !zone.closedShape;
                EditorUtility.SetDirty(zone);
            }
        }
        // Clear button for both modes
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Clear", GUILayout.Height(24)))
        {
            Undo.RecordObject(zone, "Clear Points");
            if (zone.mode == AudioZone.ZoneMode.Shape)
            {
                zone.points.Clear();
                s_selectedShapeIndex = -1;
            }
            else if (zone.mode == AudioZone.ZoneMode.MultiEmitter)
            {
                zone.multiEmitterPoints.Clear();
                s_selectedMultiEmitterIndex = -1;
            }
            EditorUtility.SetDirty(zone);
        }
        GUI.backgroundColor = prevColor;
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // Draw on Mesh toggle and offset (using persistent settings)
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = SoundShapesSettings.DrawOnMesh ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
        if (GUILayout.Button("Draw on Mesh", GUILayout.Width(100)))
        {
            SoundShapesSettings.DrawOnMesh = !SoundShapesSettings.DrawOnMesh;
        }
        GUI.backgroundColor = prevColor;
        GUILayout.Space(5);
        GUILayout.Label("Height Offset:", GUILayout.Width(80));
        SoundShapesSettings.DrawMeshHeightOffset = EditorGUILayout.FloatField(SoundShapesSettings.DrawMeshHeightOffset, GUILayout.Width(60));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Draw on Collider toggle (using persistent settings)
        GUILayout.BeginHorizontal();
        GUIStyle smallButtonStyle = new GUIStyle(GUI.skin.button);
        smallButtonStyle.fontSize = 11; // set desired font size

        GUI.backgroundColor = SoundShapesSettings.DrawOnCollider ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
        if (GUILayout.Button("Draw on Collider", smallButtonStyle, GUILayout.Width(100)))
        {
            SoundShapesSettings.DrawOnCollider = !SoundShapesSettings.DrawOnCollider;
        }
        GUI.backgroundColor = prevColor;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndArea();

        Handles.EndGUI();

        HandleDrawingMode(zone, drawingPlane);
        HandleExistingPoints(zone);
    }

    #endregion

    #region Handle Existing Points

    private static void HandleExistingPoints(AudioZone zone)
    {
        if (zone.mode == AudioZone.ZoneMode.Shape)
        {
            DrawShapeOutline(zone);
            HandleShapePointManipulation(zone);
        }
        else if (zone.mode == AudioZone.ZoneMode.MultiEmitter)
        {
            HandleMultiEmitterPointManipulation(zone);
        }
    }

    #endregion

    #region Shape-Specific Logic

    public static void DrawShapeOutline(AudioZone zone)
    {
        if (zone.points == null || zone.points.Count == 0)
            return;

        Handles.color = Color.green;
        Transform zoneTransform = zone.transform;

        if (zone.closedShape && zone.points.Count > 2)
        {
            Vector3[] worldPoints = new Vector3[zone.points.Count];
            for (int i = 0; i < zone.points.Count; i++)
                worldPoints[i] = zoneTransform.TransformPoint(zone.points[i]);

            for (int i = 0; i < worldPoints.Length - 1; i++)
                Handles.DrawAAPolyLine(2f, worldPoints[i], worldPoints[i + 1]);
            // Close the loop
            Handles.DrawAAPolyLine(2f, worldPoints[worldPoints.Length - 1], worldPoints[0]);
        }
        else
        {
            for (int i = 0; i < zone.points.Count - 1; i++)
            {
                Vector3 p1 = zoneTransform.TransformPoint(zone.points[i]);
                Vector3 p2 = zoneTransform.TransformPoint(zone.points[i + 1]);
                Handles.DrawAAPolyLine(2f, p1, p2);
            }
        }
    }

    private static void HandleShapePointManipulation(AudioZone zone)
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete &&
            s_selectedShapeIndex >= 0 && s_selectedShapeIndex < zone.points.Count)
        {
            Undo.RecordObject(zone, "Delete Shape Point");
            zone.points.RemoveAt(s_selectedShapeIndex);
            EditorUtility.SetDirty(zone);
            s_selectedShapeIndex = -1;
            e.Use();
            return;
        }

        Transform zoneTransform = zone.transform;
        for (int i = 0; i < zone.points.Count; i++)
        {
            Vector3 worldPos = zoneTransform.TransformPoint(zone.points[i]);
            float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.08f;
            if (Handles.Button(worldPos, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap))
                s_selectedShapeIndex = i;

            if (i == s_selectedShapeIndex)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.PositionHandle(worldPos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Move Shape Point");
                    zone.points[i] = zoneTransform.InverseTransformPoint(newWorld);
                    EditorUtility.SetDirty(zone);
                }
            }
        }
    }

    private static void HandleDrawingMode(AudioZone zone, Plane drawingPlane)
    {
        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        if (zone.mode == AudioZone.ZoneMode.Shape)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && HandleUtility.nearestControl == controlID)
            {
                Vector3 worldPt = GetDrawingPoint(zone, drawingPlane, HandleUtility.GUIPointToWorldRay(e.mousePosition));
                if (!TryInsertPointOnSegment(zone, worldPt, 0.3f))
                {
                    Undo.RecordObject(zone, "Add Shape Point");
                    zone.points.Add(zone.transform.InverseTransformPoint(worldPt));
                    EditorUtility.SetDirty(zone);
                }
                e.Use();
            }
            else if (zone.freehandMode && e.type == EventType.MouseDrag && e.button == 0 && !e.alt && HandleUtility.nearestControl == controlID)
            {
                Vector3 worldPt = GetDrawingPoint(zone, drawingPlane, HandleUtility.GUIPointToWorldRay(e.mousePosition));
                Undo.RecordObject(zone, "Add Freehand Point");
                zone.points.Add(zone.transform.InverseTransformPoint(worldPt));
                EditorUtility.SetDirty(zone);
                e.Use();
            }
        }
        else if (zone.mode == AudioZone.ZoneMode.MultiEmitter)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && HandleUtility.nearestControl == controlID)
            {
                Vector3 worldPt = GetDrawingPoint(zone, drawingPlane, HandleUtility.GUIPointToWorldRay(e.mousePosition));
                Undo.RecordObject(zone, "Add Multi-Emitter Point");
                zone.multiEmitterPoints.Add(zone.transform.InverseTransformPoint(worldPt));
                EditorUtility.SetDirty(zone);
                e.Use();
            }
        }
    }

    #endregion

    #region Multi-Emitter Logic

    private static void HandleMultiEmitterPointManipulation(AudioZone zone)
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete &&
            s_selectedMultiEmitterIndex >= 0 && s_selectedMultiEmitterIndex < zone.multiEmitterPoints.Count)
        {
            Undo.RecordObject(zone, "Delete Multi Emitter Point");
            zone.multiEmitterPoints.RemoveAt(s_selectedMultiEmitterIndex);
            EditorUtility.SetDirty(zone);
            s_selectedMultiEmitterIndex = -1;
            e.Use();
            return;
        }

        Transform zoneTransform = zone.transform;
        for (int i = 0; i < zone.multiEmitterPoints.Count; i++)
        {
            Vector3 worldPt = zoneTransform.TransformPoint(zone.multiEmitterPoints[i]);
            float handleSize = HandleUtility.GetHandleSize(worldPt) * 0.08f;
            if (Handles.Button(worldPt, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap))
                s_selectedMultiEmitterIndex = i;

            if (i == s_selectedMultiEmitterIndex)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.PositionHandle(worldPt, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(zone, "Move Multi Emitter Point");
                    zone.multiEmitterPoints[i] = zoneTransform.InverseTransformPoint(newWorld);
                    EditorUtility.SetDirty(zone);
                }
            }
        }
    }

    #endregion

    #region Shared Helpers

    public static Vector3 GetDrawingPoint(AudioZone zone, Plane drawingPlane, Ray ray)
    {
        // Try to get a hit point from the mesh (BVH) raycast using persistent settings
        Vector3 meshHitPoint = Vector3.zero;
        bool meshHitFound = false;
        float meshHitDistance = float.MaxValue;
        if (SoundShapesSettings.DrawOnMesh)
        {
            if (TryBVHRaycast(ray, out meshHitPoint))
            {
                meshHitPoint.y += SoundShapesSettings.DrawMeshHeightOffset;
                meshHitDistance = Vector3.Distance(ray.origin, meshHitPoint);
                meshHitFound = true;
            }
        }

        // Try to get a hit point from the collider raycast using persistent settings
        Vector3 colliderHitPoint = Vector3.zero;
        bool colliderHitFound = false;
        float colliderHitDistance = float.MaxValue;
        if (SoundShapesSettings.DrawOnCollider)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                colliderHitPoint = hit.point;
                colliderHitDistance = hit.distance;
                colliderHitFound = true;
            }
        }

        // Choose the hit point that is closest to the ray origin if both are found
        Vector3 worldPoint = Vector3.zero;
        if (meshHitFound && colliderHitFound)
        {
            worldPoint = (meshHitDistance <= colliderHitDistance) ? meshHitPoint : colliderHitPoint;
        }
        else if (meshHitFound)
        {
            worldPoint = meshHitPoint;
        }
        else if (colliderHitFound)
        {
            worldPoint = colliderHitPoint;
        }
        else
        {
            // Fall back to the drawing plane if no collider or mesh hit is found
            if (drawingPlane.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                worldPoint.y = zone.transform.position.y;
            }
        }
        return worldPoint;
    }

    private static bool TryBVHRaycast(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (bvh == null)
            return false;

        List<(float distance, Vector3 point, GameObject obj)> hits = new List<(float, Vector3, GameObject)>();
        List<SoundShapes_BVHNode> potentialHits = bvh.Traverse(ray);
        if (potentialHits == null || potentialHits.Count == 0)
            return false;

        foreach (var node in potentialHits)
        {
            if (node.TryRaycast(ray, out List<(Vector3, GameObject)> nodeHits))
            {
                foreach (var (nodeHitPoint, nodeHitObj) in nodeHits)
                {
                    if (nodeHitObj == null)
                        continue;
                    float dist = Vector3.Distance(ray.origin, nodeHitPoint);
                    hits.Add((dist, nodeHitPoint, nodeHitObj));
                }
            }
        }
        if (hits.Count > 0)
        {
            var closest = hits.OrderBy(h => h.distance).First();
            hitPoint = closest.point;
            return true;
        }
        return false;
    }

    public static bool TryInsertPointOnSegment(AudioZone zone, Vector3 worldPoint, float threshold)
    {
        int count = zone.points.Count;
        if (count < 2)
            return false;

        float bestDistance = threshold;
        int bestSegment = -1;
        Vector3 bestProjection = Vector3.zero;
        Transform t = zone.transform;
        for (int i = 0; i < count - 1; i++)
        {
            Vector3 p1 = t.TransformPoint(zone.points[i]);
            Vector3 p2 = t.TransformPoint(zone.points[i + 1]);
            Vector3 proj = ProjectPointOnLineSegment(p1, p2, worldPoint);
            float dist = Vector3.Distance(worldPoint, proj);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestSegment = i;
                bestProjection = proj;
            }
        }
        if (zone.closedShape && count >= 3)
        {
            Vector3 p1 = t.TransformPoint(zone.points[count - 1]);
            Vector3 p2 = t.TransformPoint(zone.points[0]);
            Vector3 proj = ProjectPointOnLineSegment(p1, p2, worldPoint);
            float dist = Vector3.Distance(worldPoint, proj);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestSegment = count - 1;
                bestProjection = proj;
            }
        }
        if (bestSegment == -1)
            return false;

        Undo.RecordObject(zone, "Insert Shape Point");
        Vector3 localPoint = t.InverseTransformPoint(bestProjection);
        if (bestSegment == count - 1 && zone.closedShape)
            zone.points.Add(localPoint);
        else
            zone.points.Insert(bestSegment + 1, localPoint);
        EditorUtility.SetDirty(zone);
        return true;
    }

        public static void DrawFilledShape(AudioZone zone)
        {
            int count = zone.points?.Count ?? 0;
            if (count < 3)
                return;

            Transform zoneTransform = zone.transform;
            Vector3[] worldPoints = new Vector3[count];
            for (int i = 0; i < count; i++)
                worldPoints[i] = zoneTransform.TransformPoint(zone.points[i]);

            // Compute a best-fit plane for the polygon using the first three points.
            // (This assumes the points are coplanar.)
            Vector3 normal = Vector3.Cross(worldPoints[1] - worldPoints[0], worldPoints[2] - worldPoints[0]).normalized;

            // Build an orthonormal basis for the plane.
            // Use absolute dot to handle cases where normal is nearly up or down.
            Vector3 tangent = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.right : Vector3.Cross(normal, Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            // Project world points into 2D space.
            Vector2[] poly2D = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                // Use dot products to get 2D coordinates in the plane.
                poly2D[i] = new Vector2(Vector3.Dot(worldPoints[i], tangent), Vector3.Dot(worldPoints[i], bitangent));
            }

            // Triangulate the polygon using ear clipping.
            int[] indices = Triangulate(poly2D);
            if (indices == null || indices.Length < 3)
                return;

            // Draw each triangle
            Color fillColor = new Color(0.2f, 1f, 0.2f, 0.2f);
            Handles.color = fillColor;
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3[] triangle = new Vector3[3]
                {
            worldPoints[indices[i]],
            worldPoints[indices[i + 1]],
            worldPoints[indices[i + 2]]
                };

                // Check for degenerate triangles
                Vector3 edge1 = triangle[1] - triangle[0];
                Vector3 edge2 = triangle[2] - triangle[0];
                if (Vector3.Cross(edge1, edge2).sqrMagnitude > 0.0001f)
                {
                    Handles.DrawAAConvexPolygon(triangle);
                }
            }

            // Draw outline for clarity
            Handles.color = new Color(0.2f, 1f, 0.2f, 0.8f);
            Handles.DrawPolyLine(worldPoints);
            Handles.DrawLine(worldPoints[count - 1], worldPoints[0]);
        }

        // Simple ear clipping triangulation algorithm for concave polygons
        private static int[] Triangulate(Vector2[] points)
        {
            List<int> indices = new List<int>();
            List<Vector2> polygon = new List<Vector2>(points);
            List<int> vertexIndices = new List<int>();

            // Initialize vertex indices
            for (int i = 0; i < points.Length; i++)
                vertexIndices.Add(i);

            int n = points.Length;

            while (n > 3)
            {
                bool earFound = false;
                // Iterate over all vertices looking for an ear.
                for (int i = 0; i < n; i++)
                {
                    int a = vertexIndices[i];
                    int b = vertexIndices[(i + 1) % n];
                    int c = vertexIndices[(i + 2) % n];

                    // Check if vertex b forms an ear with a and c
                    if (IsEar(polygon, vertexIndices, n, i))
                    {
                        // Add triangle indices for this ear.
                        indices.Add(a);
                        indices.Add(b);
                        indices.Add(c);

                        // Remove vertex b and update the count.
                        vertexIndices.RemoveAt((i + 1) % n);
                        n--;
                        earFound = true;
                        break;
                    }
                }

                // Protection: If no ear is found in this pass, break out early.
                if (!earFound)
                {
                    Debug.LogWarning("Triangulation failed: No valid ear found. The polygon may be degenerate.");
                    return null;
                }
            }

            // Add the final triangle.
            if (vertexIndices.Count == 3)
            {
                indices.Add(vertexIndices[0]);
                indices.Add(vertexIndices[1]);
                indices.Add(vertexIndices[2]);
            }

            return indices.ToArray();
        }


        private static bool IsEar(List<Vector2> polygon, List<int> vertexIndices, int n, int i)
    {
        int a = vertexIndices[i];
        int b = vertexIndices[(i + 1) % n];
        int c = vertexIndices[(i + 2) % n];

        Vector2 va = polygon[a];
        Vector2 vb = polygon[b];
        Vector2 vc = polygon[c];

        // Check if vertex b forms a convex angle
        if (!IsConvex(va, vb, vc))
            return false;

        // Check if any other vertex is inside the triangle
        for (int j = 0; j < n; j++)
        {
            if (j == i || j == (i + 1) % n || j == (i + 2) % n)
                continue;

            int p = vertexIndices[j];
            if (IsPointInTriangle(polygon[p], va, vb, vc))
                return false;
        }

        return true;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        return Cross(b - a, c - b) >= 0;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Compute barycentric coordinates
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
    public static Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        return a + Mathf.Clamp01(t) * ab;
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Texture2D result = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = col;
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    #endregion
}
}