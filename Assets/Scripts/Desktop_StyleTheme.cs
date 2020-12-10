using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Desktop_Style Theme_", menuName = "ScriptableObjects/Desktop_Style Theme", order = 1000)]
public class Desktop_StyleTheme : ScriptableObject
{
    public Color WindowTitlebarColorActive;
    public Color WindowTitlebarColorInactive;

    public Sprite windowCloseButton;
    public Sprite windowMinimizeButton;

    public Sprite cursorPointer;
    public Sprite cursorHyperlink;
    public Sprite cursorDragX;
    public Sprite cursorDragY;
    public Sprite cursorDragXY;
    public Sprite cursorLoading;
    //TODO: colors for buttons and stuff
    //TODO: fonts
}
