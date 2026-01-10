using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MOYV.RunTime.Game.Tool;
using QFramework;
using Runtime.System;
using UnityEngine;
using YooAsset;

namespace MOYV.RunTime.Game.Logic
{
    public abstract class BaseActor : AFuncDecorate
    {
        public BaseActorData data;
        public GameObject gameObject { get; private set; }
        public Transform transform { get; private set; }

        public GameObject model { get; protected set; }

        private AssetHandle modelHandle;
        ///<summary>碰撞体</summary>
        public Collider collider;

        public GameObject trigger { get; protected set; }
        private ColliderType colType;
        private ColliderType trigType;

        protected string assetFileName;
        public bool assetLoadFinish { get; protected set; }

        #region locomotion

        /*public virtual bool isGrounded => characterCtrl?.isGrounded ?? true;
        public CharacterController characterCtrl { get; private set; }*/

        #endregion

        public bool isVisible { get; private set; } = true;

        private static StringBuilder tempSB = new StringBuilder(200);

        protected ActorSystem actorSystem;
        
        public BaseActor OnInit(BaseActorData data, Action<BaseActor> callback)
        {
            actorSystem = GameArchitecture.Interface.GetSystem<ActorSystem>();
            this.data = data;
            
            actorSystem.AddActor(this);
            
            OnRegisterEvent();
            OnCreateBaseGo();
            OnLoadRes(callback);
            return this;
        }

        public override void OnRecycle()
        {
            
            if (m_WaitExeCMDList != null)
            {
                ListPool<IWaitCMD>.Release(m_WaitExeCMDList);
                m_WaitExeCMDList = null;
            }
            
            OnUnRegisterEvent();
            RemoveAllFunc();
            RecycleModel();

            if (collider != null)
            {
                
            }
            collider = null;
            
            assetLoadFinish = false;

            gameObject?.PushToPool(PoolHelper.GetActorRootPoolName(colType));
            gameObject = null;
            
            trigger?.PushToPool(PoolHelper.GetActorRootPoolName(trigType));
            trigger = null;
            transform = null;
            
            data = null;
            isVisible = true;
            
            base.OnRecycle();
        }

        #region 事件
        ///<summary>注册事件</summary>
        protected virtual void OnRegisterEvent()
        {
            //CEvent.Register<DontDisplayType, bool, long>(EventIdConst.onActorScreenStateChanged, OnScreenStateChangedEvent);
        }

        ///<summary>注销事件</summary>
        protected virtual void OnUnRegisterEvent()
        {
            //CEvent.UnRegister<DontDisplayType, bool, long>(EventIdConst.onActorScreenStateChanged, OnScreenStateChangedEvent);
        }

        #endregion
        
        ///<summary>创建外壳实体</summary>
        /// 将“外壳创建”和“模型加载”解耦，先有可参与逻辑/碰撞的根节点，再异步加载模型。
        /// 使用配置优先的方式确定碰撞类型；无配置时采用默认与自动推断。
        /// 通过对象池减少频繁实例化开销。
        protected virtual void OnCreateBaseGo()
        {
            string assetPath = null;
            // 通过data的数据创建模型路径 用于碰撞体创建
            tempSB.Clear();
            if (!data.path.IsNullOrEmpty())
            {
                tempSB.Append(AssetPathConst.prefab_actor);
                tempSB.Append(data.path);
                tempSB.Append(".prefab");
                assetPath = tempSB.ToString();
            }

            assetFileName = Path.GetFileNameWithoutExtension(assetPath);

            colType = ColliderType.Box; //如果不走配置, 根据模型大小会默认添加box, 所以这里默认为box
            trigType = ColliderType.Sphere;

            if (!assetFileName.IsNullOrEmpty())
            {
                // 从配置中读取碰撞体类型
                var colType = ColliderType.Box;
            }
            
            gameObject = PoolHelper.GetOneActorRootGameObject(colType);
            transform = gameObject.transform;
            var trans = gameObject.transform;
            trans.name = data.uid.ToString();
            trans.localScale = data.scale;
            trans.position = data.mapInfo.pos;
            trans.localEulerAngles = data.mapInfo.angle;
            
            if (data.actorUsage == ActorUsage.Default)
            {
                InitCollider();
            }
        }
        
