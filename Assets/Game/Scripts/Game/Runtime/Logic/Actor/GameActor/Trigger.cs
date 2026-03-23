using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;
using UnityEngine;

namespace MOYV.RunTime.Game.Logic
{
    public class Trigger : BaseActor
    {
        EventRouterQF eventRouter = new EventRouterQF();

        private TriggerActorData m_Data;
        TriggerActorData tData => m_Data ??= (data as TriggerActorData);
        
        public override void OnRecycle()
        {
            m_Data = null;
#if UNITY_EDITOR

#endif
            base.OnRecycle();
        }
        
        protected override void OnAddOtherFuncs()
        {
            base.OnAddOtherFuncs();
            AddFunc(CPool.Pop<AFunc_Test>(), this);
            AddFunc(CPool.Pop<AFunc_TestE>(), this);
            
        }

        protected override void OnRegisterEvent()
        {
            eventRouter.Register<ZTestEvent>(ConstEventID.ZTestEvent, OnZTestEvent);
        }

        private void OnZTestEvent(ZTestEvent e)
        {
            MDebug.Error("Trigger OnZTestEvent:" + e.EventID);
            MDebug.Error(data.uid.ToString());
        }

        protected override void OnAddFuncOver()
        {
            base.OnAddFuncOver();
        }
        
        public override void OnUpdate(float delta)
        {
            base.OnUpdate(delta);
#if UNITY_EDITOR
            DrawBox();
#endif
        }
        
        
#if UNITY_EDITOR
        /// <summary>
        /// 获取目标GameObject的边界
        /// 优先获取渲染组件的边界，如果没有则获取碰撞器的边界
        /// </summary>
        private Bounds? GetTargetBounds(GameObject target)
        {
            // 尝试获取渲染组件的边界（包括所有子物体）
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                return combinedBounds;
            }

            // 尝试获取碰撞器的边界（包括所有子物体）
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds combinedBounds = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    combinedBounds.Encapsulate(colliders[i].bounds);
                }
                return combinedBounds;
            }
            return null;
        }
        private LineRenderer lineRenderer;
        private Bounds? editorBounds;
        private void DrawBox()
        {
            TriggerActorData tData = data as TriggerActorData;
            if (editorBounds == null)
            {
                editorBounds = GetTargetBounds(gameObject);
            }
            
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddMissingComponent<LineRenderer>();
                lineRenderer.positionCount = 24; // 12条边 × 2个顶点
                lineRenderer.loop = false;
                lineRenderer.useWorldSpace = true; // 使用世界空间坐标
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;

                // 配置材质（URP兼容）
                var material = new Material(Shader.Find("Unlit/Color"));
                material.color = Color.green;
                lineRenderer.material = material;
            }
            
            if (editorBounds.HasValue)
            {
                lineRenderer.enabled = true;
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
                lineRenderer.startWidth = 0.01f;
                lineRenderer.endWidth = 0.01f;

                // 计算边界的8个顶点
                Vector3 center = editorBounds.Value.center;
                Vector3 extents = editorBounds.Value.extents;

                Vector3 p0 = center + new Vector3(-extents.x, -extents.y, -extents.z);
                Vector3 p1 = center + new Vector3(extents.x, -extents.y, -extents.z);
                Vector3 p2 = center + new Vector3(extents.x, -extents.y, extents.z);
                Vector3 p3 = center + new Vector3(-extents.x, -extents.y, extents.z);
                Vector3 p4 = center + new Vector3(-extents.x, extents.y, -extents.z);
                Vector3 p5 = center + new Vector3(extents.x, extents.y, -extents.z);
                Vector3 p6 = center + new Vector3(extents.x, extents.y, extents.z);
                Vector3 p7 = center + new Vector3(-extents.x, extents.y, extents.z);

                // 设置12条边的顶点（每条边2个顶点）
                lineRenderer.SetPositions(new[]
                {
                    p0, p1,   // 下底面后
                    p1, p2,   // 下底面右
                    p2, p3,   // 下底面前
                    p3, p0,   // 下底面左
                    p4, p5,   // 上底面后
                    p5, p6,   // 上底面右
                    p6, p7,   // 上底面前
                    p7, p4,   // 上底面左
                    p0, p4,   // 左侧面后
                    p1, p5,   // 右侧面后
                    p2, p6,   // 右侧面前
                    p3, p7    // 左侧面前
                });
            }
        }
        
        // 绘制单条线（封装GL.Vertex调用）
        private void DrawLine(Vector3 start, Vector3 end)
        {
            GL.Vertex(start);
            GL.Vertex(end);
        }
#endif
    }
}