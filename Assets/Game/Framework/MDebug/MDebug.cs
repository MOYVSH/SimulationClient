using System;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace MOYV.RunTime.Game.Tool
{
    public enum LogLevel
    {
        ///<summary>全部打印</summary>
        All = 0,
        ///<summary>log,error,exception</summary>
        Log = 1,
        ///<summary>打印warring,error,exception</summary>
        Warring = 2,
        ///<summary>打印error,exception</summary>
        Error = 3
    }
    /// <summary>
    /// 日志管理
    /// </summary>
    public static class MDebug
    {
        static public LogLevel logLv = LogLevel.All;
        ///<summary>显示堆栈信息</summary>
        static public bool showStackTrace = true;
        ///<summary>设置日志等级</summary>
        static public void SetLogLv(LogLevel lv)
        {
            logLv = lv;
        }
        static public void Log(string context, params object[] paras)
        {
            if (logLv == LogLevel.Log || logLv == LogLevel.All)
            {
                string rest = $"[{DateTime.Now:HH:mm:ss:fff}-{Time.frameCount}]";
                if (paras != null && paras.Length > 0)
                {
                    rest += string.Format(context, paras);
                }
                else
                {
                    rest += context;
                }
                if (showStackTrace) rest += "\n" + new System.Diagnostics.StackTrace().ToString();
                Debug.Log(rest);
            }
        }
        static public void Warning(string context, params object[] paras)
        {
            if (logLv == LogLevel.Warring || logLv == LogLevel.All)
            {
                string rest = $"[{DateTime.Now:HH:mm:ss:fff}] ";
                if (paras != null && paras.Length > 0)
                {
                    rest += string.Format(context, paras);
                }
                else
                {
                    rest += context;
                }
                if (showStackTrace) rest += "\n" + new System.Diagnostics.StackTrace().ToString();
                Debug.LogWarning(rest);
            }
        }
        static public void Error(string context, params object[] paras)
        {
            if (logLv == LogLevel.Error || logLv == LogLevel.All)
            {
                string rest = $"[{DateTime.Now:HH:mm:ss:fff}] ";
                if (paras != null && paras.Length > 0)
                {
                    rest += string.Format(context, paras);
                }
                else
                {
                    rest += context;
                }
                // rest ="<color=#ff0000>" + rest + ColorConst.ugui_close+"</color>";
                rest += "\n" + new System.Diagnostics.StackTrace().ToString();
                Debug.LogError(rest);
            }
        }


#if UNITY_EDITOR
        // 全局缓存，避免重复创建
        private static readonly Dictionary<int, LineRenderer> s_LineRenderers = new Dictionary<int, LineRenderer>();
        static LineRenderer AcquireLineRender(int uid, Color? color = null, float lineWidth = 0.02f)
        {
            if (!s_LineRenderers.TryGetValue(uid, out var lr))
            {
                var go = new GameObject($"LineRenderer_{uid}");
                lr = go.AddComponent<LineRenderer>();

                lr.useWorldSpace = true;
                lr.loop = true; // 闭合
                lr.alignment = LineAlignment.View; // 面向摄像机
                lr.material = new Material(Shader.Find("Sprites/Default")); // 默认支持透明颜色
                s_LineRenderers[uid] = lr;
            }

            // 设置颜色
            Color c = color ?? Color.white;
            lr.startColor = c;
            lr.endColor = c;

            // 设置线宽
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            return lr;
        }
        /// <summary>
        /// 绘制多边形（由LineRenderer实现）
        /// </summary>
        /// <param name="uid">唯一ID，用于区分不同LineRenderer</param>
        /// <param name="pos">圆心位置</param>
        /// <param name="radius">半径</param>
        /// <param name="edges">边数，最小3</param>
        /// <param name="color">线颜色</param>
        /// <param name="lineWidth">线宽</param>
        public static void DrawPolyWithLineRenderer(int uid, Vector3 pos, float radius = 0.1f, int edges = 3, Color? color = null, float lineWidth = 0.02f)
        {
            var lr = AcquireLineRender(uid, color, lineWidth);

            // 顶点数
            lr.positionCount = edges = Mathf.Max(edges, 3);

            // 计算顶点
            float step = 2f * Mathf.PI / edges;
            for (int i = 0; i < edges; i++)
            {
                float angle = i * step;
                Vector3 p = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius + pos;
                lr.SetPosition(i, p);
            }
        }

        /// <summary>
        /// 绘制坐标组连线
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="posArr"></param>
        /// <param name="color"></param>
        /// <param name="lineWidth"></param>
        public static void DrawPosArrWithLineRenderer(int uid, Vector3[] posArr, Color? color = null, float lineWidth = 0.02f)
        {
            var len = posArr?.Length ?? 0;
            if (len > 0)
            {
                var lr = AcquireLineRender(uid, color, lineWidth);
                for (int i = 0; i < posArr.Length; i++)
                {
                    Vector3 pos = posArr[i];
                    lr.SetPosition(i, pos);
                }
            }
            else
            {
                RemoveLineRenderer(uid);
            }
        }

        /// <summary>
        /// 移除指定uid的LineRenderer
        /// </summary>
        public static void RemoveLineRenderer(int uid)
        {
            if (s_LineRenderers.TryGetValue(uid, out var lr))
            {
                UObject.Destroy(lr.gameObject);
                s_LineRenderers.Remove(uid);
            }
        }

        /// <summary>
        /// 清空所有LineRenderer
        /// </summary>
        public static void ClearAllLineRenderers()
        {
            foreach (var lr in s_LineRenderers.Values)
            {
                if (lr != null) UObject.Destroy(lr.gameObject);
            }
            s_LineRenderers.Clear();
        }
#endif
    }
}