        ///<summary>加载其他必要资源</summary>
        protected virtual void OnLoadRes(Action<BaseActor> callback)
        {
            string assetPath = AssetPathConst.prefab_actor + "/Cube";
            string assetName = "Cube";
            
            // 通过data的数据创建模型路径 专门用来加载模型
            if (assetPath.IsNullOrEmpty() || assetName.IsNullOrEmpty()) // 临时的
            {
                callback?.Invoke(this);
                return;
            }

            _OnLoadRes(assetPath, assetName, callback);
        }

        /// <summary>加载完成的函数</summary>
        protected void _OnLoadRes(string assetPath, string assetName, Action<BaseActor> callback)
        {
            // 通过YooAssets 异步加载资源
            if (YooAssets.CheckLocationValid(assetPath))
            {
                int flag = useFlagId;
                if (modelHandle != null)
                {
                    modelHandle.Release();
                    modelHandle = null;
                }

                // 加载完成后 
                modelHandle = YooAssets.LoadAssetAsync<GameObject>(assetPath);
                modelHandle.Completed += handle =>
                {
                    if (handle.IsValid)
                    {
                        if (IsNullOrChanged(this, flag)) // 避免在加载过程中对象被回收或替换
                        {
                            handle.Release();
                            actorSystem.RemoveActor(this);
                            callback?.Invoke(null);
                            return;
                        }
                        else
                        {
                            DestroyModel(); // 回收旧模型
                            model = GameObject.Instantiate(handle.AssetObject) as GameObject;
                            model.name = assetName;
                            transform.AddChild(model.transform, true);
                            OnModelLoad();
                        }
                    }
                    else
                    {
                        MDebug.Error("资源加载失败：" + assetPath);
                    }
                    
                    OnSetLayer(gameObject);

                    assetLoadFinish = true;
                    assetFileName = assetName;
                    OnResLoaded(gameObject);
                    callback?.Invoke(this);
                };
            }
            else
            {
                MDebug.Error($"资源:{assetPath} 未找到，请相关负责人检查资源是否正确提交或者使用!");
                actorSystem.RemoveActor(this);
                callback?.Invoke(null);
            }
        }
        
        private void DestroyModel()
        {
            if (model != null)
            {
                GameObject.Destroy(model);
                model = null;
            }
        }
        
        private void RecycleModel()
        {
            if (model != null)
            {
                GameObject.Destroy(model);
                model = null;
            }
            modelHandle?.Release();
            modelHandle = null;
        }
        
        /// <summary>
        /// 资源加载完成时调用，无论是否加载成功
        /// </summary>
        /// <param name="pObject">如果加载失败则为null</param>
        protected virtual void OnResLoaded(GameObject pObject) { assetLoadFinish = true; }

        protected virtual void OnModelLoad()
        {
            //>添加功能（在加载过程中的指令要如何处理？？::TODO）
            AddFuncs();

            //>添加功能结束
            OnAddFuncOver();
        }

        #region Update

        public virtual void OnUpdate(float delta)
        {
            UpdateFunc(delta);
        }

        public void OnFixedUpdate(float delta)
        {
            FixedUpdateFunc(delta);
        }

        #endregion
        
        #region layer
        //protected virtual byte actorDefLayer { get { return LayerDefine.defaultBit; } }
        ///<summary>设置层级</summary>
        /// 想法是用于处理碰撞和渲染的分层 目前还没做相关的设计 所以暂时搁置
        public virtual void OnSetLayer(GameObject go)
        {
            /*if (go == null) return;
            go.SetLayer(Layer);*/
        }
        
        #endregion
        
