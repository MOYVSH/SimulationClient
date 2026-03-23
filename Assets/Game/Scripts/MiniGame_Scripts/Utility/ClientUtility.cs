using UnityEngine;

public static class ClientUtility
{
    #region GameObject

    public static bool SetActiveEX(this GameObject go, bool active)
    {
        if (go.activeSelf != active)
        {
            go.SetActive(active);
            return true;
        }
        return false;
    }
    
    public static T AddMissingComponent<T>(this GameObject tra) where T : Component
    {
        if (!tra.TryGetComponent<T>(out var t))
        {
            t = tra.gameObject.AddComponent<T>();
        }
        return t;
    }

    #endregion
    

    #region Transform

    public static T AddMissingComponent<T>(this Transform tra) where T : Component
    {
        if (!tra.TryGetComponent<T>(out var t))
        {
            t = tra.gameObject.AddComponent<T>();
        }
        return t;
    }
    
    public static Transform AddChild(this Transform tra, Transform child, bool active, bool setToLocal = true)
    {
        child.SetParent(tra);
        child.gameObject.SetActiveEX(active);
        if (setToLocal)
        {
            child.localScale = Vector3.one;
            child.localEulerAngles = Vector3.zero;
            child.localPosition = Vector3.zero;
        }
        return child;
    }    

    #endregion

}