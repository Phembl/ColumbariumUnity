/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/


using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace TelePresent.SoundShapes
{
    [Serializable]
    public class SoundShapes_EditorStartupHelper : ScriptableObject
    {
        private static SoundShapes_EditorStartupHelper singletonInstance;

        public static SoundShapes_EditorStartupHelper Singleton
        {
            get
            {
                if (singletonInstance == null)
                {
                    singletonInstance = Resources.Load<SoundShapes_EditorStartupHelper>("SoundShapes_EditorStartupHelper");
                    if (singletonInstance == null)
                    {
                        singletonInstance = CreateInstance<SoundShapes_EditorStartupHelper>();
                    }
                }
                return singletonInstance;
            }
        }

        [SerializeField] private bool displayWelcomeMessageOnLaunch = true;
        [SerializeField] private bool firstInitialization = true;

        public static bool DisplayWelcomeOnLaunch
        {
            get => Singleton.displayWelcomeMessageOnLaunch;
            set
            {
                if (value != Singleton.displayWelcomeMessageOnLaunch)
                {
                    Singleton.displayWelcomeMessageOnLaunch = value;
                    PersistStartupPreferences();
                }
            }
        }

        public static bool FirstInitialization
        {
            get => Singleton.firstInitialization;
            set
            {
                if (value != Singleton.firstInitialization)
                {
                    Singleton.firstInitialization = value;
                    PersistStartupPreferences();
                }
            }
        }

        public static void PersistStartupPreferences()
        {
            if (!AssetDatabase.Contains(Singleton))
            {
                var temporaryCopy = CreateInstance<SoundShapes_EditorStartupHelper>();
                EditorUtility.CopySerialized(Singleton, temporaryCopy);

                string assetPath = "Assets/TelePresent/Sound Shapes/Scripts/Resources/SoundShapes_EditorStartupHelper.asset";

                singletonInstance = Resources.Load<SoundShapes_EditorStartupHelper>("SoundShapes_EditorStartupHelper");
                if (singletonInstance == null)
                {
                    Debug.Log("Creating new SoundShapes_EditorStartupHelper asset");
                    AssetDatabase.CreateAsset(temporaryCopy, assetPath);
                    AssetDatabase.Refresh();
                    singletonInstance = temporaryCopy;
                    return;
                }
                EditorUtility.CopySerialized(temporaryCopy, singletonInstance);
            }
            EditorUtility.SetDirty(Singleton);
        }
    }
}
#endif