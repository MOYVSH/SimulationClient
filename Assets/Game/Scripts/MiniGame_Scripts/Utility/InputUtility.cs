using UnityEngine;
using UnityEngine.InputSystem;

public class InputUtility
{
    public static Vector3 GetMousePositionWS(Camera camera)
    {
#if ENABLE_INPUT_SYSTEM
        var mousePosition = (Vector3)Mouse.current.position.ReadValue();
#else
            var mousePosition = Input.mousePosition;
#endif
        return ScreenPointToWorld(mousePosition, camera);
    }

    public static Vector3 GetScreenMiddlePositionWS(Camera camera)
    {
        var screenMiddle = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        return ScreenPointToWorld(screenMiddle, camera);
    }

    public static Vector3 ScreenPointToWorld(Vector3 screenPoint, Camera camera)
    {
        var castPoint = camera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(castPoint, out var hit, camera.farClipPlane))
        {
            return hit.point;
        }
        return Vector3.zero;
    }
}