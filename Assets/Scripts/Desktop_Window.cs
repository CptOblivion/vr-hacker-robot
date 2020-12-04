using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_Window : MonoBehaviour, IPointerDownHandler
{
    //TODO: split window root and window element into separate classes
    //TODO: organize all these properties
    enum AnimState { Opening, Open, Closing, Minimizing, Expanding}
    AnimState animState = AnimState.Opening;
    public static Vector2 DesktopSpace = new Vector2(1920, 1016); //TODO: get this algorithmically based on the desktop size
    //TODO: allow multi-edit
    //TODO: figure out alphaHitTestMinimumThreshold and 9-sliced sprites (currently doesn't work)
    public bool AlphaHitTest = false;

    //TODO: make this a prefab and spawn it
    public Button minimizedButton; //this button can always exist, and just be disabled
    //TODO: maybe we can dynamically choose the close or minimize button based on whether or not minimizedButton exists?

    static Vector2 StartSize = new Vector2(64, 40);
    static readonly float OpenTime = 0.1f;
    static readonly float CloseTime = .1f;
    static readonly float MinimizeTime = .25f;
    static readonly float ExpandTime = .2f;
    [HideInInspector]
    public Desktop_TitleBar titleBar;

    static Desktop_Window Active;
    CanvasGroup canvasGroup;

    Vector2 TargetSize;

    float AnimTimer;
    Vector2? AnimTargetPos = null;
    Transform AnimStartPos = null;

    static readonly Vector2 ButtonTargetOffset = new Vector2(0, 128);

    protected virtual void Awake()
    {
        titleBar = GetComponentInChildren<Desktop_TitleBar>();
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
    }

    protected virtual void Start()
    {
        if (minimizedButton)
        {
            minimizedButton.onClick.AddListener(ExpandWindow);
            minimizedButton.transform.SetParent(Desktop_Taskbar.current.minimizedTray);
            minimizedButton.interactable = false;
            minimizedButton.gameObject.SetActive(false);
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
                canvasGroup.interactable = true;
            }
        }
        else if (animState == AnimState.Closing)
        {
            if (!Animate(CloseTime, true))
            {
                gameObject.SetActive(false);
                AnimTargetPos = null;//just in case this was set somehow, we don't want to play an un-minimize animation when we re-expand
            }
        }
        else if (animState == AnimState.Expanding)
        {
            if (!Animate(ExpandTime))
            {
                animState = AnimState.Open;
                canvasGroup.interactable = true;
            }
        }
        else if (animState == AnimState.Minimizing)
        {
            if (!Animate(MinimizeTime, true))
            {
                gameObject.SetActive(false);
                minimizedButton.GetComponent<CanvasGroup>().alpha = 1;
                minimizedButton.interactable = true;
                //TODO: enable taskbar button
            }
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
    public virtual void CloseWindow()
    {
        animState = AnimState.Closing;
        AnimTimer = CloseTime;
        canvasGroup.interactable = false;
    }
    public virtual void MinimizeWindow()
    {
        canvasGroup.interactable = false;
        animState = AnimState.Minimizing;
        AnimTimer = MinimizeTime;

        //reversing start and target pos because we'll be playing the anim backwards on minimize, and we'll want TargetPosition to still be set on ExpandWindow
        AnimTargetPos = transform.position;

        //TODO: add button to taskbar, hide it, set AnimTargetPos, subscribe ExpandWindow to the button
        minimizedButton.gameObject.SetActive(true);
        minimizedButton.transform.SetAsFirstSibling();
        minimizedButton.GetComponent<CanvasGroup>().alpha = 0;
        minimizedButton.interactable = false;
        //TODO: wait a frame (or do this in Update just before the animation?) to make sure the correct position is found
        AnimStartPos = minimizedButton.transform;
    }

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
            minimizedButton.interactable = false;
            minimizedButton.gameObject.SetActive(false);
            //animTargetPos should still be untouched from when we were minimized
            //TODO: make sure this is safe to call if the window has never been opened before (IE no button exists, no target position, etc) (should just play as though it's opening)
        }
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
    }

    public virtual void GainedFocus()
    {
        if (titleBar)
        {
            titleBar.SetActiveColor(true);
        }
    }
    public virtual void LostFocus()
    {
        if (titleBar)
            titleBar.SetActiveColor(false);
    }

    public void ConstrainPosition(bool ConstrainTop = true)
    {
        //optionally don't constrain to the top for animations that need to be able to pass into the taskbar
        RectTransform rect = (RectTransform)transform;
        Vector2 NewPos = rect.localPosition;
        if (ConstrainTop && NewPos.y > 0)
            NewPos.y = 0;
        else if (NewPos.y < -DesktopSpace.y + rect.rect.height)
            NewPos.y = -DesktopSpace.y + rect.rect.height;
        if (NewPos.x < 0)
            NewPos.x = 0;
        else if (NewPos.x > DesktopSpace.x - rect.rect.width)
            NewPos.x = DesktopSpace.x - rect.rect.width;
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
    /*
    public virtual void CloseWindowButton()
    {
        CloseWindow();
    }
    */
}
