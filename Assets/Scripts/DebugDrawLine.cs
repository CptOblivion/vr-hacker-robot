using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawLine : MonoBehaviour
{
    bool init = false;
    Vector3 Origin;
    Vector3 Target;
    float life = 0;
    void Start()
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.SetVertexCount(2);
        lineRenderer.SetPosition(0, Origin);
        lineRenderer.SetPosition(1, Target);
        lineRenderer.endWidth = .002f;
        lineRenderer.startWidth = .01f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!init)
        {
            init = true;
        }
        else
        {
            if (life > 0)
            {
                Destroy(gameObject, life);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    public static DebugDrawLine DrawLine(Vector3 startpoint, Vector3 endpoint, float duration = 0)
    {
        DebugDrawLine line = new GameObject().AddComponent<DebugDrawLine>();
        line.Origin = startpoint;
        line.Target = endpoint;
        line.life = duration;
        return line;
    }
}
