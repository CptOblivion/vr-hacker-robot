using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Desktop_Cursor: MonoBehaviour
{
    public enum CursorStates { pointer, hyperlink, dragX, dragY, dragXY, loading}
    static CursorStates cursorState = CursorStates.pointer;
    static Desktop_Cursor current;
    Image image;

    public Desktop_StyleTheme styleTheme;
    private void Awake()
    {
        current = this;
        image = GetComponent<Image>();
        Cursor.visible = false;
    }
    private void Update()
    {
        //transform.position = new Vector3(Input.mousePosition.x / Screen.width * 1920, Input.mousePosition.y / Screen.height * 1080, 0);
        //transform.localPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        transform.localPosition = transform.parent.InverseTransformPoint(Input.mousePosition);
    }

    public static void RequestCursor(CursorStates requestedState)
    {
        cursorState = requestedState;
        //TODO: some logic to switch back to loading if we're being asked to change to pointer while loading
        // alternately, just don't implement a loading cursor and then this function is a bit redundant
        current.UpdateCursor();
        DebugDisplay.AddLine(cursorState.ToString());
    }

    void UpdateCursor()
    {
        Cursor.visible = false;
        switch (cursorState)
        {
            case (CursorStates.pointer):
                image.sprite = styleTheme.cursorPointer;
                break;
            case (CursorStates.hyperlink):
                image.sprite = styleTheme.cursorHyperlink;
                break;
            case (CursorStates.dragX):
                image.sprite = styleTheme.cursorDragX;
                break;
            case (CursorStates.dragY):
                image.sprite = styleTheme.cursorDragY;
                break;
            case (CursorStates.dragXY):
                image.sprite = styleTheme.cursorDragXY;
                break;
            case (CursorStates.loading):
                image.sprite = styleTheme.cursorLoading;
                break;
        }
    }
}
