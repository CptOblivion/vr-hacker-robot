using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Desktop_Taskbar : MonoBehaviour
{
    public static Desktop_Taskbar current;
    public Transform minimizedTray;
    // Start is called before the first frame update
    void Awake()
    {
        current = this;
    }
}
