using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Desktop_Desktop : MonoBehaviour, IPointerDownHandler
{
    // Start is called before the first frame update
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (Desktop_Window.Active)
        {
            Desktop_Window.Active.LostFocus();
            Desktop_Window.Active = null;
        }

    }
}
