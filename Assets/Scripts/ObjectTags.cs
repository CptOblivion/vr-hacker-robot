using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTags : MonoBehaviour
{
    //TODO: custom inspector, with easy adding and removing of tags, and a library of easy-add common tags
    //class to hold tags, since unity normally only allows one tag per object
    public string[] Tags = new string[] { };

    public static bool CompareTags(ObjectTags tagsObject, string[] TagsBlacklist, string[] TagsWhitelist, bool StrictWhitelist)
    {
        if (tagsObject != null && TagsBlacklist.Length > 0 && tagsObject.Tags.Length > 0)
        {
            foreach (string tag in TagsBlacklist)
            {
                foreach (string tag2 in tagsObject.Tags)
                {
                    if (tag.Equals(tag2))
                    {
                        return false;
                    }
                }
            }
        }
        if (TagsWhitelist.Length > 0)
        {
            if (tagsObject == null)
            {
                return false;
            }
            else
            {
                bool pass = false;
                //in theory: pass is set 
                foreach (string tag in TagsWhitelist)
                {
                    pass = false;
                    foreach (string tag2 in tagsObject.Tags)
                    {
                        if (tag.Equals(tag2))
                        {
                            pass = true;
                            break;
                        }
                    }
                    if (pass == !StrictWhitelist)
                    {
                        //if StrictWhitelist is true, if we get all the way through the internal compare loop without finding a match we'll get kicked out of the outer loop with pass = false
                        //if StrictWhitelist is false, the first time we hit a match we'll exit the outer loop with pass = true
                        break;
                    }
                }
                if (!pass)
                {
                    return false;
                }

            }
        }
        return true;
    }

    public bool CompareTags(string[] TagsBlacklist, string[] TagsWhitelist, bool StrictWhitelist)
    {
        return CompareTags(this, TagsBlacklist, TagsWhitelist, StrictWhitelist);
    }
}
