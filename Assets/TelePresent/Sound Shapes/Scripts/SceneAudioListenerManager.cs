/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace TelePresent.SoundShapes
{
    public static class SceneAudioListenerManager
    {
        public static GameObject sceneAudioListener;

        /// <summary>
        /// Updates (or creates if necessary) the scene audio listener to follow the Scene view camera.
        /// </summary>
        public static void UpdateListener()
        {
#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
                return;

            Camera sceneViewCam = SceneView.lastActiveSceneView.camera;
            if (sceneAudioListener == null)
            {
                sceneAudioListener = new GameObject("SceneViewAudioListener");
                sceneAudioListener.hideFlags = HideFlags.HideAndDontSave;
                sceneAudioListener.AddComponent<AudioListener>();
            }
            sceneAudioListener.transform.position = sceneViewCam.transform.position;
            sceneAudioListener.transform.rotation = sceneViewCam.transform.rotation;
#endif
        }

        /// <summary>
        /// Disables and destroys the scene audio listener.
        /// </summary>
        public static void DisableListener()
        {
#if UNITY_EDITOR
            if (sceneAudioListener != null)
            {
                Object.DestroyImmediate(sceneAudioListener);
                sceneAudioListener = null;
            }
#endif
        }
    }
}