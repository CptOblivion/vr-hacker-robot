using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_TitleBar : Desktop_WindowElement, IDragHandler, IEndDragHandler 
{
    public Color ColorActive;
    public Color ColorInactive;
    Vector2 cursorOffset;
    bool dragging = false;
    private void OnEnable()
    {
        transform.SetAsLastSibling(); //draw on top of the rest of the window (at least, initially)
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        cursorOffset = Input.mousePosition - window.transform.position;
        dragging = true;

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            RectTransform parent = (RectTransform)window.transform;
            parent.position = (Vector2)Input.mousePosition - cursorOffset;
            window.ConstrainPosition();
        }
        //TODO: clip cursor at edge of screen
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            dragging = false;
        }
    }

    public void SetActiveColor(bool active)
    {
        GetComponent<Image>().color = active ? ColorActive : ColorInactive; 
    }

    public void SetColor(Color color)
    {
        GetComponent<Image>().color = color;
    }
}



