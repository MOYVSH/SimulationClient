using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Tool;
using UnityEngine;

namespace MOYV.RunTime.Game.Tool
{
    public static class CPool
    {
        private static Dictionary<Type, ClassPool> classDic = new();
        private static Dictionary<string, GameObjectPool> gameObjDic = new();
        private static List<ClassPool> tempList = new();

        public static void OnReleaseMaxCache()
        {
            if (classDic != null)
            {
                tempList.Clear();
                foreach (var v in classDic)
                    if (v.Value.canRelease)
                        tempList.Add(v.Value);
                    else
                        v.Value.OnReleaseMaxCache(2);

                //>如果没有引用，暂时回收掉吧
                for (var i = 0; i < tempList.Count; i++)
                {
                    Type t = tempList[i].t;
                    if (classDic.ContainsKey(t))
                    {
                        classDic[t].Recycle();
                        classDic.Remove(t);
                    }
                }
            }
            //  UnityEngine.Debug.Log("释放后类对象池个数:" + classDic.Count);

            //>清除溢出最大缓存的对象
            if (gameObjDic != null)
                foreach (var v in gameObjDic)
                    v.Value.DestroySomeObject();
        }

        #region class pool

        private static int classPoolFlag = 0;

        /// <summary>
        /// 类出栈
        /// </summary>
        /// <typeparam name="T">类类型</typeparam>
        /// <param name="allowNum">每次实例化数量</param>
        /// <param name="needInitialized">类实例化后是否要自动调用构造函数，默认是自动构造，也可以不调用但必须用在类在使用前所有字段都会被赋值的情况下，否则会出现字段脏数据或者默认值丢失!慎重使用！！</param>
        /// <returns></returns>
        public static T Pop<T>(int allowNum = 1, bool needInitialized = false) where T : IPoolable, new()
        {
            var t = typeof(T);
            return (T)Pop(t, allowNum, needInitialized);
        }

        /// <summary>
        /// 类出栈
        /// </summary>
        /// <typeparam name="t">类类型</typeparam>
        /// <param name="allowNum">每次实例化数量</param>
        /// <param name="needInitialized">类实例化后是否要自动调用构造函数，默认是自动构造，也可以不调用但必须用在类在使用前所有字段都会被赋值的情况下，否则会出现字段脏数据或者默认值丢失!慎重使用！！</param>
        /// <returns></returns>
        public static IPoolable Pop(Type t, int allowNum = 1, bool needInitialized = false)
        {
            ClassPool pool = null;
            if (!classDic.TryGetValue(t, out pool))
            {
                pool = new ClassPool().Build(t, needInitialized, (byte)allowNum);
                classDic.Add(t, pool);
            }

            IPoolable p = pool.Pop();
            return p;
        }

        ///<summary>入池</summary>
        public static void Push(IPoolable p)
        {
            if (p == null) return;
            var t = p.GetType();
            ClassPool pool = null;
            if (classDic.TryGetValue(t, out pool))
                pool.Push(p);
            else
                p.Recycle();
        }

        ///<summary>回收类对象池</summary>
        public static void RecyclePool<T>() where T : IPoolable
        {
            var t = typeof(T);
            ClassPool pool = null;
            if (classDic.TryGetValue(t, out pool))
            {
                pool.Recycle();
                classDic.Remove(t);
            }
        }

        ///<summary>回收类对象池</summary>
        public static void RecyclePool(IPoolable p)
        {
            var t = p.GetType();
            if (classDic.ContainsKey(t))
            {
                classDic[t].Recycle();
                classDic.Remove(t);
            }
        }

        #endregion

        #region gameobject pool

        private static GameObject _poolRoot;

        public static GameObject poolRoot
        {
            get
            {
                if (_poolRoot == null) _poolRoot = new GameObject("[__CPool__]");
                return _poolRoot;
            }
        }

