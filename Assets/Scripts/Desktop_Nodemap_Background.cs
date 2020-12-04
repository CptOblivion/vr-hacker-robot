using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_Nodemap_Background : Desktop_WindowElement, IPointerDownHandler
{

    public static Desktop_Nodemap_Background current;
    static float ShutdownDelay = 0;
    static float UpdateBoundsDelay = 0;
    static bool UpdateBoundsLastFrame = false;
    CanvasGroup group;
    protected override void Awake()
    {
        base.Awake();
        current = this;
        group = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (ShutdownDelay > 0)
        {
            ShutdownDelay -= Time.deltaTime;
            if (ShutdownDelay <= 0)
            {
                group.interactable = true;
            }
        }
    }

    private void LateUpdate()
    {
        if (UpdateBoundsDelay > 0 || UpdateBoundsLastFrame)
        {
            if (UpdateBoundsDelay <= 0)
            {
                UpdateBoundsLastFrame = false;
                //I'm sure this is not the best way to get one more frame after the timer runs out, but I'm tired
                //also because this runs in lateupdate I'm not sure why this timer needs an extra frame, but I'm tired
            }
            Desktop_Node.UpdateFrameBounds();
            UpdateBoundsDelay -= Time.deltaTime;
        }
    }
    public override void OnPointerDown(PointerEventData data)
    {
        base.OnPointerDown(data);
        if (group.interactable)
        {
            Desktop_Node.DeselectNode();
        }
    }

    public static void Shutdown(float t, bool UpdateBounds = false)
    {
        if (UpdateBounds && t > UpdateBoundsDelay)
        {
            UpdateBoundsLastFrame = true;
            UpdateBoundsDelay = t;
        }
        if (t > ShutdownDelay)
        {
            ShutdownDelay = t;
            current.group.interactable = false;
        }
    }
}
