using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRTouchable : MonoBehaviour
{
    public UnityEvent OnTouch;
    private void OnTriggerEnter(Collider other)
    {
        //TODO: separate collider for hands (as opposed to the "can grab" region in front of palm)
        VRGrabber grabber = other.GetComponent<VRGrabber>();
        if (grabber && grabber.grabberType == VRGrabber.GrabberType.Hand)
        {
            grabber.VibrateTouchTouchable();
            OnTouch.Invoke();

        }
    }
}
