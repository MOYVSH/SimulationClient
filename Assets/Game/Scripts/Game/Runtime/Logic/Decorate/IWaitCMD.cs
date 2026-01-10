namespace MOYV.RunTime.Game.Logic
{
    // 用于调用等待指令的接口
    public interface IWaitCMD
    {
        void Execute(AFuncDecorate decorator);
    }
    
    public struct WaitCMDNoParam : IWaitCMD
    {
        private readonly AFuncType type;
        public WaitCMDNoParam(AFuncType type) { this.type = type; }
        public void Execute(AFuncDecorate decorator)
        {
            decorator.Execute(type);
        }
    }

    public struct WaitCMD<T> : IWaitCMD where T : IAFuncCMDParam
    {
        private readonly AFuncType type;
        private readonly T param;

        public WaitCMD(AFuncType type, T param)
        {
            this.type = type;
            this.param = param;
        }

        public void Execute(AFuncDecorate decorator)
        {
            decorator.Execute(type, param);
        }
    }
}