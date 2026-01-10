using MOYV.RunTime.Game.Tool;
using QFramework;
using UnityEngine;

namespace MOYV.RunTime.Game.Core
{
    public class BaseRouter : Poolable
    {
        public virtual void OnReset() { useFlagId = -1; }
        public virtual void OnUpdate(float delta) { }
    }
}