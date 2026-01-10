using MOYV.RunTime.Game.Core;
using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    public class Trigger : BaseActor
    {
        EventRouter1 eventRouter = new EventRouter1();


        protected override void OnAddOtherFuncs()
        {
            base.OnAddOtherFuncs();
            AddFunc(CPool.Pop<AFunc_Test>(), this);
            AddFunc(CPool.Pop<AFunc_TestE>(), this);
        }

        protected override void OnRegisterEvent()
        {
            eventRouter.Register<ZTestEvent>(ConstEventID.ZTestEvent, OnZTestEvent);
        }

        private void OnZTestEvent(ZTestEvent e)
        {
            MDebug.Error("Trigger OnZTestEvent:" + e.EventID);
            MDebug.Error(data.uid.ToString());
        }
    }
}