        #region 碰撞盒
        private bool ColliderIsSameType(Transform root, ColliderType cType)
        {
            if (root.TryGetComponent<Collider>(out var collider))
            {
                switch (cType)
                {
                    case ColliderType.Box:
                        return collider is BoxCollider;
                    case ColliderType.Capsule:
                        return collider is CapsuleCollider;
                    case ColliderType.Sphere:
                        return collider is SphereCollider;
                    case ColliderType.CharacterController:
                        return collider is CharacterController;
                }
            }
            return true;
        }
        
        protected virtual void InitCollider()
        {
            if (!string.IsNullOrEmpty(assetFileName))
            {
                // 根据配置创建碰撞体
                CreateColliderByCfg();
                trigger.name = data.uid.ToString();
                return;
            }
            
            if (assetLoadFinish) // 这地方是给先加载模型后要根据模型尺寸调整的Actor准备的先保留
                CreateColliderByModelSize(true, data.needTrigger, true);
        }

        private void CreateColliderByCfg()
        {
            Transform colRoot = transform;

            // 根据配置 设置碰撞体类型
            colType = ColliderType.Box; // 暂时默认用一下啊BOX
            
            // 创建碰撞体
            if (colType != ColliderType.None)
            {
                var col = CreateCollider(colRoot, colType, default, false);
                if (col != null)
                {
                    col.enabled = data.needCollider;
                    collider = col;
                }
            }
            else
            {
                var col = colRoot.GetComponent<Collider>();
                if (col != null)
                {
                    UnityEngine.Object.Destroy(col);
                }
            }

            var trigType = ColliderType.Box; //(ColliderType)cfg.TriggerType; // 暂时默认用一下啊Box
            CreateTriggerRoot(trigType);
            if (trigType == ColliderType.None) //默认所有对象都有触发器
            {
                CreateColliderByModelSize(false, data.needTrigger, false);
            }
            else
            {
                this.trigType = trigType;
                // TODO: 读取配置数据 引入一个Luban
                CreateCollider(trigger.transform, trigType, default, true);
            }
            var trig = trigger.GetComponent<Collider>();
            if (trig != null) trig.enabled = data.needTrigger;
        }

