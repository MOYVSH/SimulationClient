using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class QuadExpand
{
    //Bounds的拓展方法

    //检测包围盒是否在摄像机范围内。如果该Bound的八个点都不在摄像机可视范围内，返回false，否则返回true

    //渲染管线是局部坐标系=》世界坐标系=》摄像机坐标系=》裁剪坐标系-》屏幕坐标系，其中在后三个坐标系中可以很便捷的得到某个点是否处于摄像机可视范围内。

    //在此用裁剪坐标系来判断，省了几次坐标转换，判断某个点在摄像机可视范围内方法如下：

    //1.将该点转换到裁剪空间，得到裁剪空间中的坐标为vec(x,y,z,w)，那么如果-w<x<w&&-w<y<w&&-w<z<w，那么该点在摄像机可视范围内。

    //2.对Bound来说，它有8个点，当它的8个点同时处于摄像机裁剪块上方/下方/前方/后方/左方/右方，那么该bound不与摄像机可视范围交叉
    public static bool CheckBoundIsInCamera(this Bounds bound, Camera camera)
    {
        //传入裁剪空间中的Vector4坐标，如果该点在摄像机可视范围内，返回0
        System.Func<Vector4, int> ComputeOutCode = (projectionPos) =>
        {
            int _code = 0;
            //该点位于视锥体的左方
            if (projectionPos.x < -projectionPos.w) _code |= 1;
            //该点位于视锥体的右方
            if (projectionPos.x > projectionPos.w) _code |= 2;
            //该点位于视锥体的下方
            if (projectionPos.y < -projectionPos.w) _code |= 4;
            //该点位于视锥体的上方
            if (projectionPos.y > projectionPos.w) _code |= 8;
            //该点位于视锥体的后方
            if (projectionPos.z < -projectionPos.w) _code |= 16;
            //该点位于视锥体的前方
            if (projectionPos.z > projectionPos.w) _code |= 32;
            return _code;
        };

        Vector4 worldPos = Vector4.one;
        int code = 63;
        //对Bound的八个点
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                for (int k = -1; k <= 1; k += 2)
                {
                    worldPos.x = bound.center.x + i * bound.extents.x;
                    worldPos.y = bound.center.y + j * bound.extents.y;
                    worldPos.z = bound.center.z + k * bound.extents.z;
                    //projectionMatrix为投影矩阵，是从摄像机空间转换到裁剪空间的矩阵；worldToCameraMatrix为从世界空间变换为摄像机空间的矩阵
                    code &= ComputeOutCode(camera.projectionMatrix * camera.worldToCameraMatrix * worldPos);
                    if(code==0)
                        return true;
                }
            }
        }
        return false;
    }
    
    public static bool CheckBoundIsInCameraSIMD(this Bounds bound, Camera camera)
    {
        // 合并矩阵，减少每次循环的乘法次数
        var matrix = camera.projectionMatrix * camera.worldToCameraMatrix;

        // 预先缓存center和extents，减少属性访问
        var center = bound.center;
        var extents = bound.extents;

        // 用于存储8个顶点的坐标
        float4[] points = new float4[8];
        int idx = 0;

        // 生成8个顶点坐标
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                for (int k = -1; k <= 1; k += 2)
                {
                    // 使用float4存储顶点坐标，方便SIMD批量运算
                    points[idx++] = new float4(
                        center.x + i * extents.x,
                        center.y + j * extents.y,
                        center.z + k * extents.z,
                        1f
                    );
                }
            }
        }

        int code = 63;
        // 批量处理8个顶点
        for (int n = 0; n < 8; n++)
        {
            // 使用math.mul进行矩阵乘法，利用SIMD加速
            float4 proj = math.mul(matrix, points[n]);

            int _code = 0;
            // 判断该点是否在视锥体外部
            if (proj.x < -proj.w) _code |= 1;
            if (proj.x > proj.w) _code |= 2;
            if (proj.y < -proj.w) _code |= 4;
            if (proj.y > proj.w) _code |= 8;
            if (proj.z < -proj.w) _code |= 16;
            if (proj.z > proj.w) _code |= 32;

            code &= _code;
            // 只要有一个点在视锥体内，直接返回true
            if (code == 0)
                return true;
        }
        // 所有点都在视锥体外部，返回false
        return false;
    }
}