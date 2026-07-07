# 输入与相机控制系统 Wiki

> **Phase 0.2 产出** | 状态：已通过测试 | 最后更新：2026-07-06

## 目录

- [1. 系统概述](#1-系统概述)
- [2. 架构设计](#2-架构设计)
- [3. 文件清单](#3-文件清单)
- [4. GameInput 详解](#4-gameinput-详解)
- [5. IsometricCameraController 详解](#5-isometriccameracontroller-详解)
- [6. 球坐标数学原理](#6-球坐标数学原理)
- [7. 场景配置指南](#7-场景配置指南)
- [8. 操作说明](#8-操作说明)
- [9. 对外接口契约](#9-对外接口契约)
- [10. 性能优化](#10-性能优化)
- [11. 扩展指南](#11-扩展指南)
- [12. 常见问题](#12-常见问题)

---

## 1. 系统概述

本系统为斜 45° 模拟经营游戏提供基础的输入采集和相机控制能力，是所有后续系统（世界生成、建造、选择等）的交互基石。

**核心特性：**

- 使用 Unity New Input System（1.17.0），代码内联定义 InputAction，无需 `.inputactions` 资源文件
- 球坐标（pitch / yaw / distance）定位相机，支持 45° 等轴视角
- WASD 平移、鼠标中键拖拽平移、滚轮缩放、Q/E 旋转
- 相机逻辑与模拟逻辑完全分离，不依赖任何模拟系统
- 针对每帧 CPU 开销做了 5 项优化（缓存输入值、缓存方向向量、脏标记跳过等）

---

## 2. 架构设计

```
┌─────────────────────────────────────────────────┐
│                  GameScene                       │
│                                                  │
│  ┌──────────────┐       ┌─────────────────────┐ │
│  │   GameInput   │──────▶│ IsometricCamera     │ │
│  │  (输入管理器)  │ 读取   │ Controller          │ │
│  │               │       │ (相机控制器)         │ │
│  │ Camera Map    │       │                     │ │
│  │  ├─ Move      │       │ Focus Point (球心)   │ │
│  │  ├─ Zoom      │       │   ↑                  │ │
│  │  └─ Rotate    │       │ Pitch / Yaw / Dist   │
│  │ Gameplay Map  │       │   ↓                  │ │
│  │  ├─ Select    │       │ transform.position   │ │
│  │  └─ Build     │       │ transform.LookAt     │ │
│  └──────────────┘       └─────────────────────┘ │
│         │                        │               │
│         │                        │               │
│    下游 Phase 4/5            下游 Phase 1/2      │
│    (放置/选择操作)          (ChunkManager 激活)   │
└─────────────────────────────────────────────────┘
```

**数据流：**

```
用户输入 → Input System 事件 → GameInput.Update() 缓存
    → IsometricCameraController.LateUpdate() 读取缓存
    → 计算 focus / yaw / distance → 更新 transform
```

**时序保证：**

- `GameInput.Update()` 先执行 → 缓存当帧输入值
- `IsometricCameraController.LateUpdate()` 后执行 → 读取已缓存的值
- 消除 1 帧输入延迟

---

## 3. 文件清单

| 文件 | 路径 | 命名空间 |
|------|------|---------|
| GameInput.cs | `Assets/Game/Scripts/Simulation/Input/` | `Simulation` |
| IsometricCameraController.cs | `Assets/Game/Scripts/MiniGame_Scripts/Controller/Camera/` | （全局） |

两个文件均属于 `Assembly-CSharp` 程序集，可直接引用项目中所有框架。

---

## 4. GameInput 详解

### 4.1 设计理念

通过代码内联创建 `InputAction` 和 `InputActionMap`，不依赖 `.inputactions` 资源文件。优势：

- 无需在编辑器中配置 Input Action Asset
- 代码自包含，绑定关系一目了然
- 版本控制友好（纯代码，无二进制/JSON 资源）

### 4.2 Action Map 结构

#### Camera Map

| Action | 类型 | 绑定 | 说明 |
|--------|------|------|------|
| `Move` | Value (Vector2) | WASD（2DVector 复合） | 相机平移方向 |
| `Zoom` | Value (float) | `<Mouse>/scroll/y` | 滚轮缩放 |
| `Rotate` | Value (float) | Q/E（1DAxis 复合） | 相机旋转 |

#### Gameplay Map

| Action | 类型 | 绑定 | 说明 |
|--------|------|------|------|
| `Select` | Button | `<Mouse>/leftButton` | 选择（供 Phase 4/5 使用） |
| `Build` | Button | `<Mouse>/rightButton` | 建造（供 Phase 4/5 使用） |

### 4.3 公开属性

| 属性 | 返回类型 | 说明 |
|------|---------|------|
| `CameraMove` | `Vector2` | WASD 输入，归一化向量（A=-1, D=+1, W=+1, S=-1） |
| `CameraZoom` | `float` | 滚轮 Y 轴值，正值=向前缩放，负值=向后拉远 |
| `CameraRotate` | `float` | Q/E 输入，-1=左旋，+1=右旋 |
| `SelectPressed` | `bool` | 鼠标左键当帧按下返回 true |
| `BuildPressed` | `bool` | 鼠标右键当帧按下返回 true |

### 4.4 输入缓存机制

```csharp
private void Update()
{
    // 每帧只调用一次 ReadValue / WasPressedThisFrame，缓存结果
    _cameraMove = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    _cameraZoom = _zoomAction?.ReadValue<float>() ?? 0f;
    _cameraRotate = _rotateAction?.ReadValue<float>() ?? 0f;
    _selectPressed = _selectAction != null && _selectAction.WasPressedThisFrame();
    _buildPressed = _buildAction != null && _buildAction.WasPressedThisFrame();
}
```

**为什么缓存？** Input System 的 `ReadValue<T>()` 内部需要遍历绑定、检查状态，单次调用开销不大但多次调用会累积。属性 getter 直接返回缓存值，将每帧 `ReadValue` 调用从 3-5 次降为 1 次。

### 4.5 生命周期

| 方法 | 时机 | 操作 |
|------|------|------|
| `Awake()` | 初始化 | 创建 InputActionMap 和所有 InputAction |
| `OnEnable()` | 组件启用 | 启用所有 ActionMap |
| `Update()` | 每帧 | 一次性读取所有输入值并缓存 |
| `OnDisable()` | 组件禁用 | 禁用所有 ActionMap |

---

## 5. IsometricCameraController 详解

### 5.1 球坐标模型

相机围绕一个 **焦点（Focus Point）** 运动，位置由三个参数决定：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Pitch` | 45° | 俯仰角（与水平面夹角），固定不变 |
| `Yaw` | 45° | 方位角（围绕 Y 轴旋转），可由 Q/E 调整 |
| `Distance` | 20 | 相机到焦点的距离，可由滚轮调整 |

```
         Camera
        / |
       /  | Distance * sin(pitch)
      /   |
     /----+---- Distance * cos(pitch)
    /     |
 Focus    |-- Distance * cos(pitch) (水平投影)
```

### 5.2 Inspector 参数

#### 视角设置

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `_pitch` | float | 45 | 俯仰角，45° 为标准等轴视角 |
| `_yaw` | float | 45 | 方位角，45° 给出经典等轴外观 |
| `_distance` | float | 20 | 初始距离 |

#### 距离限制

| 字段 | 默认 | 说明 |
|------|------|------|
| `_minDistance` | 5 | 最近缩放距离 |
| `_maxDistance` | 60 | 最远缩放距离 |

#### 移动速度

| 字段 | 默认 | 说明 |
|------|------|------|
| `_moveSpeed` | 20 | WASD 平移速度（世界单位/秒） |
| `_dragSpeed` | 0.05 | 鼠标中键拖拽速度（世界单位/像素） |

#### 缩放

| 字段 | 默认 | 说明 |
|------|------|------|
| `_zoomSensitivity` | 0.5 | 滚轮灵敏度 |
| `_zoomSmoothTime` | 0.15 | SmoothDamp 平滑时间（秒） |

#### 旋转

| 字段 | 默认 | 说明 |
|------|------|------|
| `_rotateSpeed` | 90 | 旋转速度（度/秒） |

### 5.3 核心方法

| 方法 | 可见性 | 说明 |
|------|--------|------|
| `FocusOn(Vector3)` | public | 将焦点移动到指定世界坐标 |
| `HandleKeyboardMove()` | private | WASD 平移处理 |
| `HandleMouseDragMove()` | private | 鼠标中键拖拽平移处理 |
| `HandleZoom()` | private | 滚轮缩放处理（带 SmoothDamp） |
| `HandleRotate()` | private | Q/E 旋转处理 |
| `UpdateCameraTransform()` | private | 根据球坐标计算 transform |
| `UpdateCachedDirections()` | private | 缓存 forward/right 向量 |

### 5.4 脏标记机制

```csharp
private void LateUpdate()
{
    _dirty = false;

    HandleKeyboardMove();   // 有输入时 _dirty = true
    HandleMouseDragMove();  // 有输入时 _dirty = true
    HandleZoom();           // 距离变化时 _dirty = true
    HandleRotate();         // yaw 变化时 _dirty = true

    // 只在有变化或缩放仍在平滑中时更新 transform
    if (_dirty || Mathf.Abs(_distance - _targetDistance) > 0.001f)
    {
        UpdateCameraTransform();
    }
}
```

**效果：** 无输入且缩放已稳定时，完全跳过 `transform.position` 和 `transform.LookAt`，零开销。

---

## 6. 球坐标数学原理

### 6.1 相机位置计算

```
offset.x = distance * cos(pitch) * sin(yaw)
offset.y = distance * sin(pitch)
offset.z = distance * cos(pitch) * cos(yaw)

camera.position = focus + offset
camera.LookAt(focus)
```

### 6.2 方向向量

**Forward（W 键方向 = 相机看向方向在 XZ 平面的投影）：**

```
forward = (-sin(yaw), 0, -cos(yaw))
```

**Right（D 键方向 = Cross(up, forward)）：**

```
right = (-cos(yaw), 0, sin(yaw))
```

> **注意：** Right 向量使用 `Cross(up, forward)` 计算，不是简单的 `(cos, 0, -sin)`。方向反了会导致 A/D 键反转。

### 6.3 平移方向

```
moveDir = right * input.x + forward * input.y
focus += moveDir * speed * deltaTime
```

- W → focus 沿 forward 移动 → 相机前进 → 场景向屏幕下方移动
- D → focus 沿 right 移动 → 相机右移 → 场景向屏幕左方移动

### 6.4 鼠标拖拽方向

```
focus -= right * mouseDelta.x * dragSpeed
focus -= forward * mouseDelta.y * dragSpeed
```

取反实现"抓住地面"效果：鼠标向右拖 → 场景向右移 → focus 向左移。

### 6.5 默认视角 (yaw=45°, pitch=45°)

```
offset = (dist * 0.5, dist * 0.707, dist * 0.5)
```

相机在焦点的 **东北上方**，看向西南下方。这是经典的等轴视角。

---

## 7. 场景配置指南

### 7.1 在 GameScene 中配置

1. 打开 `Assets/Game/MiniGame_Res/Scene/GameScene.unity`
2. 创建 GameObject，命名 `GameInput`
   - Add Component → 搜索 `GameInput` 并添加
3. 选中场景中的 Camera GameObject
   - Add Component → 搜索 `IsometricCameraController` 并添加
   - `[RequireComponent(typeof(Camera))]` 确保有 Camera 组件

### 7.2 组件关系

```
GameScene
├── GameInput (GameObject)
│   └── GameInput.cs
└── Main Camera (GameObject)
    ├── Camera.cs
    └── IsometricCameraController.cs
```

- `GameInput` 和 `IsometricCameraController` **不需要**挂在同一个 GameObject 上
- Controller 通过 `FindFirstObjectByType<GameInput>()` 自动查找
- `GameInput` 必须在场景中存在，否则 Controller 不工作

### 7.3 注意事项

- **不要修改 ApplicationScene.unity** — 它仅作初始化入口
- `Active Input Handling` 必须为 `Both`（Project Settings → Player）
- Camera 建议使用透视相机（Perspective），Field of View 按需调整

---

## 8. 操作说明

| 操作 | 按键 | 效果 |
|------|------|------|
| 平移（前进/后退） | W / S | 相机沿焦点前后移动 |
| 平移（左/右） | A / D | 相机沿焦点左右移动 |
| 平移（拖拽） | 鼠标中键拖拽 | "抓住地面"模式平移 |
| 缩放（拉近） | 滚轮上滚 | 相机靠近焦点 |
| 缩放（拉远） | 滚轮下滚 | 相机远离焦点 |
| 旋转（左旋） | Q | 相机围绕焦点逆时针旋转 |
| 旋转（右旋） | E | 相机围绕焦点顺时针旋转 |
| 选择 | 鼠标左键 | （供 Phase 4/5 使用，当前无效果） |
| 建造 | 鼠标右键 | （供 Phase 4/5 使用，当前无效果） |

**调试：** 在 Scene 视图中选中 Camera，可看到绿色线框球表示焦点位置。

---

## 9. 对外接口契约

### 9.1 IsometricCameraController

| 接口 | 类型 | 下游使用方 |
|------|------|-----------|
| `FocusPosition` | `Vector3` (get) | Phase 1: ChunkManager 作为激活中心 |
| `CurrentZoom` | `float` (get) | Phase 2: 根据缩放级别调整 LOD |
| `FocusOn(Vector3)` | void | Phase 1: 初始定位 / Phase 3: 跟随工人 |

### 9.2 GameInput

| 接口 | 类型 | 下游使用方 |
|------|------|-----------|
| `SelectPressed` | `bool` (get) | Phase 4: 选择树木/建筑/工人 |
| `BuildPressed` | `bool` (get) | Phase 5: 放置建筑/道路 |

---

## 10. 性能优化

### 10.1 优化清单

| # | 优化项 | 修改前 | 修改后 | 效果 |
|---|--------|--------|--------|------|
| 1 | 输入值缓存 | getter 每次调 `ReadValue`（3-5 次/帧） | `Update` 中一次性读取 | 减少 Input System 内部调用 |
| 2 | LateUpdate | `Update` 中读输入 | `LateUpdate` 确保在 `GameInput.Update` 之后 | 消除 1 帧延迟 |
| 3 | 方向向量缓存 | 每帧 4 次 `Sin/Cos` | yaw 不变时 0 次 | 减少数学运算 |
| 4 | 脏标记跳过 | 每帧执行 `UpdateCameraTransform` | 无变化时跳过 | 减少 transform 操作 |
| 5 | pitch 弧度缓存 | 每帧 `pitch * Deg2Rad` | `Start` 中缓存一次 | 微优化 |

### 10.2 Profiler 诊断

如果仍有 CPU 延迟，用 Profiler（Window → Analysis → Profiler）排查：

| 开销来源 | 可能原因 | 解决方案 |
|---------|---------|---------|
| `InputSystem.Update` | Input System 处理开销 | 减少同时启用的 Action |
| `Camera.Render` | 渲染开销 | 检查场景 Renderer 数量、Shadow |
| `EditorOverhead` | Editor Play Mode 额外开销 | Build 后运行对比 |

---

## 11. 扩展指南

### 11.1 添加新输入

以添加「暂停」快捷键（Space）为例：

```csharp
// GameInput.cs
private InputAction _pauseAction;
private bool _pausePressed;

public bool PausePressed => _pausePressed;

private void SetupGameplayMap()
{
    // ... 已有代码 ...
    _pauseAction = _gameplayMap.AddAction("Pause", InputActionType.Button, "<Keyboard>/space");
}

private void Update()
{
    // ... 已有代码 ...
    _pausePressed = _pauseAction != null && _pauseAction.WasPressedThisFrame();
}
```

### 11.2 添加屏幕边界限制

在 `HandleKeyboardMove` 和 `HandleMouseDragMove` 之后添加：

```csharp
// 在 LateUpdate 中，UpdateCameraTransform 之前
_focusPosition.x = Mathf.Clamp(_focusPosition.x, _boundsMin.x, _boundsMax.x);
_focusPosition.z = Mathf.Clamp(_focusPosition.z, _boundsMin.z, _boundsMax.z);
```

### 11.3 添加相机震动

```csharp
// 在 UpdateCameraTransform 中
Vector3 shakeOffset = Random.insideUnitSphere * _shakeAmount;
transform.position = _focusPosition + offset + shakeOffset;
```

---

## 12. 常见问题

### Q: A/D 方向反了？

检查 `UpdateCachedDirections()` 中的 `_cachedRight` 计算：

```csharp
// 正确（Cross(up, forward)）：
_cachedRight = new Vector3(-Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));

// 错误（方向相反）：
// _cachedRight = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
```

### Q: 相机不动？

检查清单：
1. 场景中是否有 `GameInput` 组件？
2. `GameInput` 组件是否启用？
3. `Active Input Handling` 是否为 `Both`？
4. Console 是否有 Input System 相关报错？

### Q: Editor 中 CPU 延迟高？

Editor Play Mode 有额外开销（Scene 渲染、Inspector 刷新等），建议 Build 后对比。用 Profiler 定位具体开销来源。

### Q: 如何修改默认视角？

在 Inspector 中调整 `Pitch`（俯仰角）和 `Yaw`（方位角）。常见组合：

| 风格 | Pitch | Yaw |
|------|-------|-----|
| 标准等轴 | 45° | 45° |
| 低角度 | 30° | 45° |
| 高角度 | 60° | 45° |
| 正面 | 45° | 0° |
