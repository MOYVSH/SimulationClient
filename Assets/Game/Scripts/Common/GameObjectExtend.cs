using UnityEngine;

public static class GameObjectExtend
{
    public static bool SetActiveEX(this GameObject go, bool active)
    {
        if (go.activeSelf != active)
        {
            go.SetActive(active);
            return true;
        }
        return false;
    }
}