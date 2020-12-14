using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Desktop_Desktop : MonoBehaviour, IPointerDownHandler
{
    public static Desktop_Desktop current;
    public Desktop_StyleTheme DefaultTheme;
    public static Desktop_StyleTheme currentTheme;
    public static int pixelScale;
    public float Window_ResizeThreshold_Edge = 4;
    public float Window_ResizeThreshold_Corner = 25;

    float GlobalToPixel = 1;

    public bool AdjustScaleForRender = false;
    public bool AdjustMouseScale = false;

    public Camera cameraRender;
    public Camera cameraEvent;
    public Transform root;
    public RenderTexture DesktopOutput;
    public RectTransform DesktopWindowFrame;

    public Vector2 DesktopMin = new Vector2(0, -1016);
    public Vector2 DesktopMax = new Vector2(1920, 0);

    public Vector2 ScreenLast;

    bool init = false;

    public delegate void ThemeChangedHandler(Desktop_StyleTheme newTheme);
    public event ThemeChangedHandler OnThemeChanged;

    private void Awake()
    {
        pixelScale = 1920 / DesktopOutput.width;
        current = this;
        currentTheme = current.DefaultTheme;

        ScreenLast = new Vector2(Screen.width, Screen.height);
        UpdateCanvasScale();

        //UpdateDesktopLimits(); //commented this out since the canvas doesn't get its proper scale until some point in time between Start and the first Update
    }
    void Update()
    {
        if (!init)
        {
            init = true;
            UpdateDesktopLimits();
        }
        if (ScreenLast.x != Screen.width || ScreenLast.y != Screen.height)
        {
            ScreenLast.x = Screen.width;
            ScreenLast.y = Screen.height;
            UpdateCanvasScale();
        }
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {

        if (Desktop_Window.Active)
        {
            Desktop_Window.Active.LostFocus();
            Desktop_Window.Active = null;
        }
    }
    void UpdateCanvasScale()
    {
        cameraEvent.transform.position = new Vector3(Screen.width/2, Screen.height / 2, -10);
        cameraEvent.orthographicSize = cameraEvent.transform.position.y;
        if (AdjustScaleForRender)
        {
            cameraRender.orthographicSize = Screen.height / 2;
            float scale = ((float)Screen.width) / 1920;
            root.localScale = new Vector3(scale, scale, scale);
            root.position = cameraEvent.ScreenToWorldPoint(new Vector3(0, 0, 10));
            GlobalToPixel = scale * pixelScale;
        }
    }

    void UpdateDesktopLimits()
    {
        //TODO: check if this works for non- 16/9 ratios
        DesktopMin = new Vector2(0, -DesktopWindowFrame.rect.height);
        DesktopMax = new Vector2(DesktopWindowFrame.rect.width, 0);
    }

    public static void ChangeTheme(Desktop_StyleTheme newTheme)
    {
        //TODO: maybe we should store the theme contents in a temporary version that we load into on game start (so the player can make changes without the game having to update or save prefabs)
        currentTheme = newTheme;
        current.OnThemeChanged?.Invoke(newTheme);
    }

    public static Vector2 SnapGlobalToPixel(Vector2 GlobalPosition)
    {
        if (current)
        {
            return new Vector2(Mathf.Round(GlobalPosition.x / current.GlobalToPixel) * current.GlobalToPixel, (Mathf.Round(GlobalPosition.y / current.GlobalToPixel)) * current.GlobalToPixel);
        }
        return GlobalPosition;

    }

    public static Vector2 SnapCanvasToPixel(Vector2 CanvasPosition)
    {
        return new Vector2(Mathf.Round(CanvasPosition.x / pixelScale) * pixelScale, (Mathf.Round(CanvasPosition.y / pixelScale)) * pixelScale);
    }
}
