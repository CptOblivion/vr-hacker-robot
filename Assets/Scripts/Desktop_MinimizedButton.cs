using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Desktop_MinimizedButton : MonoBehaviour
{
    public Text text;
    public Button button;
    public Image icon;
    [HideInInspector]
    public CanvasGroup canvasGroup;
    [HideInInspector]
    public RectTransform rect;
    Desktop_Window window;

    public static Desktop_MinimizedButton AddMinimizedButton(Desktop_MinimizedButton prefab, Desktop_Window application)
    {
        Desktop_MinimizedButton newButton = Instantiate(prefab);
        newButton.window = application;
        if (newButton.window.appIcon)
        {
            newButton.icon.sprite = newButton.window.appIcon;
        }
        else
        {
            newButton.icon.gameObject.SetActive(false);
        }
        newButton.text.text = newButton.window.AppName; //TODO: expand based on name length?
        newButton.canvasGroup = newButton.gameObject.AddComponent<CanvasGroup>();
        newButton.rect = (RectTransform)newButton.transform;
        if (Desktop_Taskbar.current)
        {
            newButton.rect.SetParent(Desktop_Taskbar.current.minimizedTray);
        }
        else
        {
            //slower, but works even if this script initializes before Desktop_Taskbar
            newButton.rect.SetParent(FindObjectOfType<Desktop_Taskbar>().minimizedTray);
        }
        newButton.rect.localScale = Vector3.one;
        newButton.button.interactable = false;
        newButton.gameObject.SetActive(false);
        newButton.button.onClick.AddListener(newButton.Clicked);
        return newButton;
    }

    void Clicked()
    {
        window.Program_Launch();
        button.interactable = false;
        gameObject.SetActive(false);
    }

    public void WindowMiniming()
    {
        gameObject.SetActive(true);
        transform.SetAsFirstSibling();
        canvasGroup.alpha = 0;
        button.interactable = false;
    }
    public void WindowMinimized()
    {
        canvasGroup.alpha = 1;
        button.interactable = true;
    }

}
