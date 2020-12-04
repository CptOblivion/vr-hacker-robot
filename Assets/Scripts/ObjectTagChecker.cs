using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectTagChecker : MonoBehaviour
{
    public string[] TagsBlacklist = new string[] { };
    public string[] TagsWhitelist = new string[] { };
    public bool StrictWhitelist;

    public UnityEvent OnTagPass;
    public UnityEvent OnTagFail;

    public void CheckTags(GameObject checkOb)
    {
        if (ObjectTags.CompareTags(checkOb.GetComponent<ObjectTags>(), TagsBlacklist, TagsWhitelist, StrictWhitelist))
        {
            OnTagPass.Invoke();
        }
        else
        {
            OnTagFail.Invoke();
        }
    }
}
