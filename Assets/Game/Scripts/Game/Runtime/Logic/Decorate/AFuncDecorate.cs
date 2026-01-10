using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;
using UnityEngine.Pool;

namespace MOYV.RunTime.Game.Logic
{
    public abstract class AFuncDecorate : Poolable, IComparable
    {
        protected AFuncDecorate _master;
        /// <summary>
        /// 被装饰对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T master<T>() where T : AFuncDecorate => _master as T;
        
        /// <summary>
        /// 排序值,确定update等执行的顺序
        /// </summary>
        public virtual int order { get; private set; }
        
        /// <summary>
        /// 功能列表 用于复合装饰器
        /// </summary>
        protected List<AFuncDecorate> funcList;
        public int funcCount { get; private set; }

        // 事件路由 用于回收阶段自动卸载事件 挂载
        protected EventRouter eventRoute;

        /// <summary>
        /// 功能类型
        /// </summary>
        public virtual AFuncType funcType { get; private set; }
        /// <summary>
        /// 命令职责范围
        /// </summary>
        protected virtual int cmdOffset { get; private set; }
        
        /// <summary>
        /// 装饰器命令路由
        /// </summary>
        protected DecorateCMDRouter cmdRouter { get; private set; }
        
        /// <summary>
        /// 初始化结束
        /// </summary>
        protected bool initOver { get; private set; } = false;

        public virtual void Init(AFuncDecorate master)
        {
            if (FuncDecoratorStaticRegistry.TryGetInfo(GetType(), out (AFuncType funcType, int offset, int order) info))
            {
                funcType = info.funcType;
                cmdOffset = info.offset;
                order = info.order;
            }
            else
            {
                MDebug.Log($"{GetType()}没使用{typeof(FuncDecoratorAttribute)}注册装饰器或者注册失败，请检查。");
                funcType = AFuncType.NONE;
                cmdOffset = 0;
                order = 0;
            }

            if (master == null)
            {
                MDebug.Log("请注意，初始化master为空，请检查。");
            }

            this._master = master;
            cmdRouter = CPool.Pop<DecorateCMDRouter>();
            OnRegisterCMD();
            eventRoute = CPool.Pop<EventRouter>();
            OnRegisterEvent();
            InitFunc();
        }

        protected virtual void OnRegisterEvent() { }
        protected virtual void OnUnRegisterEvent() { }
        
        /// <summary>
        /// 注册装饰器功能命令
        /// </summary>
        protected abstract void OnRegisterCMD();

        /// <summary>
        /// 确定命令是否可以执行
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        // 举个栗子 
        // 那么假设 当前这个装饰器是一个 AFuncType.MyFunc1 = 80001 这个装饰器的cmdOffset是10
        // 那么我传入的是一个AFuncType.MyFunc2 = 80002 到 AFuncType.MyFunc9 = 80009 都会返回true
        public bool CanExecute(AFuncType cmd)
        {
            AFuncType cur = funcType;
            return cmd >= cur && cmd < (cur + cmdOffset);
        }


        public virtual void AddFunc(AFuncDecorate func,AFuncDecorate master)
        {
            funcList ??= ListPool<AFuncDecorate>.Get();
            AFuncDecorate dec = func;
            if (!funcList.Contains(func))
            {
                funcList.Add(dec);
                funcCount++;
                if (funcList.Count > 1)
                {
                    funcList.Sort();
                }
                dec.Init(master);

                if (initOver)
                {
                    dec.OnInitFuncOver();
                }
            }
            else
            {
                MDebug.Error($"向对象{this.GetType().Name}重复添加装饰器功能:{dec.funcType}，请检查。");
                dec.PushToPool();
            }
        }

        /// <summary>
        /// 移除功能 如果本层没有 则递归子装饰器
        /// </summary>
        /// <param name="funcId"></param>
        public virtual void RemoveFunc(AFuncType funcId)
        {
            AFuncDecorate dec = GetFuncDecorate(funcId);
            if (dec != null)
            {
                if (dec.funcType == funcType)
                {
                    dec.PushToPool();
                    funcList.Remove(dec);
                    funcCount--;
                }
                else
                {
                    dec.RemoveFunc(funcId);
                }
            }
        }
        
        protected void RemoveAllFunc()
        {
            if (funcCount == 0 || funcList == null) return;

            for (int i = funcCount - 1; i >= 0; i--)
            {
                funcList[i].PushToPool();
            }
            funcList.Clear();
            ListPool<AFuncDecorate>.Release(funcList);
            funcList = null;
            funcCount = 0;
        }

        /// <summary>
        /// 获得指定功能模块
        /// </summary>
        /// <param name="funcId"></param>
        /// <returns></returns>
        /// 每个装饰器都有一个职责区间：[funcType, funcType + cmdOffset)（半开区间）。
        /// 子装饰器的区间是父装饰器区间的子区间，且通常互不重叠，能把父区间切分覆盖。
        /// GetFuncDecorate 只在“本层的子装饰器列表”里找，调用子装饰器的 CanExecute 看命令是否落在它的区间内；由于区间划分不重叠，命令只会匹配到唯一一个子装饰器，于是拿到“指定模块”。
        /// 倒序遍历配合按 order 排序，能让优先级更高（order 大）的模块先命中，用于处理重叠/覆盖的特殊场景。
        public AFuncDecorate GetFuncDecorate(AFuncType funcId)
        {
            if (funcCount == 0 || funcList == null) return null;
            for (int i = funcCount - 1; i >= 0; i--)
            {
                if (funcList[i].CanExecute(funcId))
                {
                    return funcList[i];
                }
            }
            return null;
        }

