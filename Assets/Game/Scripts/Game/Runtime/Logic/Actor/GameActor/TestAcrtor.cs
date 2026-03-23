using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    public class TestAcrtor : BaseActor
    {
        EventRouterQF eventRouter = new EventRouterQF();


        protected override void OnAddOtherFuncs()
        {
            base.OnAddOtherFuncs();
            AddFunc(CPool.Pop<AFunc_Test>(), this);
        }

        protected override void OnRegisterEvent()
        {
            eventRouter.Register<ZTestEvent>(ConstEventID.ZTestEvent, OnZTestEvent);
            eventRouter.Register<ZTestEvent1>(ConstEventID.ZTestEvent1, OnZTestEvent1);
        }

        private void OnZTestEvent(ZTestEvent e)
        {
            MDebug.Error("Trigger OnZTestEvent:" + e.EventID);
            MDebug.Error(data.uid.ToString());
        }
        
        private void OnZTestEvent1(ZTestEvent1 e)
        {
            MDebug.Error("Trigger OnZTestEvent:" + e.EventID);
            MDebug.Error(data.uid.ToString());
        }
        
        public override void UpdateFunc(float deltaTime)
        {
            base.UpdateFunc(deltaTime);
            MDebug.Error(data.uid.ToString() + data.ToString());
        }
    }
}