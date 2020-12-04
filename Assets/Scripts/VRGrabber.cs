using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class VRGrabber : MonoBehaviour
{
    class VelocityContainer
    {
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public Vector3 CrossVelocity;
        public Vector3 RealVelocity;
        public VelocityContainer(Vector3 vel, Vector3 angVel, Vector3 crossVel)
        {
            Velocity = vel;
            AngularVelocity = angVel;
            CrossVelocity = crossVel;
            RealVelocity = vel + crossVel;
        }
    }
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
    public VRGrabbable heldObject = null;

    public float GrabbableRoughnessFreq = 1000;
    public float GrabbableRoughnessAmp = .1f;


    static int VelocityQueueSize = 5;
    Queue<VelocityContainer> Velocity = new Queue<VelocityContainer>(VelocityQueueSize);
    Vector3 GrabbedOffset;

    Vector3 LastVelocity;
    Quaternion LastRotation;


    int JustReleased = 0;

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
        LastVelocity = transform.position;
        LastRotation = transform.rotation;
    }

    void UpdateVelocity()
    {
        if (Velocity.Count == 10)
        {
            Velocity.Dequeue();
        }
        (transform.rotation * Quaternion.Inverse(LastRotation)).ToAngleAxis(out float angle, out Vector3 axis);
        angle *= Mathf.Deg2Rad;
        Vector3 AngularVel = (angle * axis) / Time.deltaTime;
        Vector3 Vel = (transform.position - LastVelocity) / Time.deltaTime;
        Vector3 CrossVel = Vector3.Cross(AngularVel*Time.deltaTime, transform.TransformPoint(GrabbedOffset)-transform.position) * 10;
        Velocity.Enqueue(new VelocityContainer(Vel, AngularVel, CrossVel));
        LastVelocity = transform.position;
        LastRotation = transform.rotation;

        DebugDrawLine.DrawLine(heldObject.transform.position, heldObject.transform.position + CrossVel);
    }

    Vector3[] GetReleaseVelocity()
    {
        Vector3[] ReleaseVel = GetReleaseVelocitySmooth();
        /*
        foreach (VelocityContainer container in Velocity.ToArray())
        {
            DebugDrawLine.DrawLine(heldObject.transform.position, heldObject.transform.position + container.RealVelocity, 3);
        }
        DebugDrawLine.DrawLine(heldObject.transform.position, heldObject.transform.position + ReleaseVel[0], 3);
        */
        return ReleaseVel;
    }
    Vector3[] GetReleaseVelocitySmooth()
    {
        //attempts to get the player's intended throw by finding the frames in the buffered velocities with the most similar direction
        //then averages a few frames around that, dropping any inputs that head the other direction (EG a sudden stop)
        int AverageSize = 1;
        int averageCount = 1;
        int TargetIndex = 0;
        float PeakVelChange = 0;
        VelocityContainer[] Vel = Velocity.ToArray();

        for (int i = 1; i < Vel.Length; i++)
        {
            float currentVelChange = Vector3.Dot(Vel[i].RealVelocity, Vel[i-1].RealVelocity);
            if (currentVelChange > PeakVelChange)
            {
                PeakVelChange = currentVelChange;
                TargetIndex = i;
            }
        }
        Vector3 AverageVel = Vel[TargetIndex].RealVelocity;
        Vector3 AverageAngular = Vel[TargetIndex].AngularVelocity;
        for (int i = TargetIndex - AverageSize; i <= TargetIndex + AverageSize; i++)
        {
            if (i > 0 && i < Vel.Length && i != TargetIndex)
            {
                if ( Vector3.Dot(AverageVel, Vel[i].RealVelocity) > 0)
                {
                    AverageVel += Vel[i].RealVelocity;
                    AverageAngular += Vel[i].AngularVelocity;
                    averageCount++;
                }
            }
        }
        AverageVel /= averageCount;
        AverageAngular /= averageCount;
        return new Vector3[] { AverageVel, AverageAngular };
    }

    Vector3[] GetReleaseVelocityOffset()
    {

        int AverageSize = 1;
        int averageCount = 0;
        int peakIndex = 3;
        VelocityContainer[] Vel = Velocity.ToArray();

        peakIndex = Mathf.Max(Vel.Length - peakIndex, 0);
        Vector3 AverageVel = Vector3.zero;
        Vector3 AverageAngular = Vector3.zero;
        for (int i = peakIndex - AverageSize; i <= peakIndex + AverageSize; i++)
        {
            if (i > 0 && i < Vel.Length)
            {
                AverageVel += Vel[i].Velocity + Vel[i].CrossVelocity;
                AverageAngular += Vel[i].AngularVelocity;
                averageCount++;
            }
        }
        AverageVel /= averageCount;
        AverageAngular /= averageCount;
        return new Vector3[] { AverageVel, AverageAngular };
    }


    Vector3[] GetReleaseVelocityAverage()
    {
        VelocityContainer[] Vel = Velocity.ToArray();
        Vector3 AverageVel = Vector3.zero;
        Vector3 AverageAngular = Vector3.zero;
        for (int i = 0; i < Vel.Length; i++)
        {
            AverageVel += Vel[i].Velocity + Vel[i].CrossVelocity;
            AverageAngular += Vel[i].AngularVelocity;
        }
        AverageVel /= Vel.Length;
        AverageAngular /= Vel.Length;
        Velocity.Clear();
        return new Vector3[] { AverageVel, AverageAngular };
    }

    Vector3[] GetReleaseVelocityPeak()
    {
        int AverageSize = 1;
        int averageCount = 0;
        int peakIndex = 0;
        float peakValue = 0;
        VelocityContainer[] Vel = Velocity.ToArray();

        for(int i = 0; i < Vel.Length; i++)
        {
            float speed = Vel[i].Velocity.magnitude + Vel[i].CrossVelocity.magnitude;
            if (speed > peakValue)
            {
                peakValue = speed;
                peakIndex = i;
            }
        }
        Vector3 AverageVel = Vector3.zero;
        Vector3 AverageAngular = Vector3.zero;
        for (int i = peakIndex - AverageSize; i <= peakIndex+AverageSize; i++)
        {
            if (i > 0 && i < Vel.Length)
            {
                AverageVel += Vel[i].Velocity + Vel[i].CrossVelocity;
                AverageAngular += Vel[i].AngularVelocity;
                averageCount++;
            }
        }
        AverageVel /= averageCount;
        AverageAngular /= averageCount;
        return new Vector3[] { AverageVel, AverageAngular };
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

                    //if the object is unevenly scaled, treat the smallest axis as the basis for bump scaling
                    float Scale = Mathf.Min(grabbable.transform.lossyScale.x, grabbable.transform.lossyScale.y, grabbable.transform.lossyScale.z);
                    distance = (NewPosition - grabbablePositions[grabbable]).magnitude * Scale;
                    if (distance > 1 / GrabbableRoughnessFreq) //in theory, this just keeps our last position on the last bump until we hit a new one, if we're traveling at less than one bump per timestep
                    {
                        VRHaptics.Slide(distance, GrabbableRoughnessFreq, GrabbableRoughnessAmp, 0, 0, hand);
                        grabbablePositions[grabbable] = NewPosition;
                    }
                }
            }
        }
        if (heldObject && heldObject.isDynamic)
        {
            UpdateVelocity();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnterCollision(other);
    }
    private void OnTriggerExit(Collider other)
    {
        ExitCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnterCollision(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        ExitCollision(collision.collider);
    }


    void EnterCollision(Collider other)
    {
        VRGrabbable grabbable = other.GetComponent<VRGrabbable>();
        if (grabbable)
        {
            if (ObjectTags.CompareTags(other.GetComponent<ObjectTags>(), TagsBlacklist, TagsWhitelist, StrictWhitelist))
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

    void ExitCollision(Collider other)
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
                heldObject = grabbable;
                Collider collider = grabbable.GetComponent<Collider>();
                if (!collider.isTrigger && !collider.attachedRigidbody.isKinematic)
                {
                    collider.attachedRigidbody.isKinematic = true;
                }
                if (heldObject.grabbed)
                {
                    heldObject.grabbed.GrabSteal();
                }
                heldObject.rootObject.SetParent(transform);
                GrabbedOffset = heldObject.rootObject.localPosition;
                if (grabberType != GrabberType.Hand)
                {
                    //TODO: lerp into place
                    if (heldObject.HolsterOffset == null)
                    {
                        heldObject.rootObject.localPosition = Vector3.zero;
                        heldObject.rootObject.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        heldObject.rootObject.localRotation = Quaternion.Inverse(heldObject.HolsterOffset.localRotation);
                        heldObject.rootObject.position = (heldObject.transform.position - heldObject.HolsterOffset.position)+transform.position;
                    }
                }
                heldObject.OnGrab(this);
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
        if (heldObject)
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
        heldObject = null;
        GetComponent<Collider>().enabled = true;
        OnRelease.Invoke();
    }
    void GrabRelease()
    {
        heldObject.rootObject.SetParent(null, true);
        heldObject.OnRelease();
        Collider collider = heldObject.GetComponent<Collider>(); //TODO: should I be checking on currentObject.rootobject?
        if (heldObject.isDynamic) 
        { 
            collider.attachedRigidbody.isKinematic = false;
            Vector3[] Velocities = GetReleaseVelocity();
            collider.attachedRigidbody.velocity = Velocities[0];
            collider.attachedRigidbody.angularVelocity = Velocities[1];
        }
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
