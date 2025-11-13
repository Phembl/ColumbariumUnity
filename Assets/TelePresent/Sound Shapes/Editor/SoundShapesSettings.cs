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
        private const string KDrawOnMeshKey = "AudioZoneSettings_DrawOnMesh";
        private const string KDrawOnColliderKey = "AudioZoneSettings_DrawOnCollider";
        private const string KDrawMeshHeightOffsetKey = "AudioZoneSettings_DrawMeshHeightOffset";
        private const string k_2DZDepth = "AudioZoneSettings_2DZDepth";


        public static bool DrawOnMesh
        {
            get => EditorPrefs.GetBool(KDrawOnMeshKey, true);
            set => EditorPrefs.SetBool(KDrawOnMeshKey, value);
        }

        public static bool DrawOnCollider
        {
            get => EditorPrefs.GetBool(KDrawOnColliderKey, true);
            set => EditorPrefs.SetBool(KDrawOnColliderKey, value);
        }

        public static float DrawMeshHeightOffset
        {
            get => EditorPrefs.GetFloat(KDrawMeshHeightOffsetKey, 0.1f);
            set => EditorPrefs.SetFloat(KDrawMeshHeightOffsetKey, value);
        }
        
        public static float  TwoDZDepth
        {
            get => EditorPrefs.GetFloat(k_2DZDepth, 0f);
            set => EditorPrefs.SetFloat(k_2DZDepth, value);
        }
    }
}