using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MOYV.RunTime.Game.Tool
{
    [Serializable]
    /// <summary>
    /// 类对象池
    /// </summary>
    public class ClassPool
    {
        private static int poolClassFlag = int.MinValue;
        private Stack<IPoolable> stack;
        private byte reAllowNum;
        public Type t;

        ///<summary>已经分配了多少了，用来对比池中的数量，如果一致，则表示可以释放该类了</summary>
        private int allowedNum = 0;

        ///<summary>是否需要自动初始化(执行构造函数),不执行构造函数可以有效的提高性能，但是只能用在所有字段都需要重新进行赋值的情况下使用</summary>
        private bool needInitialized;

        public ClassPool()
        {
            stack = new Stack<IPoolable>();
        }

        public ClassPool Build(Type t, bool needInitialized = false, byte allowNum = 1, byte reAllowNum = 1)
        {
            this.t = t;
            this.needInitialized = needInitialized;
            if (allowNum < 1) allowNum = 1;
            Allow(allowNum);
            this.reAllowNum = reAllowNum;
            if (this.reAllowNum == 0) this.reAllowNum = 1;
            return this;
        }

        ///<summary>分配类对象</summary>
        private void Allow(byte num)
        {
            for (byte i = 0; i < num; i++)
            {
                IPoolable p = null;
                if (needInitialized)
                {
                    p = FormatterServices.GetUninitializedObject(t) as IPoolable;
                }
                else
                {
                    p = Activator.CreateInstance(t) as IPoolable;
                }

                p.IsInPool = true;
                stack.Push(p);
                allowedNum++;
            }
        }

        ///<summary>入栈一个类对象</summary>
        public void Push(IPoolable t)
        {
            if (t == null) return;
            if (t.IsInPool)
            {
                return;
            }

            if (!stack.Contains(t))
            {
                t.Recycle();
                t.IsInPool = true;
                t.useFlagId = 0;
                stack.Push(t);
            }
        }

        ///<summary>出栈一个类对象</summary>
        public IPoolable Pop()
        {
            if (stack.Count <= 0) Allow(reAllowNum);
            IPoolable t = null;
            while (true)
            {
                if (stack.Count <= 0) Allow(reAllowNum);
                t = stack.Pop();
                if (t != null) break;
            }

            t.IsInPool = false;
            t.isWillRemove = false;
            poolClassFlag++;
            t.useFlagId = poolClassFlag;
            return t;
        }

        public bool canRelease
        {
            get { return allowedNum > 1 && allowedNum == stack.Count; }
        }

        ///<summary>场景切换完成后释放掉一些类来获得内存</summary>
        public void OnReleaseMaxCache(int cache)
        {
            if (stack.Count > cache)
            {
                while (stack.Count > cache)
                {
                    stack.Pop();
                    allowedNum--;
                }
            }
        }

        ///<summary>该对象池被回收</summary>
        public void Recycle()
        {
            byte count = (byte)stack.Count;
            for (byte i = 0; i < count; i++)
            {
                IPoolable t = stack.Pop();
                if (!Poolable.IsNull(t)) t.Recycle();
            }
        }
    }
}