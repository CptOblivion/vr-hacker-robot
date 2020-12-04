using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackObject : MonoBehaviour
{
    public Transform target;
    public bool UseTargetUp;
    Vector3 Upwards = Vector3.up;

    Quaternion StartRotation;
    void Awake()
    {
        StartRotation = transform.localRotation;
    }
    void Update()
    {
        if (UseTargetUp) Upwards = target.up;
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, Upwards);
    }

    public void ResetRotation()
    {
        transform.localRotation = StartRotation;
    }
}
