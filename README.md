# EFMI MOD 冲突检测工具 v1.0.3

🌐 **[English Documentation (README.EN.md)](./README.EN.md)**

**《明日方舟：终末地》(Arknights: Endfield) EFMI / XXMI / 3DMigoto 跨 Mod 静态冲突检测与排查工具**  
**100% 纯只读安全检测 · 零外部依赖单文件 `.exe` · 中英双语实时切换 (`🌐 Language: 中文 / English`)**

---

## 📖 使用说明

> 💡 **放置位置与路径选择提示**：
> 1. **强烈建议直接将 `EFMI_Conflict_Auditor.exe` 放在 `EFMI` 根目录下运行**，这样每次双击启动时程序会自动锁定当前目录并直接完成扫描，避免每次启动都要重新选路径。
> 2. 如果手动浏览选择路径，请注意选择的是 **`EFMI` 根目录**（即包含 `Mods` 和 `ShaderFixes` 的那一层目录），**不要选成 `EFMI\Mods` 子目录**！

操作流程在界面中从上至下：确认好 `EFMI目录` 位置后，点击 **「🔍 开始扫描」**。一般日常排查无需勾选「扫描已禁用的 Mod」。

### 1. ⚠️ 风险预警 与 ⚡ 缓存检测清理
- **⚠️ 风险预警（ShaderFixes 全局着色器状态）**：
  当 `ShaderFixes` 文件夹中含有旧版反虚化补丁，或部分作者提供的局部阴影/描边修复补丁时，都会在这里进行提示。只要用了这种直接替换底层着色器的打补丁方式，都有概率造成角色身体**青一块、紫一块、少一块**的问题。
  - **解决方式**：点击右侧 **「📂 打开目录」** 按钮，将 `ShaderFixes` 文件夹内的文件**全部清空**！
- **⚡ 缓存检测与清理（ShaderCache 管理）**：
  - **着色器缓存开关检测与一键切换**：同时检测 `EFMI\d3dx.ini` 中的 `[Rendering] cache_shaders = 1` 以及 `Mods` 目录下（含二级子目录，如 `RabbitFX -ENDMI-`）的 `ShaderCacheSettings.ini` 是否被移出 `DISABLED` 启用。点击 **「⚡ 一键开启 / ⏸ 一键关闭」** 即可在 `d3dx.ini` 中统一管理全局缓存开关，并自动将外层的 `ShaderCacheSettings.ini` 收纳回 `DISABLED` 文件夹，避免重复配置冲突。
  - **🧹 一键清理 `ShaderCache`**：部分冲突 MOD 生成的错误着色器缓存若不清空，即使禁用冲突 MOD 并在游戏内按 `F10` 刷新，画面仍会残留错误的贴图或黑块。点击 **「🧹 一键清理」** 即可一键清空 `EFMI\ShaderCache` 内的缓存文件，随后回游戏按 `F10` 即可恢复正常。

### 2. 🔥 冲突检测
程序会先对 MOD 的**“制程”**进行自动识别与分类：
- `全部类型`
- `🦴 统一骨骼` (`Merged Skeleton`)
- `🔗 CrossIB`
- `📦 简易 MOD` (`Simple Mod`)
- `✨ 前置插件 (FX Plugin)`（如 `RabbitFX`、`CrossIBClassifier.ini`、`KatFXMatcap` 等纯着色器/特效前置插件）

#### 风险等级说明（由高到低）
- 🔴 **`HIGH`**：**高冲突风险，需要重点关注**（极易导致其他角色模型变黑、贴图错乱、冗余全局分类器冲突或严重掉帧）。
- 🟠 **`WARNING`**：**中等风险**，通常 `CrossIB` 架构或带唯一常量 ID 自保护（`if_nz` 隔离）的独立透明遮罩（`CutoutMask.ini`）会带有此类标记，常规少量开启可兼容运行，仅在大量叠加或画面异常时需排查。
- 🔵 **`INFO`**：**低风险提示**，用于展示合规的私有命名空间分类器（`namespace = ...`）以及同角色重复开启 / 双形态配套提示。

#### 四大检测分类说明
- **`A. ShaderOverride 警告`**：
  `ShaderOverride` 在 XXMI / 3DMigoto 下相当于最高优先级，有点代码被强行劫持的感觉。如果在该项目下被判定为 **`HIGH`** 风险级（例如使用私有编号覆盖了公共着色器），那这款 MOD 大概率就是导致其他角色浑身变黑、贴图错乱的冲突根源。（注：常规 `CrossIB` 使用的 `200~205` 阶段标记以及兼容 `RabbitFX` 的 `1718.1 / 1718.2` 标签会自动归为 `WARNING` 级）。
