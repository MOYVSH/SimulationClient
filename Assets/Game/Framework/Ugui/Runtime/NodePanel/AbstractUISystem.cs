using System;
using System.Collections.Generic;
using System.Linq;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;

namespace MOYV
{
    public abstract class AbstractUISystem : AbstractSystem
    {
        // 界面资源加载路径集合        
        private readonly Dictionary<Type, string> _panelLoadPathDic = new();
        
        // 缓存的面板
        private readonly Dictionary<Type, AbstractBasePanel> _cachedPanelsDic = new();
        
        // 显示的Panel
        private readonly List<AbstractBasePanel> _openedPanelsList = new();

        private readonly Queue<Type> _loadingPanelQueue = new();
        private bool _isLoadingPanel = false;
        
        // 排序顺序的增量
        private int SortingOrderAddition = 100;
        
        // 界面根节点
        public Transform UIRoot { get; protected set; }

        // UI摄像机
        public Camera UICamera { get; protected set; }
        
        /// <summary>
        /// 设置UI根节点/摄像机（如果有）
        /// </summary>
        protected abstract void SetupEnvironment();
        
        /// <summary>
        /// 注册资源加载路径
        /// </summary>
        protected abstract void RegisterPanelsLoadPath();
        
        /// <summary>
        /// 加载资源的方法
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected abstract Object LoadAsset(string path);

        protected abstract UniTask<Object> LoadAssetAsync(string path);
        #region 打开UI
        /// <summary>
        /// 显示面板
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T OpenPanel<T>(Action onOpenCallback = null) where T : AbstractBasePanel
        {
            return OpenPanel<T>(null, onOpenCallback);
        }
        
        /// <summary>
        /// 显示面板
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T OpenPanel<T>(BasePanelData data, Action onOpenCallback = null) where T : AbstractBasePanel
        {
            var key = typeof(T);
            var panelComponent = _openedPanelsList.FirstOrDefault(panel => panel.GetType() == key);
            if (panelComponent != null)
            {
                panelComponent.InitWithPanelData(data);
                panelComponent.SetSortingOrder(GetNewPanelSortingOrder(panelComponent));
                PauseOtherPanels(panelComponent);
                return panelComponent as T;
            }

            if (_cachedPanelsDic.TryGetValue(key, out panelComponent))
            {
                panelComponent.InitWithPanelData(data);
                panelComponent.SetSortingOrder(GetNewPanelSortingOrder(panelComponent));
                panelComponent.Open(onOpenCallback);
                PauseOtherPanels(panelComponent);
                _cachedPanelsDic.Remove(key);
                return panelComponent as T;
            }

            if (!_panelLoadPathDic.TryGetValue(key, out var path))
            {
                LogError($"没有注册 {key.Name} 的资源路径");
                return null;
            }
            var prefab = LoadAsset(path);
            if (!prefab)
            {
                LogError($"未能加载 {key.Name} 的资源");
                return null;
            }
            var go = Object.Instantiate(prefab, UIRoot) as GameObject;
            if (go == null) return null;
            go.name = key.Name;
            panelComponent = go.GetComponent<T>();
            if (panelComponent == null)
            {
                LogError($"{key.Name} 没有继承 BasePanel");
                return null;
            }
            panelComponent.InitWithPanelData(data);
            panelComponent.SetSortingOrder(GetNewPanelSortingOrder(panelComponent));
            panelComponent.Open(onOpenCallback);
            return (T) panelComponent;
        }
        

        public async UniTask<T> OpenPanelAsync<T>(BasePanelData data = null, Action onOpenCallback = null) where T : AbstractBasePanel
        {
            var panelType = typeof(T);
            
            var key = typeof(T);
            var panelComponent = _openedPanelsList.FirstOrDefault(panel => panel.GetType() == key);
            if (panelComponent != null)
            {
                panelComponent.InitWithPanelData(data);
                panelComponent.SetSortingOrder(GetNewPanelSortingOrder(panelComponent));
                PauseOtherPanels(panelComponent);
                return panelComponent as T;
            }

            if (_cachedPanelsDic.TryGetValue(key, out panelComponent))
            {
                panelComponent.InitWithPanelData(data);
                panelComponent.SetSortingOrder(GetNewPanelSortingOrder(panelComponent));
                panelComponent.Open(onOpenCallback);
                PauseOtherPanels(panelComponent);
                _cachedPanelsDic.Remove(key);
                return panelComponent as T;
            }
            
            var go = await EnqueuePanelAsync(panelType, data, onOpenCallback);
            if (go == null) return null;
            var panel = go.GetComponent<T>();
            return panel;
        }
        
