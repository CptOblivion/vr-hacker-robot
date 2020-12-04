using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    public Transform target;
    public bool FixBlenderRotation = true;
    public bool FlipPole; //TODO: replace with pole target
    public bool UseShoulder = false;
    public Vector2 ShoulderWeights = new Vector2(.25f, .1f); //TODO: implement shoulder roll

    int ChainLength = 2;
    Transform[] Joints;
    Quaternion[] JointRestAngles;
    float[] JointLengths;
    public float EndpointOverreachLerpDistance;
    float MaxLength = 0;
    public int IterationLimit = 10;

    [Range(0,1)]
    public float HandRollElbowAdjust = .3f;

    Vector3[] LastRotation;
    //TODO: add MinLength for bones that aren't perfectly even lengths (for VR purposes, make sure arm bone lengths are exactly even so player can always reach near-to-shoulder locations even if code doesn't correctly guess where their body is)

    void Start()
    {
        if (UseShoulder) 
        {
            ChainLength = 3;
        }
        LastRotation = new Vector3[ChainLength];
        Joints = new Transform[ChainLength];
        JointLengths = new float[ChainLength];
        JointRestAngles = new Quaternion[ChainLength];
        Transform tChild = transform;
        for (int i = 0; i < ChainLength; i++) //segment information is stored from the extremety inwards so indices 0 and 1 are always the trig-solved ones
        {
            Joints[i] = tChild.parent;
            JointLengths[i] = (Joints[i].parent.InverseTransformPoint(tChild.position) - Joints[i].parent.InverseTransformPoint(Joints[i].position)).magnitude;
            JointRestAngles[i] = Joints[i].localRotation;
            if (i < 2)
            {
                //don't include shoulder in SumLength
                MaxLength += JointLengths[i];
            }
            tChild = Joints[i];
        }
    }


    void LateUpdate()
    {
        Vector3 TargetVector;
        if (UseShoulder)
        {
            //TargetVector = Joints[2].parent.InverseTransformPoint(target.position - Joints[2].position);
            TargetVector = Joints[2].parent.InverseTransformPoint(target.position) - Joints[2].parent.InverseTransformPoint(Joints[2].position);
            if (TargetVector.magnitude == 0)
            {
                Joints[2].localRotation = JointRestAngles[2];
            }
            else
            {
                Quaternion ShoulderTargetRotation;
                Vector3 UpVec = Vector3.up;
                if (FlipPole) UpVec = UpVec * -1;

                ShoulderTargetRotation = Quaternion.LookRotation(TargetVector, UpVec); //TODO: consider joint's rest position when determining "up"
                float factor = ShoulderWeights[0] * Mathf.Clamp01(TargetVector.magnitude / MaxLength);
                //factor = 1;
                //Joints[2].localRotation = Quaternion.Lerp(JointRestAngles[2], ShoulderTargetRotation, factor);

                Joints[2].localRotation = ShoulderTargetRotation;
                BlenderFixTransform(Joints[2], true);
                Joints[2].localRotation = Quaternion.Lerp(JointRestAngles[2], Joints[2].localRotation, factor);



            }
        }
        //TODO: consider magnitude 0
        TargetVector = Joints[1].parent.InverseTransformPoint(target.position) - Joints[1].parent.InverseTransformPoint(Joints[1].position);
        if (TargetVector.magnitude > 0) //TODO: special case for when TargetVector.magnitude==0 (keep upper arm in same orientation, point forearm straight at shoulder)
        {
            if (UseShoulder)
            {
                Vector3 UpVec = Vector3.left;
                if (FlipPole) UpVec *= -1;

                //TODO: pick which axis is up/down (it'll vary per model)
                //TODO: also factor in how far forwards on the body the hands are (EG high up but far forwards should have a reasonably high blend value)
                float blend = TargetVector.normalized.x;
                if (FlipPole) blend *= -1; //TODO: once axis selection is there, just allow negative axis to be selected
                blend = blend / 2 + .5f;

                //TODO: nonlinear blend scale (should fall off pretty fast as target passes below shoulder level)

                //TODO: test if Vector3.forward is correct for the non-FixBlenderRotation version)
                UpVec = Vector3.Lerp(FixBlenderRotation ? Vector3.up : Vector3.forward, UpVec, blend); //consider 
                UpVec = Vector3.Lerp(UpVec, Joints[2].InverseTransformDirection(transform.forward), HandRollElbowAdjust*blend); //consider hand facing direction (stronger as arms are lower, weaker as arms are higher)

                Joints[1].localRotation = Quaternion.LookRotation(TargetVector, UpVec);
            }
            else
            {
                Joints[1].localRotation = Quaternion.LookRotation(TargetVector, Joints[1].parent.InverseTransformVector(target.up));
            }

            BlenderFixTransform(Joints[1]);
            Vector3 GlobalTargetVector = target.position - Joints[1].position;

            //Joints[0].rotation = Quaternion.LookRotation(GlobalTargetVector, target.up);
            //BlenderFixTransform(Joints[0]);
            Joints[0].localRotation = JointRestAngles[0]; 
            
            //rotate hands to match target rotation, with a lerp to just pointing at the target position if it's too far away
            transform.rotation = Quaternion.Lerp(target.rotation, Quaternion.LookRotation(GlobalTargetVector, target.up), (TargetVector.magnitude - MaxLength) / EndpointOverreachLerpDistance);
            BlenderFixTransform(transform);

            if (TargetVector.magnitude != 0 && TargetVector.magnitude < MaxLength)
            {
                int flipDirection = 1;
                if (FlipPole) flipDirection = -1;
                //TODO: this is currently hardcoded to 2-joint IK (plus optional shoulder)
                Joints[1].Rotate(0, 0, -JointAngle(JointLengths[0], JointLengths[1], TargetVector.magnitude) * flipDirection, Space.Self); //TODO: getting "Assertion failed on expression: 'CompareApproximately(SqrMagnitude(result),1.0F)' a lot of times (which then passes NaN to every value in Rotate)
                Joints[0].Rotate(0, 0, (180 - (JointAngle(TargetVector.magnitude, JointLengths[1], JointLengths[0]))) * flipDirection, Space.Self); //TODO: that 180-(expression) is probably just because I got my trig wrong
                transform.rotation = target.rotation;
                BlenderFixTransform(transform);
            }
        }
    }

    float JointAngle(float A, float B, float C)
    {
        return (Mathf.Rad2Deg * (Mathf.Acos((B * B + C * C - A * A) / (2 * B * C))));
    }

    void BlenderFixTransform(Transform t, bool swizzle = false)
    {
        if (FixBlenderRotation)
        {
            if (swizzle) //I think this is required when doing a blenderfix in parent space
            {
                t.Rotate(90, 0, 0, Space.Self);
                t.Rotate(0, -90, 0, Space.Self);
            }
            else
            {
                t.Rotate(90, 0, 0, Space.Self);
                t.Rotate(0, 180, 0, Space.Self);
            }
        }
    }
}
