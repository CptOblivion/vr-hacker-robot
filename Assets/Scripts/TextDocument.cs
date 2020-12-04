using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DOC_", menuName = "ScriptableObjects/TextDocument", order = 999)]
public class TextDocument : ScriptableObject
{
    public string Title;
    [TextArea(5,100)]
    public string Contents;
}


