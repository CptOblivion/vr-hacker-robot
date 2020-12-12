using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Desktop_Cursor: MonoBehaviour
{
    public enum CursorStates { pointer, hyperlink, dragX, dragY, dragDiagonal1, dragDiagonal2, loading}
    static CursorStates cursorState = CursorStates.pointer;
    static Desktop_Cursor current;
    Image image;
    public bool HideCursor = true;

    private void Awake()
    {
        current = this;
        image = GetComponent<Image>();
        if (HideCursor) Cursor.visible = false;
        Desktop_Desktop.current.OnThemeChanged += UpdateTheme;
    }
    private void LateUpdate()
    {
        //transform.position = new Vector3(Input.mousePosition.x / Screen.width * 1920, Input.mousePosition.y / Screen.height * 1080, 0);
        //transform.localPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        transform.position = Desktop_Desktop.SnapGlobalToPixel(Input.mousePosition);
    }
    private void OnDestroy()
    {
        Desktop_Desktop.current.OnThemeChanged -= UpdateTheme;
    }

    public static void RequestCursor(CursorStates requestedState)
    {
        cursorState = requestedState;
        //TODO: some logic to switch back to loading if we're being asked to change to pointer while loading
        // alternately, just don't implement a loading cursor and then this function is a bit redundant
        current.UpdateCursor();
        DebugDisplay.AddLine(cursorState.ToString());
    }

    void UpdateTheme(Desktop_StyleTheme newTheme)
    {
        UpdateCursor();
    }

    void UpdateCursor()
    {
        if (HideCursor) Cursor.visible = false;
        switch (cursorState)
        {
            case (CursorStates.pointer):
                image.sprite = Desktop_Desktop.currentTheme.cursorPointer;
                break;
            case (CursorStates.hyperlink):
                image.sprite = Desktop_Desktop.currentTheme.cursorHyperlink;
                break;
            case (CursorStates.dragX):
                image.sprite = Desktop_Desktop.currentTheme.cursorDragX;
                break;
            case (CursorStates.dragY):
                image.sprite = Desktop_Desktop.currentTheme.cursorDragY;
                break;
            case (CursorStates.dragDiagonal1):
                image.sprite = Desktop_Desktop.currentTheme.cursorDragDiagonal1;
                break;
            case (CursorStates.dragDiagonal2):
                image.sprite = Desktop_Desktop.currentTheme.cursorDragDiagonal2;
                break;
            case (CursorStates.loading):
                image.sprite = Desktop_Desktop.currentTheme.cursorLoading;
                break;
        }
    }
}
