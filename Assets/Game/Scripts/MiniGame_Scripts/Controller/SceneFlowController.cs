using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonsterLove.StateMachine;
using UnityEngine;
using Cysharp.Threading.Tasks;
using MOYV.RunTime.Game.Logic;
using MOYV.RunTime.Game.Tool;
using QFramework;
using Runtime.System;
using UniFramework.Pooling;
using UnityEngine.SceneManagement;

public class SceneFlowController : MonoSingleton<SceneFlowController>, IController 
{
    public enum SceneStates
    {
        Idle,           // 空闲状态
        Loading,        // 场景加载中
        Loaded,         // 场景加载完毕
        Transitioning,  // 场景切换中
        Error           // 错误状态
    }
    
    public class Driver
    {
        public StateEvent Update;
        public StateEvent<string> OnSceneChanged;
        public StateEvent<float> OnLoadProgress;
        public StateEvent OnPause;
        public StateEvent OnResume;
    }
    
    private string targetSceneName;
    [SerializeField] private float loadingProgress = 0f;
    [SerializeField] private bool autoStartLoading = true;
    
    private StateMachine<SceneStates, Driver> fsm;
    private UISystem uiSystem;
    
    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
    
    // 实现 QFramework 接口成员
    public string logTag => "SceneFlowController";
    public void Log(object msg, GameObject context = null) => Debug.Log($"[{logTag}] {msg}", context);
    public void LogWarning(object msg, GameObject context = null) => Debug.LogWarning($"[{logTag}] {msg}", context);
    public void LogError(object msg, GameObject context = null) => Debug.LogError($"[{logTag}] {msg}", context);
    
    private void Awake()
    {
        fsm = new StateMachine<SceneStates, Driver>(this);
        uiSystem = this.GetSystem<UISystem>();
        fsm.ChangeState(SceneStates.Idle);
    }
    
    #region 状态机方法
    
    void Idle_Enter()
    {
        Log("场景控制器进入空闲状态");
        loadingProgress = 0f;
    }
    
    void Loading_Enter()
    {
        Log("开始加载场景...");
        //uiSystem?.OpenPanel<LoadingView>();
        if (uiSystem == null) 
            return;
        
        UniTask.Void(async () =>
        {
            await uiSystem.OpenPanelAsync<LoadingView>();
            LoadSceneCoroutine();
        });
    }
    

    void Loading_Update()
    {
        fsm.Driver.OnLoadProgress.Invoke(loadingProgress);
        uiSystem?.GetOpenedPanel<LoadingView>()?.SetProgress(loadingProgress);
    }
    
    void Loading_OnLoadProgress(float progress)
    {
        loadingProgress = progress;
    }
    
    void Loaded_Enter()
    {
        Log("场景加载完成");
        uiSystem?.GetOpenedPanel<LoadingView>()?.Complete();
        uiSystem?.ClosePanel<LoadingView>();
        fsm.ChangeState(SceneStates.Idle);
    }
    
    void Transitioning_Enter()
    {
        Log("开始场景切换");
        TransitionToNewScene().Forget();
    }
    
    void Error_Enter()
    {
        Debug.LogError("场景流程出现错误");
    }
    
    #endregion
    
    #region 异步方法

