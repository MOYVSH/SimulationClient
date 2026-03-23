using MOYV.RunTime.Game.Logic;
using MOYV.RunTime.Game.Tool;
using UnityEngine;

public abstract class BaseActorData : Poolable
{
    ///<summary>唯一ID</summary>
    public long uid { get; set; }
    
    ///<summary>外壳 资源路径</summary>
    public string path { get; set; }
    ///<summary>缩放</summary>
    public Vector3 scale =  Vector3.one;
    ///<summary>地图位置信息</summary>
    public MapPosInfo mapInfo;

    ///<summary>角色用途</summary>
    public ActorUsage actorUsage;
    ///<summary>角色类型</summary>
    public ActorType actorType;

    
    ///<summary>默认需要自动开启的碰撞器</summary>
    public virtual bool needCollider { get; } = true;

    ///<summary>默认需要自动创建的触发器</summary>
    public virtual bool needTrigger { get; } = true;

    
    public unsafe void RefreshMapInfo(long mapId, UnityEngine.Vector3 pos, float angle)
    {
        fixed (MapPosInfo* info = &mapInfo)
        {
            info->CopyFrom(mapId, pos, angle);
        }
    }
}

/// <summary>
/// 角色用途定义
/// </summary>
public enum ActorUsage
{
    ///<summary>默认</summary>
    Default,
    ///<summary>UI场景中</summary>
    UIScene,
    /// <summary>
    /// 平面UI中
    /// </summary>
    UI,
}

/// <summary>
/// 角色类型定义
/// </summary>
public enum ActorType
{
    ///<summary>未定义，错误类型</summary>
    None = 0,
    
    ///<summary>动态场景物件(根据条件控制显隐的场景物件)</summary>
    DynamicBuildItem = 50,
}