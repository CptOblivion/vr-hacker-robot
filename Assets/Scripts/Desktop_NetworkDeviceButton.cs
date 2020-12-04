using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Desktop_NetworkDeviceButton : Desktop_WindowElement
{
    #region user variables
    public Text textObject;
    public float IntroTime = .2f;
    #endregion

    #region hidden/private variables
    [HideInInspector]
    public Button button;

    NetworkDevice device;
    int UIElementIndex;
    static Sprite defaultSprite;

    static List<Desktop_NetworkDeviceButton> buttonPool = new List<Desktop_NetworkDeviceButton>();
    static int ButtonCount = 0;
    static Desktop_NetworkDeviceButton buttonPrefabStatic;
    static RectTransform root;
    #endregion

    protected override void Awake()
    {
        base.Awake();
        button = GetComponent<Button>();
        if (!defaultSprite)
        {
            defaultSprite = GetComponent<Image>().sprite;
        }
    }

    public static void InitializeButtons(Desktop_NetworkDeviceButton prefab, RectTransform buttonParent)
    {
        //only run once per scene
        buttonPrefabStatic = prefab;
        root = buttonParent;
        //buttonList = new List<Desktop_NetworkDeviceButton>();
    }

    public static void ResetButtons()
    {
        //TODO: some sort of animation (flyout?) to indicate the button relation
        foreach (Desktop_NetworkDeviceButton button in buttonPool)
        {
            button.gameObject.SetActive(false);
            Desktop_Anim anim = button.GetComponent<Desktop_Anim>();
            if (anim)
            {
                Destroy(anim);
            }
        }
        ButtonCount = 0;
    }

    public static void AddButton(NetworkDevice networkDevice, int elementIndex, RectTransform OriginButton)
    {
        Desktop_NetworkDeviceButton current;
        ButtonCount++;
        if (ButtonCount < buttonPool.Count)
        {
            current = buttonPool[ButtonCount];
            current.gameObject.SetActive(true);
        }
        else
        {
            current = Instantiate(buttonPrefabStatic, root);
            buttonPool.Add(current);
        }

        current.device = networkDevice;
        current.UIElementIndex = elementIndex;
        NetworkDevice.NetworkDeviceUIElement element = networkDevice.DeviceUI[elementIndex];

        RectTransform rect = (RectTransform)current.transform;

        Desktop_Anim anim = current.gameObject.AddComponent<Desktop_Anim>();
        anim.DestroyGameobject = false;

        anim.local = true;
        anim.StartSizeOffset = Vector2.zero;
        anim.StartPosOffset = new Vector2(OriginButton.rect.width / 4, 0);
        //TODO: button should fly out from the right-hand edge of the list button, not the center of it
        anim.Initialize(OriginButton, element.Location, element.Scale, current.IntroTime, 0);

        /*
            rect.anchoredPosition = element.Location;

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, element.Scale.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, element.Scale.y);
        */

        current.button.onClick.RemoveAllListeners();
        current.button.onClick.AddListener(current.device.DeviceUI[current.UIElementIndex].clickEvent.Invoke);

        if (element.sprite != null)
        {
            current.GetComponent<Image>().sprite = element.sprite;
            current.textObject.gameObject.SetActive(false);
        }
        else
        {
            current.GetComponent<Image>().sprite = defaultSprite;
            current.textObject.text = element.Label;
            current.textObject.gameObject.SetActive(true);
        }
    }
}