    private async UniTaskVoid LoadSceneCoroutine()
    {
        Log("加载场景异步开始");
        
        // 模拟加载延迟
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f), false);
        // 使用UniTask加载场景
        await LoadSceneAsync();
        
        DoAfterLevelLoad();
    }
    
    private async UniTask LoadSceneAsync()
    {
        try
        {
            var utility = this.GetUtility<YooassetUtility>();
            var handle = await utility.LoadSceneAsync(targetSceneName, e =>
            {
                if (e != EErrorCode.None)
                {
                    Debug.LogError($"场景加载错误: {e}");
                    fsm.ChangeState(SceneStates.Error);
                }
            }, 
                LoadSceneMode.Single,
                (s, l, arg3) =>
                {
                    
                });
    
            loadingProgress = 1f;
            fsm.ChangeState(SceneStates.Loaded);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"加载场景时发生异常: {ex.Message}");
            fsm.ChangeState(SceneStates.Error);
        }
    }

    private async UniTaskVoid TransitionToNewScene()
    {
        Log("加载场景异步开始");
        await UniTask.Delay(TimeSpan.FromSeconds(1f), false);
        fsm.ChangeState(SceneStates.Loading);
    }
    
    #endregion
    
    #region 公共方法

    public async UniTaskVoid FirstLoadMainScene()
    {
        await this.GetUtility<YooassetUtility>().InitPackage(); // 初始化资源包
        ShaderWarmUp(); // 预热着色器
        await Task.Delay(TimeSpan.FromSeconds(0.2f));
        var system = GameArchitecture.Interface.GetSystem<UISystem>();
        system.OpenPanel<LoadingView>();
        system.GetOpenedPanel<LoadingView>().SetProgress(0.1f);
        
        await LoadConfig(); // 加载配置数据
        system.GetOpenedPanel<LoadingView>().SetProgress(0.2f);
        
        await Task.Delay(TimeSpan.FromSeconds(0.2f));
        this.SendCommand<InitModelDataCmd>(); // 初始化存档数据
        system.GetOpenedPanel<LoadingView>().SetProgress(0.3f);
        
        GenerateObjectPool();
        StartLoading("GameScene");
    }

    private void ShaderWarmUp()
    {
        ShaderVariantCollection shaderVariantCollection = null;
        shaderVariantCollection =
            this.GetUtility<YooassetUtility>().LoadAssetSync<ShaderVariantCollection>("MyShaderVariants");
        
        if (shaderVariantCollection == null)
        {
            Debug.LogError($"着色器预热失败，未找到着色器变体集合");
        }
        else
        {
            shaderVariantCollection.WarmUp();
            Debug.Log($"预热了着色器变体集合中的 {shaderVariantCollection.shaderCount} 个着色器");
        }
    }

    private void GenerateObjectPool()
    {
        UniPooling.Initalize();
        // 创建孵化器
        var spawner = UniPooling.CreateSpawner(YooassetUtility.PackageName);
    }
    
    public void StartLoading(string sceneName)
    {
        targetSceneName = sceneName;
        fsm.ChangeState(SceneStates.Loading);
    }
    
    public void TransitionToScene(string newSceneName)
    {
        targetSceneName = newSceneName;
        fsm.ChangeState(SceneStates.Transitioning);
    }
    
    public SceneStates CurrentState => fsm.State;
    public bool IsInTransition => fsm.IsInTransition;
    
    #endregion 
    
    # region 项目方法

    private void DoAfterLevelLoad(bool isCurrent = true)
    {
        // 发送场景加载完成后的命令
        this.SendCommand<AfterSceneInitLogicCmd>();
        
        // 打开 UI 等操作
        
        // 测试一下Actor
        var actorSystem = this.GetSystem<ActorSystem>();
        var testData = new TriggerActorData();
        testData.uid = 19971216;
        testData.path = "BaseActor";
        testData.RefreshMapInfo(20251124, new Vector3(0, 0, 0), 45f);
        var actor = actorSystem.CreateActor(testData, (ba) =>
        {
            GameArchitecture.Interface.SendEvent<ZTestEvent>();
            ba.AddFunc(CPool.Pop<AFunc_Trigger>(),ba);

            AFuncCMD_Trigger_Exeute cmd = new AFuncCMD_Trigger_Exeute();
            cmd.Trigger = null;
            cmd.Pass = false;
            ba.ExecuteCMD<AFuncCMD_Trigger_Exeute>(AFuncType.Trigger_Exeute, cmd);
            
            var param = new AFuncCMD_Test_Param();
            param.ID = 123;
            ba.ExecuteCMD<AFuncCMD_Test_Param>(AFuncType.test, param);
            ba.ExecuteCMD(AFuncType.testE);
        });
        


        var testData2 = new TestAcrtorData();
        testData2.uid = 20240606;
        testData2.path = "BaseActor";
        testData2.RefreshMapInfo(2026111, new Vector3(2, 0, 0), 0f);
        var actor2 = actorSystem.CreateActor(testData2, (ba) =>
        {
            GameArchitecture.Interface.SendEvent<ZTestEvent>();
            GameArchitecture.Interface.SendEvent<ZTestEvent1>();
            
            var param = new AFuncCMD_Test_Param();
            param.ID = 456;
            actor.ExecuteCMD<AFuncCMD_Test_Param>(AFuncType.test, param);
        });

        //actorSystem.RemoveActor(actor);
    }

    private async UniTask LoadConfig()  
    {
        Log($"数据表加载开始，当前时间: {Time.time}");

        await this.GetUtility<LubanUtility>().Initialize();
        await this.GetUtility<YooassetUtility>().UnloadUnusedAssets();
        
        foreach (var table in this.GetUtility<LubanUtility>().Tables.TbFirst.DataList)
        {
            Log($"<color=red>加载数据表:</color> {table.Name}");
        }
        foreach (var table in this.GetUtility<LubanUtility>().Tables.TbSecond.DataList)
        {
            Log($"<color=red>加载数据表:</color> {table.Name}");
        }
        
        Log($"数据表加载完成，当前时间: {Time.time}");
    }
    
    #endregion
}