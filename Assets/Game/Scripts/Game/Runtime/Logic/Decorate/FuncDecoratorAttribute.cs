using System;

namespace MOYV.RunTime.Game.Logic
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FuncDecoratorAttribute : Attribute
    {
        public AFuncType FuncType { get; }
        public AFuncCMDOffset CmdOffset { get; }
        public int Order { get; }
        
        public FuncDecoratorAttribute(AFuncType funcType, AFuncCMDOffset cmdOffset, int order)
        {
            this.FuncType = funcType;
            this.CmdOffset = cmdOffset;
            this.Order = order;
        }
    }
}