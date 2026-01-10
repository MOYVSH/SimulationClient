using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    [FuncDecorator(AFuncType.testE,AFuncCMDOffset.H,AFuncOrder.test)]
    public class AFunc_TestE : AFuncDecorate
    {
        private int id = 10086;

        public override void OnRecycle()
        {
            base.OnRecycle();
            id = 0;
        }

        protected override void OnRegisterCMD()
        {
            cmdRouter.Register(AFuncType.testE, OnTestCMD);
        }

        private void OnTestCMD()
        {
            MDebug.Log("AFunc_TestE OnTestCMD Order:"+ order);
            MDebug.Log("AFunc_TestE OnTestCMD id:"+id);
        }
    }
}