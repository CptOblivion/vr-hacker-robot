using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecurityCameraControls : Desktop_Window //TODO: inherit from Desktop_Window
{
    //TODO: override MinimizeWindow, disable security camera when minimizing (to save rendering overhead)
    public Transform cameraListFrame;
    public GameObject camDisplay;
    public Desktop_ListButton buttonPrefab;
    static SecurityCameraControls current;
    readonly Dictionary<SecurityCamera, Desktop_ListButton> activeCameras = new Dictionary<SecurityCamera, Desktop_ListButton>();

    public Color SelectedColor;
    public Color DeselectedColor;

    protected override void Awake()
    {
        base.Awake();
        current = this;
    }

    public static void ActivateCamera(SecurityCamera securityCamera)
    {
        current.ExpandWindow();
        current.FocusWindow();
        //TODO: if the window is currently closed, subscribe a function to the window OnFinishedINIT to wait until that's done before actually activating the camera
        //  alternately, do the opposite- wait a frame before starting window init, so whatever processing lag is gonna happen finishes before the window animation starts
        if (securityCamera)
        {
            if (!current.activeCameras.ContainsKey(securityCamera))
            {
                Desktop_ListButton camButton = Instantiate(current.buttonPrefab.gameObject, current.cameraListFrame).GetComponent<Desktop_ListButton>();
                current.activeCameras.Add(securityCamera, camButton);
                camButton.Setup(securityCamera);
            }
            if(SecurityCamera.selectedCamera == null)
            {
                current.camDisplay.SetActive(true);
            }
            securityCamera.SetSelected();
        }
    }
    public static void UpdateActiveCamera(SecurityCamera newCam, SecurityCamera oldCam)
    {
        //TODO: put a little red light on the currently active camera
        //TODO: instead of colors, treat the camera buttons as folder tabs and pop out the current one (recess the rest)
        if (oldCam != null && current.activeCameras.ContainsKey(oldCam))
        {
            current.activeCameras[oldCam].SetInactiveButton();
        }
        if (current.activeCameras.ContainsKey(newCam))
        {
            current.activeCameras[newCam].SetActiveButton();
        }

    }

    public void PanLeft()
    {
        SecurityCamera.PanLeft();
    }
    public void PanRight()
    {
        SecurityCamera.PanRight();
    }
    public void PanUp()
    {
        SecurityCamera.PanUp();
    }
    public void PanDown()
    {
        SecurityCamera.PanDown();
    }
    public void ZoomIn()
    {
        SecurityCamera.ZoomIn();
    }
    public void ZoomOut()
    {
        SecurityCamera.ZoomOut();
    }
    public void Recenter()
    {
        SecurityCamera.Recenter();
    }
}
