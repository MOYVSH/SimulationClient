---
kind: logging_system
name: 日志系统 — MDebug 与 QFramework AbstractLogger 双轨输出
category: logging_system
scope:
    - '**'
source_files:
    - Assets/Game/Framework/MDebug/MDebug.cs
    - Assets/Game/Framework/Qframework/Runtime/QFramework.cs
    - Assets/Game/Framework/Base/RuntimeDebug.cs
---

本仓库采用“业务层统一门面 + 框架层轻量基类”的双轨日志方案，底层全部委托给 Unity 的 `UnityEngine.Debug`，未引入第三方日志库。两套 API 并存、职责清晰：

1. 业务/工具层门面：MOYV.MDebug（Assets/Game/Framework/MDebug/MDebug.cs）
   - 提供静态方法 `Log / Warning / Error`，统一在消息前拼接 `[HH:mm:ss:fff]` 时间戳，并可选择附加完整堆栈。
   - 通过全局枚举 `LogLevel { All, Log, Warring, Error }` 控制输出级别，由 `MDebug.logLv` 和 `MDebug.showStackTrace` 两个静态开关集中管理。
   - 所有调用最终转发到 `Debug.Log / Debug.LogWarning / Debug.LogError`，属于轻量封装而非独立 sink。
   - 该文件还附带编辑器下基于 LineRenderer 的可视化调试辅助方法，与日志无关但同属调试工具集。

2. 架构层基类：QFramework.AbstractLogger（Assets/Game/Framework/Qframework/Runtime/QFramework.cs 中定义）
   - 为 `AbstractSystem / AbstractModel / AbstractCommand / AbstractQuery<T>` 等架构组件提供 `Log / LogWarning / LogError` 虚方法。
   - 默认实现以类名作为 `logTag`，格式化为 `[ClassName] msg` 后输出到 `Debug.Log*`；子类可重写 `logTag` 自定义标签。
   - 该抽象类仅做最小包装，不实现级别过滤或异步落盘。

3. 运行时调试入口：RuntimeDebug（Assets/Game/Framework/Base/RuntimeDebug.cs）
   - 是一个可在运行时按屏幕区域点击触发的调试开关组件，内部使用 `Debug.Log / Debug.LogWarning` 打印状态切换信息，不属于结构化日志子系统，而是开发期交互调试工具。

4. 实际使用模式
   - 业务代码普遍直接调用 `MDebug.Error / MDebug.Warning / MDebug.Log`，如 Actor、装饰器、事件触发处均可见。
   - 框架内部分散位置仍直接使用 `Debug.Log / Debug.LogError`（例如 IOC Container、AnimationComposer 等），说明尚未完全迁移到统一门面。

5. 关键约定与约束
   - 无跨进程/磁盘持久化 sink，所有日志仅输出至 Unity Console。
   - 没有 JSON/结构化字段规范，日志内容为拼接字符串，便于人类阅读但不利于机器解析。
   - 级别控制集中在 `MDebug.logLv`，建议在应用启动时根据平台/构建配置设置一次，避免频繁动态调整。
   - 若需区分模块来源，优先使用 QFramework 架构组件并通过重写 `logTag`；纯脚本建议用 `MDebug` 并在消息中包含上下文标识。

6. 待改进点（供参考）
   - 将全仓 `Debug.*` 调用逐步收敛到 `MDebug` 或注入式 logger，以便未来接入 sink 替换。
   - 增加结构化字段（如场景名、帧号、对象 ID）以便后期分析。
   - 考虑将 `showStackTrace` 改为按级别/模块粒度控制，减少生产环境开销。