---
kind: frontend_style
name: Unity UGUI 原生 UI 风格体系（无 CSS/主题系统）
category: frontend_style
scope:
    - '**'
source_files:
    - Assets/Game/Framework/Ugui/Runtime/SafeAreaUGUI.cs
    - Assets/Game/Framework/Ugui/Runtime/RoundCornerRawImage.cs
    - Assets/Game/Framework/Ugui/Runtime/AdvancedImage.cs
    - Assets/Game/Framework/Ugui/Runtime/WrapLayout/
    - Assets/TextMesh Pro/Resources/TMP Settings.asset
---

本仓库为 Unity 模拟客户端工程，前端 UI 完全基于 Unity UGUI 与 TextMeshPro 构建，**不存在任何 CSS、SCSS、Tailwind 或 Web 样式系统**。UI 视觉风格通过以下机制统一：

1. **UGUI 组件 + C# 扩展**：`Assets/Game/Framework/Ugui/Runtime/` 下提供 `AdvancedImage`、`RoundCornerRawImage`、`SafeAreaUGUI`、`WrapLayout`、`ToggleImage` 等自定义组件，封装圆角、安全区适配、自动换行布局等通用视觉行为。
2. **TextMeshPro 资源驱动**：字体、材质、Sprite 资产与默认 Style Sheet 均位于 `Assets/TextMesh Pro/Resources/`，通过 TMP Settings 的 `m_defaultStyleSheet` 字段全局生效。
3. **Prefab 内联样式**：所有 UI 外观（颜色、字号、间距、阴影等）直接配置在 Prefab 的 RectTransform、Image、TextMeshProUGUI 等组件属性中，未见集中式主题文件或设计令牌。
4. **无响应式策略**：未检测到屏幕尺寸检测、自适应缩放或平台差异化样式逻辑；安全区由 `SafeAreaUGUI.cs` 单独处理。

结论：该仓库没有跨模块的前端样式框架或主题系统，UI 风格是分散在 Prefab 与少量 C# 扩展中的 UGUI 原生实践。