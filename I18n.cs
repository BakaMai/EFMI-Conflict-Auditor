using System;
using System.Globalization;

namespace EfmiConflictAuditor
{
    public enum Lang
    {
        ZH,
        EN
    }

    public static class I18n
    {
        public static Lang Current { get; set; } =
            CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? Lang.ZH
                : Lang.EN;

        public static bool IsZh => Current == Lang.ZH;

        public static string T(string zh, string en) => IsZh ? zh : en;

        // Window & Header
        public static string AppTitle => T(
            "EFMI MOD 冲突检测工具 v1.0.2",
            "EFMI Mods Conflict Auditor v1.0.2");

        public static string LangToggleText => IsZh ? "🌐 Language: 中文" : "🌐 Language: English";

        // Target Bar
        public static string TargetLabel => T("EFMI目录:", "EFMI Path:");
        public static string BrowseBtn => T("📁 浏览...", "📁 Browse...");
        public static string OpenFolderBtn => T("📂 打开目录", "📂 Open Folder");

        // Options & Actions Bar
        public static string ScanNowBtn => T("🔍 开始扫描", "🔍 Scan Now");
        public static string IncludeDisabledLabel => T(
            "扫描已禁用的 Mod (DISABLED_ 前缀)",
            "Scan disabled Mods (DISABLED_ prefix)");
        public static string IncludeDisabledTooltip => T(
            "默认不勾选（仅扫描启用中的 Mod）。勾选后将同时扫描 DISABLED_ 开头的文件夹/INI 并标记为 [已禁用]，且始终跳过所有备份文件。",
            "Unchecked by default (scans active Mods only). When checked, also scans DISABLED_ folders/INIs and marks them [DISABLED], while always skipping backup files.");

        public static string AutoSaveReportLabel => T(
            "自动生成 TXT 报告",
            "Auto-save TXT Report");
        public static string ExportReportBtn => T("📄 导出报告", "📄 Export Report");

        // Section Headers
        public static string WarningSectionHeader => T("⚠️ 风险预警", "⚠️ Risk Warnings");
        public static string CacheSectionHeader => T("⚡ 缓存检测与清理", "⚡ ShaderCache Manager");
        public static string ConflictSectionHeader => T("🔥 冲突检测", "🔥 Conflict Detection");

        // Warning Card 1: ShaderFixes Status Indicator
        public static string SfIdleTitle => T(
            "🛠️ ShaderFixes 目录状态：等待扫描",
            "🛠️ ShaderFixes Status: Waiting for Scan");
        public static string SfIdleSub => T(
            "扫描时检测 ShaderFixes 是否存在全局着色器文件",
            "Checks whether any files exist inside ShaderFixes");

        public static string SfSafeTitle => T(
            "✔ ShaderFixes 状态：空 (未使用)",
            "✔ ShaderFixes Status: Empty (Clean)");
        public static string SfSafeSub => T(
            "文件夹内无文件，不会造成全局着色器干扰",
            "No files present — safe from global overrides");

        public static string SfWarnTitle(int count) => IsZh
            ? $"⚠️ ShaderFixes：检测到 {count} 个文件"
            : $"⚠️ ShaderFixes: {count} Files Detected";
        public static string SfWarnSub => T(
            "全局着色器补丁生效中，与部分 MOD 存在冲突风险",
            "Global shader files active in ShaderFixes");

        // Cache Panel Sub-Card 1: ShaderCache Switch (d3dx.ini + RabbitFX ShaderCacheSettings.ini)
        public static string CacheSwitchIdleTitle => T(
            "⚙️ 着色器缓存：等待检测",
            "⚙️ Shader Cache: Pending");
        public static string CacheSwitchIdleSub => T(
            "请选择包含 d3dx.ini 的 EFMI 根目录",
            "Select EFMI root folder containing d3dx.ini");

        public static string CacheSwitchOnTitle => T(
            "✔ 着色器缓存：已全局开启",
            "✔ Shader Cache: Globally Enabled");
        public static string CacheSwitchOnSub => T(
            "d3dx.ini: 开启 (1) · RabbitFX 缓存配置: 未启用",
            "d3dx.ini: ON (1) · RabbitFX Cache INI: Off");

        public static string CacheSwitchRabbitOnlyTitle => T(
            "⚡ 着色器缓存：由 RabbitFX 开启",
            "⚡ Shader Cache: ON (via RabbitFX)");
        public static string CacheSwitchRabbitOnlySub => T(
            "d3dx.ini 未设，由 RabbitFX 配置全局生效",
            "d3dx.ini unset; enabled globally by RabbitFX");

        public static string CacheSwitchDupOnTitle => T(
            "⚠️ 着色器缓存：重复开启",
            "⚠️ Shader Cache: Duplicate ON");
        public static string CacheSwitchDupOnSub => T(
            "d3dx.ini 与 RabbitFX 同时启用 (切换可自动规范)",
            "Active in both d3dx.ini & RabbitFX (toggle to fix)");

        public static string CacheSwitchConflictTitle => T(
            "⚠️ 着色器缓存：配置冲突 (未生效)",
            "⚠️ Shader Cache: Config Conflict");
        public static string CacheSwitchConflictSub => T(
            "d3dx.ini 关闭(0)，但 RabbitFX 仍开启了缓存配置",
            "d3dx.ini is OFF(0), while RabbitFX INI is ON");

        public static string CacheSwitchOffTitle => T(
            "⏸ 着色器缓存：未开启",
            "⏸ Shader Cache: Disabled");
        public static string CacheSwitchOffSub(D3dxCacheSettingState d3dxState) => d3dxState == D3dxCacheSettingState.Disabled
            ? T("d3dx.ini: 关闭 (0) · RabbitFX 缓存配置: 未启用", "d3dx.ini: OFF (0) · RabbitFX Cache INI: Off")
            : T("d3dx.ini: 未开启 · RabbitFX 缓存配置: 未启用", "d3dx.ini: Unset · RabbitFX Cache INI: Off");

