using QFramework;
using Runtime.System;
using UnityEngine;

public class AfterSceneInitLogicCmd : AbstractCommand
{
    protected override void OnExecute()
    {
        Log("执行场景加载完成后初始化工作");
        // 初始化数据
        
        /*this.GetSystem<MapGridSystem>().AfterSceneInit();
        this.GetSystem<MapQuadTreeSystem>().AfterSceneInit();
        this.GetSystem<PathfindSystem>().AfterSceneInit();*/
        
        var _terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (_terrain == null)
        {
            Debug.LogError("未找到 Terrain");
        }
        else
        {
            this.GetModel<GameWorldModel>().InitWorldData(_terrain);
        }
        

        this.GetSystem<ActorSystem>().AfterSceneInit();
        this.GetSystem<ChunkSystem>().AfterSceneInit();
    }
}