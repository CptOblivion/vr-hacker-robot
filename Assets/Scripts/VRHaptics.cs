using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VRHaptics : MonoBehaviour
{
    public static float SlideThreshold = 0;
    public static SteamVR_Action_Vibration vibration;

    public static void Init(SteamVR_Action_Vibration inputVibration)
    {
        if (vibration == null)
        {
            vibration = inputVibration;
        }
    }


    /// <summary>
    /// Rumbles the given controller according to a distance traveled over the last frame, with a degree of roughness, to feel like sliding over a surface
    /// Should be called in fixedupdate, if possible (for consistent results)
    /// </summary>
    /// <param name="distance"> Distance traveled</param>
    /// <param name="SurfaceAmplitude"> depth of the surface bumps</param>
    /// <param name="SurfaceFrequency"> Roughness of the surface: number of bumps per meter</param>
    /// <param name="SurfaceUnevennessAmplitude"> depth of the randomness in the surface</param>
    /// <param name="SurfaceUnevennessFrequency"> frequency of the randomness in the surface</param>
    /// <param name="source"> Controller to send the rumble to</param>
    public static void Slide(float distance, float SurfaceFrequency, float SurfaceAmplitude, float SurfaceUnevennessAmplitude, float SurfaceUnevennessFrequency, SteamVR_Input_Sources source)
    {
        if (source != SteamVR_Input_Sources.LeftHand && source != SteamVR_Input_Sources.RightHand)
        {
            Debug.LogError($"invalid vibration source: {source}");
            return;
        }

        //since this is theoretically being called in fixedupdate, we can pretty reliably trust the duration of the vibrations
        //uses Time.fixedDeltaTime to set rumble duration (this is the expected time until the next fixedupdate)
        float SlideDistance = distance * Time.fixedDeltaTime / Time.deltaTime;
        if (SlideDistance > SlideThreshold)
        { 
            //TODO: implement roughness
            float Amplitude;
            float PeakDistance = 1 / SurfaceFrequency;

            //peak amplitude at 2 bumps per timestep
            if (SlideDistance / PeakDistance > 2)
            {
                Amplitude = 1 / (SurfaceFrequency * SlideDistance*.5f);
            }
            else
            {
                Amplitude = .5f * SurfaceFrequency * SlideDistance - 1;
                Amplitude = 1 - Amplitude*Amplitude*4;
                //Amplitude = .5f * SurfaceFrequency * SlideDistance;
            }
            Amplitude *= SurfaceAmplitude;
            //DebugDisplay.AddLine(Amplitude);

            //TODO: set a different amplitude for if thumb is on touchpad (since haptic effect will be felt much more strongly with direct contact with touchpad)
            //surfacefrequency is bumps over distance
            //rumble frequency is bumps per second
            float Frequency = SurfaceFrequency * distance / Time.deltaTime;
            vibration.Execute(0, Time.fixedDeltaTime, Frequency, Amplitude, source);
        }
    }
}