        private void CreateTriggerRoot(ColliderType colType)
        {
            if (trigger != null)
                return;
            
            trigger = PoolHelper.GetOneActorRootGameObject(colType);
            transform.AddChild(trigger.transform, true);
            trigger.layer = transform.gameObject.layer;
            trigger.name = data.uid.ToString();
            trigger.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        
        private Collider CreateCollider(Transform root, ColliderType cType, float[][] param, bool isTrigger)
        {
            if (!ColliderIsSameType(root, cType))
            {
                UnityEngine.Object.Destroy(root.GetComponent<Collider>());
            }

            Collider col = null;
            switch (cType)
            {
                case ColliderType.Box:
                    col = AddBoxCollider(root, param, isTrigger);
                    break;
                case ColliderType.Capsule:
                    col = AddCapsuleCollider(root, param, isTrigger);
                    break;
                case ColliderType.Sphere:
                    col = AddSphereCollider(root, param, isTrigger);
                    break;
            }

            return col;
        }

        private Collider AddBoxCollider(Transform root, float[][] param, bool isTrigger)
        {
            var box = root.AddMissingComponent<BoxCollider>();
            if (param != default)
            {
                box.center = new Vector3(param[0][0], param[0][1], param[0][2]);
                box.size = new Vector3(param[1][0], param[1][1], param[1][2]);  
            }
            box.isTrigger = isTrigger;
            return box;
        }

        private Collider AddCapsuleCollider(Transform root, float[][] param, bool isTrigger)
        {
            var cap = root.AddMissingComponent<CapsuleCollider>();
            cap.center = new Vector3(param[0][0], param[0][1], param[0][2]);
            cap.radius = param[1][0];
            cap.height = param[2][0];
            cap.isTrigger = isTrigger;
            return cap;
        }

        private Collider AddSphereCollider(Transform root, float[][] param, bool isTrigger)
        {
            var sph = root.AddMissingComponent<SphereCollider>();
            sph.center = new Vector3(param[0][0], param[0][1], param[0][2]);
            sph.radius = param[1][0];
            sph.isTrigger = isTrigger;
            return sph;
        }

        private void CreateColliderByModelSize(bool needCol, bool needTri, bool checkCfg)
        {
            // TODO: 根据模型尺寸创建碰撞体 
            // 目前没太想好怎么做这个感觉全都按照配置做会不会好一点
        }

        #endregion
        
        #region 功能装饰部分
        
        protected override void OnRegisterCMD() { }
        
        private List<IWaitCMD> m_WaitExeCMDList;
        protected List<IWaitCMD> waitExeCMDList => m_WaitExeCMDList ??= ListPool<IWaitCMD>.Get();

        private void AddFuncs()
        {
            // 添加默认的功能模块
            
            OnAddOtherFuncs();
        }
        
        protected virtual void OnAddOtherFuncs() { }

        protected virtual void OnAddFuncOver()
        {
            base.OnInitFuncOver();
            
            if (m_WaitExeCMDList != null)
            {
                if (m_WaitExeCMDList.Count > 0)
                {
                    foreach (var waiter in m_WaitExeCMDList)
                    {
                        if (waiter != null)
                        {
                            waiter.Execute(this);
                        }
                    }
                }
                ListPool<IWaitCMD>.Release(m_WaitExeCMDList);
                m_WaitExeCMDList = null;
            }
        }

        /// <summary>
        /// actor存在对应功能的装饰器？
        /// </summary>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        public bool ExistFunc(AFuncType cmdType) => GetFuncDecorate(cmdType) != null;
        
        /// <summary>
        /// 执行无参无返回值装饰器功能
        /// </summary>
        /// <param name="cmdType"></param>
        public void ExecuteCMD(AFuncType cmdType)
        {
            if (initOver)
            {
                base.Execute(cmdType);
            }
            else
            {
                waitExeCMDList.Add(new WaitCMDNoParam(cmdType));
                MDebug.Warning($"对象:{data.uid} 类型:{data.actorType} 执行指令:{cmdType} 失败，因为装饰器还没有初始化完成!");
            }
        }

                /// <summary>
        /// 执行无参有返回值装饰器功能
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="cmdType"></param>
        /// <returns></returns>
        public R ExecuteCMD<R>(AFuncType cmdType) where R : IAFuncCMDResult
        {
            if (initOver)
            {
                return base.Execute<R>(cmdType);
            }
            else
            {
                waitExeCMDList.Add(new WaitCMDNoParam(cmdType));
                MDebug.Warning($"对象:{data.uid} 类型:{data.actorType}    执行指令:{cmdType} 失败，因为装饰器还没有初始化完成!");
                return default;
            }
        }
        /// <summary>
        /// 执行带参无返回值装饰器功能
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdType"></param>
        /// <param name="param"></param>
        public void ExecuteCMD<T>(AFuncType cmdType, T param) where T : IAFuncCMDParam
        {
            if (initOver)
            {
                base.Execute(cmdType, param);
            }
            else
            {
                waitExeCMDList.Add(new WaitCMD<T>(cmdType, param));
                MDebug.Warning($"对象:{data.uid} 类型:{data.actorType}    执行指令:{cmdType} 失败，因为装饰器还没有初始化完成!");
            }
        }
        /// <summary>
        /// 执行带参有返回值装饰器功能
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="cmdType"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public R ExecuteCMD<T, R>(AFuncType cmdType, T param) where T : IAFuncCMDParam where R : IAFuncCMDResult
        {
            if (initOver)
            {
                return base.Execute<R, T>(cmdType, param);
            }
            else
            {
                waitExeCMDList.Add(new WaitCMD<T>(cmdType, param));
                MDebug.Warning($"对象:{data.uid} 类型:{data.actorType}    执行指令:{cmdType} 失败，因为装饰器还没有初始化完成!");
                return default;
            }
        }
        
        #endregion
        
        internal void SetRenderVisible(bool visible, bool force = false)
        {
            if (isVisible == visible && !force) return;
            isVisible = visible;
            var renderers = model?.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length != 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = visible;
                }
            }
        }
    }
    

}