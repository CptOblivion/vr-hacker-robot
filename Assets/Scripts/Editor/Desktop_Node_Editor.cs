using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Desktop_Node))]
[CanEditMultipleObjects]
public class Desktop_Node_Editor: Editor
{
    SerializedProperty NodeName;
    SerializedProperty PIN;
    SerializedProperty leafFrameContentsTemp;
    SerializedProperty nodeStyle;
    SerializedProperty Leaves;

    int SelectedLeaf;


    private void OnEnable()
    {
        NodeName = serializedObject.FindProperty("NodeName");
        PIN = serializedObject.FindProperty("PIN");
        leafFrameContentsTemp = serializedObject.FindProperty("leafFrameContentsTemp");
        nodeStyle = serializedObject.FindProperty("nodeStyle");
        Leaves = serializedObject.FindProperty("Leaves");

        SelectedLeaf = -1;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(NodeName);
        EditorGUILayout.PropertyField(PIN);
        EditorGUILayout.PropertyField(nodeStyle);
        GUILayout.BeginVertical(EditorStyles.helpBox); //the box itself
        {
            EditorGUILayout.LabelField("Node Contents", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            GUILayout.BeginHorizontal(); //columns for the node list and contents
            {

                Rect NodeList = EditorGUILayout.BeginVertical("Box", GUILayout.Width(150)); //leaf list and controls
                {
                    for (int i = 0; i < Leaves.arraySize; i++)
                    {
                        if (i == SelectedLeaf)
                        {
                            EditorGUILayout.LabelField(Leaves.GetArrayElementAtIndex(i).FindPropertyRelative("LeafName").stringValue, GUILayout.Width(145));
                        }
                        else
                        {
                            if (GUILayout.Button(Leaves.GetArrayElementAtIndex(i).FindPropertyRelative("LeafName").stringValue))
                            {
                                //TODO: draw icon on button
                                SelectedLeaf = i;
                            }
                        }
                    }
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("  +  ", GUILayout.Width(45)))
                        {
                            if (SelectedLeaf == -1)
                            {
                                Leaves.InsertArrayElementAtIndex(Leaves.arraySize);
                                if (Leaves.arraySize == 1)
                                {
                                    Leaves.GetArrayElementAtIndex(0).FindPropertyRelative("LeafName").stringValue = "Dummy Node";
                                    Leaves.GetArrayElementAtIndex(0).FindPropertyRelative("leafIcon").enumValueIndex = (int)Desktop_Node_StyleSet.LeafIcons.Unknown;
                                }
                                SelectedLeaf = Leaves.arraySize - 1;
                            }
                            else
                            {
                                Leaves.InsertArrayElementAtIndex(SelectedLeaf);
                                SelectedLeaf++;
                            }
                        }
                        EditorGUI.BeginDisabledGroup(Leaves.arraySize == 0);
                        {
                            if (GUILayout.Button("  -  ", GUILayout.Width(45)))
                            {
                                if (SelectedLeaf > -1)
                                {
                                    Leaves.DeleteArrayElementAtIndex(SelectedLeaf);
                                    SelectedLeaf -= 1;
                                }
                                else
                                {
                                    Leaves.DeleteArrayElementAtIndex(Leaves.arraySize - 1);
                                }
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.BeginDisabledGroup(SelectedLeaf < 1);
                        {
                            if (GUILayout.Button(" ^ ", GUILayout.Width(25)))
                            {
                                Leaves.MoveArrayElement(SelectedLeaf, SelectedLeaf - 1);
                                SelectedLeaf--;
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.BeginDisabledGroup(SelectedLeaf == -1 || SelectedLeaf == Leaves.arraySize - 1);
                        {
                            if (GUILayout.Button(" v ", GUILayout.Width(25)))
                            {
                                Leaves.MoveArrayElement(SelectedLeaf, SelectedLeaf + 1);
                                SelectedLeaf++;
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                
                //TODO: actually use DragLocation so we can drop the new device between or onto other nodes
                NetworkDevice nd = DragHandler.NetworkDeviceDrag(NodeList, out Vector2?  DragLocation);
                if (nd)
                {
                    Leaves.InsertArrayElementAtIndex(Leaves.arraySize);
                    SerializedProperty newLeaf = Leaves.GetArrayElementAtIndex(Leaves.arraySize - 1);
                    newLeaf.FindPropertyRelative("leafType").enumValueIndex = (int)Desktop_Node.Node_Leaf.LeafTypes.NetworkDevice;
                    newLeaf.FindPropertyRelative("DeviceID").stringValue = nd.DeviceID;
                    //TODO: put this in a function so we can use it for dropping a device into the DeviceID field too
                    string leafName;
                    //TODO: in the function, don't change the name by default (so user-set names aren't changed)
                    //  but do change the name if a new node is being made, or dropped in on top of an old node wholesale
                    int leafIcon;
                    SecurityCamera cam = nd.GetComponent<SecurityCamera>();
                    if (cam)
                    {
                        leafName = cam.CameraName;
                        leafIcon = (int)Desktop_Node_StyleSet.LeafIcons.Camera;
                    }
                    else
                    {
                        leafName = nd.DeviceName;
                        leafIcon = (int)Desktop_Node_StyleSet.LeafIcons.Generic_IOT;
                    }
                    newLeaf.FindPropertyRelative("LeafName").stringValue = leafName;
                    newLeaf.FindPropertyRelative("leafIcon").enumValueIndex = leafIcon;
                    SelectedLeaf = Leaves.arraySize - 1;
                }
                else //Leaves.arraySize gets updated but the actual leaf array doesn't until it's applied, maybe? At any rate, the following throws an error unless it's canceled on the frame the leaf array is updated
                {
                    GUILayout.BeginVertical("Box", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) }); //leaf contents
                    {
                        if (SelectedLeaf > -1)
                        {
                            SerializedProperty leaf = Leaves.GetArrayElementAtIndex(SelectedLeaf);
                            EditorGUI.indentLevel--;
                            //TODO: autofill icon if dummy, doc, or image
                            GUILayout.BeginHorizontal();
                            { 
                                SerializedProperty leafIcon = leaf.FindPropertyRelative("leafIcon");
                                EditorGUIUtility.labelWidth = 75;
                                EditorGUILayout.PropertyField(leafIcon);
                                if (nodeStyle.objectReferenceValue != null)
                                {
                                    Desktop_Node_StyleSet styleSet = (Desktop_Node_StyleSet)nodeStyle.objectReferenceValue;
                                    Sprite sprite = null;
                                    switch (leafIcon.enumValueIndex)
                                    {
                                        case (int)Desktop_Node_StyleSet.LeafIcons.Unknown:
                                            {
                                                sprite = styleSet.LeafIcon_Unknown;
                                                break;
                                            }
                                        case (int)Desktop_Node_StyleSet.LeafIcons.Camera:
                                            {
                                                //TODO: figure out if the referenced device is a camera and set the icon to that
                                                sprite = styleSet.LeafIcon_Camera;
                                                break;
                                            }
                                        case (int)Desktop_Node_StyleSet.LeafIcons.Generic_IOT:
                                            {
                                                //TODO: figure out if the referenced device is a camera and set the icon to that
                                                sprite = styleSet.LeafIcon_Generic_IOT;
                                                break;
                                            }
                                        case (int)Desktop_Node_StyleSet.LeafIcons.Doc:
                                            {
                                                sprite = styleSet.LeafIcon_Doc;
                                                break;
                                            }
                                    }

                                    if (sprite)
                                    {
                                        Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(30));
                                        //EditorGUILayout.LabelField("pretend I'm drawing an image here");
                                        if (iconRect.width > iconRect.height)
                                            iconRect.width = iconRect.height;
                                        else if (iconRect.height > iconRect.width)
                                            iconRect.height = iconRect.width;
                                        GUI.DrawTexture(iconRect, sprite.texture);
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.BeginHorizontal();
                            {
                                EditorGUIUtility.labelWidth = 75;
                                EditorGUILayout.PropertyField(leaf.FindPropertyRelative("LeafName"));
                                EditorGUIUtility.labelWidth = 65;
                                EditorGUILayout.PropertyField(leaf.FindPropertyRelative("leafType"));
                                //TODO: show icons from current set in dropdown, if possible
                                //TODO: on change, update icon based on the new type

                            }
                            GUILayout.EndHorizontal();
                            
                            EditorGUIUtility.labelWidth = 40;
                            EditorGUILayout.PropertyField(leaf.FindPropertyRelative("PIN"));

                            //Network Device
                            int leafType = leaf.FindPropertyRelative("leafType").enumValueIndex;
                            if (leafType == (int)Desktop_Node.Node_Leaf.LeafTypes.NetworkDevice)
                            {
                                //TODO: there's gotta be a way to loop through all the NetworkDevices in a scene and populate a list to pick from in the editor (though drag and drop also works)
                                EditorGUIUtility.labelWidth = 65;
                                Rect fullRect = EditorGUILayout.GetControlRect();
                                Rect fieldRect = fullRect;
                                fieldRect.width = EditorGUIUtility.labelWidth;
                                EditorGUI.LabelField(fieldRect, "Device ID");
                                fieldRect = fullRect;
                                fieldRect.x += EditorGUIUtility.labelWidth;
                                fieldRect.width -= EditorGUIUtility.labelWidth;
                                nd = DragHandler.NetworkDeviceDrag(fieldRect, out _);
                                if (nd != null)
                                {
                                    leaf.FindPropertyRelative("DeviceID").stringValue = nd.DeviceID;
                                }
                                EditorGUI.PropertyField(fieldRect, leaf.FindPropertyRelative("DeviceID"), GUIContent.none);
                            }
                            else if (leafType == (int) Desktop_Node.Node_Leaf.LeafTypes.Doc)
                            {
                                EditorGUILayout.PropertyField(leaf.FindPropertyRelative("doc"));
                            }
                            EditorGUIUtility.labelWidth = 0;
                            EditorGUI.indentLevel++;
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();
        }GUILayout.EndVertical();
        EditorGUI.indentLevel--;
        serializedObject.ApplyModifiedProperties();
    }

    class DragHandler
    {
        public static NetworkDevice NetworkDeviceDrag(Rect rect, out Vector2? DragLocation)
        {
            NetworkDevice outDevice = null;
            EventType eventType = Event.current.type;
            DragLocation = null;
            if (eventType == EventType.DragUpdated|| eventType == EventType.DragPerform)
            {

                foreach(Object ob in DragAndDrop.objectReferences)
                {
                    if (ob is GameObject)
                    {
                        NetworkDevice n = ((GameObject)ob).GetComponent<NetworkDevice>();
                        if (n)
                        {
                            DragLocation = Event.current.mousePosition;
                            outDevice = n;
                            if (rect.Contains((Vector2)DragLocation))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            }
                            break;
                        }
                    }
                }
                if (eventType == EventType.DragPerform && rect.Contains(Event.current.mousePosition))
                {
                    return outDevice;
                }
            }
            return null;
        }
    }
}
