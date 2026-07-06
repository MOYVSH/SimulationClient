---
kind: build_system
name: Unity 工程与 Luban 数据管线构建体系
category: build_system
scope:
    - '**'
source_files:
    - MiniGame1-master.slnx
    - SimulationClient.slnx
    - LubanConfig/Template/luban.conf
    - LubanConfig/Template/gen.bat
    - LubanConfig/Template/gen.sh
    - LubanConfig/Template/Datas/__tables__.xlsx
    - LubanConfig/Template/Datas/__beans__.xlsx
    - LubanConfig/Template/Datas/__enums__.xlsx
    - Assets/Game/Resources/YooAssetSettings.asset
    - Assets/Game/Resources/AssetBundleCollectorSetting.asset
---

本仓库的构建体系由两部分组成：基于 .slnx 的 Unity/C# 聚合工程，以及以 Luban 为核心的配置驱动代码生成管线。两者共同支撑模拟客户端的编译、资源打包与运行时数据加载。

1. 构建系统与工具链
- 使用 Unity 2022+（URP）作为主编辑器，通过 MiniGame1-master.slnx / SimulationClient.slnx 两个 .slnx 聚合文件统一引用所有子项目（Assembly-CSharp、QFramework、MOYVBase、Luban.Runtime、YooAsset、AstarPathfindingProject、UniTask 等），在 Visual Studio/VS Code 中实现跨项目跳转与一键编译。
- 资源打包采用 YooAsset，Assets/Game/Resources/YooAssetSettings.asset 定义默认包目录 yoo 与包清单前缀 MiniGame1；同时保留 AssetBundleCollectorSetting.asset 兼容旧 AB 收集器。
- 无 Makefile/Dockerfile/GitHub Actions 等外部 CI 脚本，本地构建依赖 dotnet + Unity 编辑器。

2. Luban 配置与代码生成管线
- 配置文件位于 LubanConfig\Template\luban.conf，声明三个 group（c/s/e 对应 client/server/enum）、schemaFiles（Defines、__tables__.xlsx、__beans__.xlsx、__enums__.xlsx）以及 targets（server/client/all）。
- 入口脚本 gen.bat（Windows）与 gen.sh（Linux/macOS）调用内置的 Luban.dll，分别输出 C# bin 序列化代码到 Assets/Game/Scripts/ConfigCode，二进制数据到 Assets/Game/MiniGame_Res/Config；gen.sh 则输出 JSON 到 output 目录。
- 模板集位于 LubanConfig\Template\Luban\Templates，覆盖 cs-bin、cs-dotnet-json、cs-newtonsoft-json、cs-simple-json、go-bin/json、java-bin/json、python-json、rust-bin/json、typescript-bin/json、flatbuffers、protobuf 等多语言后端，均基于 Scriban 模板。
- 示例数据表 __tables__.xlsx/__beans__.xlsx/__enums__.xlsx 与 builtin.xml 内置类型定义位于 Datas/ 与 Defines/，供开发者直接修改后重新生成。

3. 架构与约定
- 数据层：策划维护 Excel 表 → Luban 生成 C# 类与 TableManager → 运行时通过 cfg.Tables 访问；服务端/客户端共享同一份 schema，仅目标 group 不同。
- 资源层：YooAsset 负责包清单与增量更新，StreamingAssets/yoo 下存放 BuildinCatalog 与版本文件；Bundles/StandaloneWindows64/MiniGame1/Simulate 为模拟模式下的本地包缓存。
- 工程组织：Assets/Framework 承载框架库（QFramework/MOYV/FSM/MPool/Ugui 等），Assets/Scripts 放置业务逻辑，Assets/MiniGame_Res 放运行时资源，LubanConfig 独立于 Unity 工程树之外便于复用。

4. 开发者应遵循的规则
- 新增或修改数据表时，务必同步更新 __tables__.xlsx/__beans__.xlsx/__enums__.xlsx，并执行 gen.bat/gen.sh 重新生成 C# 代码与数据，禁止手写 ConfigCode 目录下的生成产物。
- 新增字段需先在 __beans__.xlsx 中声明 bean 结构，再在 __tables__.xlsx 的表中引用，确保 schema 一致性。
- 选择目标 group：仅客户端读取用 c，仅服务端用 s，同时需要两端时用 all；luban.conf 中的 groups/targets 可按需扩展。
- 资源打包通过 YooAsset 编辑器窗口操作，不要手动改动 StreamingAssets/yoo 下的清单文件；版本号由 YooAsset 自动生成。
- 跨平台构建时优先使用 gen.sh（Linux/macOS）并在 CI 中安装 dotnet SDK，避免 Windows 路径差异。