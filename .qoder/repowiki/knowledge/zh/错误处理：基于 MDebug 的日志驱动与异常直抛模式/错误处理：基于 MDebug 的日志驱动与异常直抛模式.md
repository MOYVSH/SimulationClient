---
kind: error_handling
name: 错误处理：基于 MDebug 的日志驱动与异常直抛模式
category: error_handling
scope:
    - '**'
source_files:
    - Assets/Game/Framework/MDebug/MDebug.cs
---

本仓库未建立统一的错误类型体系或结构化错误码机制，错误处理呈现“日志 + 异常直抛”的两条并行路径：

1. **运行时错误上报**：通过 `Assets/Game/Framework/MDebug/MDebug.cs` 提供的静态类 `MDebug`（命名空间 `MOYV.RunTime.Game.Tool`）统一输出。其内部定义 `LogLevel`（All/Log/Warring/Error），所有业务层以 `MDebug.Error(...)` / `Warning(...)` / `Log(...)` 调用，自动附加时间戳、帧号与完整堆栈后转发到 `UnityEngine.Debug.LogError`。这是游戏逻辑中“可恢复错误”和“诊断信息”的主要载体。

2. **不可恢复错误**：框架基础库（`BetterList`、`OrderedDictionary`、`PlistCS`、`StringTypeUtils`、各自定义集合等）在参数非法时直接抛出标准 .NET 异常（`ArgumentOutOfRangeException`、`KeyNotFoundException`、`InvalidOperationException`、`DataMisalignedException` 等），上层未做 catch 包装，依赖 Unity 崩溃日志定位。业务脚本中也存在少量 `try/catch(Exception e)` 包裹外部工具调用的场景（如 Editor 下的 AssetChecker、TextureTools、SortingLayerDrawer 等），但均仅用于记录 HelpBox 或忽略，不向上返回结构化错误对象。

3. **缺失的统一抽象**：全仓未发现任何自定义 `Error`/`ErrorCode` 枚举或 `Exception` 派生类；没有错误中间件、没有返回值式错误（Result/Either）、也没有全局 panic/recover 策略。Luban 数据管线侧同样未见 C# 端错误封装，主要依赖生成模板与命令行输出。

**开发者应遵循的规则**
- 可预期且可恢复的业务异常使用 `MDebug.Error/Warning` 上报，不要吞掉异常也不要用 `throw new Exception("...")` 代替。
- 对输入参数的校验直接抛标准 .NET 异常，保持 API 契约清晰。
- 仅在调用第三方/编辑器工具等不可控代码时使用 `try/catch(Exception)`，捕获后通过 `MDebug.Error` 记录上下文并给出降级行为。
- 避免引入新的自定义 Error 类型，除非后续需要跨模块传递结构化错误码。