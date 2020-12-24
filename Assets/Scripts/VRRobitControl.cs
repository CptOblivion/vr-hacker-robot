using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.Playables;

public class VRRobitControl : MonoBehaviour
{
    //TODO: custom editor that is actually readable
    [System.Serializable]
    public class Finger
    {
        [System.Serializable]
        public class Joint
        {
            public Transform joint;
            public float MaxAngle;
            public float CornerWidth = .01f;
            //public Transform ray;
            [HideInInspector]
            public Quaternion home;
            [HideInInspector]
            public Quaternion OverrideRotation;
        }
        public Joint[] joints;
    }
    public SteamVR_PlayArea PlayArea;
    public float CompressDistance = .15f;
    public float StretchDistance = .075f;
    //public float CompressAdjust = 7;
    //public float StretchAdjust = 12;

    public Transform HeadBone;
    public Transform EyelineBone;
    //public Transform WaistBone;
    public Transform[] SpineStretchArray;
    float[] SpineStretchRest;
    public Transform HeadVRTarget;
    public Transform HeadVRRoot;
    public Transform[] HandIKTargets = new Transform[2];
    public Transform[] HandVRRoots = new Transform[2];
    [Range(0,1)]
    public float TorsoRotationHandWeight = .5f;
    public float TorsoRotationHandDistanceNear = .02f;
    public float TorsoRotationHandDistanceFar = .05f;
    public SteamVR_Action_Boolean menuButton;
    public SteamVR_Action_Boolean snapTurnLeft;
    public SteamVR_Action_Boolean snapTurnRight;
    public SteamVR_Action_Single Pinch;

    public Finger[] FingersL;
    public Finger[] FingersR;
    bool OverrideHandL = false;
    bool OverrideHandR = false;

    public VRGrabber[] hands = new VRGrabber[2];

    public float SnapTurnAngle = 15;

    public float HandFloorDistanceMargin = .05f;

    float ButtonHoldTime = 0;
    readonly float OriginResetTime = 2f;

    public float HandSafetyMoveDistance = .2f;

    ISteamVR_Action_In HoldingMenu = null;

    readonly Vector3?[] HandFloorLast = new Vector3?[] { null, null };

    Animator anim;
    Camera vrCamera;
    float RobotRestHeight;
    public float PlayerRestHeight = 0;

    bool init = false;

    bool FreezeRig = false;

    readonly List<DebugLine> tempDebugLines = new List<DebugLine>();
    readonly List<DebugLine> debugLines = new List<DebugLine>();
    //TODO: move debug lines stuff into its own class
    bool AddDebugLines = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        menuButton.AddOnStateDownListener(OnMenuButtonDown, SteamVR_Input_Sources.Any);
        snapTurnLeft.AddOnStateDownListener(OnSnapTurnLeft, SteamVR_Input_Sources.Any);
        snapTurnRight.AddOnStateDownListener(OnSnapTurnRight, SteamVR_Input_Sources.Any);
        Pinch.AddOnChangeListener(OnPinchLeft, SteamVR_Input_Sources.LeftHand);
        Pinch.AddOnChangeListener(OnPinchRight, SteamVR_Input_Sources.RightHand);

        hands[0].OnGrab.AddListener(OnGrabLeft);
        hands[0].OnRelease.AddListener(OnReleaseLeft);
        hands[1].OnGrab.AddListener(OnGrabRight);
        hands[1].OnRelease.AddListener(OnReleaseRight);

        //TODO: scale-agnostic version that gets eyeline bone position in root object space
        RobotRestHeight = EyelineBone.position.y;

        SpineStretchRest = new float[SpineStretchArray.Length];
        for (int i = 0; i < SpineStretchArray.Length; i++)
        {
            //TODO: blenderfix option for these, probably
            SpineStretchRest[i] = SpineStretchArray[i].localPosition.y;
        }
        //leaving these comments in because what was I even thinking, all of this stuff cancels itself out
        //CompressAdjust = 10 - (StretchDistance / (StretchDistance + CompressDistance))*10;
        //StretchAdjust = 10 + (StretchDistance / (StretchDistance + CompressDistance))*10;
        vrCamera = FindObjectOfType<SteamVR_CameraHelper>().GetComponent<Camera>();

