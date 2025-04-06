/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEditor;
namespace TelePresent.SoundShapes
{
    public static class SoundShapesSettings
    {
        private const string kDrawOnMeshKey = "AudioZoneSettings_DrawOnMesh";
        private const string kDrawOnColliderKey = "AudioZoneSettings_DrawOnCollider";
        private const string kDrawMeshHeightOffsetKey = "AudioZoneSettings_DrawMeshHeightOffset";

        public static bool DrawOnMesh
        {
            get { return EditorPrefs.GetBool(kDrawOnMeshKey, true); }
            set { EditorPrefs.SetBool(kDrawOnMeshKey, value); }
        }

        public static bool DrawOnCollider
        {
            get { return EditorPrefs.GetBool(kDrawOnColliderKey, true); }
            set { EditorPrefs.SetBool(kDrawOnColliderKey, value); }
        }

        public static float DrawMeshHeightOffset
        {
            get { return EditorPrefs.GetFloat(kDrawMeshHeightOffsetKey, 0.1f); }
            set { EditorPrefs.SetFloat(kDrawMeshHeightOffsetKey, value); }
        }
    }
}