- **`B. ShaderRegex 警告`**：
  检测非前置插件的角色 MOD 内置的 `[ShaderRegex]`：
  - 若使用了**独立私有 `namespace`** 且不占用公共 `filter_index`（如 `SlotClassifier.ini`），会自动识别为 **`INFO`** 合规提示，并提醒切勿误删；
  - 若使用了**唯一常量 ID + `if_nz` 条件隔离**的字节码替换（如独立 `CutoutMask.ini`），会归为 **`WARNING`** 兼容提示；
  - 若未做隔离直接占用公共 `filter_index` 或裸改字节码，则标记为 **`HIGH`** 高风险。
- **`C. 正则匹配器重复`**：
  精准区分三类情形：
  1. **共享 `filter_index` 重复（`HIGH`）**：如角色目录内私自夹带了与根目录公共 `CrossIBClassifier.ini` / `RabbitFX` 相同的无命名空间副本，会直接点名可安全删除的角色内冗余副本路径；
  2. **私有 `namespace` 分类器模板复用（`INFO`） / 唯一 ID 隔离的 `Pattern.Replace` 叠加（`WARNING`）**：明确提示各 Mod 状态彼此隔离、切勿误删内部功能文件（仅当同类型字节码注入叠加过多 ≥6 个或 ID 撞号时升级为 `HIGH`）；
  3. **前置插件互斥冲突（`HIGH`）**：如同时开启了底层正则冲突的 `KatFXMatcap` 与 `RabbitFX`，提醒关闭其中一个。
- **`D. 同角色重复开启检测`**：
  此项风险等级为最低的 **`INFO`** 级。仅在扫描**启用中**的 MOD 时生效，避免同一角色身上不小心同时开启了两个或以上的皮肤 MOD。自动识别同一角色的**「常态 + 大招变身（Ultimate / ult / henshin）」**双形态配套包并提示需同时保留开启；其余条目请自行判断是否属于同一个角色的不同皮肤，若属于不同角色即无冲突风险。

### 3. 🛠️ 冲突详情与代码检查器
- 点击左侧任意条目即可在右侧查看冲突原因分析、处理建议、涉及的所有 `.ini` 文件及具体行号，并提供实时 INI 节代码预览。
- **📝 打开 INI**：自动调用 Windows 系统默认的 `.ini` 关联编辑器（如 VS Code、Notepad++、记事本等）直接打开对应文件。
- **📂 定位文件**：一键在 Windows 资源管理器中选中并高亮对应的 `.ini` 文件。

### 4. 📄 导出报告与 AI 辅助排查
- **本地报告生成**：勾选 **「自动生成 TXT 报告」** 或点击 **「📄 导出报告」**，即可在本地 `EFMI` 根目录下生成完整的纯文本检测报告（如 `EFMI_Conflict_Report_xxxxxx.txt`）。
- **发给 AI 助手二次分析**：导出的 `.txt` 报告内附带了所有被标记 Mod 的路径、`.ini` 具体行号、Shader Hash 与 `filter_index` 明细。你可以直接把这份 `.txt` 报告投喂给各类 **AI 助手**（如 ChatGPT、Claude、Gemini、DeepSeek 等），让 AI 帮你进一步定位冲突原因或自动给出 `.ini` 修改方案！

---

## 🔒 安全承诺与从源码编译

- **角色 MOD 本体 100% 无损安全**：冲突扫描过程完全只读，**绝不修改、重命名或删除任何角色 MOD 本体文件**（仅在你主动点击界面上的缓存管理按钮时，才会切换 `d3dx.ini` 的缓存开关、将外层的 `ShaderCacheSettings.ini` 收纳回 `DISABLED` 文件夹，或清空 `ShaderCache` 临时着色器缓存）。
- **单文件零依赖编译**：基于 Windows 原生内置 `.NET Framework 4.8` (WPF)，任何 Windows 10 / 11 系统均可直接双击运行：

```powershell
dotnet build EFMI_Conflict_Auditor.csproj -c Release
```

编译产物路径：`bin\Release\net48\EFMI_Conflict_Auditor.exe`

---

## 📄 开源协议

[MIT License](./LICENSE)
