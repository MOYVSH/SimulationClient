---
kind: dependency_management
name: Unity 项目依赖管理：本地源码 + .slnx 聚合 + Luban 二进制分发
category: dependency_management
scope:
    - '**'
source_files:
    - MiniGame1-master.slnx
    - SimulationClient.slnx
    - Assembly-CSharp.csproj
    - LubanConfig/Template/Luban/Luban.deps.json
---

## 1. 使用的系统/方法

本仓库采用 **Unity 原生工程模型** 作为依赖管理的核心，配合以下机制：

- **Unity Package Manager (UPM) 的 Library/PackageCache**：所有 Unity 官方包（URP、Entities、InputSystem、TextMeshPro 等）以及第三方 UPM 包均通过 `Library/PackageCache` 缓存，由 Unity 编辑器在打开工程时自动解析与下载。根目录 `Packages/` 为空，说明未使用自定义 registry 或 git URL 声明。
- **本地源码直接纳入工程**：QFramework、MOYVBase、MOYVCollections、MOYVDoTween、UniTask、AstarPathfindingProject、YooAsset、MonsterLove.StateMachine、Drawing、andywiecko.BurstTriangulator 等均以 `.csproj` 形式存在于仓库根目录，被 Unity 自动生成并编译为 DLL，属于“源码级依赖”。
- **.slnx 聚合解决方案**：`MiniGame1-master.slnx` 与 `SimulationClient.slnx` 两个 VS Solution 文件集中列出所有参与构建的 `.csproj`，用于 IDE 侧导航与跨项目引用，不改变 Unity 自身的依赖解析流程。
- **Luban 数据管线以二进制程序集分发**：`LubanConfig/Template/Luban/` 下直接包含 `Luban.dll`、`Luban.CSharp.dll`、`Scriban.dll`、`Newtonsoft.Json.dll` 等已编译好的 .NET 程序集及其 `*.deps.json`，由 `gen.bat` / `gen.sh` 脚本驱动执行，不走 NuGet。

## 2. 关键文件与位置

| 类别 | 路径 | 作用 |
|---|---|---|
| 聚合方案 | `MiniGame1-master.slnx`、`SimulationClient.slnx` | 列举所有 csproj，统一 IDE 视图 |
| 主工程 | `Assembly-CSharp.csproj` | Unity 自动生成的主程序集，禁用 NuGet (`ResolveNuGetPackages=false`)，引用大量 `Library/PackageCache` 下的 Analyzer |
| 框架源码 | `Assets/Game/Framework/*` 下的各 `MOYV*.csproj`、`QFramework.csproj`、`UniTask.csproj` 等 | 本地源码形式的依赖，随工程一起编译 |
| 第三方插件 | `Assets/Samples/A_ Pathfinding Project/`、`Assets/Samples/QFramework/` | 以 Samples 方式引入的第三方源码 |
| Luban 运行时 | `LubanConfig/Template/Luban/*.dll` + `*.deps.json` | 代码生成器二进制及依赖清单 |
| 资源打包 | `Assets/Resources/YooAssetSettings.asset`、`StreamingAssets/yoo/` | YooAsset 配置与离线包清单 |

## 3. 架构与约定

- **分层策略**：引擎层（Unity 内置 + UPM 包）→ 基础框架层（QFramework/MOYV/UniTask 等源码）→ 业务逻辑层（`Assets/Game/Scripts`）。上层仅通过 C# 引用下层，不出现循环依赖。
- **版本锁定**：所有 UPM 包的版本号固化在 `Library/PackageCache/com.unity.*@<hash>` 中，无全局 lockfile；升级需通过 Unity Editor → Package Manager 操作后重新生成。
- **IDE 同步**：`.slnx` 与 Unity 生成的 `.csproj` 一一对应，新增框架需同时加入 slnx 才能被 Rider/VS 识别。
- **Luban 独立于游戏运行期**：Luban 仅在开发机执行，不参与 Player 构建；其依赖全部以预编译 DLL 形式随模板提交，避免 CI 环境安装 .NET SDK。

## 4. 开发者应遵循的规则

1. **新增第三方库优先走 UPM**：通过 Package Manager 安装，不要手动把 DLL 丢进 `Assets/Plugins`，否则无法被 UPM 统一管理。
2. **框架类库以源码形式组织**：新建子框架应在根目录创建独立的 `<Name>.csproj`，并在两个 `.slnx` 文件中注册。
3. **禁止在业务代码中直接 `#r` 或硬编码 DLL 路径**：所有引用必须通过 Unity 的 Assembly Reference 或 UPM 解析。
4. **Luban 模板更新需同步提交 `LubanConfig/Template/Luban/` 下的所有 `*.dll` 与 `*.deps.json`**，确保不同机器上行为一致。
5. **升级 UPM 包后检查 `Assembly-CSharp.csproj` 中的 Analyzer 引用是否被 Unity 正确重写**，必要时清理 `Library/` 后重新导入工程。

## 5. 置信度

**medium** — 仓库确实存在清晰的依赖分层与聚合方案，但缺少显式的 `packages.lock.json`、`nuget.config` 或私有 registry 配置，版本锁定完全依赖 Unity 内部缓存，可复现性较弱。