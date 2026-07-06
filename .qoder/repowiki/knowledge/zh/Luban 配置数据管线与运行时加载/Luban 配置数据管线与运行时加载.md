---
kind: configuration_system
name: Luban 配置数据管线与运行时加载
category: configuration_system
scope:
    - '**'
source_files:
    - LubanConfig/Template/luban.conf
    - LubanConfig/Template/gen.bat
    - LubanConfig/Template/gen.sh
    - Assets/Game/Scripts/ConfigCode/Tables.cs
    - Assets/Game/Scripts/MiniGame_Scripts/Utility/LubanUtility.cs
    - Assets/Game/Scripts/MiniGame_Scripts/Controller/SceneFlowController.cs
---

本仓库采用 Luban 作为配置数据管线，将 Excel 定义的数据表在构建期生成 C# 代码与二进制序列化文件，并在 Unity 运行时通过自定义 Utility 加载。整个系统分为构建期生成和运行期加载两个阶段。

### 1. 构建期：Luban 配置与模板
- 配置文件 LubanConfig/Template/luban.conf 声明了 schema 来源（Defines、Datas/__tables__.xlsx、__beans__.xlsx、__enums__.xlsx）、分组（c/s/e）以及三个 target（server/client/all），统一由 Tables 管理器暴露。
- 示例数据表位于 LubanConfig/Template/Datas/，其中 __tables__.xlsx 注册表名，__beans__.xlsx 定义行结构，__enums__.xlsx 定义枚举。
- 生成脚本 gen.bat / gen.sh 调用 dotnet Luban.dll -t all -c cs-bin --conf luban.conf -x outputDataDir=... -x outputCodeDir=...，把生成的 C# 代码输出到 Assets/Game/Scripts/ConfigCode/，把二进制数据输出到 Assets/Game/MiniGame_Res/Config/。
- 生成的核心入口是 Assets/Game/Scripts/ConfigCode/Tables.cs，它提供 cfg.Tables 类，构造函数接收一个 Func<string, ByteBuf> 用于按文件名懒加载字节流，并自动解析跨表引用。

### 2. 运行期：Unity 中的加载流程
- Assets/Game/Scripts/MiniGame_Scripts/Utility/LubanUtility.cs 实现 IUtility，在 Initialize() 中通过 YooassetUtility.LoadConfigsAsync("Assets/Game/MiniGame_Res/Config/test_tbfirst") 异步拉取所有 .bytes 配置资源，组装成 Dictionary<string, byte[]>，再传入 new cfg.Tables(file => new ByteBuf(dict[file])) 完成初始化。
- SceneFlowController.LoadConfig() 在场景启动时调用 GetUtility<LubanUtility>().Initialize()，随后即可访问 Tables.TbFirst.DataList、TbSecond.DataList 等强类型数据。
- 预加载阶段 PreloadResource.LoadConfig() 也会触发配置加载，确保后续 UI/逻辑消费时无阻塞。

### 3. 架构约定与约束
- 数据源唯一性：所有游戏数值/文案/关卡表必须放在 LubanConfig/Template/Datas/ 下并通过 __tables__.xlsx 注册，禁止硬编码路径或手写 JSON。
- 分组隔离：通过 luban.conf 的 groups（c/s/e）区分客户端/服务端/公共表，target 组合控制生成范围，避免客户端携带服务端专属数据。
- 生成产物不可手动修改：ConfigCode/ 下的文件带 auto-generated 注释，任何本地改动会在下次生成时被覆盖；应修改 Excel 后重新执行 gen.bat。
- 运行时只读：cfg.Tables 暴露的是只读集合，业务层不应尝试写入；如需变更应在编辑器侧调整数据表。
- 资源路径耦合点：LubanUtility.gameConfDir 与 gen.bat 的 -x outputDataDir 必须保持一致，否则运行时会找不到对应 .bytes 文件。

### 4. 开发者规则
- 新增数据表 -> 在 __tables__.xlsx 注册 -> 在 __beans__.xlsx 定义字段 -> 运行 gen.bat -> 通过 cfg.Tables.<Table>.DataList 读取。
- 不要直接操作 Assets/Game/MiniGame_Res/Config/*.bytes，它们由生成管线产出。
- 若需要为不同平台生成不同子集，修改 luban.conf 的 groups/targets 而非复制多份配置。
- 异常处理：当前 LubanUtility.Initialize() 仅 throw 原始异常，建议在实际工程中记录日志并回退到默认表或提示用户。