        anim.SetLayerWeight(1, 0);
        anim.SetLayerWeight(2, 0);

    }
    void Update()
    {
        DrawDebugLines();
        if (HoldingMenu != null)
        {
            OnMenuButtonHold();
        }
        if (!FreezeRig)
        {
            //TODO: limit elbow roll (~90 degree arc)
            //TODO: neck/back bending simulation
            //TODO: on locomotion, body pulls ahead of camera a bit (delay final position update until just before render?) slightly nauseating if looking down
            //   simple fix: just hide body (and holstered items) from VR view (preferred: actually fix the issue)

            //TODO: add UI option to select hands (expand armature heierarchy)


            HeadVRTarget.rotation = vrCamera.transform.rotation;
            HeadVRTarget.position = vrCamera.transform.position;

            float VirtualFloorHeight = PlayerRestHeight - RobotRestHeight;


            float HandOffsetL = HandVRRoots[0].parent.localPosition.y - (VirtualFloorHeight);
            float HandOffsetR = HandVRRoots[1].parent.localPosition.y - (VirtualFloorHeight);

            //TODO: vibrate on putting hand in floor

            float HandOffset = Mathf.Min(Mathf.Min(0, HandOffsetL), Mathf.Min(0, HandOffsetR));

            //TODO: this should be using the virtual hand locations, rather than IRL controller positions
            //  then since the model origin is 0, we can just use the y position of the hand bone in armature space (or an object parented to it to set a reasonable hand position)
            if (HandOffset < 0)
            {
                //currently, just uses whichever hand is lower
                //TODO: when hands are within a certain height of each other, use an average point between them (and maybe factor in rotation too)
                int LowerHand = 0;
                if (HandOffsetR < HandOffsetL) LowerHand = 1;

                if (HandFloorLast[LowerHand] != null)
                {
                    Vector3 HandShift = (Vector3)(HandFloorLast[LowerHand] - HandVRRoots[LowerHand].parent.position);
                    HandShift.y = 0;
                    //transform.position += HandShift;
                    vrCamera.transform.parent.position += HandShift;

                }

            }
            if (HandOffsetL < 0)
            {
                HandFloorLast[0] = HandVRRoots[0].parent.position;
            }
            else
            {
                HandFloorLast[0] = null;
            }
            if (HandOffsetR < 0)
            {
                HandFloorLast[1] = HandVRRoots[1].parent.position;
            }
            else
            {
                HandFloorLast[1] = null;
            }

            Vector3 HandVector = (HandVRRoots[0].position + HandVRRoots[1].position) / 2;
            HandVector -= transform.position;

            HandVector.y = 0;
            float factor = TorsoRotationHandWeight * Mathf.Clamp01((HandVector.magnitude - TorsoRotationHandDistanceNear) / (TorsoRotationHandDistanceFar - TorsoRotationHandDistanceNear));
            //DebugDisplay.AddLine(factor);
            transform.rotation = Quaternion.Lerp(Quaternion.LookRotation(new Vector3(HeadVRTarget.forward.x, 0, HeadVRTarget.forward.z), Vector3.up), Quaternion.LookRotation(HandVector, Vector3.up), factor);
            //float TiltAngle = Mathf.Acos(HeadVRTarget.up.y) * Mathf.Rad2Deg;

            //offset play space vertically if we're more than stretchheight + headoffset above ground, or less than headoffset-compressheight
            //TODO: add collision, set to current floor height instead of hardcoded y=0
            //TODO: if hands would be below the floor, rotate the IK targets to lay hands on the floor, rather than trying to match hand position
            float OffsetToApply = VirtualFloorHeight + HandOffset;
            float PlayerCurrentHeight = vrCamera.transform.localPosition.y;
            float StretchFactor;
            if (PlayerCurrentHeight > PlayerRestHeight) //stretching
            {
                StretchFactor = Mathf.Min(StretchDistance, (PlayerCurrentHeight - PlayerRestHeight));
                if (PlayerCurrentHeight > PlayerRestHeight + StretchDistance)
                {
                    OffsetToApply += PlayerCurrentHeight - (PlayerRestHeight + StretchDistance);
                    //max stretch
                }
                Transform t;
                for (int i = 0; i < SpineStretchArray.Length; i++)
                {
                    t = SpineStretchArray[i];
                    t.localPosition = new Vector3(t.localPosition.x, SpineStretchRest[i] + ((StretchFactor / SpineStretchArray.Length) / 100), t.localPosition.z); //TODO: that /100 should probably be togglable with a BlenderFix option
                }
            }
            else //squashing
            {
                StretchFactor = Mathf.Max(-CompressDistance, (PlayerCurrentHeight - PlayerRestHeight));
                if (PlayerCurrentHeight < PlayerRestHeight - CompressDistance)
                {
                    OffsetToApply += PlayerCurrentHeight - (PlayerRestHeight - CompressDistance);
                    //max squash
                }
                Transform t;
                for (int i = 0; i < SpineStretchArray.Length; i++)
                {
                    t = SpineStretchArray[i];
                    t.localPosition = new Vector3(t.localPosition.x, SpineStretchRest[i] + ((StretchFactor / SpineStretchArray.Length) / 100), t.localPosition.z);
                }
            }
            vrCamera.transform.parent.position = new Vector3(vrCamera.transform.parent.position.x, -OffsetToApply, vrCamera.transform.parent.position.z);

            //Vector3 ActualHeadVRRootPosition = vrCamera.transform.parent.TransformPoint(HeadVRRoot.position); //TODO: figure out why this is necessary (the "global" position of the children of [CameraRig] doesn't update when CameraRig moves)
            //transform.position = new Vector3(ActualHeadVRRootPosition.x, 0 - HandOffset, ActualHeadVRRootPosition.z);
            transform.position = new Vector3(HeadVRRoot.position.x, 0 - HandOffset, HeadVRRoot.position.z);

            //SpineStretchArray[0].rotation = Quaternion.LookRotation(SpineStretchArray[0].position - ActualHeadVRRootPosition, transform.forward); //TODO: this is wrong, I think
            SpineStretchArray[0].rotation = Quaternion.LookRotation(SpineStretchArray[0].position - HeadVRRoot.position, transform.forward);
            SpineStretchArray[0].Rotate(-90, 0, 0, Space.Self);

            HeadBone.rotation = HeadVRRoot.rotation;

            HandIKTargets[0].position = HandVRRoots[0].position;
            HandIKTargets[0].rotation = HandVRRoots[0].rotation;
            HandIKTargets[1].position = HandVRRoots[1].position;
            HandIKTargets[1].rotation = HandVRRoots[1].rotation;

            //TODO: probably don't need to set this twice, but it'll help keep track of where stuff actually is
            HeadVRTarget.position = vrCamera.transform.position;
        }
    }

    private void LateUpdate()
    {
        if (!init)
        {
            init = true;
            foreach (Finger finger in FingersL)
            {
                foreach (Finger.Joint joint in finger.joints)
                {
                    joint.home = joint.joint.localRotation;
                }
            }
            foreach (Finger finger in FingersR)
            {
                foreach (Finger.Joint joint in finger.joints)
                {
                    joint.home = joint.joint.localRotation;
                }
            }
            anim.SetLayerWeight(1, 1);
            anim.SetLayerWeight(2, 1);
        }
        //TODO: look into playables API to mask the animation instead of just setting rotations after animator every frame
        if (OverrideHandL)
        {
            foreach (Finger finger in FingersL)
            {
                foreach (Finger.Joint joint in finger.joints)
                {
                    joint.joint.localRotation = joint.OverrideRotation;
                }
            }
        }
        if (OverrideHandR)
        {
            foreach (Finger finger in FingersR)
            {
                foreach (Finger.Joint joint in finger.joints)
                {
                    joint.joint.localRotation = joint.OverrideRotation;
                }
            }
        }
    }

    void OnMenuButtonDown(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        ButtonHoldTime = Time.unscaledTime;
        HoldingMenu = actionIn;
        menuButton.AddOnStateUpListener(OnMenuButtonUp, SteamVR_Input_Sources.Any);
    }

    void OnMenuButtonHold()
    {
        //TODO: some sort of visual indicator that this is happening
        //TODO: reorient direction as well
        if(Time.unscaledTime - ButtonHoldTime > OriginResetTime)
        {
            Debug.Log("Updating origin");
            PlayerRestHeight = vrCamera.transform.localPosition.y;
            MenuButtonReleased();
        }
    }

    void OnMenuButtonUp(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        FreezeRig = !FreezeRig;
        if (FreezeRig)
        {
            //TODO: this horribly breaks the throwing (probably doesn't really matter since freezing the rig is a debug function)
            hands[0].enabled = false;
            hands[1].enabled = false;
        }
        else
        {
            hands[0].enabled = true;
            hands[1].enabled = true;
        }
        Debug.Log("freeze rig");
        MenuButtonReleased();
    }

    void MenuButtonReleased()
    {
        menuButton.RemoveOnStateUpListener(OnMenuButtonUp, SteamVR_Input_Sources.Any);
        HoldingMenu = null;
    }

    void OnSnapTurn(int direction)
    {
        vrCamera.transform.parent.RotateAround(vrCamera.transform.position, Vector3.up, SnapTurnAngle * direction);
    }

    void OnSnapTurnLeft(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        OnSnapTurn(-1);
    }

    void OnSnapTurnRight(ISteamVR_Action_In actionIn, SteamVR_Input_Sources sources)
    {
        OnSnapTurn(1);
    }

    void OnPinchLeft(SteamVR_Action_Single fromAction, SteamVR_Input_Sources sources, float newAxis, float newDelta)
    {
        OnPinch(true, newAxis);
    }
    void OnPinchRight(SteamVR_Action_Single fromAction, SteamVR_Input_Sources sources, float newAxis, float newDelta)
    {
        OnPinch(false, newAxis);
    }

    void OnPinch(bool hand, float amount)
    {
        //off is right, on is left
        if (hand)
        {
            anim.SetFloat("HandClosed_L", amount);
        }
        else
        {
            anim.SetFloat("HandClosed_R", amount);
        }
    }

    void OnGrabLeft()
    {
        Pinch.RemoveOnChangeListener(OnPinchLeft, SteamVR_Input_Sources.LeftHand);
        ArrangeFingers(true);
    }

    void OnGrabRight()
    {
        Pinch.RemoveOnChangeListener(OnPinchRight, SteamVR_Input_Sources.RightHand);
        ArrangeFingers(false);
    }

    void OnReleaseLeft()
    {
        ClearDebugLines();
        Pinch.AddOnChangeListener(OnPinchLeft, SteamVR_Input_Sources.LeftHand);
        anim.SetFloat("HandClosed_L", Pinch.GetAxis(SteamVR_Input_Sources.LeftHand));
        OverrideHandL = false;
    }
    void OnReleaseRight()
    {
        ClearDebugLines();
        Pinch.AddOnChangeListener(OnPinchRight, SteamVR_Input_Sources.RightHand);
        anim.SetFloat("HandClosed_R", Pinch.GetAxis(SteamVR_Input_Sources.RightHand));
        OverrideHandR = false;
    }

    void ArrangeFingers(bool left)
    {
        Collider collider;
        Finger[] array;
        if (left)
        {
            collider = hands[0].heldObject.GetComponent<Collider>();
            array = FingersL;
            OverrideHandL = true;
        }
        else
        {
            collider = hands[1].heldObject.GetComponent<Collider>();
            array = FingersR;
            OverrideHandR = true;
        }

        Quaternion currentRotation;
        Quaternion tempRotation;
        Finger.Joint RootJoint;
        int RootIndex;
        Vector3 RayStart;
        Vector3 RayDirection;
        //Transform JointRay;
        float CurrentAngle;
        float tempAngle;
        bool DidRayHit;

        //cast a ray from home position to target position for each joint, collect all ray hits
        //figure out which takes the least rotation for the root joint to point at
        //lock all joints from that the one that cast that ray earlier
        //repeat, with just the unlocked joints (lowest unlocked joint is root)

        foreach (Finger finger in array)
        {
            foreach (Finger.Joint joint in finger.joints)
            {
                joint.joint.localRotation = joint.home;
                joint.OverrideRotation = joint.home;
            }
            RootIndex = 0;
            while (RootIndex < finger.joints.Length)
            {
                RootJoint = finger.joints[RootIndex];
                CurrentAngle = RootJoint.MaxAngle;
                currentRotation = Quaternion.identity;
                for (int i = RootIndex; i < finger.joints.Length; i++)
                {
                    DidRayHit = false;
                    RayStart = finger.joints[i].joint.GetChild(0).position; //TODO: we should probably make joint head be an assignable object

                    RayDirection = collider.transform.position - RayStart;
                    int c = -1;
                    int add = 1;
                    if (finger.joints[i].CornerWidth == 0)
                    {
                        c = 0;
                        add = 2;
                    }
                    for (; c < 2; c+= add)
                    {
                        Vector3 RayOffset = RootJoint.joint.right * c * finger.joints[i].CornerWidth * .5f;
                        if (CurvedRay(RayStart + RayOffset, RootJoint.joint, -RootJoint.MaxAngle, 6, .005f, collider, out RaycastHit hit))
                        {
                            DidRayHit = true;
                            tempRotation = Quaternion.LookRotation(hit.point - RayOffset - RootJoint.joint.position, -RootJoint.joint.forward) * Quaternion.Euler(90, 0, 0);
                            tempAngle = Quaternion.Angle(tempRotation, RootJoint.joint.rotation);
                            if (tempAngle > 90)
                            {
                                //why is this necessary? I have no clue
                                tempRotation *= Quaternion.Euler(0, 180, 0);
                                tempAngle = Quaternion.Angle(tempRotation, RootJoint.joint.rotation);
                            }
                            if (tempAngle < CurrentAngle)
                            {
                                CurrentAngle = tempAngle;
                                currentRotation = tempRotation;
                                RootIndex = i;
                            }
                        }
                    }
                    
                    if (!DidRayHit && !collider.Raycast(new Ray(RayStart, RayDirection), out _, RayDirection.magnitude+.1f)) //shoot just a touch further than the origin in case the object is 2d
                    {
                        //if the previous ray didn't hit anything, shoot a ray from the raystart to the collider center- if that also doesn't hit anything, we're inside the collider already (probably, doesn't necessarily work with complex collider shapes)
                        currentRotation = finger.joints[i].home;
                        CurrentAngle = 0;
                        RootIndex = i;
                    }
                }
                if (currentRotation == Quaternion.identity)
                {
                    //if none of the rays hit anything, rotate the current root joint to its max rotation and try again one joint further up
                    RootJoint.joint.Rotate(-RootJoint.MaxAngle, 0, 0, Space.Self);
                    RootJoint.OverrideRotation = RootJoint.joint.localRotation;
                }
                else
                {
                    //if a ray did hit something, use the hit that takes the least rotation to match, and then repeat with all joints after the one that cast that ray
                    //RootJoint.joint.rotation = currentRotation;
                    RootJoint.joint.Rotate(-CurrentAngle, 0, 0, Space.Self); //rotate slightly less to account for finger width
                    RootJoint.OverrideRotation = RootJoint.joint.localRotation;
                }

                TransferDebugLines(RootJoint.joint);
                RootIndex++;
            }
            /*
            
            */
        }
    }

    bool CurvedRay(Vector3 Start, Transform origin, float angle, int segmentCount, float Thickness, Collider collider, out RaycastHit hit)
    {
        Vector3 RayStart = Start;
        for(int i = 0; i < segmentCount; i++)
        {

            Vector3 RayEnd = origin.TransformPoint(Quaternion.Euler(angle/segmentCount, 0, 0) * origin.InverseTransformPoint(RayStart));
            Vector3 RayDirection = RayEnd - Start;
            if (AddDebugLines) tempDebugLines.Add(new DebugLine(RayStart, RayEnd));
            if (collider.Raycast(new Ray(Start, RayDirection), out hit, RayDirection.magnitude))
            {
                hit.point -= RayDirection.normalized * Thickness;
                return true;
            }
            RayStart = RayEnd;
        }
        hit = new RaycastHit(); //TODO: this is probably wrong
        return false;
    }
    bool CurvedRay(Vector3 Start, Transform origin, float angle, int segmentCount, Collider collider, out RaycastHit hit)
    {
        return CurvedRay(Start, origin, angle, segmentCount, 0, collider, out hit);
    }

    public class DebugLine
    {
        //TODO: swap with DebugDrawLine so we can see it in VR
        Vector3 start;
        Vector3 end;
        readonly Transform parent;
        public void Draw()
        {
            if (parent)
            {
                Debug.DrawLine(parent.TransformPoint(start), parent.TransformPoint(end));
            }
            else
            {
                Debug.DrawLine(start, end);
            }
        }

        public DebugLine(Vector3 LineStart, Vector3 LineEnd)
        {
            InitializeBasic(LineStart, LineEnd);
        }
        public DebugLine(Vector3 LineStart, Vector3 LineEnd, Transform ParentObject)
        {
            if (ParentObject == null)
            {
                InitializeBasic(LineStart, LineEnd);
            }
            else
            {
                parent = ParentObject;
                InitializeBasic(parent.InverseTransformPoint(LineStart), parent.InverseTransformPoint(LineEnd));
            }
        }

        void InitializeBasic(Vector3 LineStart, Vector3 LineEnd)
        {
            start = LineStart;
            end = LineEnd;
        }
        public Vector3 GetStart()
        {
            if (parent)
            {
                return parent.InverseTransformPoint(start);
            }
            return start;
        }
        public Vector3 GetEnd()
        {
            if (parent)
            {
                return parent.InverseTransformPoint(end);
            }
            return end;
        }
        public Transform GetParent()
        {
            return parent;
        }
    }

    void ClearDebugLines()
    {
        debugLines.Clear();
    }
    void DrawDebugLines()
    {
        foreach (DebugLine line in debugLines)
        {
            line.Draw();
        }
    }
    void TransferDebugLines( Transform parent)
    {
        foreach (DebugLine line in tempDebugLines)
        {
            debugLines.Add(new DebugLine(line.GetStart(), line.GetEnd(), parent));
        }
        tempDebugLines.Clear();
    }
}