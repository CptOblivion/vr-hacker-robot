using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Desktop_Cursor: MonoBehaviour
{
    private void Awake()
    {
        Cursor.visible = false;
    }
    private void Update()
    {
        //transform.position = new Vector3(Input.mousePosition.x / Screen.width * 1920, Input.mousePosition.y / Screen.height * 1080, 0);
        transform.localPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
    }
}
