using System;
using System.Collections.Generic;

namespace MOYV.RunTime.Game.Logic
{
    public sealed partial class FuncDecoratorStaticRegistry
    {
        /// <summary>
        /// 尝试根据父装饰类型和功能命令填充装饰器信息
        /// 填充为父->子
        /// </summary>
        /// <param name="paFunType"></param>
        /// <param name="chFuncType"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool TryPopulateSortInfo(AFuncType paFunType, AFuncType chFuncType, List<(AFuncType funcType, Type type, int offset)> list)
        {
            if (Func2Registries != null && Func2Registries.Count > 0)
            {
                list.Clear();
                foreach (var (funcType, (type, offset, _)) in Func2Registries)
                {
                    if (funcType <= paFunType || funcType > chFuncType)
                    {
                        continue;
                    }
                    else if (chFuncType >= funcType && chFuncType < (funcType + offset))
                    {
                        list.Add((funcType, type, offset));
                    }
                }
                var count = list.Count;
                if (count > 0)
                {
                    if (count > 1)
                    {
                        list.Sort((lhs, rhs) => lhs.offset.CompareTo(rhs.offset));
                    }
                    return true;
                }
            }
            return false;
        } 
        
        /// <summary>
        /// 根据装饰器类型获取注册信息
        /// </summary>
        public static bool TryGetInfo(Type decoratorType, out (AFuncType funcType, int offset, int order) info)
        {
            return Type2Registries.TryGetValue(decoratorType, out info);
        }
    }
}