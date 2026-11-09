# EFMI Mods Conflict Auditor v1.0.1

🌐 **[中文说明文档 (README.md)](./README.md)**

**Static Cross-Mod Conflict Auditor & Troubleshooter for *Arknights: Endfield* (EFMI / XXMI / 3DMigoto)**  
**100% Read-Only & Non-Destructive · Zero-Dependency Single-File `.exe` · Real-Time Bilingual UI (`🌐 Language: 中文 / English`)**

---

## 📖 User Guide

> 💡 **Placement & Path Selection Tips**:
> 1. **We strongly recommend placing `EFMI_Conflict_Auditor.exe` directly inside your `EFMI` root folder.** That way, every time you launch the program, it will automatically detect the current directory and scan immediately without needing to select a path each time.
> 2. If you manually browse for a folder, make sure to select the **`EFMI` root directory** (the parent folder that contains `Mods` and `ShaderFixes`), **NOT the `EFMI\Mods` subfolder**!

The workflow in the UI goes from top to bottom: once your `EFMI Path` is set, click **"🔍 Scan Now"**. For normal troubleshooting, there is no need to check *"Scan disabled Mods (DISABLED_ prefix)"*.

### 1. ⚠️ Risk Warnings (Check This First)
- **ShaderFixes Status**:
  Alerts you whenever legacy anti-blur patches or custom shadow/outline shader replacement files exist inside the `ShaderFixes` folder. Because files in `ShaderFixes` globally replace low-level shaders across the entire game, they frequently cause missing body parts, discolored patches, or broken shading on characters.
  - **Fix**: Click the **"📂 Open Folder"** button on the right and **clear all files inside `ShaderFixes`**!

### 2. 🔥 Conflict Detection
The tool first classifies Mods by their **architecture ("process")**:
- `All Mod Types`
- `🦴 MergedSkeleton`
- `🔗 CrossIB`
- `📦 Simple Mod`
- `✨ FX Plugin` (Standalone shader/effect plugins such as `RabbitFX`, `CrossIBClassifier.ini`, `KatFXMatcap`, etc.)

#### Severity Levels (Highest to Lowest)
- 🔴 **`HIGH`**: **High conflict risk — requires immediate attention** (very likely to cause other characters to turn black, break textures, or cause FPS drops).
- 🟠 **`WARNING`**: **Moderate risk** — commonly present in `CrossIB` Mods and normally compatible unless visual glitches occur.
- 🔵 **`INFO`**: **Low-risk notice** — used solely to check whether multiple Mods for the same character are enabled simultaneously.

#### Four Detection Categories
- **`A. ShaderOverride Warnings`**:
  In XXMI / 3DMigoto, `[ShaderOverride]` has the highest global priority and acts like a hard shader hijack. If a Mod is flagged as **`HIGH`** in this category (e.g., overriding a shared shader with a custom private `filter_index`), it is very likely the root cause of other characters turning black or having scrambled textures. *(Note: Standard `CrossIB` pass markers `200~205` and standard `RabbitFX`-compatible `1718.1 / 1718.2` tags are automatically downgraded to `WARNING`).*
- **`B. ShaderRegex Warnings`**:
  Although `[ShaderRegex]` has lower priority than `[ShaderOverride]`, some Mod authors do not follow the AGMF community standard — instead of relying on shared standalone plugins, they privately bundle classifier/cutout `.ini` files inside their character Mod folder or paste `[ShaderRegex]` code directly into the character's main `.ini`, which easily conflicts with shared FX plugins.
- **`C. Duplicate ShaderRegex`**:
  Prevents duplicate placement of regex classifiers and detects mutually incompatible FX plugins. Shared plugins (such as `RabbitFX` / `CrossIBClassifier.ini`) should only exist once in the `Mods` root directory. Even if an author renames the file (e.g. `CharacterShaderClassifier.ini` or `ShaderPassClassifier.ini`) or copy-pastes the regex classifier directly into a character's main `.ini`, this category will catch and report all duplicates. It also alerts you if conflicting FX plugins (such as `KatFXMatcap` and `RabbitFX`) are enabled at the same time so you can disable one of them.
- **`D. Duplicate Active Character`**:
  Assigned the lowest **`INFO`** severity level. Active only when scanning **enabled** Mods, preventing you from accidentally enabling two or more Mods for the same character at the same time. **Please judge from the results whether the listed Mods belong to the same character — if they belong to different characters, there is no conflict risk.**

### 3. 🛠️ Conflict Details & Code Inspector
- Click any item on the left to inspect its root-cause analysis, troubleshooting advice, all involved `.ini` files with exact line numbers, and a live INI section code preview.
- **📝 Open INI**: Opens the `.ini` file directly in your default Windows editor (VS Code, Notepad++, Notepad, etc.).
- **📂 Reveal in Explorer**: Highlights the `.ini` file directly in Windows File Explorer.

### 4. 📄 Export Report & AI-Assisted Analysis
- **Local Report Saving**: Check **"Auto-save TXT Report"** or click **"📄 Export Report"** to save a full plain-text audit report (`EFMI_Conflict_Report_xxxxxx.txt`) directly inside your local `EFMI` root folder.
- **Feed to AI Assistants**: The exported `.txt` report contains structured details of every flagged Mod, exact `.ini` file paths, line numbers, shader hashes, and `filter_index` values. You can feed this `.txt` file directly to any **AI assistant** (ChatGPT, Claude, Gemini, DeepSeek, etc.) for deeper diagnosis or automated `.ini` fix instructions!

---

## 🔒 Safety Guarantee & Build from Source

- **100% Read-Only & Non-Destructive**: This tool only reads and parses the `Mods` and `ShaderFixes` subdirectories. **It never modifies, renames, or deletes any user files.**
- **Zero-Dependency Single-File Build**: Built on native Windows `.NET Framework 4.8` (WPF) — runs out of the box on any Windows 10 / 11 PC:

```powershell
dotnet build EFMI_Conflict_Auditor.csproj -c Release
```

Compiled binary path: `bin\Release\net48\EFMI_Conflict_Auditor.exe`

---

## 📄 License

[MIT License](./LICENSE)
