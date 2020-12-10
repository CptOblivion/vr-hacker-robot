using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Rendering;
using UnityEngine.UI;

public class DebugDisplay : MonoBehaviour
{
    static int lineCount = 1;
    static readonly int MaxLines = 5;
    static string text = "";
    //public Camera outputCam;
    Text textOb;

    private void Start()
    {
        textOb = GetComponent<Text>();
    }

    /*
    private void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += CamPostRender;
    }
    private void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= CamPostRender;
    }
    private void CamPostRender(ScriptableRenderContext context, Camera camera)
    {
        //write text onto image
        if (camera.targetTexture != null)
        {

        }
        text = "";
    }
    */

    private void Update()
    {
        textOb.text = text;
    }

    public static void SetText(string debugText)
    {
        text = debugText;
        lineCount = 1; //TODO: find line breaks in debugtext and properly set linecount
    }

    public static void AddLine(string debugText)
    {
        if (text == "")
        {
            text = debugText;
        }
        else if (lineCount >= MaxLines)
        {
            text = text.Substring(text.IndexOf('\n')+1) + '\n' + debugText;
        }
        else
        {
            text += '\n' + debugText;
            lineCount++;
        }
    }
    public static void AddLine(float debugFloat)
    {
        AddLine(debugFloat.ToString());
    }
    public static void AddLine(int debugInt)
    {
        AddLine(debugInt.ToString());
    }
    public static void AddLine(bool debugBool)
    {
        AddLine(debugBool.ToString());
    }
}
