using UnityEngine;

public static class GizmosUtility
{
    public static void DrawArrow(Vector3 from, Vector3 to, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.DrawLine(from, to);
        var direction = to - from;
        var right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        var left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        Gizmos.DrawRay(to, right * arrowHeadLength);
        Gizmos.DrawRay(to, left * arrowHeadLength);
    }

    public static void DrawCross(Vector3 position, float size)
    {
        Gizmos.DrawLine(position + new Vector3(-size, 0, 0), position + new Vector3(size, 0, 0));
        Gizmos.DrawLine(position + new Vector3(0, 0, -size), position + new Vector3(0, 0, size));
    }

    public static void DrawX(Vector3 position, float size)
    {
        var topLeft = position + new Vector3(-size, 0.0f, size) * Mathf.Sqrt(0.5f);
        var topRight = position + new Vector3(size, 0.0f, size) * Mathf.Sqrt(0.5f);
        var bottomLeft = position + new Vector3(-size, 0.0f, -size) * Mathf.Sqrt(0.5f);
        var bottomRight = position + new Vector3(size, 0.0f, -size) * Mathf.Sqrt(0.5f);

        Gizmos.DrawLine(topLeft, bottomRight);
        Gizmos.DrawLine(topRight, bottomLeft);
    }
}