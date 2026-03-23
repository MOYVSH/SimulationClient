using MOYV.RunTime.Game.Tool;
using UnityEngine.Events;

namespace MOYV.RunTime.Game.Logic
{
    /// <summary>触发器逻辑基类</summary>
    public abstract class AFunc_Trigger_Base : Poolable
    {
        protected Trigger Master { get; private set; }
        protected TriggerActorData m_MasterData { get; private set; }
        private UnityAction m_OnCompleted;


        public virtual void Initialize(Trigger pMaster, UnityAction pOnOneFinished)
        {
            this.Master = pMaster;
            this.m_MasterData = pMaster.data as TriggerActorData;
            this.m_OnCompleted = pOnOneFinished;
        }

        public override void OnRecycle()
        {
            Master = null;
            m_MasterData = null;
            base.OnRecycle();
        }

        internal virtual void RepeatTrigger(bool pass) { }

        public abstract void Execute();


        // Reverse execution, used at the opposite moment of the trigger timing. Not nessesary
        public virtual void ReverseExecute() { }


        // Each implementation must call this method manually while logic execution is completed.
        protected void Finish()
        {
            if (!base.isWillRemove)
            {
                this.m_OnCompleted.Invoke();
            }
        }
    }
}