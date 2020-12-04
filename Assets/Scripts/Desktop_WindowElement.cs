using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Desktop_WindowElement : MonoBehaviour, IPointerDownHandler
{
    //TODO: split window root and window element into separate classes
    //TODO: allow multi-edit
    //TODO: figure out alphaHitTestMinimumThreshold and 9-sliced sprites (currently doesn't work)
    public bool AlphaHitTest = false;

    [HideInInspector]
    public Desktop_Window window;

    protected virtual void Awake()
    {
        window = transform.parent.GetComponentInParent<Desktop_Window>();
        if (AlphaHitTest) //TODO: figure out if I can test for image editability and automatically disable this if it's not available (catch an error might be the only way?)
        {
            Image image = GetComponent<Image>();
            if (image)
            {
                image.alphaHitTestMinimumThreshold = .9f;
            }
        }
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        window.FocusWindow();
    }
}
