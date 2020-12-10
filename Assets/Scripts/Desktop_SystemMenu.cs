using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Desktop_SystemMenu : Desktop_Window
{
    public Button systemButton;
    void Start()
    {
        systemButton.onClick.AddListener(OpenWindow);
        gameObject.SetActive(false);
    }

    public override void CloseWindow()
    {
        base.CloseWindow();
        systemButton.onClick.RemoveAllListeners();
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        systemButton.onClick.RemoveAllListeners();
    }
    protected override void FinishedOpening()
    {
        base.FinishedOpening();
        systemButton.onClick.AddListener(CloseWindow);
    }

    protected override void FinishedClosing()
    {
        base.FinishedClosing();
        systemButton.onClick.AddListener(OpenWindow);
    }

    //TODO: subwindow class, for popups that are technically WindowElements but that animate open and closed like a regular window, and are expected to be able to move outside the window bounds
    //  WindowElement should also check for a Subwindow on awake, and use that for dragging/focus instead of the Window (subwindow will pass along focus but not drag to the root window)
    //  Window should have its own internal (non-static) Subwindow active tracker, to tell the active subwindow when it's lost focus (for auto closing, etc)
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
