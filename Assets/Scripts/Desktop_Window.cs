using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_Window : MonoBehaviour, IPointerDownHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    //TODO: split window root and window element into separate classes
    //TODO: organize all these properties
    enum AnimState { Opening, Open, Closing, Minimizing, Expanding}
    AnimState animState = AnimState.Opening;
    public static Vector2 DesktopSpace = new Vector2(1920, 1016); //TODO: get this algorithmically based on the desktop size
    //TODO: allow multi-edit
    //TODO: figure out alphaHitTestMinimumThreshold and 9-sliced sprites (currently doesn't work)
    public bool AlphaHitTest = false;
    public bool KillOnFocusLoss = false;
    public bool HideOnStartup = false;
    public bool CloseToTaskbar = false;
    public bool Resizable = false;
    public Vector2 MinSize = Vector2.zero;
    //TODO: reference the style scriptable instead
    public Desktop_StyleTheme theme;
    public Sprite appIcon;
    public string AppName;

    bool PointerInWindow = false;
    Vector2 MousePositionLast;
    bool Resizing = false;
    Vector2Int ResizeDirection;
    Vector2 ResizeOrigin;
    Vector2 ResizeOffset;


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
                titleBar.stowButton.GetComponent<Image>().sprite = theme.windowMinimizeButton;
                minimizedButton = Desktop_MinimizedButton.AddMinimizedButton(minimizedPrefab, this);
            }
            else
            {
                titleBar.stowButton.GetComponent<Image>().sprite = theme.windowCloseButton;
            }
            titleBar.stowButton.onClick.AddListener(Program_Stow);
        }

        RectTransform rectTransform = (RectTransform)transform;
        TargetSize = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
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
                if (Resizable)
                {
                    CheckResizeStart(MousePositionLast);
                }
            }
        }
    }

    protected virtual void OnDestroy()
    {
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
        if (Resizable)
        {
            ResizeDirection = CheckResizeStart(eventData.position);
            Resizing = true;
            ResizeOrigin = eventData.position;
            //TODO: set resizeOffset (eg if the cursor is 1 pixel inside of the left edge, keep track of that)
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {

    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        Resizing = false;
    }
    

    Vector2Int CheckResizeStart(Vector2 cursorPosition)
    {
        Vector2Int output = Vector2Int.zero;
        Desktop_Cursor.CursorStates state = Desktop_Cursor.CursorStates.pointer;
        float resizeMargin = 4;
        Vector4 ActualRect = GetActualRect(rect.localPosition);
        Vector2 CursorPosition = rect.parent.InverseTransformPoint(cursorPosition);
        if (CursorPosition.x > ActualRect.x && CursorPosition.x < ActualRect.x + resizeMargin)
        {
            output.x -= 1;
            state = Desktop_Cursor.CursorStates.dragX;
        }
        else if (CursorPosition.x < ActualRect.z && CursorPosition.x > ActualRect.z - resizeMargin)
        {
            output.x += 1;
            state = Desktop_Cursor.CursorStates.dragX;
        }
        if (CursorPosition.y > ActualRect.y && CursorPosition.y < ActualRect.y + resizeMargin)
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
        else if (CursorPosition.y < ActualRect.w && CursorPosition.y > ActualRect.w - resizeMargin)
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

        if (Resizable)
        {
            CheckResizeStart(MousePositionLast);
        }
    }
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        PointerInWindow = false;
        if (Resizable)
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
        //optionally don't constrain to the top for animations that need to be able to pass into the taskbar
        RectTransform rect = (RectTransform)transform;
        Vector2 NewPos = rect.localPosition;

        Vector4 ActualRect = GetActualRect(NewPos);

        if (ConstrainTop && ActualRect.w> 0)
            NewPos.y -= ActualRect.w;
        else if (ActualRect.y < -DesktopSpace.y)
            NewPos.y -= ActualRect.y - -DesktopSpace.y; //I should just put that in as a plus but it's easier for my dumb brain to think of it as subtracting a negative
        if (ActualRect.x < 0)
            NewPos.x -= ActualRect.x;
        else if (ActualRect.z > DesktopSpace.x)
            NewPos.x -= ActualRect.z -DesktopSpace.x;
        rect.localPosition = NewPos;
    }
    /// <summary>
    /// returns the rect around the given position (MinX, MinY, MaxX, Max)
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector4 GetActualRect(Vector2 pos)
    {
        //TODO: probably doesn't account for scale (test)
        return new Vector4(pos.x + rect.rect.min.x, pos.y + rect.rect.min.y, pos.x + rect.rect.max.x, pos.y + rect.rect.max.y);
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
}
