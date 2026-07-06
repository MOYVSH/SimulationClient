---
title: "Phase 0：项目基础"
phase: "0"
owner: "海豹"
status: "planned"
dependencies: []
keywords: ["Unity", "项目初始化", "相机", "输入系统", "New Input System", "等轴视角"]
created: "2026-07-06"
last_modified: "2026-07-06"
---

# Phase 0：项目基础

## 负责人

海豹

## 目标

在已有项目基础上，建立模拟经营系统的目录规范、输入系统和斜 45° 相机，为后续所有系统提供可运行的基础。

## 依赖

无。本 Phase 为模拟经营系统的起点（项目已有基础框架和 `ApplicationScene.unity`）。

## 任务列表

| # | 任务 | 关键产出 | 验收标准 |
|---|------|----------|----------|
| 0.1 | 模拟经营目录初始化 | `Assets/Game/Scripts/MiniGame_Scripts/` 目录结构、`MiniGame_Res/Prefabs/`、`ScriptableObjects/` | 目录结构符合约定，项目无报错 |
| 0.2 | 斜 45° 相机与输入 | `IsometricCameraController.cs`、`GameInput.cs` | 可在场景中平移/缩放/旋转观察平地 |

## 0.1 模拟经营目录初始化

### 产出文件

- `Assets/Game/Scripts/Simulation/` 目录结构（见 `00-overview.md` 全局目录）
- `Assets/Game/MiniGame_Res/Prefabs/Trees/`、`Workers/`、`Buildings/`、`UI/`
- `Assets/Game/MiniGame_Res/ScriptableObjects/TreeTypes/`、`BuildingTypes/`、`RoadTypes/`

### 实现细节

1. 项目已使用 Unity 6 (6000.3.2f1)，渲染管线为 URP
2. 已启用 New Input System
3. 模拟经营代码放在 `Assets/Game/Scripts/Simulation/` 下，属于 `Assembly-CSharp` 程序集
4. 命名约定：
   - 类名：PascalCase
   - 私有序列化字段：`_camelCase`
   - 接口：`I` 前缀
   - 常量/枚举：PascalCase

### 验收标准

- 项目打开无编译错误
- 目录结构与 `00-overview.md` 一致
- 不影响已有的 MiniGame / Framework 代码

## 0.2 斜 45° 相机与输入

### 产出文件

- `Assets/Game/Scripts/Simulation/Camera/IsometricCameraController.cs`
- `Assets/Game/Scripts/Simulation/Input/GameInput.cs`

> **注意**：项目已有 `Assets/Game/Scripts/MiniGame_Scripts/Controller/Camera/` 中的相机控制器，供小游戏使用。模拟经营需要独立的等轴视角相机，不修改已有代码。

### 实现细节

1. **相机**：
   - 固定 45° 等轴视角
   - 支持鼠标拖拽平移
   - 支持滚轮缩放
   - 支持中键/右键旋转（可选）
   - 相机逻辑与模拟逻辑完全分离
   - 在 `ApplicationScene.unity` 中配置

2. **输入**：
   - 使用 New Input System
   - Action Maps：`Camera`、`Gameplay`
   - Actions：
     - `Move`（Vector2，相机平移）
     - `Zoom`（float，缩放）
     - `Rotate`（float，旋转）
     - `Select`（Button，选择）
     - `Build`（Button，建造）

### 对外接口

```csharp
public class IsometricCameraController : MonoBehaviour
{
    public Vector3 FocusPosition { get; }
    public float CurrentZoom { get; }
    public void FocusOn(Vector3 worldPos);
}

public class GameInput : MonoBehaviour
{
    public Vector2 CameraMove { get; }
    public float CameraZoom { get; }
    public float CameraRotate { get; }
    public bool SelectPressed { get; }
    public bool BuildPressed { get; }
}
```

### 验收标准

- Play Mode 中可用 WASD/中键拖拽平移
- 滚轮缩放平滑
- 相机不依赖任何模拟系统即可工作
- 输入系统无异常报错
- 不影响已有 `ApplicationScene.unity` 中的其他功能

## 提供给下游 Phase 的契约

- `ApplicationScene.unity` 为后续所有系统测试的基准场景
- `IsometricCameraController.FocusPosition` 将供 `ChunkManager` 作为激活中心
- `GameInput` 为 Phase 4/5 的放置/选择操作提供输入事件

## 阻塞 downstream 的风险

- 如果 New Input System 未正确配置，后续所有交互功能都无法测试
- 相机焦点获取不正确会导致 Chunk 激活窗口偏移