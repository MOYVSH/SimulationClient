using QFramework;
using UnityEngine;
using UnityEngine.InputSystem;
using Simulation;

/// <summary>
/// 斜 45° 等轴视角相机控制器。
/// 使用球坐标（pitch / yaw / distance）围绕 focus point 定位相机，
/// 支持 WASD 平移、鼠标中键拖拽平移、滚轮缩放、Q/E 旋转。
/// 相机逻辑与模拟逻辑完全分离，不依赖任何模拟系统。
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsometricCameraController : AbstractMonoBehaviourController
{
    public override IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }

    [Header("视角设置")]
    [Tooltip("俯仰角（与水平面夹角），固定 45° 为标准等轴视角")]
    [SerializeField] private float _pitch = 45f;

    [Tooltip("方位角（围绕 Y 轴旋转），默认 45°")]
    [SerializeField] private float _yaw = 45f;

    [Tooltip("相机与焦点的距离")]
    [SerializeField] private float _distance = 20f;

    [Header("距离限制")]
    [SerializeField] private float _minDistance = 5f;
    [SerializeField] private float _maxDistance = 60f;

    [Header("移动速度")]
    [Tooltip("WASD 平移速度（世界单位/秒）")]
    [SerializeField] private float _moveSpeed = 20f;

    [Tooltip("鼠标中键拖拽平移速度（世界单位/像素）")]
    [SerializeField] private float _dragSpeed = 0.05f;

    [Header("缩放")]
    [Tooltip("滚轮缩放灵敏度")]
    [SerializeField] private float _zoomSensitivity = 0.5f;

    [Tooltip("缩放平滑时间（秒）")]
    [SerializeField] private float _zoomSmoothTime = 0.15f;

    [Header("旋转")]
    [Tooltip("旋转速度（度/秒）")]
    [SerializeField] private float _rotateSpeed = 90f;

    // ---- 运行时状态 ----
    private Vector3 _focusPosition = Vector3.zero;
    private float _targetDistance;
    private float _zoomVelocity;
    private GameInput _gameInput;

    // ---- 缓存（减少每帧 Sin/Cos 计算）----
    private float _lastYaw = float.MinValue;
    private Vector3 _cachedForward;
    private Vector3 _cachedRight;
    private float _cachedPitchRad;
    private bool _dirty = true;

    /// <summary>当前相机焦点世界坐标。供 ChunkManager 作为激活中心。</summary>
    public Vector3 FocusPosition => _focusPosition;

    /// <summary>当前缩放距离。值越小越近。</summary>
    public float CurrentZoom => _distance;

    /// <summary>将相机焦点移动到指定世界坐标。</summary>
    public void FocusOn(Vector3 worldPos)
    {
        _focusPosition = worldPos;
    }

    private void Awake()
    {
        _targetDistance = _distance;
    }

    private void Start()
    {
        _gameInput = FindFirstObjectByType<GameInput>();
        _cachedPitchRad = _pitch * Mathf.Deg2Rad;
        UpdateCachedDirections();
        UpdateCameraTransform();
    }

    private void LateUpdate()
    {
        if (_gameInput == null)
        {
            _gameInput = FindFirstObjectByType<GameInput>();
            if (_gameInput == null) return;
        }

        _dirty = false;

        HandleKeyboardMove();
        HandleMouseDragMove();
        HandleZoom();
        HandleRotate();

        // 只在有变化或缩放仍在平滑中时更新 transform
        if (_dirty || Mathf.Abs(_distance - _targetDistance) > 0.001f)
        {
            UpdateCameraTransform();
        }
    }

    /// <summary>WASD 键盘平移。</summary>
    private void HandleKeyboardMove()
    {
        Vector2 moveInput = _gameInput.CameraMove;
        if (moveInput.sqrMagnitude < 0.001f) return;

        UpdateCachedDirections();
        Vector3 moveDir = _cachedRight * moveInput.x + _cachedForward * moveInput.y;
        _focusPosition += moveDir * _moveSpeed * Time.deltaTime;
        _dirty = true;
    }

    /// <summary>鼠标中键拖拽平移（"抓住地面"模式）。</summary>
    private void HandleMouseDragMove()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.middleButton.isPressed) return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < 0.001f) return;

        UpdateCachedDirections();

        // 拖拽方向取反：鼠标向右拖 → 场景向右移动 → focus 向左移动
        _focusPosition -= _cachedRight * mouseDelta.x * _dragSpeed;
        _focusPosition -= _cachedForward * mouseDelta.y * _dragSpeed;
        _dirty = true;
    }

    /// <summary>滚轮缩放，带 SmoothDamp 平滑。</summary>
    private void HandleZoom()
    {
        float zoomInput = _gameInput.CameraZoom;
        if (Mathf.Abs(zoomInput) > 0.001f)
        {
            _targetDistance -= zoomInput * _zoomSensitivity;
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
        }

        float newDistance = Mathf.SmoothDamp(_distance, _targetDistance, ref _zoomVelocity, _zoomSmoothTime);
        if (Mathf.Abs(newDistance - _distance) > 0.0001f)
        {
            _distance = newDistance;
            _dirty = true;
        }
    }

    /// <summary>Q/E 键旋转方位角。</summary>
    private void HandleRotate()
    {
        float rotateInput = _gameInput.CameraRotate;
        if (Mathf.Abs(rotateInput) < 0.001f) return;

        _yaw += rotateInput * _rotateSpeed * Time.deltaTime;
        _yaw = Mathf.Repeat(_yaw, 360f);
        _dirty = true;
    }

    /// <summary>根据 pitch / yaw / distance 计算相机位置并 LookAt 焦点。</summary>
    private void UpdateCameraTransform()
    {
        UpdateCachedDirections();

        float cosPitch = Mathf.Cos(_cachedPitchRad);
        float sinPitch = Mathf.Sin(_cachedPitchRad);

        Vector3 offset = new Vector3(
            _distance * cosPitch * _cachedForward.x * -1f,  // -sin(yaw)
            _distance * sinPitch,
            _distance * cosPitch * _cachedForward.z * -1f   // -cos(yaw)
        );

        transform.position = _focusPosition + offset;
        transform.LookAt(_focusPosition);
    }

    /// <summary>缓存方向向量，只在 yaw 变化时重新计算。</summary>
    private void UpdateCachedDirections()
    {
        if (_yaw == _lastYaw) return;

        float yawRad = _yaw * Mathf.Deg2Rad;
        _cachedForward = new Vector3(-Mathf.Sin(yawRad), 0f, -Mathf.Cos(yawRad));
        _cachedRight = new Vector3(-Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));
        _lastYaw = _yaw;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_focusPosition, 0.5f);
    }
}
