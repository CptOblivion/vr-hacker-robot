using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class VRGrabber : MonoBehaviour
{
    public enum GrabberType {
        Auto, //grab anything that runs into this, as long as it's not already grabbed
        Holster, //only grabs if a hand lets go of the grabbable while colliding with this (grabbable can't be dropped in from a distance or scooped up)
        Force, //grabs anything that runs into this, unless that grabber is also set to Force (will steal from player's hands)
        Hand}; //TODO: this can probably be reduced down to just "Hand"
    public GrabberType grabberType = GrabberType.Auto;
    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Input_Sources controller;
    public SteamVR_Action_Vibration vibrator;

    public SteamVR_Input_Sources hand;

    public string[] TagsBlacklist = new string[] { };
    public string[] TagsWhitelist = new string[] { };
    public bool StrictWhitelist = false; //if true, requires all whitelist tags to be matched

    [HideInInspector]
    public bool Grabbing = false;

    public UnityEvent OnGrab;
    public UnityEvent OnRelease;

    //TODO: turn this into Dictionary<VRGrabbable, Vector3> to track last position relative to this object
    readonly List<VRGrabbable> grabbables = new List<VRGrabbable>();
    readonly Dictionary<VRGrabbable, Vector3> grabbablePositions = new Dictionary<VRGrabbable, Vector3>();
    public VRGrabbable currentObject = null;

    public float GrabbableRoughnessFreq = 1000;
    public float GrabbableRoughnessAmp = .1f;


    int JustReleased = 0;

    static readonly float CubeDiagonal = Mathf.Sqrt(3);

    private void Start()
    {
        if (vibrator != null)
        {
            VRHaptics.Init(vibrator);
        }
    }

    private void OnEnable()
    {
        if (grabberType == GrabberType.Hand)
        {
            //grabAction.AddOnStateDownListener(ToggleGrab, hand);
            grabAction.AddOnStateDownListener(HandGrab, hand);
            grabAction.AddOnStateUpListener(HandRelease, hand);
        }
    }

    private void OnDisable()
    {
        if (grabberType == GrabberType.Hand)
        {
            //grabAction.RemoveOnStateDownListener(ToggleGrab, hand);
            grabAction.RemoveOnStateDownListener(HandGrab, hand);
            grabAction.RemoveOnStateUpListener(HandRelease, hand);
        }
    }

    private void Update()
    {
        if (JustReleased>0)
        {
            JustReleased--;
        }
    }

    private void FixedUpdate()
    {
        if (grabberType == GrabberType.Hand)
        {
            if (grabbables.Count > 0)
            {
                Vector3 NewPosition;
                float distance;
                foreach (VRGrabbable grabbable in grabbables)
                {
                    //we use the hand's position in grabbable space rather than the inverse, to allow for sliding effects when rotating the object
                    //TODO: account for object scale
                    NewPosition = grabbable.transform.InverseTransformPoint(transform.position);
                    distance = (NewPosition - grabbablePositions[grabbable]).magnitude * (grabbable.transform.lossyScale.magnitude/CubeDiagonal);
                    if (distance > 1 / GrabbableRoughnessFreq) //in theory, this just keeps our last position on the last bump until we hit a new one, if we're traveling at less than one bump per timestep
                    {
                        VRHaptics.Slide(distance, GrabbableRoughnessFreq, GrabbableRoughnessAmp, 0, 0, hand);
                        grabbablePositions[grabbable] = NewPosition;
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        VRGrabbable grabbable = other.GetComponent<VRGrabbable>();
        if (grabbable)
        {
            if (ObjectTags.CompareTags(other.GetComponent<ObjectTags>(), TagsBlacklist, TagsWhitelist,  StrictWhitelist))
            {
                if (grabberType == GrabberType.Force || (grabberType == GrabberType.Auto && grabbable.grabbed == null))
                {
                    InitiateGrab(grabbable);
                }
                else
                {
                    grabbables.Insert(0, grabbable);
                    grabbablePositions.Add(grabbable, grabbable.transform.InverseTransformPoint(transform.position));
                    if (JustReleased == 0 && grabberType == GrabberType.Hand)
                    {
                        VibrateTouchGrabbable();
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        VRGrabbable grabbable = other.GetComponent<VRGrabbable>();
        if (grabbable)
        {
            grabbables.Remove(grabbable);
            grabbablePositions.Remove(grabbable);
        }
    }



    void InitiateGrab(VRGrabbable grabbable)
    {
        if (grabbable != null)
        {
            if (!grabbable.grabbed || grabbable.grabbed.grabberType != GrabberType.Force)
            {
                Grabbing = true;
                currentObject = grabbable;
                if (currentObject.grabbed)
                {
                    currentObject.grabbed.GrabSteal();
                }
                currentObject.rootObject.SetParent(transform);
                if (grabberType != GrabberType.Hand)
                {
                    //TODO: lerp into place
                    if (currentObject.HolsterOffset == null)
                    {
                        currentObject.rootObject.localPosition = Vector3.zero;
                        currentObject.rootObject.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        currentObject.rootObject.localRotation = Quaternion.Inverse(currentObject.HolsterOffset.localRotation);
                        currentObject.rootObject.position = (currentObject.transform.position - currentObject.HolsterOffset.position)+transform.position;
                    }
                }
                currentObject.OnGrab(this);
                OnGrab.Invoke();
                GetComponent<Collider>().enabled = false;
                grabbables.Clear();
                grabbablePositions.Clear();
            }
        }
    }

    void HandGrab(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        if (grabbables.Count > 0)
        {
            InitiateGrab(grabbables[0]);
            //TODO: shape fingers according to object
        }
    }

    void HandRelease(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        if (currentObject)
        {
            GrabRelease();
        }
    }

    public void GrabSteal()
    {
        Released();
    }

    void Released()
    {
        Grabbing = false;
        JustReleased = 2;
        currentObject = null;
        GetComponent<Collider>().enabled = true;
        OnRelease.Invoke();
    }
    void GrabRelease()
    {
        currentObject.rootObject.SetParent(null, true);
        currentObject.OnRelease();
        Released();
    }

    public void Handoff(VRGrabbable grabbable)
    {
        //this is called by the grabbed object if it's released while colliding with this. Initiates grab.
        InitiateGrab(grabbable);
    }

    //TODO: a vibrate styleguide or something
    public void VibrateTouchGrabbable()
    {
        vibrator.Execute(0, .3f, 80, 1, controller);
    }

    public void VibrateTouchHolster()
    {
        vibrator.Execute(0, .05f, 80, .5f, controller);
    }

    public void VibrateTouchTouchable()
    {
        vibrator.Execute(0, .07f, 90, .6f, controller);
    }
}
