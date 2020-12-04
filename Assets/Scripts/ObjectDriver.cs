using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDriver : MonoBehaviour
{
    [System.Serializable]
    public class DriverAxis
    {
        public enum TransformType { Position, Rotation, Scale}
        public enum Axes {X, Y, Z}
        public Transform target;
        public TransformType InTransformType;
        public Axes InAxis;
        public TransformType OutTransformType;
        public Axes OutAxis;
        [HideInInspector]
        public Vector3 TargetOriginAxis;
        [HideInInspector]
        public Vector3 TargetOriginPos;
        [Tooltip("InMin, InMax, OutMin, OutMax")]
        public Vector4 InOutMinMax; //todo: with custom editor, label the individual entries properly
    }
    //TODO: cusom editor to show/hide relevant fields, as well as allow proper editing of DriverAxes
    public enum DriverTypes { TrackTo, StretchTo, AxisValue}
    public DriverTypes driverType;
    public Transform target;
    public DriverAxis[] DriverAxes;


    Vector3 OriginPosition;
    Quaternion OriginRotation;
    Vector3 OriginScale;

    Vector3 OriginRight, OriginUp, OriginForward;
    float RestDistance;
    private void Awake()
    {
        OriginPosition = transform.localPosition;
        OriginRotation = transform.localRotation;
        OriginScale = transform.localScale;
        OriginRight = transform.parent.InverseTransformDirection(transform.right);
        OriginUp = transform.parent.InverseTransformDirection(transform.up);
        OriginForward = transform.parent.InverseTransformDirection(transform.forward);
        if (driverType == DriverTypes.TrackTo || driverType == DriverTypes.StretchTo)
        {
            RestDistance = (transform.parent.InverseTransformVector(target.position) - transform.localPosition).magnitude;
        }
        else
        {
            foreach(DriverAxis axis in DriverAxes)
            {
                if (axis.InTransformType == DriverAxis.TransformType.Position)
                {
                    switch (axis.InAxis)
                    {
                        case DriverAxis.Axes.X:
                            axis.TargetOriginAxis = LocalPositionOrigin(target, target.right);
                            break;
                        case DriverAxis.Axes.Y:
                            axis.TargetOriginAxis = LocalPositionOrigin(target, target.up);
                            break;
                        case DriverAxis.Axes.Z:
                            axis.TargetOriginAxis = LocalPositionOrigin(target, target.forward);
                            break;
                    }
                    axis.TargetOriginPos = target.localPosition;
                }
            }
        }
    }

    void Update()
    {
        if (driverType == DriverTypes.TrackTo || driverType == DriverTypes.StretchTo)
        {

        }
        else
        {
            float AxisValue = 0;
            Vector3 ChannelOut;
            Vector3 PositionOffset = Vector3.zero;
            foreach(DriverAxis axis in DriverAxes)
            {
                switch (axis.InTransformType)
                {
                    case DriverAxis.TransformType.Position:
                        //localPosition is in parent space (aligned with parent axes and zeroed on parent), we should instead get parent origin position at init, and do a dot product using the vector from the current position to the origin, and target.OriginAxis
                        //  the forward axis should be the starting one, not the current one
                        //AxisValue = axis.target.localPosition[(int)axis.InAxis];

                        //TODO: untested:
                        AxisValue = Vector3.Dot(axis.target.localPosition - axis.TargetOriginPos, axis.TargetOriginAxis);
                        break;
                    case DriverAxis.TransformType.Rotation:
                        AxisValue = axis.target.localRotation.eulerAngles[(int)axis.InAxis];
                        break;
                    case DriverAxis.TransformType.Scale:
                        AxisValue = axis.target.localScale[(int)axis.InAxis];
                        break;
                }
                AxisValue = Mathf.Lerp(axis.InOutMinMax.z, axis.InOutMinMax.w, Mathf.InverseLerp(axis.InOutMinMax.x, axis.InOutMinMax.y, AxisValue));
                //DebugDisplay.AddLine($"{axis.target.localRotation.eulerAngles.x}, {axis.target.localRotation.eulerAngles.y}, {axis.target.localRotation.eulerAngles.z}");
                switch (axis.OutTransformType)
                {
                    case DriverAxis.TransformType.Position:
                        //position is relative to the parent position, so we need to accumulate all the position drivers and apply them after the loop
                        //the axes are aligned with the parent object, but the axes we want are this object's so we have to use the transform axes
                        //rotation and scale should be fine to 
                        switch (axis.OutAxis)
                        {
                            case DriverAxis.Axes.X:
                                PositionOffset += OriginRight * AxisValue;
                                break;
                            case DriverAxis.Axes.Y:
                                PositionOffset += OriginUp * AxisValue;
                                break;
                            case DriverAxis.Axes.Z:
                                PositionOffset += OriginForward * AxisValue;
                                break;
                        }
                        break;
                    case DriverAxis.TransformType.Rotation:
                        ChannelOut = transform.localRotation.eulerAngles;
                        ChannelOut[(int)axis.OutAxis] = AxisValue;
                        transform.localRotation = Quaternion.Euler(ChannelOut);
                        break;
                    case DriverAxis.TransformType.Scale:
                        ChannelOut = transform.localScale;
                        ChannelOut[(int)axis.OutAxis] = AxisValue;
                        transform.localScale = ChannelOut;
                        break;
                }
            }
            transform.localPosition = OriginPosition + PositionOffset;
        }
    }

    Vector3 LocalPositionOrigin(Transform t, Vector3 vector)
    {
        if (t.parent)
        {
            return (t.parent.InverseTransformDirection(vector));
        }
        return vector;
    }
}
