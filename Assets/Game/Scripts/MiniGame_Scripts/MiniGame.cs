using Runtime.Modules;
using UnityEngine;

public class MiniGame : ArchitectureProxy<MiniGame>
{
    public override void Init()
    {
        Debug.Log("<color=green>MiniGame Architecture Init</color>");
        RegisterUtility();
        RegisterModel();
        RegisterSystem();
    }

    private void RegisterModel()
    {
        RegisterModel(new ActorModel());
        RegisterModel(new GameWorldModel());
    }

    private void RegisterSystem()
    {
        RegisterSystem(new ChunkSystem());
        RegisterSystem(new TerrainSystem());
    }

    private void RegisterUtility()
    {
        this.RegisterUtility<YooassetUtility>(new YooassetUtility());
        this.RegisterUtility<LubanUtility>(new LubanUtility());
    }
}
