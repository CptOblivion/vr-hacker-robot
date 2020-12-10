using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Desktop_Desktop : MonoBehaviour, IPointerDownHandler
{
    public static Desktop_Desktop current;
    public Desktop_StyleTheme DefaultTheme;
    public static Desktop_StyleTheme currentTheme;

    public delegate void ThemeChangedHandler(Desktop_StyleTheme newTheme);
    public event ThemeChangedHandler OnThemeChanged;

    private void Awake()
    {
        current = this;
        currentTheme = current.DefaultTheme;
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {

        if (Desktop_Window.Active)
        {
            Desktop_Window.Active.LostFocus();
            Desktop_Window.Active = null;
        }
    }

    public static void ChangeTheme(Desktop_StyleTheme newTheme)
    {
        //TODO: maybe we should store the theme contents in a temporary version that we load into on game start (so the player can make changes without the game having to update or save prefabs)
        currentTheme = newTheme;
        current.OnThemeChanged?.Invoke(newTheme);
    }
}
