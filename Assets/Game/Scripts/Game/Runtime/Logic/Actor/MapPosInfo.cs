using UnityEngine;

namespace MOYV.RunTime.Game.Logic
{
    public struct MapPosInfo
    {
        public long mapId { get; private set; }
        public Vector3 pos;
        public Vector3 angle;
        
        ///<summary>值类型复制，请使用指针调用，防止赋值失效</summary>
        public MapPosInfo CopyFrom(long mapId, Vector3 pos, float angle)
        {
            this.mapId = mapId;
            this.pos = pos;
            this.angle = new Vector3(0, angle, 0);
            return this;
        }
    }
}