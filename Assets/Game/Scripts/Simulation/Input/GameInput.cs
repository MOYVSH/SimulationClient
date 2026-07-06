using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulation
{
    /// <summary>
    /// 模拟经营游戏统一输入管理器。
    /// 使用 New Input System，通过代码内联定义 InputAction，
    /// 不依赖 .inputactions 资源文件。
    /// Action Maps：Camera（相机控制）、Gameplay（游戏交互）。
    /// </summary>
    public class GameInput : MonoBehaviour
    {
        // ---- Camera Action Map ----
        private InputActionMap _cameraMap;
        private InputAction _moveAction;
        private InputAction _zoomAction;
        private InputAction _rotateAction;

        // ---- Gameplay Action Map ----
        private InputActionMap _gameplayMap;
        private InputAction _selectAction;
        private InputAction _buildAction;

        // ---- 每帧缓存（Update 中一次性读取，避免 getter 多次调用 ReadValue）----
        private Vector2 _cameraMove;
        private float _cameraZoom;
        private float _cameraRotate;
        private bool _selectPressed;
        private bool _buildPressed;

        /// <summary>相机平移输入（WASD），归一化 Vector2。</summary>
        public Vector2 CameraMove => _cameraMove;

        /// <summary>相机缩放输入（滚轮 Y 轴），正值向前缩放、负值向后拉远。</summary>
        public float CameraZoom => _cameraZoom;

        /// <summary>相机旋转输入（Q/E），-1 左旋、+1 右旋。</summary>
        public float CameraRotate => _cameraRotate;

        /// <summary>选择按钮（鼠标左键），当帧按下返回 true。</summary>
        public bool SelectPressed => _selectPressed;

        /// <summary>建造按钮（鼠标右键），当帧按下返回 true。</summary>
        public bool BuildPressed => _buildPressed;

        private void Awake()
        {
            SetupCameraMap();
            SetupGameplayMap();
        }

        private void SetupCameraMap()
        {
            _cameraMap = new InputActionMap("Camera");

            // Move: WASD（2DVector 复合绑定）
            _moveAction = _cameraMap.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            // Zoom: 鼠标滚轮 Y 轴
            _zoomAction = _cameraMap.AddAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");

            // Rotate: Q/E（1DAxis 复合绑定）
            _rotateAction = _cameraMap.AddAction("Rotate", InputActionType.Value);
            _rotateAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");
        }

        private void SetupGameplayMap()
        {
            _gameplayMap = new InputActionMap("Gameplay");

            // Select: 鼠标左键
            _selectAction = _gameplayMap.AddAction("Select", InputActionType.Button, "<Mouse>/leftButton");

            // Build: 鼠标右键
            _buildAction = _gameplayMap.AddAction("Build", InputActionType.Button, "<Mouse>/rightButton");
        }

        private void Update()
        {
            // 每帧只调用一次 ReadValue / WasPressedThisFrame，缓存结果
            _cameraMove = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            _cameraZoom = _zoomAction?.ReadValue<float>() ?? 0f;
            _cameraRotate = _rotateAction?.ReadValue<float>() ?? 0f;
            _selectPressed = _selectAction != null && _selectAction.WasPressedThisFrame();
            _buildPressed = _buildAction != null && _buildAction.WasPressedThisFrame();
        }

        private void OnEnable()
        {
            _cameraMap?.Enable();
            _gameplayMap?.Enable();
        }

        private void OnDisable()
        {
            _cameraMap?.Disable();
            _gameplayMap?.Disable();
        }
    }
}
