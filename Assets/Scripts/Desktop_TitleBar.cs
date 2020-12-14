using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Desktop_TitleBar : Desktop_WindowElement, IPointerUpHandler, IDragHandler, IEndDragHandler , IBeginDragHandler
{
    public bool dragging = false;
    public Button stowButton;
    public TMP_Text title;
    Vector2 DragOrigin;
    private void OnEnable()
    {
        transform.SetAsLastSibling(); //draw on top of the rest of the window (at least, initially)
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        //TODO: add a check to see if we're resizing instead of moving
        base.OnPointerDown(eventData);
        window.BeginResize(eventData);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        window.EndDrag();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //TODO: use delta from pressPosition so cursor doesn't desync with window when window is stopped by desktop edge
        if (!window.Resizing)
        {
            dragging = true;
            DragOrigin = Desktop_Desktop.SnapGlobalToPixel(transform.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window.Resizing)
        {
            window.OnDrag(eventData);
        }
        else if (dragging)
        {
            window.transform.position = DragOrigin + Desktop_Desktop.SnapGlobalToPixel(eventData.position) - Desktop_Desktop.SnapGlobalToPixel(eventData.pressPosition);
            window.ConstrainPosition();
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (window.Resizing)
        {
            window.OnEndDrag(eventData);
        }
        else
        {
            dragging = false;
        }
    }

    public void SetActiveColor(bool active)
    {
        GetComponent<Image>().color = active ? Desktop_Desktop.currentTheme.WindowTitlebarColorActive : Desktop_Desktop.currentTheme.WindowTitlebarColorInactive; 
    }

    public void SetColor(Color color)
    {
        GetComponent<Image>().color = color;
    }
}



