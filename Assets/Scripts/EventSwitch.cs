using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventSwitch : NetworkDevice
{
    //TODO: custom editor that hides network stuff if networked is unchecked
    public bool Networked; //do not change at runtime!
    public enum StartupActions { None, TurnOn, TurnOff}
    public StartupActions startupAction = StartupActions.None;
    public enum SwitchType { Toggle, Impulse};
    public SwitchType switchType = SwitchType.Toggle;
    public bool PoweredOn = false;

    public UnityEvent OnPowerOn;
    public UnityEvent OnPowerOff;

    protected override void Awake()
    {
        if (Networked)
        {
            base.Awake();
        }
    }
    protected override void ActivateDevice()
    {
        if (Networked)
        {
            base.ActivateDevice();
        }
    }
    protected override void OnDestroy()
    {
        if (Networked)
        {
            base.OnDestroy();
        }
    }
    private void Start()
    {
        if (startupAction == StartupActions.TurnOn)
        {
            SWITCH_TurnOn();
        }
        else if (startupAction == StartupActions.TurnOff)
        {
            SWITCH_TurnOff();
        }
    }
    public void SWITCH_Interact()
    {
        if (switchType == SwitchType.Toggle)
        {
            if (PoweredOn == false)
            {
                OnPowerOn.Invoke();
            }
            else
            {
                OnPowerOff.Invoke();
            }
            PoweredOn = !PoweredOn;
        }
        else if (switchType == SwitchType.Impulse)
        {
            OnPowerOn.Invoke();
        }
    }
    public void SWITCH_TurnOn()
    {
        if (!PoweredOn)
        {
            OnPowerOn.Invoke();
            PoweredOn = true;
        }
    }
    public void SWITCH_TurnOff()
    {
        if (PoweredOn)
        {
            OnPowerOff.Invoke();
            PoweredOn = false;
        }
    }
}
