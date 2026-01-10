using UnityEngine;

namespace MOYV.RunTime.Game.Tool
{
    public static class PoolExtend
    {
        public static void AddChild_Pool(this Transform trans, Transform child, bool isActive = true)
        {
            child.SetParent(trans);
            child.gameObject.SetActive(isActive);
        }

        public static T PopFromPool<T>(this T obj) where T : IPoolable, new()
        {
            return CPool.Pop<T>(1);
        }

        public static void PushToPool(this GameObject go, string poolName)
        {
            CPool.Push(poolName, go);
        }
    }
}

