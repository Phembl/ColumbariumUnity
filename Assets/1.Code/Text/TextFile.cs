using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewTextFile", menuName = "Text/NewTextFile")]
public class TextFile : ScriptableObject
{
    [Title ("Text")]
    [HideLabel]
    [MultiLineProperty(20)]
    public string text;
    
    [Title ("Text English")]
    [HideLabel]
    [MultiLineProperty(20)]
    public string textEng;
}
