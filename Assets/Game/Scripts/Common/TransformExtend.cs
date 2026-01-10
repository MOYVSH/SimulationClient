using UnityEngine;

public static class TransformExtend
{
    public static T AddMissingComponent<T>(this Transform tra) where T : Component
    {
        if (!tra.TryGetComponent<T>(out var t))
        {
            t = tra.gameObject.AddComponent<T>();
        }
        return t;
    }
    
    static public Transform AddChild(this Transform tra, Transform child, bool active, bool setToLocal = true)
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
}