using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Core
{
    public class EventRouter : BaseRouter
    {
        private Dictionary<int, Delegate> dic = new Dictionary<int, Delegate>();

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

        # region 注册
        public void Register(int eKey, Action callBack)
        {
            if (dic.TryGetValue(eKey, out _))
            {
                MDebug.Error("不允许同一个模块中重复注册事件:" + eKey);
            }
            else
            {
                dic.Add(eKey, callBack);
                MEvent.Register(eKey, callBack);
            }
        }

        public void Register<T>(int eKey, Action<T> callBack)
        {
            if (dic.TryGetValue(eKey, out _))
            {
                MDebug.Error("不允许同一个模块中重复注册事件:" + eKey);
            }
            else
            {
                dic.Add(eKey, callBack);
                MEvent.Register(eKey, callBack);
            }
        }

        // TODO: 添加拥有更多参数的 Register 方法
        #endregion

        #region 注销

        public void UnRegister(int eKey)
        {
            Delegate callback = null;
            if (dic.TryGetValue(eKey, out callback))
            {
                MEvent.UnRegisterCB(eKey, callback);
                dic.Remove(eKey);
            }
        }

        public void UnRegisterAll()
        {
            foreach (var v in dic)
            {
                MEvent.UnRegisterCB(v.Key, v.Value);
            }
            dic.Clear();
        }
        #endregion

        #region 派发

        public void send(int eKey)
        {
            MEvent.Send(eKey);
        }
        
        public void send<T>(int eKey, T param)
        {
            MEvent.Send(eKey, param);
        }

        // TODO: 添加拥有更多参数的 send 方法
        #endregion
    }
}