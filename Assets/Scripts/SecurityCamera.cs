using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityCamera : NetworkDevice
{
    public string CameraName = "[Unnamed Camera]";

    public static SecurityCamera selectedCamera;

    //TODO: is CamList actually used anywhere?
    static readonly List<SecurityCamera> CamList = new List<SecurityCamera>();

    public bool BlenderFix = true;

    public bool StartingCam = false;

    public Vector3 YawPitchZoomStep = new Vector3(5, 5, 5);
    public Vector3 YawPitchZoomMax = new Vector3(30,15,20);
    public Vector3 YawPitchZoomMin= new Vector3(-30,-15,0);
    public Vector3 ServoSpeed = new Vector3(3, 3, 3);
    public Camera cameraObject;
    public Transform cameraPost;
    public Transform cameraBody;
    public GameObject activeLight;
    public bool ControlsEnabled = true;
    bool ActiveCamera = false;
    //TODO: cusom editor that presents sweep as a bool (false for 0, true for 1)
    public int Sweep = 1; //set to 0 for no sweep
    public float SweepDelay = 2;
    float SweepDelayTimer;
    bool Sweeping = false;
    //TODO: pause pan at edges of pan

    public float FrameRate = 15;
    float FrameRateTimer = 0;

    Quaternion OriginRotation;
    float OriginZoom;
    Vector3 StartPosition = Vector3.zero;
    Vector3 TargetPosition = Vector3.zero;
    float LerpTime = 0;
    float LerpTimer = 0;
    protected override void Awake()
    {
        DeviceName = CameraName; //TODO: unity won't let me override device name, so we'll just set it after the fact.
        base.Awake();
        cameraObject.enabled = false;
        CamList.Add(this);
        OriginRotation = cameraBody.localRotation;
        OriginZoom = cameraObject.fieldOfView;
        if (StartingCam && !selectedCamera)
        {
            /*
            if (selectedCamera)
            {
                selectedCamera.OnDeselect();
            }
            ActiveCamera = true;
            selectedCamera = this;
            */
            SetSelected();
        }
        else
        {
            activeLight.SetActive(false);
            //enabled = false;
        }
        StartSweep();
    }

    private void Update()
    {
        if (ActiveCamera)
        {
            if (FrameRateTimer > 0)
            {
                FrameRateTimer -= Time.deltaTime;
            }
            else
            {
                UpdateCamera();
            }
        }

        if (LerpTime > 0)
        {
            LerpTimer += Time.deltaTime;
            UpdatePosition();
            if (LerpTimer >= LerpTime)
            {
                LerpTime = 0;
                if (Sweeping)
                {
                    StartSweep();
                }
            }
        }
        else if (Sweeping)
        {
            if (SweepDelayTimer > 0)
            {
                SweepDelayTimer -= Time.deltaTime;
            }
            else
            {
                if (Sweep > 0)
                {
                    ServoToPosition(new Vector3(YawPitchZoomMax.x, 0), .5f);
                }
                else
                {
                    ServoToPosition(new Vector3(YawPitchZoomMin.x, 0), .5f);
                }
                Sweep *= -1;
            }
        }
    }

    void StartSweep(bool ResumeSweep = false)
    {
        if (Sweep != 0)
        {
            Sweeping = true;
            if (ResumeSweep)
            {
                Sweep *= -1; //in theory, since it flips at the end of each sweep, this'll resume on the original direction
                //TODO: should this set the direcion based on which edge it's closer to? Or maybe based on which horizontal direction it last moved, regardless of whether it was a sweep or a manual control?
            }
            else
            {
                SweepDelayTimer = SweepDelay;
            }
        }
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        CamList.Remove(this);
        if (selectedCamera == this)
        {
            selectedCamera = null;
        }
        //TODO: remove from active cameras in SecurityCameraControls
    }

    protected override void ActivateDevice()
    {
        //don't use base network device access
        SecurityCameraControls.ActivateCamera(this);
    }

    public static void PanLeft()
    {
        selectedCamera.StepPosition(new Vector3(-selectedCamera.YawPitchZoomStep.x, 0));
    }
    public static void PanRight()
    {
        selectedCamera.StepPosition(new Vector3(selectedCamera.YawPitchZoomStep.x, 0));
    }
    public static void PanUp()
    {
        selectedCamera.StepPosition(new Vector3(0, -selectedCamera.YawPitchZoomStep.y));
    }
    public static void PanDown()
    {
        selectedCamera.StepPosition(new Vector3(0, selectedCamera.YawPitchZoomStep.y));
    }

    public static void ZoomIn()
    {
        selectedCamera.StepPosition(new Vector3(0, 0, selectedCamera.YawPitchZoomStep.z));
    }

    public static void ZoomOut()
    {
        selectedCamera.StepPosition(new Vector3(0, 0, -selectedCamera.YawPitchZoomStep.z));

    }

    void StepPosition(Vector3 offset)
    {
        if (selectedCamera.ControlsEnabled)
        {
            if (selectedCamera.Sweeping)
            {
                selectedCamera.ServoToPosition(selectedCamera.UpdatePosition(true) + offset);
                selectedCamera.Sweeping = false;
            }
            else
            {
                selectedCamera.ServoToPosition(selectedCamera.TargetPosition + offset);
            }
        }
    }

    public static void Recenter()
    {
        if (selectedCamera.ControlsEnabled)
        {
            selectedCamera.ServoToPosition(Vector3.zero);
            selectedCamera.Sweeping = false;
        }
    }
    //TODO: hold instead of click buttons
    //  alternate: click, but camera animated into position
    //TODO: click and drag on viewport to pan? Or maybe click on viewport to pan to point at that location?

    public void EnableControls()
    {
        ControlsEnabled = true;
    }
    public void DisableControls()
    {
        ControlsEnabled = false;
    }

    void LerpToPosition(Vector3 NewPosition, float Time)
    {
        NewPosition = Vector3.Max(Vector3.Min(NewPosition, YawPitchZoomMax), YawPitchZoomMin);
        if (Time > 0)
        {
            StartPosition = UpdatePosition(true);
            TargetPosition = NewPosition;
            LerpTime = Time;
            LerpTimer = 0;
        }
        else
        {
            LerpTime = LerpTimer = 0;
            StartPosition = TargetPosition = NewPosition;
            UpdatePosition();
        }
    }

    void ServoToPosition(Vector3 NewPosition)
    {
        ServoToPosition(NewPosition, 1);
    }
    void ServoToPosition(Vector3 NewPosition, float SpeedMult)
    {
        //TODO: slows down when target is past clamped rotation
        //  or maybe that's just chance, which happens to line up with framerate dips?
        StartPosition = TargetPosition = UpdatePosition(true);
        LerpTimer = LerpTime = 0;
        float MaxTime = Mathf.Max(Mathf.Abs(StartPosition.x - NewPosition.x) / (ServoSpeed.x * SpeedMult), Mathf.Abs(StartPosition.y - NewPosition.y) / (ServoSpeed.y * SpeedMult), Mathf.Abs(StartPosition.z - NewPosition.z) / (ServoSpeed.z * SpeedMult));
        LerpToPosition(NewPosition, MaxTime);

    }

    Vector3 UpdatePosition(bool ReturnOnly = false)
    {
        if (LerpTime > 0)
        {
            Vector3 NewPosition = Vector3.Lerp(StartPosition, TargetPosition, LerpTimer / LerpTime);
            if (!ReturnOnly)
            {
                if (BlenderFix)
                {
                    cameraPost.localRotation = Quaternion.Euler(0,0, NewPosition.x);
                }
                else
                {
                    cameraPost.localRotation = Quaternion.Euler(0, NewPosition.x, 0);
                }
                cameraBody.localRotation = OriginRotation * Quaternion.Euler(-NewPosition.y, 0, 0);
                cameraObject.fieldOfView = OriginZoom - NewPosition.z;
            }
            return NewPosition;
        }
        else
        {
            if (!ReturnOnly)
            {
                if (BlenderFix)
                {
                    cameraPost.localRotation = Quaternion.Euler(0, 0, TargetPosition.x);
                }
                else
                {
                    cameraPost.localRotation = Quaternion.Euler(0, TargetPosition.x, 0);
                }
                cameraBody.localRotation = OriginRotation * Quaternion.Euler(-TargetPosition.y, 0, 0);
                cameraObject.fieldOfView = OriginZoom - TargetPosition.z;
            }
            return TargetPosition;
        }
    }

    void StopLerp()
    {
        StartPosition = TargetPosition = UpdatePosition(true);
        LerpTime = LerpTimer = 0;
    }

    void OnDeselect()
    {
        ActiveCamera = false;
        activeLight.SetActive(false);
        if (!Sweeping)
        {
            StopLerp();
            StartSweep(true);
        }
    }
    public void SetSelected()
    {
        SecurityCameraControls.UpdateActiveCamera(this, selectedCamera);
        if (selectedCamera)
        {
            selectedCamera.OnDeselect();
        }
        selectedCamera = this;
        UpdateCamera();
        ActiveCamera = true;
        activeLight.SetActive(true);
    }

    void UpdateCamera()
    {
        FrameRateTimer = 1 / FrameRate;
        cameraObject.Render();
    }
}
