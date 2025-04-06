/*******************************************************
Product - Sound Shapes
  Publisher - TelePresent Games
              http://TelePresentGames.dk
  Author    - Martin Hansen
  Created   - 2025
  (c) 2025 Martin Hansen. All rights reserved.
/*******************************************************/

using UnityEngine;

namespace TelePresent.SoundShapes
{

    [System.Serializable]
    public class SoundShapes_CachedMeshData
    {
        public Mesh meshReference;
        public Vector3[] vertices;
        public int[] triangles;
    }
}