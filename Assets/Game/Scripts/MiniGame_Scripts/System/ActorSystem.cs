using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Logic;
using MOYV.RunTime.Game.Tool;
using QFramework;

namespace Runtime.System
{
    public class ActorSystem : AbstractSystem
    {
        /// <summary>
        /// 等待删除的UID起始位置
        /// </summary>11
        private long waitRomoveUid = -1000000000000;
        private Dictionary<long, BaseActorData> dataDic = new Dictionary<long, BaseActorData>();
        private Dictionary<long, BaseActor> actorDic = new Dictionary<long, BaseActor>();
        
        private List<BaseActor> list = new List<BaseActor>();

        protected RouteService _routeService;
        protected RouteService routeService
        {
            get
            {
                if (_routeService == null)
                {
                    _routeService = CPool.Pop<RouteService>();
                }
                return _routeService;
            }
        }
        protected EventRouterQF eventRoute;

        
        
        protected override void OnInit()
        {
            RegisterRoutes();
        }

        public void AfterSceneInit()
        {
            eventRoute = CPool.Pop<EventRouterQF>();
            eventRoute.Register<UpdateEvent>(UpdateEvent.eventID, OnUpdate);
            eventRoute.Register<FixedUpdateEvent>(FixedUpdateEvent.eventID, OnFixedUpdate);
        }

        public void ClearDataAfterChangeLevel()
        {
            routeService.OnReset();
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                BaseActor actor = list[i];
                ReallyRecycleActor(actor, i);
            }
            list.Clear();
            dataDic.Clear();
            actorDic.Clear();
            waitRomoveUid = -1000000000000;
        }
        
        protected void RegisterRoutes()
        {           
            eventRoute= routeService.Add<EventRouterQF>();     
        }

        public void OnUpdate(UpdateEvent e)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                BaseActor actor = list[i];
                if (!actor.isUnUsed)
                {
                    actor.OnUpdate(e.deltaTime);
                }
                else
                {
                    ReallyRecycleActor(actor, i);
                }
            }
        }

        public void OnFixedUpdate(FixedUpdateEvent e)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                BaseActor actor = list[i];
                if (!actor.isUnUsed)
                {
                    actor.OnFixedUpdate(e.deltaTime);
                }
                else
                {
                    ReallyRecycleActor(actor, i);
                }
            }
        }

        private void ReallyRecycleActor(BaseActor actor, int i)
        {
            MDebug.Log($"移除对象:{actor.data.uid}");
            actorDic.Remove(actor.data.uid);
            if (i != -1)
            {
                list.RemoveAt(i);
            }
            else
            {
                for (int n = 0; n < list.Count; n++)
                {
                    if (list[n] == actor)
                    {
                        list.RemoveAt(n);
                        break;
                    }
                }
            }
            actor?.PushToPool();
        }
        
        public BaseActor CreateActor(BaseActorData data, Action<BaseActor> initOverCB)
        {
            if (!dataDic.ContainsKey(data.uid))
            {
                AddData(data);
            }
            else
            {
                if (dataDic[data.uid].isUnUsed)//>同ID数据卸载中
                {
                    initOverCB?.Invoke(null);
                    return null;
                }
            }

            BaseActor actor = null;

            actor = CPool.Pop<Trigger>();
            
            actor.OnInit(data, initOverCB);
            actor.transform.position = data.mapInfo.pos;
            actor.transform.eulerAngles = data.mapInfo.angle;
            
            return actor;
        }

        public void AddActor(BaseActor actor)
        {
            if (actor == null || actor.isUnUsed)
            {
                return;
            }
            BaseActor temp = null;
            if (actorDic.TryGetValue(actor.data.uid, out temp))
            {
                if (temp != actor)
                {
                    //>不是同一个actor,需要移除旧的，添加新的  
                    actorDic.Remove(actor.data.uid);
                    list.Remove(temp);
                    list.Add(actor);
                    temp.PushToPool();
                    actorDic.Add(actor.data.uid, actor);
                    //CDebug.Log($"AAA add actor uid{actor.data.uid} dataID{actor.data.dataID}");
                }
                else
                {
                    MDebug.Error("重复的添加actor--->" + actor.data.actorType + "/" + actor.data.uid);
                }
            }
            else
            {
                actorDic.Add(actor.data.uid, actor);
                //CDebug.Log($"BBB add actor uid{actor.data.uid} dataID{actor.data.dataID}");
                list.Add(actor);
            }
        }

        public void RemoveActor(BaseActor baseActor)
        {
            if (baseActor == null || baseActor.isUnUsed)
            {
                return;
            }
            BaseActor temp = null;
            if (actorDic.TryGetValue(baseActor.data.uid, out temp))
            {
                temp.isWillRemove = true;
                temp.data.isWillRemove = true;
                
                var uid = baseActor.data.uid;
                var newUid = waitRomoveUid--;
                actorDic.Remove(uid);
                actorDic.Add(newUid, temp);
                dataDic.Remove(uid);
                dataDic.Add(newUid, baseActor.data);
            }
            else
            {
                RemoveData(baseActor.data);
                baseActor?.PushToPool();
            }
        }

        #region Data

        public void AddData(BaseActorData data)
        {
            if (data == null || data.isUnUsed)
            {
                return;
            }
            
            BaseActorData tempData = null;
            if (dataDic.TryGetValue(data.uid, out tempData))
            {
                if (tempData != data) //>不是同一个data,需要移除旧的，添加新的
                {
                    RemoveData(tempData.uid);
                    dataDic.Add(data.uid, data);
                }
            }
            else
            {
                dataDic.Add(data.uid, data);
            }
        }
        public BaseActorData GetData(long uid)
        {
            dataDic.TryGetValue(uid, out var tempData);
            if (tempData != null && tempData.isUnUsed) return null;
            return tempData;
        }
        public T GetData<T>(long uid) where T : BaseActorData
        {
            if (dataDic.TryGetValue(uid, out var data))
            {
                if (data != null && data.isUnUsed) return null;
                return data as T;
            }
            return null;
        }

        public void RemoveData(long uid)
        {
            if (dataDic.TryGetValue(uid, out BaseActorData tempData))
            {
                dataDic.Remove(uid);
                tempData?.PushToPool();
            }
        }
        
        public void RemoveData(BaseActorData data)
        {
            if (data == null || data.isUnUsed)
            {
                return;
            }
            if (dataDic.TryGetValue(data.uid, out BaseActorData tempData))
            {
                dataDic.Remove(data.uid);
            }
            tempData?.PushToPool();
        }
        
        #endregion
    }
}

