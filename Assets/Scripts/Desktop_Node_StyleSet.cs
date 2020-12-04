using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Desktop_Node_SharedProperties", menuName = "ScriptableObjects/Node_Style_", order = 1000)]
public class Desktop_Node_StyleSet : ScriptableObject
{
    public enum LeafIcons{Unknown, Generic_IOT, Camera, Doc, Image}
    public Color SelectedColor;
    public Color DeselectedColor;
    public Color PasswordFail;

    public Sprite UnvisitedNode;
    public Sprite EmptyNode;
    public Sprite ExhaustedNode;

    public float NodeAnimFlyoutTime = .15f;

    public float ButtonAnimFlyoutTime = .25f;
    public float ButtonAnimFlyoutFirstDelay = .1f;
    public float ButtonFlyoutDelay = .1f;

    public float ButtonAnimSquashTime = .15f;

    public Color AlreadyUsedLeaf;

    public Button button_Node_Prefab;
    public Desktop_ListButton button_NodeContents_Prefab;

    public Sprite LeafIcon_Unknown;
    public Sprite LeafIcon_Generic_IOT;
    public Sprite LeafIcon_Camera;
    public Sprite LeafIcon_Doc;
}