        static readonly List<(AFuncType funcType, Type type, int offset)> sr_ChildList = new();

        protected AFuncDecorate TryAddChildDecorate(AFuncType funcType)
        {
            if (FuncDecoratorStaticRegistry.TryPopulateSortInfo(this.funcType, funcType, sr_ChildList))
            {
                var info = sr_ChildList[0];
                AFuncDecorate childDec = CPool.Pop(info.type) as AFuncDecorate;
                if (childDec != null)
                {
                    var root = _master ?? this; // 这地方是用来处理根节点的 根节点并没有master
                    AddFunc(childDec, root);
                }

                return childDec;
            }
            else
            {
                MDebug.Log($"<color=red>【前端同学】</color>请注意，{funcType}装饰器期望的子装饰器AFuncType.{funcType}不存在，请检查。");
            }
            
            return null;
        }

        public virtual void InitFunc() { }

        public virtual void OnInitFuncOver()
        {
            if (isUnUsed)
            {
                return;
            }

            initOver = true;
            
            if (funcCount == 0 || funcList == null) return;

            for (int i = funcCount - 1; i >= 0; i--)
            {
                AFuncDecorate func = funcList[i];
                func?.OnInitFuncOver();
            }
        }

        #region 功能更新
        // 树状逐级更新
        public virtual void UpdateFunc(float deltaTime)
        {
            if (funcCount == 0 || funcList == null) return;
            for (int i = funcCount - 1; i >= 0; i--)
            {
                var func = funcList[i];
                func?.UpdateFunc(deltaTime);
            }
        }

        public virtual void FixedUpdateFunc(float deltaTime)
        {
            if (funcCount == 0 || funcList == null) return;
            for (int i = 0; i < funcCount; i++)
            {
                funcList[i].FixedUpdateFunc(deltaTime);
            }
        }

        #endregion

        #region 触发

        /// <summary>
        /// 无参数无返回值
        /// </summary>
        /// <param name="func"></param>
        // 需要先确定是否有 相应的Dec 如果没有的话需要临时添加一个
        public virtual void Execute(AFuncType func)
        {
            if(isUnUsed) return;
            if (cmdRouter != null && cmdRouter.Contains(func))
            {
                cmdRouter.Execute(func);
            }
            else
            {
                var dec = GetFuncDecorate(func) ?? TryAddChildDecorate(func);
                dec?.Execute(func);
            }
        }

        /// <summary>
        /// 无参数有返回值
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public virtual R Execute<R>(AFuncType func) where R : IAFuncCMDResult
        {
            if(isUnUsed) return default;
            if (cmdRouter != null && cmdRouter.Contains(func))
            {
                return cmdRouter.Execute<R>(func);
            }
            else
            {
                var dec = GetFuncDecorate(func) ?? TryAddChildDecorate(func);
                if (dec != null)
                {
                    return dec.Execute<R>(func);
                }
            }
            return default;
        }

        public virtual void Execute<P>(AFuncType cmdType, P param) where P : IAFuncCMDParam
        {
            if (isUnUsed)
            {
                return;
            }
            if (cmdRouter != null && cmdRouter.Contains(cmdType))
            {
                cmdRouter.Execute(cmdType, param); // 自身包含则执行
            }
            else
            {
                var dec = GetFuncDecorate(cmdType) ?? TryAddChildDecorate(cmdType); // 获取或创建子装饰
                dec?.Execute(cmdType, param);
            }
        }
        
        public virtual R Execute<R, P>(AFuncType func, P param) where R : IAFuncCMDResult where P : IAFuncCMDParam
        {
            if (isUnUsed)
            {
                return default;
            }
            if (cmdRouter != null && cmdRouter.Contains(func))
            {
                return cmdRouter.Execute<R, P>(func, param); // 自身包含则执行
            }
            else
            {
                var dec = GetFuncDecorate(func) ?? TryAddChildDecorate(func); // 获取或创建子装饰
                if (dec != null)
                {
                    return dec.Execute<R, P>(func, param);
                }
            }
            return default;
        }

        #endregion
        
        /// <summary>
        /// 本对象被回收
        /// </summary>
        public override void OnRecycle()
        {
            initOver = false;
            cmdRouter?.PushToPool();
            cmdRouter = null;
            eventRoute?.PushToPool();
            eventRoute = null;
            RemoveAllFunc();
            base.OnRecycle();
            _master = null;
        }
        
        public int CompareTo(object obj)
        {
            AFuncDecorate other = obj as AFuncDecorate;
            return order < other.order ? -1 : order == other.order ? 0 : 1;
        }
    }
}