using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    public class DecorateCMDRouter : BaseRouter
    {
        private Dictionary<AFuncType, Delegate> dic;
        protected Dictionary<AFuncType, Delegate> Dic => dic ??= new Dictionary<AFuncType, Delegate>();
        
        public override void OnRecycle()
        {
            UnRegisterAll();
            base.OnRecycle();
        }
        
        public override void OnReset()
        {
            UnRegisterAll();
            base.OnReset();
        }
        
        protected void UnRegisterAll()
        {
            if (dic != null)
            {
                Dic.Clear();
                dic = null;
            }
        }

        #region 注册

        /// <summary>
        /// 注册无参无返回功能
        /// </summary>
        public void Register(AFuncType funcType, Action callBack)
        {
            if (Dic.ContainsKey(funcType))
            {
                MDebug.Error("不允许同一个模块中重复注册命令:" + funcType);
            }
            else
            {
                Dic.Add(funcType, callBack);
            }
        }
        
        /// <summary>
        /// 注册无参有返回功能
        /// </summary>
        public void Register<ResultType>(AFuncType funcType, Func<ResultType> callBack) where ResultType : IAFuncCMDResult
        {
            if (Dic.ContainsKey(funcType))
            {
                MDebug.Error("不允许同一个模块中重复注册命令:" + funcType);
            }
            else
            {
                Dic.Add(funcType, callBack);
            }
        }
        
        /// <summary>
        /// 注册有参无返回功能
        /// </summary>
        internal void Register<ParamType>(AFuncType cmdType, Action<ParamType> callback) where ParamType : IAFuncCMDParam
        {
            if (Dic.ContainsKey(cmdType))
            {
                MDebug.Error("不允许同一个模块中重复注册命令:" + cmdType);
            }
            else
            {
                Dic.Add(cmdType, callback);
            }
        }
        
        /// <summary>
        /// 注册有参有返回功能
        /// </summary>
        internal void Register<ParamType, ResultType>(AFuncType cmdType, Func<ParamType, ResultType> callback) where ParamType : IAFuncCMDParam where ResultType : IAFuncCMDResult
        {
            if (Dic.ContainsKey(cmdType))
            {
                MDebug.Error("不允许同一个模块中重复注册命令:" + cmdType);
            }
            else
            {
                Dic.Add(cmdType, callback);
            }
        }
        
        #endregion

        #region 执行
        internal bool Contains(AFuncType funcType)
        {
            return dic != null && dic.ContainsKey(funcType);
        }

        /// <summary>
        ///  执行无参无返回功能
        /// </summary>
        internal void Execute(AFuncType funcType)
        {
            if(dic != null && dic.TryGetValue(funcType, out var deleg))
            {
                var action = deleg as Action;
                action?.Invoke();
            }
            else
            {
                MDebug.Error("未注册的命令:" + funcType);
            }
        }
        
        /// <summary>
        /// 执行无参有返回功能
        /// </summary>
        internal ResultType Execute<ResultType>(AFuncType funcType) where ResultType : IAFuncCMDResult
        {
            if (dic != null && dic.TryGetValue(funcType, out var deleg))
            {
                var func = deleg as Func<ResultType>;
                if (func != null)
                {
                    return func.Invoke();
                }
            }
            else
            {
                MDebug.Error("未注册的命令:" + funcType);
            }

            return default;
        }
        
        /// <summary>
        /// 执行有参无返回功能
        /// </summary>
        internal void Execute<ParamType>(AFuncType funcType, ParamType param) where ParamType : IAFuncCMDParam
        {
            if (dic != null && dic.TryGetValue(funcType, out var deleg))
            {
                var action = deleg as Action<ParamType>;
                action?.Invoke(param);
            }
            else
            {
                MDebug.Error("未注册的命令:" + funcType);
            }
        }
        
        /// <summary>
        /// 注册有参有返回功能
        /// </summary>
        internal ResultType Execute<ResultType ,ParamType>(AFuncType funcType, ParamType param) where ResultType : IAFuncCMDResult where ParamType : IAFuncCMDParam 
        {
            if (dic != null && dic.TryGetValue(funcType, out var deleg))
            {
                var func = deleg as Func<ParamType, ResultType>;
                if (func != null)
                {
                    return func.Invoke(param);
                }
            }
            else
            {
                MDebug.Error("未注册的命令:" + funcType);
            }

            return default;
        }
        #endregion
    }
}