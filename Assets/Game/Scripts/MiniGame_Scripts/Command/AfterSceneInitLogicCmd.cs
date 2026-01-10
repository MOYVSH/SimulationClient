using QFramework;
using Runtime.System;

public class AfterSceneInitLogicCmd : AbstractCommand
{
    protected override void OnExecute()
    {
        Log("执行场景加载完成后初始化工作");
        // 初始化数据
        
        /*this.GetSystem<MapGridSystem>().AfterSceneInit();
        this.GetSystem<MapQuadTreeSystem>().AfterSceneInit();
        this.GetSystem<PathfindSystem>().AfterSceneInit();*/
        
        this.GetSystem<ActorSystem>().AfterSceneInit();
    }
}