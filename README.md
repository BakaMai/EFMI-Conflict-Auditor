# EFMI MOD 冲突检测工具 v1.0 / EFMI Mods Conflict Auditor v1.0

**《明日方舟：终末地》(Arknights: Endfield) EFMI / XXMI / 3DMigoto 跨 Mod 静态冲突审计工具**  
**100% 纯只读安全检测 · 零外部依赖单文件 `.exe` · 中英双语实时切换**

---

## ✨ 核心特性 (Features)

1. **🔒 100% 纯只读安全检测 (100% Read-Only & Non-Destructive)**
   - 仅静态解析目标目录下的 `Mods` 与 `ShaderFixes` 子文件夹，**绝不修改、重命名或写入任何用户 Mod 文件**。
   - 内置严格的子目录防误扫机制：仅当目标路径下存在 `Mods` 或 `ShaderFixes` 文件夹时才执行扫描，自动忽略 `Core`、`FrameAnalysis`、`ShaderCache` 等无关目录及各类备份文件。

2. **⚠️ 独立风险预警面板 (Risk Warnings Panel)**
   - **ShaderFixes 全局着色器补丁预警**：独立检测 `ShaderFixes` 目录内是否存在全局着色器替换文件（不混入 `Mods` 冲突统计），支持点击右侧小按钮打开 `ShaderFixes` 目录排查。

3. **🔥 5 种 Mod 架构识别 × 4 大核心检测分类 (Conflict Detection Matrix)**
   - **Mod 架构类型自动识别与筛选**：
     - `全部类型`
     - `🦴 统一骨骼`
     - `🔗 CrossIB`
     - `📦 简易 MOD`
     - `✨ 前置插件 (FX Plugin)`（如 `RabbitFX`、`CrossIBClassifier.ini`、`KatFXMatcap` 等纯着色器/特效前置插件）
   - **4 大检测与警告分类**：
     - **`A. ShaderOverride 警告`**：列出所有包含 `[ShaderOverride]` 代码的角色/外观 Mod（按 Mod 汇总名单，因为任何 `[ShaderOverride]` 都会在 3DMigoto 全局 Hash 表中抢占并覆盖 `RabbitFX` 或 `CrossIBClassifier` 的 `[ShaderRegex]`，导致其它角色黑块或破皮）。
     - **`B. ShaderRegex 警告`**：排除 `前置插件 (FX Plugin)` 外，列出所有额外私自增加 `[ShaderRegex]` 代码的角色 Mod（如私自打包分类器或内置 `[ShaderRegex.*.Pattern.Replace]` 字节码注入）。
     - **`C. 正则匹配器重复`**：检测多个文件重复定义了相同 `filter_index` 的 `[ShaderRegex]`（含改名的 `CharacterShaderClassifier.ini` / `ShaderPassClassifier.ini` 等重复分类器），或叠加使用了 `[ShaderRegex.*.Pattern.Replace]`。
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
