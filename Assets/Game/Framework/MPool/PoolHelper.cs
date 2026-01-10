using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MOYV.RunTime.Game.Tool;

/// <summary>
/// 常用的对象池名称 这个是针对Actor的对象池
/// </summary>
public class PoolConst
{
    public const string emptyGPool = "EmptyGameObjectPool";
    public const string actorNoneRootGPool = "ActorRootGPool";
    public const string actorBoxRootGPool = "ActorBoxRootGPool";
    public const string actorCapsuleRootGPool = "ActorCapsuleRootGPool";
    public const string actorSphereRootGPool = "ActorSphereRootGPool";
    public const string actorCCRootGPool = "ActorCCRootGPool";
}

public static class PoolHelper
{
    static public string GetActorRootPoolName(ColliderType pType)
    {
        return pType switch
        {
            ColliderType.Box => PoolConst.actorBoxRootGPool,
            ColliderType.Capsule => PoolConst.actorCapsuleRootGPool,
            ColliderType.Sphere => PoolConst.actorSphereRootGPool,
            ColliderType.CharacterController => PoolConst.actorCCRootGPool,
            _ => PoolConst.actorNoneRootGPool,
        };
    }

    static public GameObject GetOneActorRootGameObject(ColliderType pType)
    {
        var key = GetActorRootPoolName(pType);
        if (!CPool.HasGameObjPool(key))
        {
            GameObject go = new GameObject(key);
            CPool.CreateGameObjectPool(key, string.Empty, go, true);
        }

        return CPool.PopG(key);
    }

    static public GameObject GetOneEmptyGameObject()
    {
        if (!CPool.HasGameObjPool(PoolConst.emptyGPool))
        {
            GameObject go = new GameObject("EmptyGameObject");
            CPool.CreateGameObjectPool(PoolConst.emptyGPool, string.Empty, go, true);
        }

        return CPool.PopG(PoolConst.emptyGPool);
    }

    static public void PushEmptyGameObject(GameObject pObject)
    {
        if (!CPool.HasGameObjPool(PoolConst.emptyGPool))
        {
            GameObject go = new GameObject("EmptyGameObject");
            CPool.CreateGameObjectPool(PoolConst.emptyGPool, string.Empty, go, true);
        }

        CPool.Push(PoolConst.emptyGPool, pObject);
    }
}