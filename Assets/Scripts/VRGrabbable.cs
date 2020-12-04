using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VRGrabbable : MonoBehaviour
{
    [System.Serializable]
    public class GameObjectEvent : UnityEvent<GameObject> { }
    //TODO: quick-setup button in inspector with options for holsterable small or medium (adds tag component if it doesn't exist, then adds relevant tag
    //  also adds box or sphere collider set to trigger, and rigidbody set to kinematic and not gravity
    public Transform rootObject;
    public Transform HolsterOffset;
    [HideInInspector]
    public VRGrabber grabbed = null;

    public GameObjectEvent OnGrabbed;
    public GameObjectEvent OnReleased;
    [HideInInspector]
    public bool isDynamic;
    public Collider collider;

    int JustGrabbed = 0;

    readonly List<VRGrabber> collidingGrabbers = new List<VRGrabber>();
    void Start()
    {
        if (rootObject == null)
        {
            rootObject = transform;
        }
        collider = GetComponent<Collider>();
        if (!collider.isTrigger && !collider.attachedRigidbody.isKinematic)
        {
            isDynamic = true;
        }
    }

    private void Update()
    {
        if (JustGrabbed > 0)
        {
            JustGrabbed--;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        VRGrabber grabber = other.GetComponent<VRGrabber>();
        if (CanBeGrabbed(grabber))
        {
            collidingGrabbers.Insert(0, grabber);
            if (grabbed && JustGrabbed == 0 && grabbed.grabberType == VRGrabber.GrabberType.Hand)
            {
                grabbed.VibrateTouchHolster();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        VRGrabber grabber = other.GetComponent<VRGrabber>();
        if (CanBeGrabbed(grabber))
        {
            collidingGrabbers.Remove(grabber);
        }
    }

    public void OnGrab(VRGrabber grabber)
    {
        grabbed = grabber;
        JustGrabbed = 2;
        OnGrabbed.Invoke(grabbed.gameObject);
    }
    public void OnRelease()
    {
        OnReleased.Invoke(grabbed.gameObject);
        grabbed = null;

        if (collidingGrabbers.Count > 0)
        {
            collidingGrabbers[0].Handoff(this);
            collidingGrabbers.RemoveAt(0);
        }
    }

    bool CanBeGrabbed(VRGrabber grabber)
    {
        return grabber &&
            ObjectTags.CompareTags(GetComponent<ObjectTags>(), grabber.TagsBlacklist, grabber.TagsWhitelist, grabber.StrictWhitelist) &&
            (grabber.grabberType == VRGrabber.GrabberType.Auto || grabber.grabberType == VRGrabber.GrabberType.Holster);
    }
}
