using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NetworkDevice : MonoBehaviour
{
    [System.Serializable]
    public class NetworkDeviceUIElement
    {
        //TODO: custom editor that sets proper default values on button creation (especially set scale to something other than 0,0)
        //TODO: layout builder
        public string ElementName = "Unnamed Element";
        public Sprite sprite = null;
        public string Label = "";
        public Vector2 Location = Vector2.zero;
        public Vector2 Scale = Vector2.one;
        public UnityEvent clickEvent;
    }

    public static Dictionary<string,NetworkDevice> registry = new Dictionary<string,NetworkDevice>();
    public string DeviceID;
    public string DeviceName = "[Unnamed Device]";
    public NetworkDeviceUIElement[] DeviceUI;
    //TODO: set up a repeat use cooldown
    protected virtual void Awake()
    {
        if (DeviceID != null && DeviceID != "")
        {
            if (registry.ContainsKey(DeviceID))
            {
                Debug.LogError($"Network Device Registry already contains key: {DeviceID}!", this);
            }
            else
            {
                registry.Add(DeviceID, this);
            }
        }
        else
        {
            Debug.LogError("Network Device doesn't have a Device ID!", this);
        }
    }

    protected virtual void ActivateDevice()
    {
        Desktop_NetworkDevices.SelectDevice(this);
    }

    public virtual string[] RequestControls()
    {
        //TODO: instead of a string, make a class that has a control name (or icon?), and links it to functions in the current (or overloaded) class
        return new string[] { "Activate" };
    }

    protected virtual void OnDestroy()
    {
        registry.Remove(DeviceID);
    }

    public static bool ActivateDevice(string DeviceID)
    {
        if (registry.ContainsKey(DeviceID))
        {
            registry[DeviceID].ActivateDevice();
            return true;
        }
        return false;
    }

    public static void OnSceneLoad()
    {
        //wipe all the previous scene objects from the registry
        //(though they should've removed themselves as the previous scene was unloaded
        //TODO: Test this ^
        registry = new Dictionary<string, NetworkDevice>();
    }
}