        public static string EnableCacheBtn => T("⚡ 一键开启", "⚡ Enable");
        public static string DisableCacheBtn => T("⏸ 一键关闭", "⏸ Disable");

        // Cache Panel Sub-Card 2: ShaderCache Directory & One-Click Cleanup
        public static string CacheFilesIdleTitle => T(
            "📦 ShaderCache：等待检测",
            "📦 ShaderCache: Pending");
        public static string CacheFilesIdleSub => T(
            "自动统计 ShaderCache 缓存文件数量与体积",
            "Checks cached shader files inside ShaderCache");

        public static string CacheFilesEmptyTitle => T(
            "✔ ShaderCache：空 (0 个)",
            "✔ ShaderCache: Empty (0)");
        public static string CacheFilesEmptySub => T(
            "当前无残留着色器缓存文件",
            "No cached shader files remaining");

        public static string CacheFilesPresentTitle(int count, string sizeText) => IsZh
            ? $"📦 ShaderCache：{count} 个 ({sizeText})"
            : $"📦 ShaderCache: {count} ({sizeText})";
        public static string CacheFilesPresentSub => T(
            "贴图残留错乱时，请先清理再按 F10 刷新",
            "Clear before F10 reload if textures glitch");

        public static string ClearCacheBtn => T("🧹 一键清理", "🧹 Clear");
        public static string OpenCacheDirTooltip => T(
            "在资源管理器中打开 ShaderCache 文件夹",
            "Open ShaderCache folder in File Explorer");

        // Conflict Detection Row 1: Mod Type Filter Cards
        public static string TypeAllTitle => T("全部类型", "All Mod Types");
        public static string TypeMergedTitle => T("🦴 统一骨骼", "🦴 MergedSkeleton");
        public static string TypeCrossIBTitle => T("🔗 CrossIB", "🔗 CrossIB");
        public static string TypeSimpleTitle => T("📦 简易 MOD", "📦 Simple Mod");
        public static string TypeShaderPatchTitle => T("✨ 前置插件 (FX Plugin)", "✨ FX Plugin");

        public static string ModTypeBadge(ModArchType type) => type switch
        {
            ModArchType.MergedSkeleton => T("🦴 统一骨骼", "🦴 MergedSkeleton"),
            ModArchType.CrossIB => T("🔗 CrossIB", "🔗 CrossIB"),
            ModArchType.ShaderPatch => T("✨ 前置插件", "✨ FX Plugin"),
            _ => T("📦 简易MOD", "📦 Simple")
        };

        // Conflict Detection Row 2: Category Filter Cards (A -> B -> C -> D)
        public static string CardATitle => T("A. ShaderOverride 警告", "A. ShaderOverride Warnings");
        public static string CardBTitle => T("B. ShaderRegex 警告", "B. ShaderRegex Warnings");
        public static string CardCTitle => T("C. 正则匹配器重复", "C. Duplicate ShaderRegex");
        public static string CardDTitle => T("D. 同角色重复开启检测", "D. Duplicate Active Character");

        // Search & List
        public static string SearchPlaceholder => T(
            "🔎 搜索 Mod 名称、Shader/IB Hash、filter_index 或文件名...",
            "🔎 Search Mod name, Shader/IB Hash, filter_index, or file name...");
        public static string ClearSearchBtn => T("清空", "Clear");
        public static string ListHeaderTitle => T("检测结果列表", "Detected Issues List");
        public static string EmptyListGood => T(
            "🎉 当前筛选条件下未发现任何冲突或风险！",
            "🎉 No conflicts or risks found under the current filter!");
        public static string EmptyListPrompt => T(
            "点击上方「🔍 开始扫描」按钮分析目标目录。",
            "Click '🔍 Scan Now' above to analyze the target directory.");

        // Badges
        public static string DisabledBadge => T("已禁用", "DISABLED");

        // Detail Pane
        public static string DetailHeaderTitle => T("冲突详情与代码检查器", "Conflict Details & Code Inspector");
        public static string SelectItemPrompt => T(
            "👈 请在左侧列表中点击任意一项查看详细冲突原因、涉及的 INI 文件及代码片段。",
            "👈 Select any item on the left to inspect its conflict reason, involved INI files, and code preview.");
        public static string InvolvedFilesTitle => T("📄 涉及的 INI 文件与快捷操作", "📄 Involved INI Files & Quick Actions");
        public static string CodePreviewTitle => T("💻 冲突节代码预览 (INI Sections)", "💻 Conflict Section Code Preview");
        public static string OpenIniBtn => T("📝 打开 INI", "📝 Open INI");
        public static string RevealFileBtn => T("📂 定位文件", "📂 Reveal in Explorer");

        // Status Bar
        public static string StatusBarSummary(int totalMods, int activeMods, int disabledMods, int iniCount, int soCount, int srCount, int issueCount)
        {
            return IsZh
                ? $"已扫描 Mod: {totalMods} 个 (启用: {activeMods}, 禁用: {disabledMods})  |  INI 文件: {iniCount}  |  ShaderOverride: {soCount}  |  ShaderRegex: {srCount}  |  发现条目: {issueCount}"
                : $"Scanned Mods: {totalMods} (Active: {activeMods}, Disabled: {disabledMods})  |  INI Files: {iniCount}  |  ShaderOverride: {soCount}  |  ShaderRegex: {srCount}  |  Findings: {issueCount}";
        }
    }
}
