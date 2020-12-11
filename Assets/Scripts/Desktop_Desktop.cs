using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Desktop_Desktop : MonoBehaviour, IPointerDownHandler
{
    public static Desktop_Desktop current;
    public Desktop_StyleTheme DefaultTheme;
    public static Desktop_StyleTheme currentTheme;

    public bool AdjustScaleForRender = false;
    public bool AdjustMouseScale = false;

    public Camera cameraRender;
    public Camera cameraEvent;
    public Transform root;
    public RenderTexture tex;
    public RectTransform DesktopWindowFrame;

    public Vector2 DesktopMin = new Vector2(0, -1016);
    public Vector2 DesktopMax = new Vector2(1920, 0);

    public Vector2 ScreenLast;

    bool init = false;

    public delegate void ThemeChangedHandler(Desktop_StyleTheme newTheme);
    public event ThemeChangedHandler OnThemeChanged;

    private void Awake()
    {
        current = this;
        currentTheme = current.DefaultTheme;

        ScreenLast = new Vector2(Screen.width, Screen.height);
        UpdateEventCamera();
        UpdateRenderCamera();
        UpdateRoot();

        //UpdateDesktopLimits();
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
            UpdateEventCamera();
            UpdateRenderCamera();
            UpdateRoot();
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
    void UpdateEventCamera()
    {
        cameraEvent.transform.position = new Vector3(Screen.width/2, Screen.height / 2, -10);
        //cameraEvent.transform.position = new Vector3(tex.width / 2, tex.height / 2, -10);
        cameraEvent.orthographicSize = cameraEvent.transform.position.y;
    }

    void UpdateRenderCamera()
    {
        if (AdjustScaleForRender)
        {
            cameraRender.orthographicSize = Screen.height/2;
        }
    }

    void UpdateRoot()
    {
        if (AdjustScaleForRender)
        {
            float scale = ((float)Screen.width) / 1920;
            root.localScale = new Vector3(scale, scale, scale);
            root.position = cameraEvent.ScreenToWorldPoint(new Vector3(0, 0, 10));
        }
    }

    void UpdateDesktopLimits()
    {
        //TODO: fix so desktopWindows can scale with desktop
        DesktopMin = new Vector2(0, -DesktopWindowFrame.rect.height);
        DesktopMax = new Vector2(DesktopWindowFrame.rect.width, 0);
    }

    public static void ChangeTheme(Desktop_StyleTheme newTheme)
    {
        //TODO: maybe we should store the theme contents in a temporary version that we load into on game start (so the player can make changes without the game having to update or save prefabs)
        currentTheme = newTheme;
        current.OnThemeChanged?.Invoke(newTheme);
    }
}
