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
    private static int sSelectedShapeIndex = -1;
    private static int sSelectedMultiEmitterIndex = -1;
    private static SoundShapesBvh bvh;

    #region BVH Rebuild

    public static void RebuildBvh()
    {
        // Clear previous BVH data
        if (bvh != null)
        {
            bvh.Clear();
            bvh = null;
        }

        // Gather nodes from GameObjects that have a MeshFilter with a valid sharedMesh
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<SoundShapesBvhNode> nodeList = new List<SoundShapesBvhNode>();
        foreach (var go in allGameObjects)
        {
            if (go.TryGetComponent(out MeshFilter mf) && mf.sharedMesh)
                nodeList.Add(new SoundShapesBvhNode(go));
        }
        // Build a new BVH from the gathered nodes
        bvh = new SoundShapesBvh(nodeList.ToArray());
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
    bool inDrawingMode =
        (zone.mode == AudioZone.ZoneMode.Shape && isShapeDrawing) ||
        (zone.mode == AudioZone.ZoneMode.MultiEmitter && isMultiEmitterDrawing);

    if (!inDrawingMode)
    {
        HandleExistingPoints(zone);
        return;
    }

    Handles.BeginGUI();

    const float windowWidth  = 300f;
    float windowHeight = 175f;
    const float padding      = 15f;
    
    if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.in2DMode)
        windowHeight += 25f;
    
    boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.85f));
    boxStyle.padding           = new RectOffset(10, 10, 10, 10);

    GUILayout.BeginArea(new Rect(padding + 35, padding, windowWidth, windowHeight), boxStyle);

        GUILayout.Space(5);
        GUI.color = new Color(0.9f, 0.9f, 1f);
        GUILayout.Label(
            zone.mode == AudioZone.ZoneMode.Shape
                ? "SHAPE DRAWING MODE"
                : "MULTI‑EMITTER DRAWING MODE",
            titleStyle);
        GUI.color = Color.white;
        GUILayout.Space(5);

        labelStyle.wordWrap = true;
        labelStyle.fontSize = 11;
        GUILayout.Label("• Left‑click: Add/insert point\n• ESC: Stop drawing\n• Delete: Remove point", labelStyle);
        GUILayout.Space(10);

        GUILayout.BeginVertical();

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

                GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button("Clear", GUILayout.Height(24)))
                {
                    Undo.RecordObject(zone, "Clear Points");
                    if (zone.mode == AudioZone.ZoneMode.Shape)
                        zone.points.Clear();
                    else
                        zone.multiEmitterPoints.Clear();

                    sSelectedShapeIndex        = -1;
                    sSelectedMultiEmitterIndex = -1;
                    EditorUtility.SetDirty(zone);
                }

                GUI.backgroundColor = prevColor;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
                GUI.backgroundColor = SoundShapesSettings.DrawOnMesh ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
                if (GUILayout.Button("Draw on Mesh", GUILayout.Width(100)))
                    SoundShapesSettings.DrawOnMesh = !SoundShapesSettings.DrawOnMesh;

                GUI.backgroundColor = prevColor;
                GUILayout.Space(5);
                GUILayout.Label("Height Offset:", GUILayout.Width(80));
                SoundShapesSettings.DrawMeshHeightOffset =
                    EditorGUILayout.FloatField(SoundShapesSettings.DrawMeshHeightOffset, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
                var smallButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };
                GUI.backgroundColor = SoundShapesSettings.DrawOnCollider ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
                if (GUILayout.Button("Draw on Collider", smallButtonStyle, GUILayout.Width(100)))
                    SoundShapesSettings.DrawOnCollider = !SoundShapesSettings.DrawOnCollider;
                GUI.backgroundColor = prevColor;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
                GUILayout.Label("2D Z‑Depth:", GUILayout.Width(80));
                float newDepth = EditorGUILayout.FloatField(SoundShapesSettings.TwoDZDepth, GUILayout.Width(60));
                if (!Mathf.Approximately(newDepth, SoundShapesSettings.TwoDZDepth))
                    SoundShapesSettings.TwoDZDepth = newDepth;
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

    private static void DrawShapeOutline(AudioZone zone)
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
            Handles.DrawAAPolyLine(2f, worldPoints[^1], worldPoints[0]);
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
            sSelectedShapeIndex >= 0 && sSelectedShapeIndex < zone.points.Count)
        {
            Undo.RecordObject(zone, "Delete Shape Point");
            zone.points.RemoveAt(sSelectedShapeIndex);
            EditorUtility.SetDirty(zone);
            sSelectedShapeIndex = -1;
            e.Use();
            return;
        }

        Transform zoneTransform = zone.transform;
        for (int i = 0; i < zone.points.Count; i++)
        {
            Vector3 worldPos = zoneTransform.TransformPoint(zone.points[i]);
            float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.08f;
            if (Handles.Button(worldPos, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap))
                sSelectedShapeIndex = i;

            if (i == sSelectedShapeIndex)
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
            sSelectedMultiEmitterIndex >= 0 && sSelectedMultiEmitterIndex < zone.multiEmitterPoints.Count)
        {
            Undo.RecordObject(zone, "Delete Multi Emitter Point");
            zone.multiEmitterPoints.RemoveAt(sSelectedMultiEmitterIndex);
            EditorUtility.SetDirty(zone);
            sSelectedMultiEmitterIndex = -1;
            e.Use();
            return;
        }

        Transform zoneTransform = zone.transform;
        for (int i = 0; i < zone.multiEmitterPoints.Count; i++)
        {
            Vector3 worldPt = zoneTransform.TransformPoint(zone.multiEmitterPoints[i]);
            float handleSize = HandleUtility.GetHandleSize(worldPt) * 0.08f;
            if (Handles.Button(worldPt, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap))
                sSelectedMultiEmitterIndex = i;

            if (i == sSelectedMultiEmitterIndex)
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

private static Vector3 GetDrawingPoint(AudioZone zone, Plane drawingPlane, Ray ray)
{
    Vector3 meshHitPoint   = Vector3.zero;
    bool    meshHitFound   = false;
    float   meshHitDist    = float.MaxValue;

    if (SoundShapesSettings.DrawOnMesh &&
        TryBvhRaycast(ray, out meshHitPoint))
    {
        meshHitPoint.y += SoundShapesSettings.DrawMeshHeightOffset;
        meshHitDist     = Vector3.Distance(ray.origin, meshHitPoint);
        meshHitFound    = true;
    }

    Vector3 colliderHitPoint = Vector3.zero;
    bool    colliderHitFound = false;
    float   colliderHitDist  = float.MaxValue;

    if (SoundShapesSettings.DrawOnCollider &&
        Physics.Raycast(ray, out RaycastHit hit))
    {
        colliderHitPoint = hit.point;
        colliderHitDist  = hit.distance;
        colliderHitFound = true;
    }

    Vector3 worldPoint = Vector3.zero;
    bool    gotPoint   = false;

    if (meshHitFound && colliderHitFound)
    {
        worldPoint = meshHitDist <= colliderHitDist ? meshHitPoint : colliderHitPoint;
        gotPoint   = true;
    }
    else if (meshHitFound)
    {
        worldPoint = meshHitPoint;
        gotPoint   = true;
    }
    else if (colliderHitFound)
    {
        worldPoint = colliderHitPoint;
        gotPoint   = true;
    }
    else if (drawingPlane.Raycast(ray, out float enter))
    {
        worldPoint   = ray.GetPoint(enter);
        worldPoint.y = zone.transform.position.y;
        gotPoint     = true;
    }

    if (SceneView.lastActiveSceneView &&
        SceneView.lastActiveSceneView.in2DMode)
    {
        if (!gotPoint)
        {
            float dirZ = ray.direction.z;
            if (Mathf.Abs(dirZ) >= 1e-6f)
            {
                float t = (SoundShapesSettings.TwoDZDepth - ray.origin.z) / dirZ;
                worldPoint = ray.origin + ray.direction * t;
            }
            else
            {
                worldPoint = ray.origin;
            }
        }

        worldPoint.z = SoundShapesSettings.TwoDZDepth;
    }

    return worldPoint;
}


    private static bool TryBvhRaycast(Ray ray, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        if (bvh == null)
            return false;

        List<(float distance, Vector3 point, GameObject obj)> hits = new List<(float, Vector3, GameObject)>();
        List<SoundShapesBvhNode> potentialHits = bvh.Traverse(ray);
        if (potentialHits == null || potentialHits.Count == 0)
            return false;

        foreach (var node in potentialHits)
        {
            if (node.TryRaycast(ray, out List<(Vector3, GameObject)> nodeHits))
            {
                foreach (var (nodeHitPoint, nodeHitObj) in nodeHits)
                {
                    if (!nodeHitObj)
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

    private static bool TryInsertPointOnSegment(AudioZone zone, Vector3 worldPoint, float threshold)
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
                if (zone.points != null)
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
            for (var i = 0; i < indices.Length; i += 3)
            {
                Vector3[] triangle = new Vector3[]
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

    private static Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
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