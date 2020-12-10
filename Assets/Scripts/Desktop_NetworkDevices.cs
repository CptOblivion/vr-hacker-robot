using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Desktop_NetworkDevices : Desktop_Window
{
    static readonly Dictionary<string, Desktop_ListButton> AvailableDevices = new Dictionary<string, Desktop_ListButton>();
    static Desktop_ListButton activeButton;
    static NetworkDevice currentDevice;
    static Desktop_NetworkDevices current;
    public Desktop_ListButton buttonPrefab;
    public RectTransform buttonListFrame;
    public RectTransform controlsFrame;
    public Desktop_NetworkDeviceButton deviceButtonPrefab;


    protected override void Awake()
    {
        base.Awake();
        current = this;
        Desktop_NetworkDeviceButton.InitializeButtons(deviceButtonPrefab, controlsFrame);
    }

    public static void SelectDevice(NetworkDevice device)
    {
        current.ExpandWindow();
        current.FocusWindow();
        if (!AvailableDevices.ContainsKey(device.DeviceID))
        {
            Desktop_ListButton deviceButton = Instantiate(current.buttonPrefab.gameObject, current.buttonListFrame).GetComponent<Desktop_ListButton>();
            AvailableDevices.Add(device.DeviceID, deviceButton);
            deviceButton.Setup(device);
            UpdateActiveButton(deviceButton);
        }
        if (currentDevice != device)
        {
            currentDevice = device;

            Desktop_NetworkDeviceButton.ResetButtons();

            for (int i = 0; i < device.DeviceUI.Length; i++)
            {
                //TODO: anchored position isn't set on the first frame the button is initialized
                Desktop_NetworkDeviceButton.AddButton(device, i, (RectTransform)AvailableDevices[device.DeviceID].transform);
            }
        }
    }

    public static void UpdateActiveButton(Desktop_ListButton newActive)
    {
        if (activeButton)
        {
            activeButton.SetInactiveButton();
        }
        activeButton = newActive;
        activeButton.SetActiveButton();
    }
}
