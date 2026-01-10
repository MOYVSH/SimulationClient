using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MOYV.RunTime.Game.Tool
{
    public interface IPoolable
    {
        ///<summary>每次使用时给的唯一标记</summary>
        int useFlagId { get; set; }

        bool IsInPool { get; set; }
        bool isWillRemove { get; set; }

        ///<summary>进入池中</summary>
        void PushToPool();

        ///<summary>该方法只能在框架层调用，逻辑层禁止调用</summary>
        void Recycle();

        void OnRecycle();
    }

    public abstract class Poolable : IPoolable
    {
        public int useFlagId { get; set; }

        public bool isUnUsed
        {
            get { return isWillRemove || IsInPool; }
        }

        public static bool IsNull(IPoolable obj)
        {
            if (obj == null || obj.isWillRemove || obj.IsInPool) return true;
            return false;
        }

        public static bool IsNullOrChanged(IPoolable obj, int flag)
        {
            if (obj == null || obj.isWillRemove || obj.IsInPool || obj.useFlagId != flag) return true;
            return false;
        }

        public bool IsInPool { get; set; }
        public bool isWillRemove { get; set; }

        public void PushToPool()
        {
            if (!IsInPool)
            {
                CPool.Push(this);
            }
        }

        ///<summary>该方法只能在框架层调用，逻辑层禁止调用</summary>
        public void Recycle()
        {
            isWillRemove = false;
            if (!IsInPool)
            {
                OnRecycle();
                IsInPool = true;
            }
        }

        public virtual void OnRecycle()
        {
        }
    }
}