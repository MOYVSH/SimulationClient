using System;
using System.Collections.Generic;
using UnityEngine;


namespace MOYV.RunTime.Game.Tool
{
    /// <summary>
    /// 事件管理器   
    /// </summary>
    public static class MEvent
    {
        private static Dictionary<int, List<Delegate>> dic = new Dictionary<int, List<Delegate>>();

        public static void OnReset()
        {
            foreach (var v in dic)
            {
                v.Value.Clear();
            }

            dic.Clear();
        }

        public static void Register(int eventId, Action cb)
        {
            RegisterObj(eventId, cb);
        }

        public static void Register<T>(int eventId, Action<T> cb)
        {
            RegisterObj(eventId, cb);
        }

        /*public static void Register<T, T1>(int eventId, Action<T, T1> cb)
        {
            RegisterObj(eventId, cb);
        }

        public static void Register<T, T1, T2>(int eventId, Action<T, T1, T2> cb)
        {
            RegisterObj(eventId, cb);
        }

        public static void Register<T, T1, T2, T3>(int eventId, Action<T, T1, T2, T3> cb)
        {
            RegisterObj(eventId, cb);
        }

        public static void Register<T, T1, T2, T3, T4>(int eventId, Action<T, T1, T2, T3, T4> cb)
        {
            RegisterObj(eventId, cb);
        }

        public static void Register<T, T1, T2, T3, T4, T5>(int eventId, Action<T, T1, T2, T3, T4, T5> cb)
        {
            RegisterObj(eventId, cb);
        }*/

        private static void RegisterObj(int eventId, Delegate cb)
        {
            List<Delegate> list = null;

            if (dic.TryGetValue(eventId, out list))
            {
                if (!list.Contains(cb))
                {
                    list.Add(cb);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("回调被重复注册->" + cb.ToString());
                }
            }
            else
            {
                dic.Add(eventId, new List<Delegate> { cb });
            }
        }

        public static void UnRegister(int eventId, Action cb)
        {
            UnRegisterCB(eventId, cb);
        }

        public static void UnRegister<T>(int eventId, Action<T> cb)
        {
            UnRegisterCB(eventId, cb);
        }

        /*public static void UnRegister<T, T1>(int eventId, Action<T, T1> cb)
        {
            UnRegisterCB(eventId, cb);
        }

        public static void UnRegister<T, T1, T2>(int eventId, Action<T, T1, T2> cb)
        {
            UnRegisterCB(eventId, cb);
        }

        public static void UnRegister<T, T1, T2, T3>(int eventId, Action<T, T1, T2, T3> cb)
        {
            UnRegisterCB(eventId, cb);
        }

        public static void UnRegister<T, T1, T2, T3, T4>(int eventId, Action<T, T1, T2, T3, T4> cb)
        {
            UnRegisterCB(eventId, cb);
        }

        public static void UnRegister<T, T1, T2, T3, T4, T5>(int eventId, Action<T, T1, T2, T3, T4, T5> cb)
        {
            UnRegisterCB(eventId, cb);
        }*/

        public static void UnRegisterCB(int eventId, Delegate cb)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Contains(cb))
                {
                    list.Remove(cb);
                }
            }
        }

        public static void Send(int eventId)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action action = list[i] as Action;
                        action?.Invoke();
                    }
                }
            }
        }

        public static void Send<T>(int eventId, T param)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T> action = list[i] as Action<T>;
                        if (action != null)
                        {
                            action.Invoke(param);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "}");
                        }
                    }
                }
            }
        }

        /*public static void Send<T, T1>(int eventId, T param, T1 param1)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T, T1> action = list[i] as Action<T, T1>;
                        if (action != null)
                        {
                            action.Invoke(param, param1);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "/" +
                                           typeof(T1) + "}");
                        }
                    }
                }
            }
        }

        public static void Send<T, T1, T2>(int eventId, T param, T1 param1, T2 param2)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T, T1, T2> action = list[i] as Action<T, T1, T2>;
                        if (action != null)
                        {
                            action.Invoke(param, param1, param2);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "/" +
                                           typeof(T1) + "/" + typeof(T2) + "}");
                        }
                    }
                }
            }
        }

        public static void Send<T, T1, T2, T3>(int eventId, T param, T1 param1, T2 param2, T3 param3)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T, T1, T2, T3> action = list[i] as Action<T, T1, T2, T3>;
                        if (action != null)
                        {
                            action.Invoke(param, param1, param2, param3);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "/" +
                                           typeof(T1) + "/" + typeof(T2) + "/" + typeof(T3) + "}");
                        }
                    }
                }
            }
        }

        public static void Send<T, T1, T2, T3, T4>(int eventId, T param, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T, T1, T2, T3, T4> action = list[i] as Action<T, T1, T2, T3, T4>;
                        if (action != null)
                        {
                            action.Invoke(param, param1, param2, param3, param4);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "/" +
                                           typeof(T1) + "/" + typeof(T2) + "/" + typeof(T3) + "/" + typeof(T4) + "}");
                        }
                    }
                }
            }
        }

        public static void Send<T, T1, T2, T3, T4, T5>(int eventId, T param, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            List<Delegate> list = null;
            if (dic.TryGetValue(eventId, out list))
            {
                if (list.Count > 0)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        Action<T, T1, T2, T3, T4, T5> action = list[i] as Action<T, T1, T2, T3, T4, T5>;
                        if (action != null)
                        {
                            action.Invoke(param, param1, param2, param3, param4, param5);
                        }
                        else
                        {
                            Debug.LogError("事件：" + eventId + " 发送失败，请保持参数类型注册和发送时一致! {" + typeof(T) + "/" +
                                           typeof(T1) + "/" + typeof(T2) + "/" + typeof(T3) + "/" + typeof(T4) + "/" +
                                           typeof(T5) + "}");
                        }
                    }
                }
            }
        }*/
    }
}