using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    /// <summary>
    /// 触发器逻辑
    /// </summary>
    [FuncDecorator(AFuncType.Trigger, AFuncCMDOffset.T, AFuncOrder.Trigger)]
    public class AFunc_Trigger : AFuncDecorate
    {
        private AFunc_Trigger_Base m_CurrentTriggerLogic = null;
        
        public override void OnRecycle()
        {
            base.OnRecycle();

            this.m_CurrentTriggerLogic?.PushToPool();
            this.m_CurrentTriggerLogic = null;
        }

        protected override void OnRegisterCMD()
        {
            cmdRouter.Register<AFuncCMD_Trigger_Exeute>(AFuncType.Trigger_Exeute, ExeTrigger);
        }

        void ExeTrigger(AFuncCMD_Trigger_Exeute cmd)
        {
            var trigger = cmd.Trigger;
            //MDebug.Log($"AFunc_Trigger.ExecuteCMD {(trigger.data as TriggerActorData).triggerType}");
            MDebug.Log($"AFunc_Trigger.ExecuteCMD");
            this.m_CurrentTriggerLogic = CPool.Pop<AFunc_Trigger_Function>();
            
            this.m_CurrentTriggerLogic.Initialize(trigger, OnLogicCompleted);
            this.m_CurrentTriggerLogic.Execute();
        }
        
        private void OnLogicCompleted()
        {
            this.m_CurrentTriggerLogic?.PushToPool();
            this.m_CurrentTriggerLogic = null;
        }
    }
}