        private async UniTask<GameObject> EnqueuePanelAsync(Type panelType, BasePanelData data, Action onOpenCallback)
        {
            if (_isLoadingPanel)
            {
                _loadingPanelQueue.Enqueue(panelType);
                return null;
            }
            _isLoadingPanel = true;

            int sortingOrder = GetNewPanelSortingOrder(null);

            if (!_panelLoadPathDic.TryGetValue(panelType, out var path))
            {
                LogError($"没有注册 {panelType.Name} 的资源路径");
                _isLoadingPanel = false;
                return null;
            }

            var prefab = await LoadAssetAsync(path);
            var go = Object.Instantiate(prefab, UIRoot) as GameObject;
            if (go == null)
            {
                LogError($"未能实例化 {panelType.Name}");
                _isLoadingPanel = false;
                return null;
            }
            go.name = panelType.Name;
            var panel = go.GetComponent(panelType) as AbstractBasePanel;
            if (panel == null)
            {
                LogError($"{panelType.Name} 没有继承 BasePanel");
                _isLoadingPanel = false;
                return null;
            }
            panel.SetSortingOrder(sortingOrder);
            panel.InitWithPanelData(data);
            panel.Open(onOpenCallback);

            _isLoadingPanel = false;
            if (_loadingPanelQueue.Count > 0)
            {
                var nextPanelType = _loadingPanelQueue.Dequeue();
                await EnqueuePanelAsync(nextPanelType, null, null);
            }
            return go;
        }
        
        #endregion

        #region 关闭UI
        /// <summary>
        /// 关闭面板
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ClosePanel<T>(Action onCloseCallback = null) where T : AbstractBasePanel
        {
            var panelType = typeof(T);
            var panel = _openedPanelsList.FirstOrDefault(panel => panel.GetType() == panelType);
            if (panel == null) return;
            if (panel.IsHideOnClose())
            {
                CachePanel(panel);
            }
            panel.Close(onCloseCallback);
            ResumeLastActivePanel();
        }

        public void ClosePanel(AbstractBasePanel panel, Action onCloseCallback = null)
        {
            if (panel == null) return;
            var panelType = panel.GetType();
            var target = _openedPanelsList.FirstOrDefault(p => p.GetType() == panelType);
            if (target == null) return;
            if (target.IsHideOnClose())
            {
                CachePanel(target);
            }
            target.Close(onCloseCallback);
            ResumeLastActivePanel();
        }


        /// <summary>
        /// 关闭所有面板
        /// </summary>
        public void CloseAllPanels()
        {
            for (var i = _openedPanelsList.Count - 1; i >= 0; i--)
            {
                var panel = _openedPanelsList[i];
                if (panel.IsHideOnClose())
                {
                    CachePanel(panel);
                }
                panel.Close();
            }
            Log("关闭所有面板");
        }
        

        #endregion


        public T GetOpenedPanel<T>() where T : AbstractBasePanel
        {
            var key = typeof(T);
            var panelComponent = _openedPanelsList.FirstOrDefault(panel => panel.GetType() == key);
            return panelComponent as T;
        }

        private void CachePanel(AbstractBasePanel panel)
        {
            _cachedPanelsDic[panel.GetType()] = panel;
        }

        /// <summary>
        /// 获得新面板的排序
        /// </summary>
        /// <returns></returns>
        private int GetNewPanelSortingOrder(AbstractBasePanel panel)
        {
            var panelWithMaxSortingOrder = _openedPanelsList.OrderByDescending(item => item.GetSortingOrder()).FirstOrDefault();
            if (panelWithMaxSortingOrder == null) return 0;
            if (panelWithMaxSortingOrder == panel)
            {
                return panel.GetSortingOrder();
            }
            return panelWithMaxSortingOrder.GetSortingOrder() + SortingOrderAddition;
        }

        /// <summary>
        /// 暂停其他面板
        /// </summary>
        /// <param name="panel"></param>
        private void PauseOtherPanels(AbstractBasePanel panel)
        {
            foreach (var p in _openedPanelsList)
            {
                if (p != panel)
                {
                    p.OnPause();
                }
            }
        }

        private void AddOpenedPanel(AbstractBasePanel panel)
        {
            if (!_openedPanelsList.Contains(panel))
            {
                _openedPanelsList.Add(panel);
            }
        }

        private void RemoveOpenedPanel(AbstractBasePanel panel)
        {
            _openedPanelsList.Remove(panel);
        }

        private void ResumeLastActivePanel()
        {
            var panel = GetLastActivePanel();
            if (panel == null) return;
            panel.OnResume();
            PauseOtherPanels(panel);
        }

        private AbstractBasePanel GetLastActivePanel()
        {
            return _openedPanelsList.Count != 0 ? _openedPanelsList[^1] : null;
        }

        protected override void OnInit()
        {
            SetupEnvironment();
            RegisterPanelsLoadPath();
            this.RegisterEvent<PanelShowEvent>(OnPanelShowEvent);
            this.RegisterEvent<PanelCloseEvent>(OnPanelCloseEvent);
        }

        private void OnPanelShowEvent(PanelShowEvent evt)
        {
            AddOpenedPanel(evt.Panel);
        }
        
        private void OnPanelCloseEvent(PanelCloseEvent evt)
        {
            RemoveOpenedPanel(evt.Panel);
        }

        /// <summary>
        ///  注册面板加载资源路径
        /// </summary>
        /// <param name="path"></param>
        /// <typeparam name="T"></typeparam>
        protected void AddPanelLoadPath<T>(string path)
        {
            _panelLoadPathDic.Add(typeof(T), path);
        }
    }
}