        public static GameObjectPool GetPool(string poolName)
        {
            GameObjectPool pool = null;
            gameObjDic.TryGetValue(poolName, out pool);
            return pool;
        }

        public static void RecycleGameObjectPool(string poolName)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                Debug.LogWarning("gameobject pool 回收异常，poolName=null");
                return;
            }

            GameObjectPool pool = null;
            if (gameObjDic.TryGetValue(poolName, out pool))
            {
                pool.OnRecycle();
                gameObjDic.Remove(poolName);
                // UnityEngine.Debug.Log("对象池被回收:" + poolName);
            }
        }

        ///<summary>对象池存不存在?</summary>
        public static bool HasGameObjPool(string poolName)
        {
            GameObjectPool pool = null;
            gameObjDic.TryGetValue(poolName, out pool);
            return pool != null;
        }

        ///<summary>创建游戏对象池</summary>
        public static void CreateGameObjectPool(string poolName, string assetName, UnityEngine.Object obj,
            bool isCloneObj,
            ushort allowNum = 1, ushort reAllowNum = 1, ushort maxCacheNum = 5)
        {
            if (!HasGameObjPool(poolName))
            {
                GameObjectPool pool = new GameObjectPool();
                pool.Create(poolName, assetName, obj, isCloneObj, poolRoot, allowNum, reAllowNum, maxCacheNum);
                gameObjDic.Add(poolName, pool);
            }
            else
            {
                Debug.LogWarning(string.Format("对象池{0}已存在，此次创建无效", poolName));
            }
        }

        ///<summary>游戏物体出池</summary>
        public static GameObject PopG(string poolName, bool active = true)
        {
            GameObjectPool pool = null;
            if (gameObjDic.TryGetValue(poolName, out pool))
            {
                return pool.Pop(active);
            }
            else
            {
                Debug.LogWarning(string.Format("对象池{0}不存在", poolName));
                return null;
            }
        }

        ///<summary>游戏物体入池</summary>
        public static void Push(string poolName, GameObject go)
        {
            if (go == null) return;
            if (!string.IsNullOrEmpty(poolName))
            {
                GameObjectPool pool = null;
                if (gameObjDic.TryGetValue(poolName, out pool))
                    pool.Push(go);
                else
                    // UnityEngine.Debug.LogWarning($"对象池{poolName}不存在");
                    GameObject.Destroy(go);
            }
            else
            {
                Debug.LogWarning(string.Format("对象{0}是非资源追踪对象，直接销毁", go.name));
                GameObject.Destroy(go);
            }
        }

        ///<summary>回收对象池</summary>
        public static void RecyclePool(string poolName)
        {
            GameObjectPool pool = null;
            if (gameObjDic.TryGetValue(poolName, out pool))
            {
                gameObjDic.Remove(poolName);
                pool.OnRecycle();
                // UnityEngine.Debug.Log("对象池被回收:"+poolName);
            }
            //else
            //{
            //    UnityEngine.Debug.LogWarning(string.Format("对象池{0}不存在", poolName));
            //}
        }

        ///<summary>尝试回收对象池(当没有使用中的对象时就可以回收了)</summary>
        public static void TryRecyclePool(string poolName)
        {
            GameObjectPool pool = null;
            if (gameObjDic.TryGetValue(poolName, out pool))
                if (pool.workStack.Count == 0)
                {
                    gameObjDic.Remove(poolName);
                    pool.OnRecycle();
                    // UnityEngine.Debug.Log("对象池被回收:" + poolName);
                }
        }

        public static void RecyclePoolByAsset(string assetName)
        {
            GameObjectPool pool = null;
            foreach (var v in gameObjDic)
                if (v.Value.AssetName == assetName)
                {
                    pool = v.Value;
                    break;
                }

            if (pool != null)
            {
                gameObjDic.Remove(pool.PoolName);
                pool.OnRecycle();
            }
        }

        #endregion
    }
}