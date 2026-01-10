using QFramework;
using Runtime.System;

public class ClearSystemDataAfterChangeLevelCmd : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetSystem<ActorSystem>().ClearDataAfterChangeLevel();
    }
}