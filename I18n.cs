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
            "EFMI MOD 冲突检测工具 v1.0",
            "EFMI Mods Conflict Auditor v1.0");

        public static string LangToggleText => IsZh ? "🌐 Language: 中文" : "🌐 Language: English";

        // Target Bar
        public static string TargetLabel => T("扫描目录:", "Target Path:");
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
        public static string ConflictSectionHeader => T("🔥 冲突检测", "🔥 Conflict Detection");

        // Warning Card 1: ShaderFixes Status Indicator
        public static string SfIdleTitle => T(
            "🛠️ ShaderFixes 目录状态：等待扫描",
            "🛠️ ShaderFixes Status: Waiting for Scan");
        public static string SfIdleSub => T(
            "扫描时将自动检测 ShaderFixes 文件夹内是否存在全局着色器文件",
            "Checks whether any files exist inside the ShaderFixes folder");

        public static string SfSafeTitle => T(
            "✔ ShaderFixes 状态：空 (未使用)",
            "✔ ShaderFixes Status: Empty (Clean)");
        public static string SfSafeSub => T(
            "文件夹内无任何文件，不会对角色 Mod 造成全局着色器干扰",
            "No files in ShaderFixes — safe from global shader overrides");

        public static string SfWarnTitle(int count) => IsZh
            ? $"⚠️ ShaderFixes：检测到 {count} 个文件"
            : $"⚠️ ShaderFixes: {count} Files Detected";
        public static string SfWarnSub => T(
            "全局着色器替换补丁正在使用中 , 与部分MOD存在冲突可能性",
            "Global shader files active in ShaderFixes");

        // Warning Card 2: Global Classifier Bundle Warning (Category W)
        public static string WarnClassifierTitle => T(
            "⚠️ 全局分类器预警",
            "⚠️ Global Classifier Warning");
        public static string WarnClassifierSub => T(
            "检测内置全局 [ShaderOverride + overrule] 分类器包的 Mod（点击查看详情）",
            "Mods with global [ShaderOverride + overrule] classifier bundles (click to inspect)");

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
        public static string CardATitle => T("A. ShaderOverride 冲突", "A. ShaderOverride Collisions");
        public static string CardBTitle => T("B. ShaderRegex 冲突", "B. ShaderRegex Conflicts");
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
