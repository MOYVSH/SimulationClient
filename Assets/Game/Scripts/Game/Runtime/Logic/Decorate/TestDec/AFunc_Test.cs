using MOYV.RunTime.Game.Tool;

namespace MOYV.RunTime.Game.Logic
{
    [FuncDecorator(AFuncType.test,AFuncCMDOffset.T,AFuncOrder.test)]
    public class AFunc_Test : AFuncDecorate
    {
        private int id = 0;

        public override void OnRecycle()
        {
            base.OnRecycle();
            id = 0;
        }

        protected override void OnRegisterCMD()
        {
            cmdRouter.Register<AFuncCMD_Test_Param>(AFuncType.test, OnTestCMD);
        }

        private void OnTestCMD(AFuncCMD_Test_Param param)
        {
            id = param.ID;
            MDebug.Log("AFunc_Test OnTestCMD id:"+id);
        }
    }
}