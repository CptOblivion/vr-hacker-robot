using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_Window : MonoBehaviour, IPointerDownHandler ,IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    //TODO: split window root and window element into separate classes
    //TODO: organize all these properties
    enum AnimState { Opening, Open, Closing, Minimizing, Expanding}
    AnimState animState = AnimState.Opening;
    //TODO: allow multi-edit
    //TODO: figure out alphaHitTestMinimumThreshold and 9-sliced sprites (currently doesn't work)
    public bool AlphaHitTest = false;
    public bool KillOnFocusLoss = false;
    public bool HideOnStartup = false;
    public bool CloseToTaskbar = false;
    public bool Resizable = false;
    public Vector2 MinSize = Vector2.zero;
    public Sprite appIcon;
    public string AppName;

    bool PointerInWindow = false;
    Vector2 MousePositionLast;
    [HideInInspector]
    public bool Resizing = false;
    Vector2Int ResizeDirection;
    Vector2 ResizeOriginalEdges = Vector2.zero;

    RectTransform rect;

    //TODO: make this a prefab and spawn it
    Desktop_MinimizedButton minimizedButton; //this button can always exist, and just be disabled
    public Desktop_MinimizedButton minimizedPrefab;
    //TODO: maybe we can dynamically choose the close or minimize button based on whether or not minimizedButton exists?

    static Vector2 StartSize = new Vector2(64, 40);
    static readonly float OpenTime = 0.1f;
    static readonly float CloseTime = .1f;
    static readonly float MinimizeTime = .25f;
    static readonly float ExpandTime = .2f;
    [HideInInspector]
    public Desktop_TitleBar titleBar;

    public static Desktop_Window Active;
    CanvasGroup canvasGroup;

    Vector2 TargetSize;

    float AnimTimer;
    Vector2? AnimTargetPos = null;
    Transform AnimStartPos = null;

    static readonly Vector2 ButtonTargetOffset = new Vector2(0, 128);

    protected virtual void Awake()
    {
        rect = (RectTransform)transform;
        titleBar = GetComponentInChildren<Desktop_TitleBar>();
        if (titleBar)
        {
            titleBar.title.text = AppName;
            if (CloseToTaskbar)
            {
                titleBar.stowButton.GetComponent<Image>().sprite = Desktop_Desktop.currentTheme.windowMinimizeButton;
                minimizedButton = Desktop_MinimizedButton.AddMinimizedButton(minimizedPrefab, this);
            }
            else
            {
                titleBar.stowButton.GetComponent<Image>().sprite = Desktop_Desktop.currentTheme.windowCloseButton;
            }
            titleBar.stowButton.onClick.AddListener(Program_Stow);
        }

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;

        if (AlphaHitTest) //TODO: figure out if I can test for image editability and automatically disable this if it's not available (catch an error might be the only way?)
        {
            Image image = GetComponent<Image>();
            if (image)
            {
                image.alphaHitTestMinimumThreshold = .9f;
            }
        }
        if (HideOnStartup)
        {
            gameObject.SetActive(false);
        }
        else
        {
            FocusWindow();
        }
        Desktop_Desktop.current.OnThemeChanged += UpdateTheme;

        rect.offsetMin = Desktop_Desktop.SnapCanvasToPixel(rect.offsetMin);
        rect.offsetMax = Desktop_Desktop.SnapCanvasToPixel(rect.offsetMax);
        TargetSize = rect.rect.size;
    }
    protected virtual void OnEnable()
    {
        if (!Active || transform.GetSiblingIndex() > Active.transform.GetSiblingIndex())
        {
            FocusWindow();
        }
    }

    protected virtual void Update()
    {
        if (animState == AnimState.Opening)
        {
            if (!Animate(OpenTime))
            {
                animState = AnimState.Open;
                FinishedOpening();
            }
        }
        else if (animState == AnimState.Closing)
        {
            if (!Animate(CloseTime, true))
            {
                AnimTargetPos = null;//just in case this was set somehow, we don't want to play an un-minimize animation when we re-expand
                FinishedClosing();
            }
        }
        else if (animState == AnimState.Expanding)
        {
            if (!Animate(ExpandTime))
            {
                animState = AnimState.Open;
                FinishedOpening();
            }
        }
        else if (animState == AnimState.Minimizing)
        {
            if (!Animate(MinimizeTime, true))
            {
                minimizedButton.WindowMinimized();
                FinishedClosing();
            }
        }
        else
        {
            //TODO: this will probably trigger on controller axis moves too
            if (PointerInWindow && (Vector2)Input.mousePosition != MousePositionLast)
            {
                MousePositionLast = Input.mousePosition;
                if (Resizable && !Resizing)
                {
                    CheckResizeStart(MousePositionLast);
                }
            }
        }
    }

    protected virtual void OnDestroy()
    {
        Desktop_Desktop.current.OnThemeChanged -= UpdateTheme;
        if (minimizedButton)
        {
            Destroy(minimizedButton.gameObject);
        }
    }

    bool Animate(float animTime, bool ReverseDirection=false)
    {
        //returns true until it's done animating
        RectTransform rectTransform = (RectTransform)transform;
        if (AnimTimer > 0)
        {
            AnimTimer -= Time.deltaTime;
            float t = 1 - (AnimTimer / animTime);
            t *= t;
            if (ReverseDirection) t = 1 - t;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(StartSize.x, TargetSize.x, t));
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(StartSize.y, TargetSize.y, t));
            if (AnimTargetPos != null)
            {
                rectTransform.position = Vector2.Lerp((Vector2)AnimStartPos.position + ButtonTargetOffset, (Vector2)AnimTargetPos, t);
            }
            ConstrainPosition(false);
            return true;
        }
        else
        {
            //TODO: rearrange window structure: every window has a child which does the clipping, covering both padding and making sure the border isn't covered by anything (all "program" components are children of the clipping child, except the title bar)
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TargetSize.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TargetSize.y);
            if (AnimTargetPos != null)
            {
                transform.position = (Vector3)AnimTargetPos;
            } 
            ConstrainPosition();
            return false;
        }
    }
    /// <summary>
    /// either opens or expands the window, depending on whether it's expected to minimize or close
    /// </summary>
    public virtual void Program_Launch()
    {
        //underscored prefix so it stands out easier in button onclick dropdowns
        if (CloseToTaskbar)
        {
            ExpandWindow();
        }
        else
        {
            OpenWindow();
        }
    }
    /// <summary>
    /// either closes or minimizes the window
    /// </summary>
    public virtual void Program_Stow()
    {
        if (CloseToTaskbar)
        {
            MinimizeWindow();
        }
        else
        {
            CloseWindow();
        }
    }
    /// <summary>
    /// opens the window as though the program is launching from a shutdown state- use Program_Launch() instead if possible (pairs with CloseWindow())
    /// </summary>
    public virtual void OpenWindow()
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, StartSize.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, StartSize.y);
            ConstrainPosition();
            animState = AnimState.Opening;
            canvasGroup.interactable = false;
            AnimTimer = OpenTime;
        }
    }
    /// <summary>
    /// closes the window as though the program has shut down- use Program_Stow() instead if possible (pairs with OpenWindow())
    /// </summary>
    public virtual void CloseWindow()
    {
        animState = AnimState.Closing;
        AnimTimer = CloseTime;
        canvasGroup.interactable = false;
    } 
    /// <summary>
    /// opens the window as though it's expanding from running in the background- use Program_Launch() instead if possible (pairs with MinimizeWindow)
    /// </summary>
    public virtual void ExpandWindow()
    {
        if (!gameObject.activeInHierarchy)
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, StartSize.x);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, StartSize.y);
            if (AnimStartPos)
                rectTransform.position = AnimStartPos.position;
            ConstrainPosition(false);
            animState = AnimState.Expanding;
            AnimTimer = ExpandTime;
            canvasGroup.interactable = false;
            //animTargetPos should still be untouched from when we were minimized
            //TODO: make sure this is safe to call if the window has never been opened before (IE no button exists, no target position, etc) (should just play as though it's opening)
        }
    }
    /// <summary>
    /// closes the window to the taskbar as though it's running in the background- use Program_Stow() instead if possible (pairs with ExpandWindow)
    /// </summary>
    public virtual void MinimizeWindow()
    {
        canvasGroup.interactable = false;
        animState = AnimState.Minimizing;
        AnimTimer = MinimizeTime;

        //reversing start and target pos because we'll be playing the anim backwards on minimize, and we'll want TargetPosition to still be set on ExpandWindow
        AnimTargetPos = transform.position;
        minimizedButton.WindowMiniming();
        //TODO: wait a frame (or do this in Update just before the animation?) to make sure the correct position is found
        AnimStartPos = minimizedButton.transform;
    }
   
    protected virtual void OnDisable()
    {
        if (Active == this)
        {
            Desktop_Window d;
            for(int i = transform.parent.childCount-1; i >= 0; i--)
            {
                d = transform.parent.GetChild(i).GetComponent<Desktop_Window>();
                if (d && d.gameObject.activeInHierarchy)
                {
                    Active = d;
                    d.GainedFocus();
                    goto FoundNewActive;
                }
            }
            Active = null;
            FoundNewActive:;
        }
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        FocusWindow();
        BeginResize(eventData);
    }
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (Resizing)
        {
            Vector2 delta = (eventData.position - eventData.pressPosition);
            delta = new Vector2(delta.x / transform.lossyScale.x, delta.y / transform.lossyScale.y);
            ResizeWindowFromEdge(delta);
        }
    }
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (Resizing)
        {
            Resizing = false;
            if (!PointerInWindow)
            {
                Desktop_Cursor.RequestCursor(Desktop_Cursor.CursorStates.pointer);
            }
        }
    }
    public void BeginResize(PointerEventData eventData)
    {
        if (Resizable)
        {
            //TODO: store the original rect and use cursor delta from eventData.pressPosition, so cursor doesn't desync with press position relative to the window edge when scaling is capped
            ResizeDirection = CheckResizeStart(eventData.position);
            if (ResizeDirection.magnitude != 0)
            {
                ResizeOriginalEdges.x = ResizeDirection.x < 0 ? rect.offsetMin.x : rect.offsetMax.x;
                ResizeOriginalEdges.y = ResizeDirection.y < 0 ? rect.offsetMin.y : rect.offsetMax.y;
                Resizing = true;
            }
        }
    }

    void ResizeWindowFromEdge(Vector2 delta)
    {
        //TODO: clip scaling if it tries to go out of the desktop
        if (ResizeDirection.x < 0)
        {
            rect.offsetMin = new Vector2(Mathf.Min(ResizeOriginalEdges.x + delta.x, rect.offsetMax.x - MinSize.x), rect.offsetMin.y);
        }
        else if (ResizeDirection.x > 0)
        {
            rect.offsetMax = new Vector2(Mathf.Max(ResizeOriginalEdges.x + delta.x, rect.offsetMin.x + MinSize.x), rect.offsetMax.y);
        }
        if (ResizeDirection.y < 0)
        {
            rect.offsetMin = new Vector2(rect.offsetMin.x, Mathf.Min(ResizeOriginalEdges.y + delta.y, rect.offsetMax.y - MinSize.y));
        }
        else if (ResizeDirection.y > 0)
        {
            rect.offsetMax = new Vector2(rect.offsetMax.x, Mathf.Max(ResizeOriginalEdges.y + delta.y, rect.offsetMin.y + MinSize.y));
        }

        rect.offsetMin = Desktop_Desktop.SnapCanvasToPixel(Vector2.Max(rect.offsetMin, Desktop_Desktop.current.DesktopMin));
        rect.offsetMax = Desktop_Desktop.SnapCanvasToPixel(Vector2.Min(rect.offsetMax, Desktop_Desktop.current.DesktopMax));
        TargetSize = rect.rect.size;

    }

    public Vector2Int CheckResizeStart(Vector2 cursorPosition)
    {
        //TODO: separate (larger, triangular) threshold for corners
        Vector2Int output = Vector2Int.zero;
        Desktop_Cursor.CursorStates state = Desktop_Cursor.CursorStates.pointer;
        float resizeMargin = 4; //TODO: ensure the margin is at least one pixel
        Vector2 CursorPosition = rect.parent.InverseTransformPoint(cursorPosition);
        if (CursorPosition.x > rect.offsetMin.x && CursorPosition.x < rect.offsetMin.x + resizeMargin)
        {
            output.x -= 1;
            state = Desktop_Cursor.CursorStates.dragX;
        }
        else if (CursorPosition.x < rect.offsetMax.x && CursorPosition.x > rect.offsetMax.x - resizeMargin)
        {
            output.x += 1;
            state = Desktop_Cursor.CursorStates.dragX;
        }
        if (CursorPosition.y > rect.offsetMin.y && CursorPosition.y < rect.offsetMin.y + resizeMargin)
        {
            output.y -= 1;
            if (output.x == 0)
            {
                state = Desktop_Cursor.CursorStates.dragY;
            }
            else
            {
                state = Desktop_Cursor.CursorStates.dragXY;
            }
        }
        else if (CursorPosition.y < rect.offsetMax.y && CursorPosition.y > rect.offsetMax.y - resizeMargin)
        {
            output.y += 1;
            if (output.x == 0)
            {
                state = Desktop_Cursor.CursorStates.dragY;
            }
            else
            {
                state = Desktop_Cursor.CursorStates.dragXY;
            }
        }
        Desktop_Cursor.RequestCursor(state);
        return output;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        PointerInWindow = true;
        MousePositionLast = Input.mousePosition;

        if (Resizable && !Resizing)
        {
            CheckResizeStart(MousePositionLast);
        }
    }
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        PointerInWindow = false;
        if (!Resizing)
        {
            Desktop_Cursor.RequestCursor(Desktop_Cursor.CursorStates.pointer);
        }
    }

    /// <summary>
    /// called when the window gains focus
    /// </summary>
    public virtual void GainedFocus()
    {
        if (titleBar)
        {
            titleBar.SetActiveColor(true);
        }
    }
    /// <summary>
    /// called when the window loses focus (when it was active, but something else became active)
    /// </summary>
    public virtual void LostFocus()
    {
        if (titleBar)
            titleBar.SetActiveColor(false);
        if (KillOnFocusLoss)
        {
            CloseWindow();
        }

    }
    /// <summary>
    /// Locks the window to the viewport
    /// </summary>
    /// <param name="ConstrainTop"></param> if false, the window can pass out the top of the viewport (leave true for user inputs, turn off for animations that need to go into the taskbar)
    public void ConstrainPosition(bool ConstrainTop = true)
    {
        //TODO: doesn't factor in anchors properly (EG if anchors are all top right, it'll be happy to stick off the side of the window)
        RectTransform rect = (RectTransform)transform;
        Vector2 NewPos = rect.localPosition;

        if (ConstrainTop && rect.offsetMax.y> Desktop_Desktop.current.DesktopMax.y)
            NewPos.y -= rect.offsetMax.y - Desktop_Desktop.current.DesktopMax.y;
        else if (rect.offsetMin.y < Desktop_Desktop.current.DesktopMin.y)
            NewPos.y -= rect.offsetMin.y - Desktop_Desktop.current.DesktopMin.y;
        if (rect.offsetMin.x < Desktop_Desktop.current.DesktopMin.x)
            NewPos.x -= rect.offsetMin.x - Desktop_Desktop.current.DesktopMin.x;
        else if (rect.offsetMax.x > Desktop_Desktop.current.DesktopMax.x)
            NewPos.x -= rect.offsetMax.x - Desktop_Desktop.current.DesktopMax.x;
        rect.localPosition = NewPos;
    }

    public void FocusWindow()
    {
        if (Active != this)
        {
            Desktop_Window OldActive = Active;
            transform.SetAsLastSibling();
            Active = this;
            if (OldActive)
            {
                OldActive.LostFocus();
            }
            GainedFocus();
        }
    }

    /// <summary>
    /// called after OpenWindow and ExpandWindow are done animating, 
    /// mostly as a hook for inheritors to use
    /// </summary>
    protected virtual void FinishedOpening() 
    {
        canvasGroup.interactable = true;
    }
    /// <summary>
    /// called after Closewindow and MinimizeWindow are done animating, 
    /// mostly as a hook for inheritors to use
    /// </summary>
    protected virtual void FinishedClosing()
    { 
        gameObject.SetActive(false);
    }

    protected void UpdateTheme(Desktop_StyleTheme newTheme)
    {
        if (titleBar)
        {
            titleBar.SetActiveColor(Active == this);
            titleBar.stowButton.GetComponent<Image>().sprite = CloseToTaskbar ? newTheme.windowMinimizeButton : newTheme.windowCloseButton;
        }
    }
}
