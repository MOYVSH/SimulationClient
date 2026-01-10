using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Tool;
using QFramework;
using UnityEngine;

namespace MOYV.RunTime.Game.Core
{
    public class EventRouter1 : BaseRouter, ICanRegisterEvent
    {
        // 在这个类中我想统一记录注册的事件，以便调试和排查问题
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

        public void Register<T>(int eventId, Action<T> cb)
        {
            if (dic.ContainsKey(eventId))
            {
                MDebug.Error("不允许同一个模块中重复注册事件:" + eventId);
            }
            else
            {
                dic.Add(eventId, cb);
                this.RegisterEvent(cb);
            }
        }

        public void UnRegister<T>(int eventId)
        {
            Delegate d = null;
            if (dic.TryGetValue(eventId, out d))
            {
                this.UnRegisterEvent((Action<T>)d);
                dic.Remove(eventId);
            }
        }

        public void UnRegisterAll()
        {
            this.UnRegisterAll();
            dic.Clear();
        }

        #region 继承 ICanRegisterEvent

        public IArchitecture GetArchitecture()
        {
            return GameArchitecture.Interface;
        }

        public string logTag { get; }

        public void Log(object msg, GameObject context = null)
        {
        }

        public void LogWarning(object msg, GameObject context = null)
        {
        }

        public void LogError(object msg, GameObject context = null)
        {
        }

        #endregion
    }
}