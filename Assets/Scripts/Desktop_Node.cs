using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Desktop_Node : MonoBehaviour
{
    public class NodeIndexEvent: UnityEvent<int> { }
    [System.Serializable]
    public class Node_Leaf
    {
        public string LeafName = "[Untitled Data]";
        public enum LeafTypes { Dummy, NetworkDevice, Doc, Image }
        public LeafTypes leafType;
        public Desktop_Node_StyleSet.LeafIcons leafIcon;
        public int PIN = -1;
        [HideInInspector]
        public bool Revealed = false;
        [HideInInspector]
        public Desktop_ListButton button;

        //TODO: add a leaf icon slot- auto-fill for image/text/dummy, manually set for NetworkDevice (EG camera/lightswitch/door lock/etc)
        //TODO: store node map in a file separate from scene, generate nodes at runtime

        public string DeviceID;
        public TextDocument doc;
        //TODO: image viewer
        //  with "set as wallpaper" option, of course
    }
    public string NodeName = "[Untitled Node]";
    public int PIN = -1;

    public Desktop_Node_StyleSet nodeStyle;

    RectTransform parentLine;
    public Node_Leaf[] Leaves;
    bool Revealed = false;

    Button button;
    Image buttonImage;

    public static Desktop_Node rootNode;
    static Desktop_Node selected = null;

    private void Awake()
    {
        button = Instantiate(nodeStyle.button_Node_Prefab, transform).GetComponent<Button>();
        button.onClick.AddListener(delegate { Clicked(-1); });
        buttonImage = button.GetComponent<Image>();
        button.transform.SetAsLastSibling();

        if (!transform.parent.GetComponent<Desktop_Node>())
        {
            rootNode = this;
            UpdateFrameBounds(true);
            buttonImage.color = nodeStyle.DeselectedColor;
        }
        else
        {
            InstantiateParentLine();
            gameObject.SetActive(false);
        }
        Node_Leaf leaf;
        for (int i = 0; i < Leaves.Length; i++)
        {
            leaf = Leaves[i];
            if (leaf.leafType == Node_Leaf.LeafTypes.Dummy)
            {
                leaf.Revealed = true;
            }
        }
    }
    private void OnDestroy()
    {
        if (parentLine)
        {
            Destroy(parentLine.gameObject);
        }
    }
    public void Clicked(int LeafIndex)
    {

        if (LeafIndex < 0)
        {
            if (PIN > 0)
            {
                DeselectNode();
                Desktop_PINPad.SummonNumpad(PIN, -1, transform.position);
                Desktop_PINPad.OnPasswordSuccess.AddListener(OnPasswordSuccess);
                Desktop_PINPad.OnPasswordFail.AddListener(OnPasswordFail);
            }
            else
            {
                SetSelected();
            }
        }
        else
        {
            Node_Leaf leaf = Leaves[LeafIndex];
            if (leaf.PIN > 0)
            {
                Desktop_PINPad.SummonNumpad(leaf.PIN, LeafIndex, leaf.button.transform.position);
                Desktop_PINPad.OnPasswordSuccess.AddListener(OnPasswordSuccess);
                Desktop_PINPad.OnPasswordFail.AddListener(OnPasswordFail);
            }
            else
            {
                if (leaf.leafType == Node_Leaf.LeafTypes.NetworkDevice)
                {
                    if (!NetworkDevice.ActivateDevice(leaf.DeviceID))
                    {
                        Debug.Log($"Device not found: {leaf.DeviceID}");
                    }
                    LeafClicked(LeafIndex) ;
                }
                else if (leaf.leafType == Node_Leaf.LeafTypes.Doc)
                {
                    if (leaf.doc)
                    {
                        Desktop_TextViewer.OpenDocument(leaf.doc);
                    }
                    LeafClicked(LeafIndex);
                }
                else //dummy node type
                {
                    //do nothing
                }
            }
        }
    }
    void OnPasswordSuccess(int index)
    {
        if (index < 0)
        {
            PIN = -1;
            Clicked(-1);
        }
        else
        {
            Node_Leaf leaf = Leaves[index];
            leaf.PIN = -1;
            Clicked(index);
            //TODO: we can serialize node paswords for saving, but maybe it's just better to set aside a bool for PasswordEntered and leave the password as-is
        }
    }
    void OnPasswordFail(int index)
    {
        if (index == -1)
        {
            buttonImage.color = nodeStyle.PasswordFail;
        }
        else
        {
            Leaves[index].button.image.color = nodeStyle.PasswordFail;
        }
    }
    void RevealChildren()
    {
        foreach (RectTransform child in transform)
        {
            Desktop_Node node = child.GetComponent<Desktop_Node>();
            if (node)
            {
                node.buttonImage.color = nodeStyle.DeselectedColor;
                node.gameObject.SetActive(true);

                //animate node
                Desktop_Anim anim = node.gameObject.AddComponent<Desktop_Anim>();
                anim.local = true;
                anim.Initialize(Vector2.zero, child.transform.localPosition, child.rect.size, child.rect.size, nodeStyle.NodeAnimFlyoutTime, 0);
                anim.DestroyGameobject = false;

                //block inputs until animation is done
                Desktop_Nodemap_Background.Shutdown(nodeStyle.NodeAnimFlyoutTime, true);
            }
        }
        UpdateFrameBounds();
    }
    void ListContents(bool FirstTime = false)
    {
        float LeafDelay = FirstTime ? nodeStyle.ButtonAnimFlyoutFirstDelay : 0;
        for (int i = 0; i < Leaves.Length; i++)
        {
            Node_Leaf leaf = Leaves[i];
            leaf.button = Instantiate(nodeStyle.button_NodeContents_Prefab.gameObject, Desktop_Nodemap_Background.current.nodeLeafList.content).GetComponent<Desktop_ListButton>();
            Sprite icon = nodeStyle.LeafIcon_Unknown;
            switch (leaf.leafIcon)
            {
                case Desktop_Node_StyleSet.LeafIcons.Generic_IOT:
                    {
                        icon = nodeStyle.LeafIcon_Generic_IOT;
                        break;
                    }
                case Desktop_Node_StyleSet.LeafIcons.Camera:
                    {
                        icon = nodeStyle.LeafIcon_Camera;
                        break;
                    }
                case Desktop_Node_StyleSet.LeafIcons.Doc:
                    {
                        icon = nodeStyle.LeafIcon_Doc;
                        break;
                    }
                case Desktop_Node_StyleSet.LeafIcons.Image:
                    {
                        icon = null;
                        break;
                    }
            }
            leaf.button.Setup(this, i, leaf.LeafName, icon);

            Desktop_ListButton leafAnim = Instantiate(nodeStyle.button_NodeContents_Prefab.gameObject, Desktop_Nodemap_Background.current.window.transform).GetComponent<Desktop_ListButton>();
            leafAnim.SetupFlyout(leaf.LeafName, icon, transform, leaf.button, nodeStyle.ButtonAnimFlyoutTime, LeafDelay);

            Desktop_Nodemap_Background.Shutdown(nodeStyle.NodeAnimFlyoutTime + LeafDelay);

            if (leaf.Revealed)
            {
                leaf.button.image.color = nodeStyle.AlreadyUsedLeaf;
            }
            LeafDelay += nodeStyle.ButtonFlyoutDelay;

        }
    }

    void LeafClicked(int index)
    {
        Leaves[index].button.image.color = nodeStyle.AlreadyUsedLeaf;
        Leaves[index].Revealed = true;
        CheckIcon(); //TODO: check if this is running multiple times on a click
    }

    public void SetSelected()
    {
        SelectNode(this);
    }
    private void OnSelected()
    {
        buttonImage.color = nodeStyle.SelectedColor;
        button.enabled = false;
        CheckIcon();
    }

    void OnDeselected()
    {
        buttonImage.color = nodeStyle.DeselectedColor;
        button.enabled = true;
    }
    public void CheckIcon()
    {
        if (Leaves.Length == 0)
        {
            buttonImage.sprite = nodeStyle.EmptyNode;
        }
        else {
            bool exhausted = true;
            Node_Leaf leaf;
            for (int i = 0; i < Leaves.Length; i++)
            {
                leaf = Leaves[i];
                if (!leaf.Revealed)
                {
                    exhausted = false;
                    break;
                }
            }
            if (exhausted)
            {
                buttonImage.sprite = nodeStyle.ExhaustedNode;
            }
        }

    }
    static void SelectNode(Desktop_Node node)
    {
        DeselectNode();
        selected = node;
        node.OnSelected();
        node.ListContents(!node.Revealed && node.Leaves.Length > 0);
        if (!node.Revealed)
        {
            node.Revealed = true;
            node.RevealChildren();
        }
    }

    public static void DeselectNode()
    {
        if (selected)
        {
            selected.OnDeselected();
        }
        selected = null;

        Transform parent = rootNode.transform.parent.parent.GetComponent<Desktop_WindowElement>().window.transform;

        for (int i = Desktop_Nodemap_Background.current.nodeLeafList.content.childCount; i > 0 ; i--)
        {
            RectTransform t = (RectTransform)Desktop_Nodemap_Background.current.nodeLeafList.content.GetChild(i-1);
            t.SetParent(parent);
            t.GetComponentInChildren<Button>().interactable = false;
            Desktop_Anim anim = t.gameObject.AddComponent<Desktop_Anim>();
            t.pivot = new Vector2(0, .5f);
            t.localPosition -= new Vector3(t.rect.width / 2, 0);
            anim.Initialize(t.position, t.position, new Vector2(t.rect.width, t.rect.height), new Vector2(0, t.rect.height), rootNode.nodeStyle.ButtonAnimSquashTime, 0);
        }
    }
    void InstantiateParentLine()
    {
        Transform tempLine = new GameObject("ParentLine").transform;
        tempLine.SetParent(transform);
        parentLine = tempLine.gameObject.AddComponent<RectTransform>();
        parentLine.localPosition = Vector2.zero;
        parentLine.SetAsFirstSibling();
        parentLine.pivot = new Vector2(0, .5f);
        parentLine.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1);
        parentLine.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 3);
        parentLine.gameObject.AddComponent<RawImage>().color = Color.black;
        parentLine.SetAsFirstSibling();
        UpdateParentLine();
    }
    void UpdateParentLine()
    {
        if (parentLine)
        {
            if (rootNode != this)
            {
                //TODO: do this in the editor, too
                Vector2 lineVec = -transform.localPosition;
                parentLine.localScale = new Vector3(lineVec.magnitude, 1, 1);
                parentLine.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(lineVec[1], lineVec[0]) * Mathf.Rad2Deg);
            }
        }
    }

    public static void UpdateFrameBounds(bool OnlyRoot = false)
    {
        Vector4 Margins = new Vector4(-16, 16, 16, -16);
        RectTransform r;
        Vector3[] corners = new Vector3[4];
        ((RectTransform)rootNode.transform).GetWorldCorners(corners);
        Vector4 RootBounds = new Vector4(corners[1].x, corners[1].y, corners[3].x, corners[3].y);
        Vector4 Bounds = RootBounds;//left, top, right, bottom
        Vector2 RootOffset = Vector2.zero;
        Vector2 CanvasScale = rootNode.GetComponentInParent<Canvas>().transform.lossyScale;
        if (!OnlyRoot)
        {
            foreach (Desktop_Node node in rootNode.GetComponentsInChildren<Desktop_Node>())
            {
                if (node.gameObject.activeInHierarchy && node && node != rootNode)
                {
                    corners = new Vector3[4];
                    r = (RectTransform)node.transform;
                    r.GetWorldCorners(corners);
                    if (corners[1].x < Bounds[0])
                    {
                        RootOffset.x -= (corners[1].x - Bounds[0]);
                        Bounds[0] = corners[1].x;
                    }
                    if (corners[1].y > Bounds[1])
                    {
                        RootOffset.y -= (corners[3].y - Bounds[1]);
                        Bounds[1] = corners[1].y;
                    }
                    if (corners[3].x > Bounds[2])
                    {
                        RootOffset.x -= (corners[3].x - Bounds[2]);
                        Bounds[2] = corners[3].x;
                    }
                    if (corners[3].y < Bounds[3])
                    {
                        RootOffset.y -= (corners[3].y - Bounds[3]);
                        Bounds[3] = corners[3].y;
                    }
                }
            }
        }
        Bounds += Margins;

        r = (RectTransform)rootNode.transform.parent;
        rootNode.transform.localPosition = (Vector3)RootOffset/2 / CanvasScale;

        foreach(Desktop_Node node in rootNode.GetComponentsInChildren<Desktop_Node>())
        {
            if (node.gameObject.activeInHierarchy && node)
            {
                node.UpdateParentLine();
            }
        }


        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (Bounds[2] - Bounds[0])/CanvasScale.x);
        r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (Bounds[1] - Bounds[3])/ CanvasScale.y);
    }
}
