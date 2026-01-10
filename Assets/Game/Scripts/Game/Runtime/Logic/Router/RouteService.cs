using System;
using System.Collections.Generic;
using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Core
{
    public class RouteService : Poolable
    {
        protected Dictionary<Type, BaseRouter> routes = new Dictionary<Type, BaseRouter>();

        public override void OnRecycle()
        {
            OnReset();
            foreach (var v in routes)
            {
                 v.Value.PushToPool();
            }

            routes.Clear();
            base.OnRecycle();
        }

        public virtual T Add<T>() where T : BaseRouter, new()
        {
            BaseRouter route = null;
            Type type = typeof(T);
            
            if (!routes.ContainsKey(type))
            {
                route = CPool.Pop<T>() as BaseRouter;
                routes.Add(type, route);
            }

            return (T)route;
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var v in routes)
            {
                v.Value.OnUpdate(deltaTime);
            }
        }

        public void OnReset()
        {
            foreach (var v in routes)
            {
                v.Value.OnReset();
            }
        }
    }
}