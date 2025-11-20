using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTextHolder", menuName = "Text/TextHolder")]
public class TextHolder : ScriptableObject
{
    [Title ("Text")]
    [HideLabel]
    [MultiLineProperty(10)]
    public string[] text;
}
