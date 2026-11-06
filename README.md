# EFMI MOD 冲突检测工具 v1.0 / EFMI Mods Conflict Auditor v1.0

**《明日方舟：终末地》(Arknights: Endfield) EFMI / XXMI / 3DMigoto 跨 Mod 静态冲突审计工具**  
**100% 纯只读安全检测 · 零外部依赖单文件 `.exe` · 中英双语实时切换**

---

## ✨ 核心特性 (Features)

1. **🔒 100% 纯只读安全检测 (100% Read-Only & Non-Destructive)**
   - 仅静态解析目标目录下的 `Mods` 与 `ShaderFixes` 子文件夹，**绝不修改、重命名或写入任何用户 Mod 文件**。
   - 内置严格的子目录防误扫机制：仅当目标路径下存在 `Mods` 或 `ShaderFixes` 文件夹时才执行扫描，自动忽略 `Core`、`FrameAnalysis`、`ShaderCache` 等无关目录及各类备份文件。

2. **⚠️ 独立双项风险预警面板 (Risk Warnings Panel)**
   - **ShaderFixes 全局着色器补丁预警**：独立检测 `ShaderFixes` 目录内是否存在全局着色器替换文件（不混入 `Mods` 冲突统计），支持一键打开 `ShaderFixes` 目录排查。
   - **全局分类器预警 (Global Classifier Warning)**：自动识别内置了全局 `[ShaderOverride + allow_duplicate_hash=overrule]` 纯分类器包的旧版 Mod，支持点击在列表中单独检视。

3. **🔥 5 种 Mod 架构识别 × 4 大核心冲突检测 (Conflict Detection Matrix)**
   - **Mod 架构类型自动识别与筛选**：
     - `全部类型 (All)`
     - `🦴 统一骨骼 (MergedSkeleton)`
     - `🔗 CrossIB (跨骨骼)`
     - `📦 简易 MOD (Simple)`
     - `✨ 前置插件 (FX Plugin)`（如 `RabbitFX`、`KatFXMatcap` 等纯着色器/特效前置插件）
   - **4 大跨 Mod 冲突分类**：
     - **`A. Shader Hash 冲突`**：多个不同 Mod 的 `[ShaderOverride]` 声明了相同的 `hash`（细分 `filter_index` 冲突、`overrule` 覆盖等风险等级）。
     - **`B. Regex 优先级抢占`**：旧版硬编码 `[ShaderOverride + overrule]` 分类器组与 `[ShaderRegex]` 争夺相同的 `filter_index`（如 `1718.1`、`203` 等）。
     - **`C. 正则匹配器重复`**：多个 Mod 重复定义了相同 `filter_index` 的 `[ShaderRegex]`，或对同类着色器叠加使用了 `[ShaderRegex.*.Pattern.Replace]` 动态字节码注入（SinsOfSeven 规范高危项）。
     - **`D. 同角色重复开启检测`**：检测多个**已启用**的 Mod 是否同时覆写了同一个角色的模型缓冲区 (IB/VB) 或贴图 Hash（勾选「扫描已禁用的 Mod」时自动隐藏并跳过该项）。

4. **🛠️ 冲突详情与代码检查器 (Inspector & Quick Actions)**
   - 点击任意冲突条目即可查看冲突原因分析、处理建议、涉及的所有 `.ini` 文件及具体行号，并提供实时 INI 节代码预览。
   - **📝 打开 INI**：自动调用 Windows 系统默认的 `.ini` 关联编辑器（如 VS Code、Notepad++、Sublime Text、记事本等）直接打开对应文件。
   - **📂 定位文件**：一键在 Windows 资源管理器中选中并高亮对应的 `.ini` 文件。

---

## 🚀 使用方法 (Usage)

1. 将编译好的单文件 [`EFMI_Conflict_Auditor.exe`](./bin/Release/net48/EFMI_Conflict_Auditor.exe) 放置在 `EFMI` 根目录（即包含 `Mods` 和 `ShaderFixes` 的同级目录）。
2. 双击运行 `EFMI_Conflict_Auditor.exe`，程序会自动扫描当前目录下的 `Mods` 与 `ShaderFixes`。
3. 右上角可随时一键切换 **中文 / English** 界面语言。

---

## 🛠️ 从源码编译 (Build from Source)

本项目基于 `.NET Framework 4.8` (Windows 原生内置 WPF)，编译生成的 `EFMI_Conflict_Auditor.exe` 为**零外部依赖单文件可执行程序**，在任何 Windows 10 / 11 系统上均可直接双击运行。

```powershell
dotnet build EFMI_Conflict_Auditor.csproj -c Release
```

编译产物路径：`bin\Release\net48\EFMI_Conflict_Auditor.exe`

---

## 📄 开源协议 (License)

[MIT License](./LICENSE)
