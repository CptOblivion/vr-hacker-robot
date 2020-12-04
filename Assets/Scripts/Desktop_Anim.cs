using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Desktop_Anim : MonoBehaviour
{
    public delegate void AnimDelegate();
    float AnimTime = 0;
    float AnimTimer = 0;
    float t;
    float startup;
    Vector2 StartPos;
    Vector2 EndPos;
    Vector2 StartScale;
    Vector2 EndScale;
    RectTransform startTransform;
    RectTransform endTransform;
    RectTransform rect;
    public event AnimDelegate OnAnimStart;
    public event AnimDelegate OnAnimFinished;
    public event AnimDelegate OnAnimUpdate;
    public bool CubicAnim = true;
    public bool DestroyGameobject = true;
    public bool local = false;
    public Vector2 StartSizeOffset = Vector2.one;
    public Vector2 StartPosOffset = Vector2.zero;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    //starting transform, target transform
    public void Initialize(Transform startingTransform, Transform TargetTransform, float duration, float delay)
    {
        AnimTime = duration;
        startup = delay;
        startTransform = (RectTransform)startingTransform;
        endTransform = (RectTransform)TargetTransform;

        rect.position = (Vector2)startTransform.position + StartPosOffset;
        SetSize(Vector2.Scale(startTransform.rect.size, StartSizeOffset));
    }

    //starting transform, target position
    public void Initialize(Transform startingTransform, Vector2 TargetPosition, Vector2 TargetScale, float duration, float delay)
    {
        AnimTime = duration;
        startup = delay;
        startTransform = (RectTransform)startingTransform;
        EndPos = TargetPosition;
        EndScale = TargetScale;

        rect.position = (Vector2)startTransform.position + StartPosOffset;
        SetSize(Vector2.Scale(startTransform.rect.size, StartSizeOffset));
    }
    //starting position, target transform
    public void Initialize(Vector2 StartingPosition, Vector2 StartingScale, Transform TargetTransform, float Duration, float delay)
    {
        AnimTime = Duration;
        startup = delay;
        StartPos = StartingPosition;
        StartScale = StartingScale;
        endTransform = (RectTransform)TargetTransform;
        gameObject.SetActive(true);

        if (local)
        {
            rect.localPosition = StartingPosition + StartPosOffset;
        }
        else
        {
            rect.position = StartingPosition + StartPosOffset;
        }
        SetSize(Vector2.Scale(StartScale, StartSizeOffset));
    }
    //starting position, target position
    public void Initialize(Vector2 StartingPosition, Vector2 TargetPosition, Vector2 StartingScale, Vector2 TargetScale, float Duration, float delay)
    {
        AnimTime = Duration;
        startup = delay;
        StartPos = StartingPosition;
        StartScale = StartingScale;
        EndPos = TargetPosition;
        EndScale = TargetScale;
        gameObject.SetActive(true);

        if (local)
        {
            rect.localPosition = StartingPosition + StartPosOffset;
        }
        else
        {
            rect.position = StartingPosition + StartPosOffset;
        }
        SetSize(Vector2.Scale(StartScale, StartSizeOffset));
    }

    void Update()
    {
        if (AnimTime <= 0)
        {
            Debug.LogError("Animation not initialized!", this);
            gameObject.SetActive(false);
        }
        else if (startup > 0)
        {
            if (startTransform)
            {
                rect.position = (Vector2)startTransform.position + StartPosOffset;
                SetSize(Vector2.Scale(startTransform.rect.size, StartSizeOffset));
            }
            startup -= Time.deltaTime;
            if (startup <= 0)
            {
                OnAnimStart?.Invoke();
            }
        }
        else
        {
            AnimTimer += Time.deltaTime;
            if (AnimTimer > AnimTime)
            {
                OnAnimFinished?.Invoke();
                if (DestroyGameobject)
                {
                    Destroy(gameObject);
                }
                else
                {
                    if (endTransform)
                    {
                        rect.position = endTransform.position;
                        SetSize(endTransform.rect.size);
                    }
                    else
                    {

                        if (local)
                        {
                            rect.localPosition = EndPos;
                        }
                        else
                        {
                            rect.position = EndPos;
                        }
                        SetSize(EndScale);
                    }
                    OnAnimUpdate?.Invoke();
                    Destroy(this);
                }
                //finished anim
            }
            else
            {
                t = AnimTimer / AnimTime;
                if (CubicAnim) t *= t;


                if (startTransform)
                {
                    StartPos = (Vector2)startTransform.position;
                    if (local)
                    {
                        StartPos = rect.parent.InverseTransformPoint(StartPos);
                    }
                    StartPos += StartPosOffset;
                    StartScale = startTransform.rect.size;
                }

                if (endTransform)
                {
                    EndPos = endTransform.position;
                    if (local)
                    {
                        rect.parent.InverseTransformPoint(EndPos);
                    }
                    EndScale = endTransform.rect.size;
                }

                if (local)
                {
                    rect.localPosition = Vector2.Lerp(StartPos, EndPos, t);
                }
                else
                {
                    rect.position = Vector2.Lerp(StartPos, EndPos, t);
                }
                SetSize(Vector2.Lerp(Vector2.Scale(StartScale, StartSizeOffset), EndScale, t));
                OnAnimUpdate?.Invoke();
            }
        }
    }

    void SetSize(Vector2 size)
    {
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }
}
