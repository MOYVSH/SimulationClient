namespace MOYV.RunTime.Game.Logic
{
    /// <summary>
    /// 功能指令偏移量
    /// </summary>
    public enum AFuncCMDOffset
    {
        Zero = 0,
        W = 10000,    //万
        K = 1000,     //千
        H = 100,      //百
        T = 10,       //十
        Y = 1,        //一
    }
    
    public enum AFuncType : int
    {
        NONE = 0,
        test = 1,
        testE = 2,
    }
}