using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOYV.RunTime.Game.Tool
{
    /// <summary>
    /// 游戏对象池
    /// </summary>
    [Serializable]
    public class GameObjectPool
    {
        public bool isReleased = false;
        private string poolName;

        public string PoolName
        {
            get { return poolName; }
        }

        private string assetName;

        public string AssetName
        {
            get { return assetName; }
        }

        ///<summary>克隆参照物</summary>
        private UnityEngine.Object obj;

        public bool isCloneObj;

        ///<summary>池对象挂载节点对象</summary>
        private GameObject poolRoot;

        ///<summary>休闲对象栈</summary>
        private Stack<GameObject> idleStack = new Stack<GameObject>();

        ///<summary>工作对象栈</summary>
        public List<GameObject> workStack = new List<GameObject>();

        ///<summary>重新分配数量</summary>
        private ushort reAllowNum;

        ///<summary>最大缓存值，如果超过则在切场景时删除多余的</summary>
        private ushort maxCache = 5;

        ///<summary>记录时间 当工作对象为0时开始计时，满足一定条件没有使用就主动释放掉节省内存 </summary>
        private float recordTime;

        ///<summary>超时自动回收时间</summary>
        public int outTime = 60;

        ///<summary>构造器</summary>
        public void Create(string poolName, string assetName, UnityEngine.Object obj, bool isCloneObj,
            GameObject poolRoot, ushort allowNum = 1, ushort reAllowNum = 1, ushort maxCache = 10)
        {
            this.poolName = poolName;
            this.assetName = assetName;
            this.obj = obj;
            this.poolRoot = poolRoot;
            this.isCloneObj = isCloneObj;
            if (isCloneObj)
            {
                this.poolRoot.transform.AddChild_Pool((obj as GameObject).transform, false);
            }

            this.reAllowNum = reAllowNum;
            this.maxCache = maxCache;
            Allow(allowNum);
        }

        ///<summary>分配</summary>
        private void Allow(ushort num)
        {
            for (int i = 0; i < num; i++)
            {
                GameObject go = GameObject.Instantiate(obj) as GameObject;
                poolRoot.transform.AddChild_Pool(go.transform, false);
                idleStack.Push(go);
            }
        }

        ///<summary>出栈</summary>
        public GameObject Pop(bool active = true)
        {
            if (idleStack.Count <= 0) Allow(reAllowNum);
            GameObject go = idleStack.Pop();
            PoolExtend.AddChild_Pool(null, go.transform, active);
            workStack.Add(go);
            recordTime = UnityEngine.Time.realtimeSinceStartup;
            return go;
        }

        ///<summary>入栈</summary>
        public void Push(GameObject go)
        {
            if (go == null) return;
            if (workStack.Contains(go)) workStack.Remove(go);
            if (idleStack.Contains(go))
            {
                return;
            }

            poolRoot.transform.AddChild_Pool(go.transform, false);
            idleStack.Push(go);
            if (workStack.Count == 0)
            {
                recordTime = UnityEngine.Time.realtimeSinceStartup;
            }
        }

        ///<summary>销毁</summary>
        public void Destroy(GameObject go)
        {
            if (go == null) return;
            if (workStack.Contains(go)) workStack.Remove(go);
            if (idleStack.Contains(go))
            {
                return;
            }

            go.SetActive(true);
            GameObject.Destroy(go);
        }

        public bool isTimeOut
        {
            get { return workStack.Count == 0 && UnityEngine.Time.realtimeSinceStartup - recordTime >= outTime; }
        }

        ///<summary>销毁多余最大缓存的游戏对象</summary>
        public void DestroySomeObject()
        {
            if (idleStack.Count > maxCache)
            {
                int num = idleStack.Count - maxCache;
                for (int i = 0; i < num; i++)
                {
                    GameObject go = idleStack.Pop();
                    go.SetActive(true);
                    GameObject.Destroy(go);
                }
            }
        }

        static bool GObjIsNull(GameObject go)
        {
            return go == null || go.name == "null";
        }

        ///<summary>池被回收</summary>
        public void OnRecycle()
        {
            isReleased = true;
            recordTime = 0;
            //1>会涉及到资源的回收  todo
            while (idleStack.Count > 0)
            {
                GameObject go = idleStack.Pop();
                if (go != null)
                {
                    go.SetActive(true);
                    GameObject.Destroy(go);
                }
            }

            idleStack.Clear();
            while (workStack.Count > 0)
            {
                GameObject go = workStack[0];
                workStack.RemoveAt(0);
                if (!GObjIsNull(go))
                {
                    go.SetActive(true);
                    GameObject.Destroy(go);
                }
            }

            workStack.Clear();
            poolName = null;
            if (obj != null && isCloneObj)
            {
                (obj as GameObject).SetActive(true);
                GameObject.Destroy(obj);
            }

            assetName = null;
        }
    }
}