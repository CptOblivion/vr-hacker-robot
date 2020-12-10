using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Desktop_ListButton : MonoBehaviour
{
    public Button button;
    public Image image;
    public Text text;
    public CanvasGroup group;
    public Image sprite;
    SecurityCamera cam;
    NetworkDevice device;
    TextDocument doc;
    Desktop_Node node;
    int nodeLeafIndex;
    public float inactiveTabOffset = 16;
    Desktop_ListButton endButton;


    public Color activeColor;
    public Color inactiveColor;
    
    public void Setup(NetworkDevice networDevice)
    {
        Setup(networDevice.DeviceName);
        device = networDevice;
    }
    public void Setup(SecurityCamera securityCamera)
    {
        Setup(securityCamera.CameraName);
        cam = securityCamera;
    }
    public void Setup(TextDocument textDocument)
    {
        Setup(textDocument.Title);
        doc = textDocument;
        Desktop_TextViewer.UpdateCurrentButton(this);
    }
    public void Setup(Desktop_Node desktopNode, int leafIndex, string buttonText, Sprite leafIcon)
    {
        Setup(buttonText, leafIcon);
        node = desktopNode;
        nodeLeafIndex = leafIndex;
    }
    public void Setup(string buttonText)
    {
        text.text = buttonText;
        button.onClick.AddListener(OnClick);
    }
    public void Setup(string buttonText, Sprite leafIcon)
    {
        Setup(buttonText);
        sprite.sprite = leafIcon;
    }
    public void OnClick()
    {
        if (device != null)
        {
            //NetworkDevice.ActivateDevice(DeviceID);
            Desktop_NetworkDevices.SelectDevice(device);
            Desktop_NetworkDevices.UpdateActiveButton(this);
        }
        else if (cam)
        {
            cam.SetSelected();
        }
        else if (doc)
        {
            Desktop_TextViewer.OpenDocument(doc);
            Desktop_TextViewer.UpdateCurrentButton(this);
        }
        else if (node)
        {
            node.Clicked(nodeLeafIndex);
            //node.CheckIcon();
        }
    }

    public void DisableCanvasGroup()
    {
        group.interactable = false;
        group.alpha = 0;
    }

    public void EnableCanvasGroup()
    {
        group.interactable = true;
        group.alpha = 1;
    }

    public void SetActiveButton()
    {
        button.transform.localPosition = new Vector3(inactiveTabOffset, 0);
        button.enabled = false;
        image.color = activeColor;
    }

    public void SetInactiveButton()
    {
        button.transform.localPosition = Vector3.zero;
        button.enabled = true;
        image.color = inactiveColor;
    }

    public void SetupFlyout(string buttonName, Sprite leafIcon, Transform startTransform, Desktop_ListButton targetButton, float animTime, float delay)
    {
        Setup(buttonName, leafIcon);
        button.interactable = false;
        Desktop_Anim anim = gameObject.AddComponent<Desktop_Anim>();
        anim.StartSizeOffset *= .25f;
        anim.Initialize(startTransform, targetButton.transform, animTime, delay);
        endButton = targetButton;
        endButton.DisableCanvasGroup();
        anim.OnAnimFinished += delegate { endButton.EnableCanvasGroup(); };
        group.alpha = 0;
        anim.OnAnimStart += delegate { group.alpha = 1; };
    }
}
