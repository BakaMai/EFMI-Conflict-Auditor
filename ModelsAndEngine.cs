using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EfmiConflictAuditor
{
    public enum ModArchType
    {
        Simple,
        ShaderPatch,
        CrossIB,
        MergedSkeleton
    }

    public class EncodedText
    {
        public string Text { get; set; }
        public Encoding Encoding { get; set; }
        public bool HasBom { get; set; }
    }

    public class ModIdentity
    {
        public string ModId { get; set; }
        public string DisplayName { get; set; }
        public bool IsDisabled { get; set; }
        public string ModRootPath { get; set; }
    }

    public class ScannedModSummary
    {
        public string ModId { get; set; }
        public bool IsDisabled { get; set; }
        public ModArchType ModType { get; set; }
        public string ModRootPath { get; set; }
        public int IniFileCount { get; set; }
    }

    public class IniSectionItem
    {
        public string Kind { get; set; } // ShaderOverride, ShaderRegex, TextureOverride
        public string Mod { get; set; }
        public bool ModIsDisabled { get; set; }
        public ModArchType ModType { get; set; }
        public string File { get; set; }
        public string RelativeFile { get; set; }
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public double? Filter { get; set; }
        public string FilterRaw { get; set; }
        public bool Overrule { get; set; }
        public string AllowDuplicate { get; set; }
        public bool PureClassifier { get; set; }
        public string ShaderModel { get; set; }
        public string PatternText { get; set; }
        public string ReplaceText { get; set; }
        public string InsertDeclText { get; set; }
        public bool HasPatternReplace { get; set; }
        public bool IsPrivateNamespaceClassifier { get; set; }
        public bool IsTagIsolatedReplace { get; set; }
        public string IsolatedGuardTag { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public int Line { get; set; }
        public string Text { get; set; }
    }

    public class ParsedIniFile
    {
        public string FilePath { get; set; }
        public string RelativePath { get; set; }
        public string Namespace { get; set; }
        public string ModId { get; set; }
        public bool ModIsDisabled { get; set; }
        public string ModRootPath { get; set; }
        public ModArchType ModType { get; set; }
        public EncodedText Loaded { get; set; }
        public List<IniSectionItem> ShaderOverrides { get; set; } = new List<IniSectionItem>();
        public List<IniSectionItem> ShaderRegexes { get; set; } = new List<IniSectionItem>();
        public List<IniSectionItem> TextureOverrides { get; set; } = new List<IniSectionItem>();
    }

    public class InvolvedFileEntry
    {
        public string ModId { get; set; }
        public bool IsDisabled { get; set; }
        public ModArchType ModType { get; set; }
        public string FilePath { get; set; }
        public string RelativePath { get; set; }
        public List<int> Lines { get; set; } = new List<int>();
        public List<string> SectionNames { get; set; } = new List<string>();

        public string DisplayMod => IsDisabled ? $"[{I18n.DisabledBadge}] {ModId}" : ModId;
        public string LinesSummary => Lines.Count > 8
            ? $"Lines {string.Join(", ", Lines.Take(8))} ... (+{Lines.Count - 8})"
            : $"Line{(Lines.Count > 1 ? "s" : "")}: {string.Join(", ", Lines)}";
    }

    public class ConflictItem
    {
        public string Id { get; set; }
        public string Category { get; set; } // "A", "B", "C", "D", "E"
        public string Severity { get; set; } // "CRITICAL", "HIGH", "MEDIUM", "WARNING", "INFO"
        public bool InvolvesDisabledMod { get; set; }

        public List<string> InvolvedMods { get; set; } = new List<string>();
        public HashSet<string> DisabledMods { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<ModArchType> InvolvedModTypes { get; set; } = new HashSet<ModArchType>();

        public string TitleZh { get; set; }
        public string TitleEn { get; set; }
        public string SubtitleZh { get; set; }
        public string SubtitleEn { get; set; }
        public string ReasonZh { get; set; }
        public string ReasonEn { get; set; }
        public string RecommendZh { get; set; }
        public string RecommendEn { get; set; }

        public List<IniSectionItem> Sections { get; set; } = new List<IniSectionItem>();
        public List<InvolvedFileEntry> FileEntries { get; set; } = new List<InvolvedFileEntry>();

        public string Title => I18n.T(TitleZh, TitleEn);
        public string Subtitle => I18n.T(SubtitleZh, SubtitleEn);
        public string Reason => I18n.T(ReasonZh, ReasonEn);
        public string Recommendation => I18n.T(RecommendZh, RecommendEn);

        public string CategoryBadge => Category switch
        {
            "A" => I18n.T("A · ShaderOverride 警告", "A · ShaderOverride Warning"),
            "B" => I18n.T("B · ShaderRegex 警告", "B · ShaderRegex Warning"),
            "C" => I18n.T("C · 正则匹配器重复", "C · Dup Regex"),
            "D" => I18n.T("D · 同角色重复开启", "D · Same-Char Overlap"),
            _ => Category
        };
    }

    public enum D3dxCacheSettingState
    {
        MissingFile,
        NotSet,
        Disabled,
        Enabled
    }

    public class ShaderCacheStatus
    {
        public string ScanRoot { get; set; }
        public bool IsValidEfmiRoot { get; set; }

        public string D3dxIniPath { get; set; }
        public bool HasD3dxIni { get; set; }
        public D3dxCacheSettingState D3dxState { get; set; } = D3dxCacheSettingState.MissingFile;

        public List<string> ActiveModCacheIniPaths { get; set; } = new List<string>();
        public List<string> ActiveModCacheEnabledPaths { get; set; } = new List<string>();
        public List<string> DisabledModCacheIniPaths { get; set; } = new List<string>();

        public string ShaderCacheDirPath { get; set; }
        public bool HasShaderCacheDir { get; set; }
        public int CacheFileCount { get; set; }
        public long CacheTotalBytes { get; set; }

        public bool HasActiveModCacheIni => ActiveModCacheIniPaths.Count > 0;
        public bool HasActiveModCacheEnabled => ActiveModCacheEnabledPaths.Count > 0;

        public bool HasDuplicateConfig =>
            (D3dxState == D3dxCacheSettingState.Enabled || D3dxState == D3dxCacheSettingState.Disabled) &&
            HasActiveModCacheIni;

        public bool IsEffectivelyEnabled =>
            D3dxState == D3dxCacheSettingState.Enabled ||
            (D3dxState == D3dxCacheSettingState.NotSet && HasActiveModCacheEnabled);
    }

    public class AuditResult
    {
        public string TargetPath { get; set; }
        public string ScanRoot { get; set; }
        public bool IncludedDisabled { get; set; }
        public DateTime ScanTime { get; set; }
        public List<ParsedIniFile> ParsedFiles { get; set; } = new List<ParsedIniFile>();
        public List<ScannedModSummary> ScannedMods { get; set; } = new List<ScannedModSummary>();
        public List<ConflictItem> AllFindings { get; set; } = new List<ConflictItem>();

        public int TotalMods { get; set; }
        public int ActiveMods { get; set; }
        public int DisabledMods { get; set; }
        public string ShaderFixesPath { get; set; }
        public bool HasShaderFixesFolder { get; set; }
        public int ShaderFixesFileCount { get; set; }
        public ShaderCacheStatus CacheStatus { get; set; } = new ShaderCacheStatus();
        public Dictionary<string, ModArchType> ModTypes { get; set; } = new Dictionary<string, ModArchType>(StringComparer.OrdinalIgnoreCase);
        public int MergedSkeletonModCount => ModTypes.Values.Count(x => x == ModArchType.MergedSkeleton);
        public int CrossIBModCount => ModTypes.Values.Count(x => x == ModArchType.CrossIB);
        public int SimpleModCount => ModTypes.Values.Count(x => x == ModArchType.Simple);
        public int ShaderPatchModCount => ModTypes.Values.Count(x => x == ModArchType.ShaderPatch);
        public int IniCount => ParsedFiles.Count;
        public int ShaderOverrideCount { get; set; }
        public int ShaderRegexCount { get; set; }
        public int TextureOverrideCount { get; set; }

        public int CountA => AllFindings.Count(x => x.Category == "A");
        public int CountB => AllFindings.Count(x => x.Category == "B");
        public int CountC => AllFindings.Count(x => x.Category == "C");
        public int CountD => AllFindings.Count(x => x.Category == "D");
        public int CountE => AllFindings.Count(x => x.Category == "E");
    }

    public static class AuditorEngine
    {
        private static readonly Regex SectionRegex = new Regex(
            @"(?ms)^[ \t]*\[(?<name>[^\]\r\n]+)\][^\r\n]*(?:\r?\n|$).*?(?=^[ \t]*\[[^\]\r\n]+\][^\r\n]*(?:\r?\n|$)|\z)",
            RegexOptions.Compiled);

        public static EncodedText ReadTextPreserveEncoding(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int offset = 0;
            bool hasBom = false;
            Encoding encoding;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(true, true);
                offset = 3;
                hasBom = true;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = new UnicodeEncoding(false, true, true);
                offset = 2;
                hasBom = true;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, true, true);
                offset = 2;
                hasBom = true;
            }
            else
            {
                var utf8Strict = new UTF8Encoding(false, true);
                try
                {
                    utf8Strict.GetString(bytes);
                    encoding = utf8Strict;
                }
                catch
                {
                    encoding = Encoding.Default;
                }
            }

            string text = encoding.GetString(bytes, offset, bytes.Length - offset);
            return new EncodedText
            {
                Text = text,
                Encoding = encoding,
                HasBom = hasBom
            };
        }

        public static void WriteTextPreserveEncoding(string path, string text, EncodedText original)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
            }

            Encoding enc = original?.Encoding ?? new UTF8Encoding(false);
            byte[] preamble = original != null && original.HasBom ? enc.GetPreamble() : Array.Empty<byte>();
            byte[] body = enc.GetBytes(text);

            if (preamble != null && preamble.Length > 0)
            {
                byte[] combined = new byte[preamble.Length + body.Length];
                Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
                Buffer.BlockCopy(body, 0, combined, preamble.Length, body.Length);
                File.WriteAllBytes(path, combined);
            }
            else
            {
                File.WriteAllBytes(path, body);
            }
        }

        public static string FormatByteSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024.0):F1} KB";
            if (bytes < 1024L * 1024L * 1024L) return $"{(bytes / (1024.0 * 1024.0)):F1} MB";
            return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
        }

        private static D3dxCacheSettingState ParseRenderingCacheSetting(string iniText, out string cacheDirectoryRaw)
        {
            cacheDirectoryRaw = null;
            if (string.IsNullOrEmpty(iniText))
                return D3dxCacheSettingState.NotSet;

            D3dxCacheSettingState? foundState = null;
            bool inRendering = false;

            using var reader = new StringReader(iniText);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("["))
                {
                    int closeIdx = trimmed.IndexOf(']');
                    if (closeIdx > 1)
                    {
                        string secName = trimmed.Substring(1, closeIdx - 1).Trim();
                        inRendering = secName.Equals("Rendering", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                }

                if (!inRendering)
                    continue;

                int eqIdx = trimmed.IndexOf('=');
                if (eqIdx <= 0)
                    continue;

                string key = trimmed.Substring(0, eqIdx).Trim();
                string rawVal = trimmed.Substring(eqIdx + 1);
                int commentIdx = rawVal.IndexOf(';');
                if (commentIdx >= 0)
                    rawVal = rawVal.Substring(0, commentIdx);
                string val = rawVal.Trim().Trim('"');

                if (key.Equals("cache_shaders", StringComparison.OrdinalIgnoreCase) && !foundState.HasValue)
                {
                    if (val.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                        val.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        val.Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        foundState = D3dxCacheSettingState.Enabled;
                    }
                    else
                    {
                        foundState = D3dxCacheSettingState.Disabled;
                    }
                }
                else if (key.Equals("cache_directory", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(cacheDirectoryRaw))
                {
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        cacheDirectoryRaw = val;
                    }
                }
            }

            return foundState ?? D3dxCacheSettingState.NotSet;
        }

        public static ShaderCacheStatus InspectShaderCache(string targetPath)
        {
            var status = new ShaderCacheStatus();
            string scanRoot;
            try
            {
                scanRoot = GetScanRoot(targetPath);
            }
            catch
            {
                return status;
            }

            status.ScanRoot = scanRoot;
            if (!Directory.Exists(scanRoot))
                return status;

            string d3dxPath = Path.Combine(scanRoot, "d3dx.ini");
            string modsDir = Path.Combine(scanRoot, "Mods");
            string shaderFixesDir = Path.Combine(scanRoot, "ShaderFixes");

            bool hasD3dx = File.Exists(d3dxPath);
            bool hasMods = Directory.Exists(modsDir);
            bool hasSf = Directory.Exists(shaderFixesDir);

            status.IsValidEfmiRoot = hasD3dx || hasMods || hasSf;
            status.D3dxIniPath = d3dxPath;
            status.HasD3dxIni = hasD3dx;

            string cacheDirRel = "ShaderCache";
            if (hasD3dx)
            {
                try
                {
                    var loaded = ReadTextPreserveEncoding(d3dxPath);
                    status.D3dxState = ParseRenderingCacheSetting(loaded.Text, out string parsedCacheDir);
                    if (!string.IsNullOrWhiteSpace(parsedCacheDir))
                    {
                        cacheDirRel = parsedCacheDir;
                    }
                }
                catch
                {
                    status.D3dxState = D3dxCacheSettingState.NotSet;
                }
            }
            else
            {
                status.D3dxState = D3dxCacheSettingState.MissingFile;
            }

            // Inspect Mods/**/ShaderCacheSettings.ini (including RabbitFX -ENDMI- in root or 2nd-level subfolders)
            if (hasMods)
            {
                try
                {
                    var modsDirInfo = new DirectoryInfo(modsDir);
                    foreach (var fi in modsDirInfo.EnumerateFiles("*ShaderCacheSettings*.ini", SearchOption.AllDirectories))
                    {
                        string nameLower = fi.Name.ToLowerInvariant();
                        if (Regex.IsMatch(nameLower, @"\.bak($|\.|_)") ||
                            Regex.IsMatch(nameLower, @"\.backup($|\.|_)") ||
                            nameLower.Contains(".xxmi_bak") ||
                            nameLower.Contains("efmicompatbackup"))
                        {
                            continue;
                        }

                        string relFromMods = GetRelativePath(fi.FullName, modsDir);
                        string[] segs = relFromMods.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        bool isBackupSeg = false;
                        bool hasDisabledSeg = false;
                        foreach (string seg in segs)
                        {
                            string s = seg.ToLowerInvariant();
                            if (s == "backup" || s == "backups" || s.StartsWith("backup_") || s.Contains("xxmi_bak"))
                            {
                                isBackupSeg = true;
                                break;
                            }
                            if (s.StartsWith("disabled"))
                            {
                                hasDisabledSeg = true;
                            }
                        }

                        if (isBackupSeg)
                            continue;

                        if (hasDisabledSeg)
                        {
                            status.DisabledModCacheIniPaths.Add(GetRelativePath(fi.FullName, scanRoot));
                        }
                        else
                        {
                            status.ActiveModCacheIniPaths.Add(fi.FullName);
                            try
                            {
                                var modIni = ReadTextPreserveEncoding(fi.FullName);
                                var modCacheState = ParseRenderingCacheSetting(modIni.Text, out _);
                                if (modCacheState == D3dxCacheSettingState.Enabled)
                                {
                                    status.ActiveModCacheEnabledPaths.Add(fi.FullName);
                                }
                            }
                            catch
                            {
                                // Ignore read errors on individual file
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore directory enumeration errors
                }
            }

            // Inspect ShaderCache directory & count cached files
            try
            {
                string cacheFull = Path.IsPathRooted(cacheDirRel)
                    ? Path.GetFullPath(cacheDirRel)
                    : Path.GetFullPath(Path.Combine(scanRoot, cacheDirRel));

                status.ShaderCacheDirPath = cacheFull;
                status.HasShaderCacheDir = Directory.Exists(cacheFull);

                if (status.HasShaderCacheDir)
                {
                    int count = 0;
                    long totalBytes = 0;
                    var dirInfo = new DirectoryInfo(cacheFull);
                    foreach (var fi in dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
                    {
                        string n = fi.Name.ToLowerInvariant();
                        if (n == "desktop.ini" || n == "thumbs.db" || n == ".ds_store")
                            continue;
                        count++;
                        totalBytes += fi.Length;
                    }
                    status.CacheFileCount = count;
                    status.CacheTotalBytes = totalBytes;
                }
            }
            catch
            {
                status.CacheFileCount = 0;
                status.CacheTotalBytes = 0;
            }

            return status;
        }

        public static void SetShaderCacheEnabled(string targetPath, bool enable, out int archivedModIniCount)
        {
            archivedModIniCount = 0;
            string scanRoot = GetScanRoot(targetPath);
            string d3dxPath = Path.Combine(scanRoot, "d3dx.ini");

            if (!File.Exists(d3dxPath))
            {
                throw new FileNotFoundException(I18n.T(
                    $"未找到 d3dx.ini 文件:\n{d3dxPath}\n请确认上方「EFMI目录」已指向正确的 EFMI 根目录。",
                    $"d3dx.ini not found:\n{d3dxPath}\nPlease ensure 'EFMI Path' points to the EFMI root directory."));
            }

            var loaded = ReadTextPreserveEncoding(d3dxPath);
            string nl = loaded.Text.Contains("\r\n") ? "\r\n" : "\n";
            string normalized = loaded.Text.Replace("\r\n", "\n");
            var lines = new List<string>(normalized.Split('\n'));

            int renderingStart = -1;
            int renderingEnd = lines.Count;

            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("["))
                {
                    int closeIdx = t.IndexOf(']');
                    if (closeIdx > 1)
                    {
                        string sec = t.Substring(1, closeIdx - 1).Trim();
                        if (renderingStart < 0)
                        {
                            if (sec.Equals("Rendering", StringComparison.OrdinalIgnoreCase))
                            {
                                renderingStart = i;
                            }
                        }
                        else
                        {
                            renderingEnd = i;
                            break;
                        }
                    }
                }
            }

            string desiredVal = enable ? "1" : "0";

            if (renderingStart < 0)
            {
                lines.Add(string.Empty);
                lines.Add("[Rendering]");
                lines.Add($"cache_shaders = {desiredVal}");
            }
            else
            {
                var activeIndices = new List<int>();
                int firstCommentedIdx = -1;
                int cacheDirIdx = -1;

                for (int i = renderingStart + 1; i < renderingEnd; i++)
                {
                    string raw = lines[i];
                    string t = raw.Trim();
                    if (t.Length == 0) continue;

                    if (t.StartsWith(";") || t.StartsWith("#"))
                    {
                        if (firstCommentedIdx < 0 && Regex.IsMatch(t, @"^[;#]\s*cache_shaders\s*=", RegexOptions.IgnoreCase))
                        {
                            firstCommentedIdx = i;
                        }
                        continue;
                    }

                    if (Regex.IsMatch(t, @"^cache_shaders\s*=", RegexOptions.IgnoreCase))
                    {
                        activeIndices.Add(i);
                    }
                    else if (cacheDirIdx < 0 && Regex.IsMatch(t, @"^cache_directory\s*=", RegexOptions.IgnoreCase))
                    {
                        cacheDirIdx = i;
                    }
                }

                if (activeIndices.Count > 0)
                {
                    int firstIdx = activeIndices[0];
                    string rawLine = lines[firstIdx];
                    var m = Regex.Match(rawLine, @"^(?<indent>\s*)cache_shaders\s*=\s*[^;#\r\n]*(?<comment>\s*[;#].*)?$", RegexOptions.IgnoreCase);
                    string indent = m.Success ? m.Groups["indent"].Value : string.Empty;
                    string comment = m.Success ? m.Groups["comment"].Value : string.Empty;
                    lines[firstIdx] = $"{indent}cache_shaders = {desiredVal}{comment}";

                    // Comment out any stray duplicate uncommented cache_shaders lines inside [Rendering]
                    for (int k = 1; k < activeIndices.Count; k++)
                    {
                        lines[activeIndices[k]] = ";" + lines[activeIndices[k]];
                    }
                }
                else if (firstCommentedIdx >= 0)
                {
                    string rawLine = lines[firstCommentedIdx];
                    var m = Regex.Match(rawLine, @"^(?<indent>\s*)[;#]\s*cache_shaders\s*=", RegexOptions.IgnoreCase);
                    string indent = m.Success ? m.Groups["indent"].Value : string.Empty;
                    lines[firstCommentedIdx] = $"{indent}cache_shaders = {desiredVal}";
                }
                else
                {
                    int insertAt = cacheDirIdx >= 0 ? cacheDirIdx + 1 : renderingStart + 1;
                    lines.Insert(insertAt, $"cache_shaders = {desiredVal}");
                }
            }

            string updatedText = string.Join(nl, lines);
            WriteTextPreserveEncoding(d3dxPath, updatedText, loaded);

            // Normalize any active ShaderCacheSettings.ini inside Mods back into DISABLED\ShaderCacheSettings.ini
            var currentStatus = InspectShaderCache(scanRoot);
            foreach (string activeIni in currentStatus.ActiveModCacheIniPaths)
            {
                try
                {
                    if (!File.Exists(activeIni)) continue;
                    string fileName = Path.GetFileName(activeIni);
                    if (fileName.IndexOf("ShaderCacheSettings", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    string parentDir = Path.GetDirectoryName(activeIni);
                    if (string.IsNullOrEmpty(parentDir)) continue;

                    string disabledDir = Path.Combine(parentDir, "DISABLED");
                    Directory.CreateDirectory(disabledDir);
                    string destPath = Path.Combine(disabledDir, fileName);

                    if ((File.GetAttributes(activeIni) & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(activeIni, File.GetAttributes(activeIni) & ~FileAttributes.ReadOnly);
                    }

                    if (File.Exists(destPath))
                    {
                        if ((File.GetAttributes(destPath) & FileAttributes.ReadOnly) != 0)
                        {
                            File.SetAttributes(destPath, File.GetAttributes(destPath) & ~FileAttributes.ReadOnly);
                        }
                        File.Copy(activeIni, destPath, true);
                        File.Delete(activeIni);
                    }
                    else
                    {
                        File.Move(activeIni, destPath);
                    }

                    archivedModIniCount++;
                }
                catch
                {
                    // Ignore individual move failure if file is locked
                }
            }
        }

        public static void ClearShaderCacheFiles(string targetPath, out int deletedCount, out int failedCount, out long freedBytes)
        {
            deletedCount = 0;
            failedCount = 0;
            freedBytes = 0;

            var status = InspectShaderCache(targetPath);
            if (string.IsNullOrEmpty(status.ShaderCacheDirPath) || !Directory.Exists(status.ShaderCacheDirPath))
                return;

            string fullRoot = Path.GetFullPath(status.ScanRoot).TrimEnd('\\', '/');
            string fullCache = Path.GetFullPath(status.ShaderCacheDirPath).TrimEnd('\\', '/');
            string dirName = Path.GetFileName(fullCache);

            if (string.Equals(fullRoot, fullCache, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "Mods", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "Core", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "ShaderFixes", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(I18n.T(
                    $"安全保护拦截：检测到非常规缓存路径 ({status.ShaderCacheDirPath})，已中止清理。",
                    $"Safety guard: Refusing to clear non-standard cache directory ({status.ShaderCacheDirPath})."));
            }

            var cacheDirInfo = new DirectoryInfo(fullCache);
            foreach (var fi in cacheDirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories))
            {
                string n = fi.Name.ToLowerInvariant();
                if (n == "desktop.ini" || n == "thumbs.db" || n == ".ds_store")
                    continue;

                try
                {
                    long len = fi.Length;
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        fi.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    fi.Delete();
                    deletedCount++;
                    freedBytes += len;
                }
                catch
                {
                    failedCount++;
                }
            }

            foreach (var subDir in cacheDirInfo.EnumerateDirectories("*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.FullName.Length))
            {
                try
                {
                    if (!subDir.EnumerateFileSystemInfos().Any())
                    {
                        subDir.Delete(false);
                    }
                }
                catch
                {
                    // Ignore locked subdirectory
                }
            }
        }

        public static string GetScanRoot(string inputTarget)
        {
            if (string.IsNullOrWhiteSpace(inputTarget))
            {
                inputTarget = AppDomain.CurrentDomain.BaseDirectory;
            }
            inputTarget = inputTarget.Trim().Trim('"').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (inputTarget.Length == 2 && inputTarget[1] == ':')
            {
                inputTarget += Path.DirectorySeparatorChar;
            }
            string full = Path.GetFullPath(inputTarget);
            if (Directory.Exists(full))
            {
                return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            if (File.Exists(full))
            {
                return Path.GetDirectoryName(full).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            throw new DirectoryNotFoundException(I18n.T(
                $"找不到目标路径: {inputTarget}",
                $"Target path not found: {inputTarget}"));
        }

        /// <summary>
        /// Always skips backups (.bak, .backup, .xxmi_bak, .EFMICompatBackup, backup folders) and conflict reports.
        /// Skips DISABLED_ folders/files when includeDisabled is false.
        /// </summary>
        public static bool ShouldSkipIni(FileInfo file, string scanRoot, bool includeDisabled, out bool hasDisabledSegment)
        {
            hasDisabledSegment = false;
            string nameLower = file.Name.ToLowerInvariant();

            // 1. Permanent skip: backup files and reports
            if (Regex.IsMatch(nameLower, @"\.bak($|\.|_)") ||
                Regex.IsMatch(nameLower, @"\.backup($|\.|_)") ||
                nameLower.Contains(".xxmi_bak") ||
                nameLower.Contains("efmicompatbackup") ||
                (nameLower.Contains("efmi_conflict_") && nameLower.Contains("report")))
            {
                return true;
            }

            string rel = GetRelativePath(file.FullName, scanRoot);
            string[] segments = rel.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string seg in segments)
            {
                string s = seg.ToLowerInvariant();
                if (s == "backup" || s == "backups" ||
                    s.StartsWith("backup_") || s.StartsWith("backup.") ||
                    Regex.IsMatch(s, @"\.bak($|\.|_)") ||
                    Regex.IsMatch(s, @"\.backup($|\.|_)") ||
                    s.Contains("xxmi_bak") ||
                    s.Contains("efmicompatbackup"))
                {
                    return true;
                }

                if (s.StartsWith("disabled"))
                {
                    hasDisabledSegment = true;
                }
            }

            if (!includeDisabled && hasDisabledSegment)
            {
                return true;
            }

            // Special guard: if an individual .ini file inside a folder starts with "disabled" (e.g. DISABLEDArclight.ini or DISABLED_mod1.ini),
            // check if that same directory already has active (non-disabled) .ini files.
            // If it does, this file is an internal disabled backup INI inside that mod and should not double-count the mod!
            if (nameLower.StartsWith("disabled") && file.Directory != null)
            {
                try
                {
                    bool hasSiblingActiveIni = file.Directory.GetFiles("*.ini", SearchOption.TopDirectoryOnly)
                        .Any(f => !f.Name.StartsWith("disabled", StringComparison.OrdinalIgnoreCase) &&
                                  !f.Name.ToLowerInvariant().Contains("efmicompatbackup") &&
                                  !Regex.IsMatch(f.Name.ToLowerInvariant(), @"\.bak($|\.|_)"));
                    if (hasSiblingActiveIni)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore IO error
                }
            }

            return false;
        }

        public static string GetRelativePath(string fullPath, string basePath)
        {
            string root = Path.GetFullPath(basePath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(fullPath);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length);
            }
            return Path.GetFileName(full);
        }

        private static bool IsCategoryGroupingFolder(string folderFullPath, Dictionary<string, bool> cache)
        {
            if (cache.TryGetValue(folderFullPath, out bool cached))
                return cached;

            string folderName = Path.GetFileName(folderFullPath);
            if (folderName.StartsWith("disabled", StringComparison.OrdinalIgnoreCase))
            {
                cache[folderFullPath] = false;
                return false;
            }

            try
            {
                // If this folder directly contains any .ini, .buf, .dds, or .hlsl files, it is a Mod folder itself
                bool hasDirectModFiles = Directory.EnumerateFiles(folderFullPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Any(f =>
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext == ".ini" || ext == ".buf" || ext == ".dds" || ext == ".hlsl";
                    });

                if (hasDirectModFiles)
                {
                    cache[folderFullPath] = false;
                    return false;
                }

                // Check subdirectories
                var subDirs = Directory.GetDirectories(folderFullPath);
                if (subDirs.Length == 0)
                {
                    cache[folderFullPath] = false;
                    return false;
                }

                // Multi-part single Mod package guard (e.g. "庄-Zhuang Fangyi Ink Cheongsam full ver 5.3"
                // containing "Zhuang Fangyi Ink Cheongsam full ver 5.3" + "Zhuang Fangyi Ultimate1.4"):
                // If an active child folder matches the outer folder's full outfit name (suffix length >= 8)
                // and all remaining active child folders are companion form/option subfolders, treat the outer
                // folder as ONE unified Mod package rather than a category grouping directory.
                var activeSubNames = subDirs
                    .Select(Path.GetFileName)
                    .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("disabled", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (activeSubNames.Count >= 2 && activeSubNames.Count <= 4)
                {
                    string cleanParent = folderName.Trim();
                    bool hasMainTwinChild = activeSubNames.Any(child =>
                        child.Length >= 8 &&
                        cleanParent.EndsWith(child, StringComparison.OrdinalIgnoreCase));

                    if (hasMainTwinChild)
                    {
                        bool othersAreCompanion = activeSubNames
                            .Where(child => !(child.Length >= 8 && cleanParent.EndsWith(child, StringComparison.OrdinalIgnoreCase)))
                            .All(IsCompanionSubfolderName);

                        if (othersAreCompanion)
                        {
                            cache[folderFullPath] = false;
                            return false;
                        }
                    }
                }

                cache[folderFullPath] = true;
                return true;
            }
            catch
            {
                cache[folderFullPath] = false;
                return false;
            }
        }

        public static ModIdentity ResolveModIdentity(string filePath, string scanRoot, Dictionary<string, bool> categoryCache)
        {
            string rel = GetRelativePath(filePath, scanRoot);
            string[] parts = rel.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 1)
            {
                bool fileDis = parts.Length == 1 && parts[0].StartsWith("disabled", StringComparison.OrdinalIgnoreCase);
                string rootName = parts.Length == 1 ? parts[0] : "(root)";
                return new ModIdentity
                {
                    ModId = rootName,
                    DisplayName = rootName,
                    IsDisabled = fileDis,
                    ModRootPath = scanRoot
                };
            }

            string firstDirPath = Path.Combine(scanRoot, parts[0]);
            if (parts.Length >= 3 && IsCategoryGroupingFolder(firstDirPath, categoryCache))
            {
                string modId = $"{parts[0]}/{parts[1]}";
                bool isDis = parts[0].StartsWith("disabled", StringComparison.OrdinalIgnoreCase) ||
                             parts[1].StartsWith("disabled", StringComparison.OrdinalIgnoreCase) ||
                             parts.Last().StartsWith("disabled", StringComparison.OrdinalIgnoreCase);
                return new ModIdentity
                {
                    ModId = modId,
                    DisplayName = modId,
                    IsDisabled = isDis,
                    ModRootPath = Path.Combine(scanRoot, parts[0], parts[1])
                };
            }
            else
            {
                string modId = parts[0];
                bool isDis = parts[0].StartsWith("disabled", StringComparison.OrdinalIgnoreCase) ||
                             parts.Last().StartsWith("disabled", StringComparison.OrdinalIgnoreCase);
                return new ModIdentity
                {
                    ModId = modId,
                    DisplayName = modId,
                    IsDisabled = isDis,
                    ModRootPath = firstDirPath
                };
            }
        }

        private static string GetKeyValue(string sectionText, string key)
        {
            string pat = @"(?mi)^[ \t]*" + Regex.Escape(key) + @"[ \t]*=[ \t]*([^;\r\n]+)";
            Match m = Regex.Match(sectionText, pat);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        private static int GetLineNumber(string wholeText, int index)
        {
            if (index <= 0) return 1;
            int count = 1;
            int max = Math.Min(index, wholeText.Length);
            for (int i = 0; i < max; i++)
            {
                if (wholeText[i] == '\n') count++;
            }
            return count;
        }

        private static bool TestPureClassifier(string sectionText)
        {
            string[] lines = sectionText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool first = true;
            foreach (string line in lines)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                string trim = line.Trim();
                if (string.IsNullOrWhiteSpace(trim)) continue;
                if (trim.StartsWith(";") || trim.StartsWith("#")) continue;
                if (Regex.IsMatch(trim, @"^(?i)(hash|filter_index|allow_duplicate_hash|match_priority)\s*="))
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private static string ExtractSectionBodyWithoutHeader(string sectionText)
        {
            int idx = sectionText.IndexOf('\n');
            if (idx < 0) return string.Empty;
            string[] lines = sectionText.Substring(idx + 1).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var validLines = lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith(";") && !l.StartsWith("#"));
            return string.Join("\n", validLines);
        }

        private static bool IsCompanionSubfolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(
                name,
                @"(?i)(\b(ultimate|ult|henshin|extra|weapon|weapons|acc|accessory|accessories|option|options|addon|addons)\b|ultimate\d|大招|变身|二阶段|武器|配件)");
        }

        private static bool IsDualFormModName(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return false;
            return Regex.IsMatch(
                modId,
                @"(?i)(\b(ultimate|ult|henshin)\b|ultimate\d|大招|变身|二阶段)");
        }

        private static string ExtractTopLevelNamespace(string wholeText)
        {
            if (string.IsNullOrEmpty(wholeText)) return null;
            using var reader = new StringReader(wholeText);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trim = line.Trim();
                if (trim.Length == 0 || trim.StartsWith(";") || trim.StartsWith("#"))
                    continue;
                if (trim.StartsWith("["))
                    break;

                var m = Regex.Match(trim, @"^(?i)namespace\s*=\s*([^;#\r\n]+)");
                if (m.Success)
                {
                    string ns = m.Groups[1].Value.Trim();
                    return string.IsNullOrEmpty(ns) ? null : ns;
                }
            }
            return null;
        }

        private static bool TestPrivateNamespaceClassifier(IniSectionItem sr)
        {
            if (sr == null || string.IsNullOrWhiteSpace(sr.Namespace) || sr.Filter.HasValue || sr.HasPatternReplace)
                return false;

            string[] lines = (sr.Text ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool first = true;
            bool hasLocalStateWrite = false;

            foreach (string raw in lines)
            {
                if (first)
                {
                    first = false;
                    continue;
                }
                string trim = raw.Trim();
                if (trim.Length == 0 || trim.StartsWith(";") || trim.StartsWith("#"))
                    continue;

                if (Regex.IsMatch(trim, @"^(?i)(shader_model|temps)\s*="))
                    continue;

                if (Regex.IsMatch(trim, @"^(?i)(if\b|elif\b|else\s+if\b|else$|endif$)"))
                    continue;

                // Strictly local namespace variable assignment ($var = ... or post $var = ...), never $\OtherNs\var
                if (Regex.IsMatch(trim, @"^(?i)(?:post\s+)?\$[A-Za-z_][A-Za-z0-9_]*\s*="))
                {
                    hasLocalStateWrite = true;
                    continue;
                }

                return false;
            }

            return hasLocalStateWrite;
        }

        private static bool AnalyzeTagIsolatedReplace(IniSectionItem sr, out string guardTag)
        {
            guardTag = null;
            if (sr == null || string.IsNullOrWhiteSpace(sr.Namespace) || string.IsNullOrWhiteSpace(sr.ReplaceText))
                return false;

            // Expand literal "\n" escapes in .Pattern.Replace so each DXBC instruction sits on its own logical line
            string expanded = sr.ReplaceText.Replace("\\n", "\n");
            string[] lines = expanded.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int ifDepth = 0;
            bool sawGuardedBlock = false;
            string foundTag = null;

            foreach (string raw in lines)
            {
                string trim = raw.Trim();
                if (trim.Length == 0 || trim.StartsWith(";") || trim.StartsWith("#"))
                    continue;

                // Pure regex capture pass-through (e.g. ${header}, ${footer}, $0, $1)
                if (Regex.IsMatch(trim, @"^\$(?:\{[A-Za-z0-9_]+\}|\d+)$"))
                    continue;

                if (ifDepth == 0)
                {
                    if (Regex.IsMatch(trim, @"^(?i)if_(?:nz|z)\b"))
                    {
                        ifDepth = 1;
                        sawGuardedBlock = true;
                        continue;
                    }

                    // Allow read-only IniParams (t120) load into a regex-declared ${temp} register
                    if (Regex.IsMatch(trim, @"^(?i)ld(?:_indexable)?[^\r\n]*\$\{[A-Za-z0-9_]+\}\.[xyzw]+\s*,[^\r\n]*\bt120\b"))
                    {
                        continue;
                    }

                    // Allow equality check comparing ${temp} against a unique literal constant ID (>= 10)
                    var eqMatch = Regex.Match(
                        trim,
                        @"^(?i)(?:eq|ieq)\s+\$\{[A-Za-z0-9_]+\}\.[xyzw]+\s*,\s*(?:\$\{[A-Za-z0-9_]+\}\.[xyzw]+\s*,\s*l\((?<num>[-+]?\d+(?:\.\d+)?)\)|l\((?<num>[-+]?\d+(?:\.\d+)?)\)\s*,\s*\$\{[A-Za-z0-9_]+\}\.[xyzw]+)");
                    if (eqMatch.Success &&
                        double.TryParse(eqMatch.Groups["num"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double numVal) &&
                        Math.Abs(numVal) >= 10.0)
                    {
                        foundTag = numVal.ToString("0.######", CultureInfo.InvariantCulture);
                        continue;
                    }

                    // Any other unguarded instruction outside if_nz / if_z disqualifies tag isolation
                    return false;
                }
                else
                {
                    if (Regex.IsMatch(trim, @"^(?i)if_(?:nz|z)\b"))
                    {
                        ifDepth++;
                    }
                    else if (Regex.IsMatch(trim, @"^(?i)endif\b"))
                    {
                        ifDepth--;
                        if (ifDepth < 0) return false;
                    }
                }
            }

            if (sawGuardedBlock && ifDepth == 0 && !string.IsNullOrEmpty(foundTag))
            {
                guardTag = foundTag;
                return true;
            }

            return false;
        }

        private static string ExtractShaderTargetSignature(string shaderModel, string patternText)
        {
            string sm = (shaderModel ?? "unknown").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(patternText))
                return $"{sm}::*";

            string norm = patternText.Replace(@"\h", " ");
            var cbTokens = new List<string>();
            foreach (Match m in Regex.Matches(norm, @"(?i)dcl_constantbuffer\s+CB(?<slot>\\d\+|\d+)\[(?<size>[^\]]+)\]"))
            {
                string slot = m.Groups["slot"].Value;
                string size = Regex.Replace(m.Groups["size"].Value, @"\s+", "");
                // Normalize CB\d+[4086|4094] and CB2[4086|4094] to the same high-specificity CB size bucket when size >= 3 digits
                string slotKey = (slot == @"\d+" || size.Length >= 4) ? "CB*" : $"CB{slot}";
                cbTokens.Add($"{slotKey}[{size}]");
            }

            if (cbTokens.Count > 0)
            {
                var sorted = cbTokens.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                return $"{sm}::{string.Join("+", sorted)}";
            }

            var outTokens = new List<string>();
            foreach (Match m in Regex.Matches(norm, @"(?i)\bo(?<idx>\d+)\b"))
            {
                outTokens.Add($"o{m.Groups["idx"].Value}");
            }
            if (outTokens.Count > 0)
            {
                var sortedOut = outTokens.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                return $"{sm}::out({string.Join(",", sortedOut)})";
            }

            return $"{sm}::*";
        }

        public static ParsedIniFile ParseIni(FileInfo file, ModIdentity modIdentity, string scanRoot)
        {
            EncodedText loaded = ReadTextPreserveEncoding(file.FullName);
            string text = loaded.Text;
            string relPath = GetRelativePath(file.FullName, scanRoot);
            string fileNamespace = ExtractTopLevelNamespace(text);

            var parsed = new ParsedIniFile
            {
                FilePath = file.FullName,
                RelativePath = relPath,
                Namespace = fileNamespace,
                ModId = modIdentity.ModId,
                ModIsDisabled = modIdentity.IsDisabled,
                ModRootPath = modIdentity.ModRootPath,
                Loaded = loaded
            };

            var regexRootSections = new Dictionary<string, IniSectionItem>(StringComparer.OrdinalIgnoreCase);
            var regexPatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var regexReplaces = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var regexInsertDecls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var regexHasReplace = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in SectionRegex.Matches(text))
            {
                string name = m.Groups["name"].Value.Trim();
                string section = m.Value;
                int line = GetLineNumber(text, m.Index);

                if (name.StartsWith("ShaderOverride", StringComparison.OrdinalIgnoreCase))
                {
                    string hash = GetKeyValue(section, "hash")?.ToLowerInvariant();
                    // Ignore commented-out / empty [ShaderOverride] headers that have no active hash
                    if (string.IsNullOrEmpty(hash))
                    {
                        continue;
                    }
                    string filter = GetKeyValue(section, "filter_index");
                    string allowDup = GetKeyValue(section, "allow_duplicate_hash");

                    double? filterNum = null;
                    if (!string.IsNullOrEmpty(filter) &&
                        double.TryParse(filter, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    {
                        filterNum = d;
                    }

                    parsed.ShaderOverrides.Add(new IniSectionItem
                    {
                        Kind = "ShaderOverride",
                        Mod = modIdentity.ModId,
                        ModIsDisabled = modIdentity.IsDisabled,
                        File = file.FullName,
                        RelativeFile = relPath,
                        Namespace = fileNamespace,
                        Name = name,
                        Hash = hash,
                        Filter = filterNum,
                        FilterRaw = filter,
                        Overrule = allowDup != null && allowDup.Trim().Equals("overrule", StringComparison.OrdinalIgnoreCase),
                        AllowDuplicate = allowDup,
                        PureClassifier = TestPureClassifier(section),
                        Index = m.Index,
                        Length = m.Length,
                        Line = line,
                        Text = section
                    });
                }
                else if (name.StartsWith("ShaderRegex", StringComparison.OrdinalIgnoreCase))
                {
                    if (name.EndsWith(".Pattern", StringComparison.OrdinalIgnoreCase))
                    {
                        string rootName = name.Substring(0, name.Length - ".Pattern".Length);
                        regexPatterns[rootName] = ExtractSectionBodyWithoutHeader(section);
                    }
                    else if (name.EndsWith(".Pattern.Replace", StringComparison.OrdinalIgnoreCase))
                    {
                        string rootName = name.Substring(0, name.Length - ".Pattern.Replace".Length);
                        regexHasReplace.Add(rootName);
                        regexReplaces[rootName] = ExtractSectionBodyWithoutHeader(section);
                    }
                    else if (name.EndsWith(".InsertDeclarations", StringComparison.OrdinalIgnoreCase))
                    {
                        string rootName = name.Substring(0, name.Length - ".InsertDeclarations".Length);
                        regexInsertDecls[rootName] = ExtractSectionBodyWithoutHeader(section);
                    }
                    else
                    {
                        string filter = GetKeyValue(section, "filter_index");
                        string shaderModel = GetKeyValue(section, "shader_model");
                        double? filterNum = null;
                        if (!string.IsNullOrEmpty(filter) &&
                            double.TryParse(filter, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        {
                            filterNum = d;
                        }

                        var item = new IniSectionItem
                        {
                            Kind = "ShaderRegex",
                            Mod = modIdentity.ModId,
                            ModIsDisabled = modIdentity.IsDisabled,
                            File = file.FullName,
                            RelativeFile = relPath,
                            Namespace = fileNamespace,
                            Name = name,
                            Filter = filterNum,
                            FilterRaw = filter,
                            ShaderModel = shaderModel,
                            Index = m.Index,
                            Length = m.Length,
                            Line = line,
                            Text = section
                        };
                        parsed.ShaderRegexes.Add(item);
                        regexRootSections[name] = item;
                    }
                }
                else if (name.StartsWith("TextureOverride", StringComparison.OrdinalIgnoreCase))
                {
                    string hash = GetKeyValue(section, "hash")?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(hash))
                    {
                        parsed.TextureOverrides.Add(new IniSectionItem
                        {
                            Kind = "TextureOverride",
                            Mod = modIdentity.ModId,
                            ModIsDisabled = modIdentity.IsDisabled,
                            File = file.FullName,
                            RelativeFile = relPath,
                            Namespace = fileNamespace,
                            Name = name,
                            Hash = hash,
                            PureClassifier = TestPureClassifier(section),
                            Index = m.Index,
                            Length = m.Length,
                            Line = line,
                            Text = section
                        });
                    }
                }
            }

            // Link ShaderRegex patterns, replace bodies, private-namespace flags & tag-isolation fingerprints
            foreach (var sr in parsed.ShaderRegexes)
            {
                if (regexPatterns.TryGetValue(sr.Name, out string pat))
                {
                    sr.PatternText = pat;
                }
                if (regexInsertDecls.TryGetValue(sr.Name, out string insDecl))
                {
                    sr.InsertDeclText = insDecl;
                }
                if (regexHasReplace.Contains(sr.Name))
                {
                    sr.HasPatternReplace = true;
                    if (regexReplaces.TryGetValue(sr.Name, out string repBody))
                    {
                        sr.ReplaceText = repBody;
                    }
                }

                sr.IsPrivateNamespaceClassifier = TestPrivateNamespaceClassifier(sr);
                if (sr.HasPatternReplace && AnalyzeTagIsolatedReplace(sr, out string guardTag))
                {
                    sr.IsTagIsolatedReplace = true;
                    sr.IsolatedGuardTag = guardTag;
                }
            }

            return parsed;
        }

        private static readonly Regex MergedSkelSignatureRegex = new Regex(
            @"(?i)(MergeSkeleton|MergedBoneMatrixBuffer|MergedSkeleton)",
            RegexOptions.Compiled);

        private static readonly Regex CrossIBSignatureRegex = new Regex(
            @"(?i)(CrossIB|vs-cb[123]\s*=\s*(?:ref\s+)?Resource|Resource[^\r\n=]*=\s*(?:ref|copy)\s+vs-cb[123]|BoneMatrixBuffer|filter_index\s*=\s*20[0-5]|extract_bones|inject_bones)",
            RegexOptions.Compiled);

        public static ModArchType DetectModArchType(IEnumerable<ParsedIniFile> modFiles)
        {
            var fileList = modFiles.ToList();

            // Standalone Shader Patch / FX Plugin (e.g. RabbitFX, EFMITTL, KatFXMatcap, root CrossIBClassifier.ini):
            // Contains active [ShaderRegex] or [ShaderOverride] sections, and 0 real mesh/texture replacement [TextureOverride] hooks
            // (pure filter_index probes like [TextureOverrideEFMITTLNormalSlot_36d7856a] are ignored here)
            int realTO = fileList.Sum(f => f.TextureOverrides.Count(t => !t.PureClassifier));
            int totalSR = fileList.Sum(f => f.ShaderRegexes.Count);
            int totalSO = fileList.Sum(f => f.ShaderOverrides.Count);
            if (realTO == 0 && (totalSR > 0 || totalSO > 0))
            {
                return ModArchType.ShaderPatch;
            }

            var sb = new StringBuilder();
            foreach (var f in fileList)
            {
                if (f.Loaded?.Text == null) continue;
                using var reader = new StringReader(f.Loaded.Text);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trim = line.TrimStart();
                    if (trim.StartsWith(";") || trim.StartsWith("#")) continue;
                    sb.AppendLine(trim);
                }
            }

            string activeText = sb.ToString();
            if (MergedSkelSignatureRegex.IsMatch(activeText))
            {
                return ModArchType.MergedSkeleton;
            }
            if (CrossIBSignatureRegex.IsMatch(activeText))
            {
                return ModArchType.CrossIB;
            }

            return ModArchType.Simple;
        }

        private static List<InvolvedFileEntry> BuildFileEntries(IEnumerable<IniSectionItem> sections)
        {
            return sections
                .GroupBy(s => s.File, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    return new InvolvedFileEntry
                    {
                        ModId = first.Mod,
                        IsDisabled = first.ModIsDisabled,
                        ModType = first.ModType,
                        FilePath = first.File,
                        RelativePath = first.RelativeFile,
                        Lines = g.Select(x => x.Line).Distinct().OrderBy(x => x).ToList(),
                        SectionNames = g.Select(x => x.Name).Distinct().ToList()
                    };
                })
                .OrderBy(x => x.ModId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void PopulateModMetadata(ConflictItem item, IEnumerable<IniSectionItem> sections)
        {
            var secList = sections.ToList();
            item.InvolvedMods = secList.Select(s => s.Mod).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            foreach (var s in secList)
            {
                item.InvolvedModTypes.Add(s.ModType);
                if (s.ModIsDisabled)
                {
                    item.DisabledMods.Add(s.Mod);
                }
            }
            item.InvolvesDisabledMod = item.DisabledMods.Count > 0;
            item.FileEntries = BuildFileEntries(secList);
        }

        public static AuditResult RunAudit(string targetPath, bool includeDisabled)
        {
            string scanRoot = GetScanRoot(targetPath);
            var categoryCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            var result = new AuditResult
            {
                TargetPath = targetPath,
                ScanRoot = scanRoot,
                IncludedDisabled = includeDisabled,
                ScanTime = DateTime.Now
            };

            if (!Directory.Exists(scanRoot))
            {
                return result;
            }

            result.CacheStatus = InspectShaderCache(scanRoot);

            string modsSubDir = Path.Combine(scanRoot, "Mods");
            string shaderFixesSubDir = Path.Combine(scanRoot, "ShaderFixes");

            bool hasModsDir = Directory.Exists(modsSubDir);
            bool hasShaderFixesDir = Directory.Exists(shaderFixesSubDir);

            // If neither subdirectory exists in the selected path, do not scan anything.
            if (!hasModsDir && !hasShaderFixesDir)
            {
                return result;
            }

            result.ShaderFixesPath = shaderFixesSubDir;
            result.HasShaderFixesFolder = hasShaderFixesDir;
            if (hasShaderFixesDir)
            {
                try
                {
                    result.ShaderFixesFileCount = Directory
                        .EnumerateFiles(shaderFixesSubDir, "*.*", SearchOption.AllDirectories)
                        .Count(f =>
                        {
                            string n = Path.GetFileName(f).ToLowerInvariant();
                            return n != "desktop.ini" && n != "thumbs.db" && n != ".ds_store";
                        });
                }
                catch
                {
                    result.ShaderFixesFileCount = 0;
                }
            }

            var iniEntries = new List<FileInfo>();

            if (hasModsDir)
            {
                var modsDirInfo = new DirectoryInfo(modsSubDir);
                foreach (var fi in modsDirInfo.EnumerateFiles("*.ini", SearchOption.AllDirectories))
                {
                    if (!ShouldSkipIni(fi, modsSubDir, includeDisabled, out _))
                    {
                        iniEntries.Add(fi);
                    }
                }
            }

            var modStatusMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var fi in iniEntries)
            {
                var modIdent = ResolveModIdentity(fi.FullName, modsSubDir, categoryCache);
                var parsed = ParseIni(fi, modIdent, scanRoot);
                result.ParsedFiles.Add(parsed);

                if (!modStatusMap.ContainsKey(modIdent.ModId))
                {
                    modStatusMap[modIdent.ModId] = modIdent.IsDisabled;
                }
                else if (modIdent.IsDisabled)
                {
                    modStatusMap[modIdent.ModId] = true;
                }
            }

            result.TotalMods = modStatusMap.Count;
            result.DisabledMods = modStatusMap.Values.Count(v => v);
            result.ActiveMods = result.TotalMods - result.DisabledMods;

            // Classify each Mod's architecture type (MergedSkeleton / CrossIB / Simple) and propagate to all sections
            foreach (var modGroup in result.ParsedFiles
                .GroupBy(p => p.ModId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var archType = DetectModArchType(modGroup);
                result.ModTypes[modGroup.Key] = archType;
                var firstFile = modGroup.First();
                result.ScannedMods.Add(new ScannedModSummary
                {
                    ModId = modGroup.Key,
                    IsDisabled = modStatusMap.TryGetValue(modGroup.Key, out bool dis) && dis,
                    ModType = archType,
                    ModRootPath = firstFile.ModRootPath,
                    IniFileCount = modGroup.Count()
                });

                foreach (var pf in modGroup)
                {
                    pf.ModType = archType;
                    foreach (var s in pf.ShaderOverrides) s.ModType = archType;
                    foreach (var s in pf.ShaderRegexes) s.ModType = archType;
                    foreach (var s in pf.TextureOverrides) s.ModType = archType;
                }
            }

            var allSO = result.ParsedFiles.SelectMany(p => p.ShaderOverrides).ToList();
            var allSR = result.ParsedFiles.SelectMany(p => p.ShaderRegexes).ToList();
            var allTO = result.ParsedFiles.SelectMany(p => p.TextureOverrides).ToList();

            result.ShaderOverrideCount = allSO.Count;
            result.ShaderRegexCount = allSR.Count;
            result.TextureOverrideCount = allTO.Count;

            int idCounter = 1;

            // =========================================================================
            // A) ShaderOverride Warnings: Every Mod that contains [ShaderOverride]
            // =========================================================================
            var categoryAItems = new List<ConflictItem>();
            var soByMod = allSO
                .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var g in soByMod)
            {
                string modId = g.Key;
                var items = g.ToList();
                var modType = items.First().ModType;

                var filters = items
                    .Where(x => x.Filter.HasValue)
                    .Select(x => x.Filter.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var hashes = items
                    .Where(x => !string.IsNullOrEmpty(x.Hash))
                    .Select(x => x.Hash)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string hashPreview = hashes.Count == 0
                    ? "-"
                    : (hashes.Count <= 4
                        ? string.Join(", ", hashes)
                        : $"{string.Join(", ", hashes.Take(4))} ... (+{hashes.Count - 4})");

                string filterDescZh = filters.Count > 0
                    ? string.Join(", ", filters.Select(f => f.ToString(CultureInfo.InvariantCulture)))
                    : "未设置 (指令/状态覆盖)";
                string filterDescEn = filters.Count > 0
                    ? string.Join(", ", filters.Select(f => f.ToString(CultureInfo.InvariantCulture)))
                    : "None (Command/State Override)";

                bool hasFilter = filters.Count > 0;
                // Standard CrossIB pass classifier: Mod is CrossIB architecture and all filter_index values are in 200..205
                bool isStandardCrossIB = modType == ModArchType.CrossIB &&
                                         hasFilter &&
                                         filters.All(f => f >= 200.0 && f <= 205.0);

                // Standard RabbitFX tag (1718.1 / 1718.2), optionally combined with CrossIB 200..205 if CrossIB
                bool isStandardRabbitFX = hasFilter &&
                                          !isStandardCrossIB &&
                                          filters.All(f =>
                                              Math.Abs(f - 1718.1) < 1e-4 ||
                                              Math.Abs(f - 1718.2) < 1e-4 ||
                                              (modType == ModArchType.CrossIB && f >= 200.0 && f <= 205.0));

                string severity = (hasFilter && !isStandardCrossIB && !isStandardRabbitFX) ? "HIGH" : "WARNING";

                string reasonZh;
                string reasonEn;
                if (isStandardCrossIB)
                {
                    reasonZh = $"CrossIB 架构 Mod「{modId}」包含 {items.Count} 个用于跨骨骼阶段识别的 [ShaderOverride] 节（filter_index: {filterDescZh}）。这是旧版 CrossIB 正常运行所需的顶点着色器标记，通常不会影响 RabbitFX，仅在跨版本 Shader Hash 变动或与其他特殊 Mod 撞 Hash 时可能产生冲突。";
                    reasonEn = $"CrossIB Mod '{modId}' contains {items.Count} [ShaderOverride] section(s) for cross-skeleton pass detection (filter_index: {filterDescEn}). These vertex shader markers are required for legacy CrossIB to function and normally do not affect RabbitFX, only risking conflicts if shader hashes change across game updates or collide with custom Mods.";
                }
                else if (isStandardRabbitFX)
                {
                    reasonZh = $"Mod「{modId}」包含 {items.Count} 个使用 RabbitFX 标准编号的 [ShaderOverride] 节（filter_index: {filterDescZh}）。由于其写入的依然是 1718.1 / 1718.2 公共标准标签，通常可与 RabbitFX 兼容，仅在绑错着色器 Hash 或跨版本更新时可能产生异常。";
                    reasonEn = $"Mod '{modId}' contains {items.Count} [ShaderOverride] section(s) using standard RabbitFX tags (filter_index: {filterDescEn}). Since it assigns the standard 1718.1 / 1718.2 values, it is generally compatible with RabbitFX, only risking issues if bound to the wrong shader hash or across game updates.";
                }
                else if (hasFilter)
                {
                    reasonZh = $"Mod「{modId}」包含 {items.Count} 个自定义 [ShaderOverride] 节（filter_index: {filterDescZh}）。该设置在全局优先生效，极易覆盖并废除 RabbitFX (1718.1 / 1718.2) 等公共插件的动态正则标签，导致同屏其他角色浑身变黑或贴图错乱！";
                    reasonEn = $"Mod '{modId}' contains {items.Count} custom [ShaderOverride] section(s) (filter_index: {filterDescEn}). Since [ShaderOverride] takes global priority over [ShaderRegex], it can overwrite shared FX plugins like RabbitFX (1718.1 / 1718.2), causing other characters to turn black or have broken textures!";
                }
                else
                {
                    reasonZh = $"Mod「{modId}」包含 {items.Count} 个未设置 filter_index 的 [ShaderOverride] 节（涉及 Hash: {hashPreview}）。[ShaderOverride] 对全游戏所有使用该着色器 Hash 的物体全局生效，若包含 handling = skip 或全局指令覆盖，可能会影响其他角色或全局渲染。";
                    reasonEn = $"Mod '{modId}' contains {items.Count} [ShaderOverride] section(s) without filter_index (hashes: {hashPreview}). [ShaderOverride] applies globally to all objects using this shader hash; global commands or handling = skip may affect other characters or global rendering.";
                }

                string recommendZh;
                string recommendEn;
                if (isStandardCrossIB)
                {
                    recommendZh = "请先确认 EFMI\\Mods 目录下是否已放置 CrossIBClassifier.ini；若游戏内仍出现画面异常，请停用该 Mod（在文件夹名前加上 DISABLED_ 前缀）。";
                    recommendEn = "First verify whether CrossIBClassifier.ini is placed in the EFMI\\Mods directory; if visual glitches still occur in-game, please disable this Mod (add the DISABLED_ prefix to its folder).";
                }
                else if (hasFilter)
                {
                    recommendZh = "若游戏内出现画面异常（如角色变黑、贴图错乱或组件消失），请直接停用该 Mod（在文件夹名前加上 DISABLED_ 前缀）。";
                    recommendEn = "If visual glitches occur in-game (such as black models, broken textures, or missing parts), please disable this Mod (add the DISABLED_ prefix to its folder).";
                }
                else
                {
                    recommendZh = "若游戏内出现画面异常，请直接停用该 Mod（在文件夹名前加上 DISABLED_ 前缀）。";
                    recommendEn = "If visual glitches occur in-game, please disable this Mod (add the DISABLED_ prefix to its folder).";
                }

                var ci = new ConflictItem
                {
                    Id = $"A_{idCounter++}",
                    Category = "A",
                    Severity = severity,
                    TitleZh = $"ShaderOverride 警告: {modId} ({items.Count} 个节)",
                    TitleEn = $"ShaderOverride Warning: {modId} ({items.Count} section(s))",
                    SubtitleZh = $"filter_index: [{filterDescZh}]  |  Shader Hash: {hashPreview}",
                    SubtitleEn = $"filter_index: [{filterDescEn}]  |  Shader Hash: {hashPreview}",
                    ReasonZh = reasonZh,
                    ReasonEn = reasonEn,
                    RecommendZh = recommendZh,
                    RecommendEn = recommendEn,
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                categoryAItems.Add(ci);
            }

            // Sort Category A so HIGH items appear at the top, followed by WARNING items
            foreach (var ci in categoryAItems.OrderByDescending(x => x.Severity == "HIGH"))
            {
                result.AllFindings.Add(ci);
            }

            // =========================================================================
            // B) ShaderRegex Warnings: Every Mod (excluding FX Plugin / ShaderPatch)
            //    that contains [ShaderRegex] sections
            // =========================================================================
            var categoryBItems = new List<ConflictItem>();
            var srByMod = allSR
                .Where(x => x.ModType != ModArchType.ShaderPatch)
                .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var g in srByMod)
            {
                string modId = g.Key;
                var items = g.ToList();
                var modType = items.First().ModType;

                var filters = items
                    .Where(x => x.Filter.HasValue)
                    .Select(x => x.Filter.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                bool hasReplace = items.Any(x => x.HasPatternReplace);
                bool allPrivateClassifiers = items.All(x => x.IsPrivateNamespaceClassifier);
                bool allSafelyIsolated = items.All(x => x.IsPrivateNamespaceClassifier || x.IsTagIsolatedReplace);

                var namespaces = items
                    .Where(x => !string.IsNullOrWhiteSpace(x.Namespace))
                    .Select(x => x.Namespace)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var guardTags = items
                    .Where(x => x.IsTagIsolatedReplace && !string.IsNullOrWhiteSpace(x.IsolatedGuardTag))
                    .Select(x => x.IsolatedGuardTag)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var secNames = items.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                string secPreview = secNames.Count <= 3
                    ? string.Join(", ", secNames)
                    : $"{string.Join(", ", secNames.Take(3))} ... (+{secNames.Count - 3})";

                string filterDesc = filters.Count > 0
                    ? string.Join(", ", filters.Select(f => f.ToString(CultureInfo.InvariantCulture)))
                    : "-";

                string nsDescZh = namespaces.Count > 0 ? string.Join(", ", namespaces) : "未声明 (全局)";
                string nsDescEn = namespaces.Count > 0 ? string.Join(", ", namespaces) : "None (Global)";

                string severity;
                string titleZh;
                string titleEn;
                string subtitleZh;
                string subtitleEn;
                string reasonZh;
                string reasonEn;
                string recommendZh;
                string recommendEn;

                if (allPrivateClassifiers)
                {
                    severity = "INFO";
                    titleZh = $"ShaderRegex 提示 (私有命名空间分类器): {modId} ({items.Count} 个节)";
                    titleEn = $"ShaderRegex Notice (Private Namespace Classifier): {modId} ({items.Count} section(s))";
                    subtitleZh = $"namespace: [{nsDescZh}]  |  filter_index: [-]  |  仅写入私有状态变量";
                    subtitleEn = $"namespace: [{nsDescEn}]  |  filter_index: [-]  |  Private state only";
                    reasonZh = $"Mod「{modId}」内置了声明独立私有命名空间（{nsDescZh}）的阶段识别分类器（共 {items.Count} 个节），未占用公共 filter_index，也未修改着色器字节码。该写法在原理上不会与 RabbitFX 或公共 CrossIBClassifier.ini 产生冲突。";
                    reasonEn = $"Mod '{modId}' uses private-namespace [ShaderRegex] state classifier(s) ({nsDescEn}) without writing shared filter_index or patching shader bytecode. By design, it cannot conflict with RabbitFX or shared CrossIBClassifier.ini.";
                    recommendZh = "合规的私有命名空间写法，可安全共存，无需处理；切勿删除或禁用该分类器文件（如 SlotClassifier.ini / 带私有 namespace 的 CrossIBClassifier.ini），否则会导致该 Mod 阶段识别失效。";
                    recommendEn = "Compliant private-namespace pattern — safe to keep as-is. Do NOT delete this Mod's internal classifier INI, or its pass detection will break.";
                }
                else if (allSafelyIsolated && hasReplace)
                {
                    severity = "WARNING";
                    string tagDesc = guardTags.Count > 0 ? string.Join(", ", guardTags) : "-";
                    titleZh = $"ShaderRegex 提示 (独立 ID 隔离注入): {modId} ({items.Count} 个节)";
                    titleEn = $"ShaderRegex Warning (Tag-Isolated Injection): {modId} ({items.Count} section(s))";
                    subtitleZh = $"namespace: [{nsDescZh}]  |  隔离常量 ID: [{tagDesc}]  |  含 Pattern.Replace: 是 (if_nz 自保护)";
                    subtitleEn = $"namespace: [{nsDescEn}]  |  Guard ID: [{tagDesc}]  |  Pattern.Replace: Yes (if_nz guarded)";
                    reasonZh = $"Mod「{modId}」包含带独立命名空间（{nsDescZh}）与唯一常量 ID（{tagDesc}）条件保护的着色器字节码注入（如独立 CutoutMask.ini 透明遮罩）及私有分类器。运行时仅对该 Mod 自身网格生效，少量开启可与公共前置插件正常共存，但每份注入仍会向匹配的着色器增加少量寄存器与指令开销。";
                    reasonEn = $"Mod '{modId}' contains [ShaderRegex.*.Pattern.Replace] bytecode injection guarded by private namespace ({nsDescEn}) and unique constant ID ({tagDesc}, e.g. standalone CutoutMask.ini). Runtime execution is isolated to this Mod's own draws and coexists normally in small numbers, with minor per-shader instruction overhead.";
                    recommendZh = "常规游玩可正常兼容共存，无需停用该 Mod，也请勿删除其内部的 CutoutMask.ini / SlotClassifier.ini 等功能文件；仅在同时开启过多同类型注入 Mod 导致加载卡顿或画面异常时再行排查。";
                    recommendEn = "Compatible in normal use — no need to disable this Mod, and do NOT delete its internal CutoutMask.ini / SlotClassifier.ini files. Only investigate if stacking many similar Mods causes shader load stutter or glitches.";
                }
                else
                {
                    severity = (modType != ModArchType.CrossIB && (hasReplace || filters.Count > 0)) ? "HIGH" : "WARNING";
                    titleZh = $"ShaderRegex 警告: {modId} ({items.Count} 个节)";
                    titleEn = $"ShaderRegex Warning: {modId} ({items.Count} section(s))";
                    subtitleZh = $"包含节: {secPreview}  |  namespace: [{nsDescZh}]  |  filter_index: [{filterDesc}]  |  含 Pattern.Replace: {(hasReplace ? "是" : "否")}";
                    subtitleEn = $"Sections: {secPreview}  |  namespace: [{nsDescEn}]  |  filter_index: [{filterDesc}]  |  Has Pattern.Replace: {(hasReplace ? "Yes" : "No")}";
                    reasonZh = $"非前置插件 Mod「{modId}」包含 {items.Count} 个未完全隔离的 [ShaderRegex] 正则匹配节（占用公共 filter_index 或包含无唯一常量隔离的字节码替换），易与公共前置插件（如 RabbitFX / CrossIBClassifier）或其他 Mod 发生冲突。";
                    reasonEn = $"Non-FX-Plugin Mod '{modId}' contains {items.Count} unisolated [ShaderRegex] section(s) (writing shared filter_index or unguarded Pattern.Replace), which may conflict with shared FX plugins (such as RabbitFX / CrossIBClassifier) or other Mods.";
                    recommendZh = "若游戏内出现画面异常，请停用该 Mod（在文件夹名前加上 DISABLED_ 前缀），或将其改造为私有命名空间 / [CustomShader] 方案。";
                    recommendEn = "If visual glitches occur in-game, disable this Mod (add the DISABLED_ prefix to its folder) or refactor it to use private namespaces / [CustomShader].";
                }

                var ci = new ConflictItem
                {
                    Id = $"B_{idCounter++}",
                    Category = "B",
                    Severity = severity,
                    TitleZh = titleZh,
                    TitleEn = titleEn,
                    SubtitleZh = subtitleZh,
                    SubtitleEn = subtitleEn,
                    ReasonZh = reasonZh,
                    ReasonEn = reasonEn,
                    RecommendZh = recommendZh,
                    RecommendEn = recommendEn,
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                categoryBItems.Add(ci);
            }

            foreach (var ci in categoryBItems.OrderBy(x => SeverityRank(x.Severity)))
            {
                result.AllFindings.Add(ci);
            }

            // =========================================================================
            // C) Duplicate ShaderRegex Conflict Detection (正则匹配器重复)
            // =========================================================================
            var categoryCItems = new List<ConflictItem>();

            // C1: Same filter_index declared by ShaderRegex in multiple different Mods
            var srFilterGroups = allSR
                .Where(x => x.Filter.HasValue)
                .GroupBy(x => x.Filter.Value);

            foreach (var g in srFilterGroups)
            {
                var items = g.ToList();
                var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (mods.Count < 2) continue;

                string fVal = g.Key.ToString(CultureInfo.InvariantCulture);
                var sharedPluginFiles = items
                    .Where(x => x.ModType == ModArchType.ShaderPatch)
                    .Select(x => x.RelativeFile)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var bundledCharFiles = items
                    .Where(x => x.ModType != ModArchType.ShaderPatch && string.IsNullOrWhiteSpace(x.Namespace))
                    .Select(x => x.RelativeFile)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string recommendZh;
                string recommendEn;
                if (sharedPluginFiles.Count > 0 && bundledCharFiles.Count > 0)
                {
                    string keepList = string.Join(", ", sharedPluginFiles);
                    string dropList = string.Join(", ", bundledCharFiles);
                    recommendZh = $"检测到角色 Mod 内部夹带了与公共分类器相同的无命名空间冗余副本！建议保留公共前置文件（{keepList}），直接删除或加 DISABLED_ 前缀停用角色目录内的冗余副本（{dropList}）。";
                    recommendEn = $"Detected redundant non-namespaced copy of a shared classifier inside character Mod folder(s)! Keep the shared plugin ({keepList}) and safely delete or prefix DISABLED_ on the duplicate copy ({dropList}).";
                }
                else
                {
                    recommendZh = "建议保留唯一的公共 ShaderRegex 提供者（如根目录 CrossIBClassifier.ini 或 RabbitFX），将其他角色 Mod 内部重复写入相同公共 filter_index 的冗余分类器副本删除或禁用。";
                    recommendEn = "Keep a single shared ShaderRegex provider (such as root CrossIBClassifier.ini or RabbitFX) and remove/disable duplicate non-namespaced copies writing the same shared filter_index.";
                }

                var ci = new ConflictItem
                {
                    Id = $"C_Filter_{idCounter++}",
                    Category = "C",
                    Severity = "HIGH",
                    TitleZh = $"正则匹配器重复 · 共享 filter_index = {fVal}",
                    TitleEn = $"Duplicate ShaderRegex · Shared filter_index = {fVal}",
                    SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}",
                    SubtitleEn = $"Involved Mods ({mods.Count}): {string.Join(" ↔ ", mods)}",
                    ReasonZh = $"多个不同的 Mod 同时定义了写入相同公共 filter_index ({fVal}) 的 [ShaderRegex]。这通常是因为角色 Mod 私自打包了一份全局分类器的冗余副本，与公共分类器产生重复正则匹配开销，且若匹配规则不一致会互相污染公共 filter 桶。",
                    ReasonEn = $"Multiple Mods define [ShaderRegex] assigning the same shared filter_index ({fVal}). Usually caused by a character Mod bundling a redundant copy of a global classifier, adding duplicate regex overhead and risking filter bucket pollution if patterns diverge.",
                    RecommendZh = recommendZh,
                    RecommendEn = recommendEn,
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                categoryCItems.Add(ci);
            }

            // Track sections already covered in C2 so C3 never double-counts them
            var c2ReportedReplaceSections = new HashSet<IniSectionItem>();

            // C2: Identical ShaderRegex .Pattern across multiple Mods
            var srPatternGroups = allSR
                .Where(x => !string.IsNullOrWhiteSpace(x.PatternText))
                .GroupBy(x => NormalizePatternForComparison(x.PatternText), StringComparer.Ordinal);

            foreach (var g in srPatternGroups)
            {
                var items = g.ToList();
                var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (mods.Count < 2) continue;

                // Avoid duplicating an exact group already reported by C1 if all items had the same filter_index
                if (items.All(x => x.Filter.HasValue) && items.Select(x => x.Filter.Value).Distinct().Count() == 1)
                {
                    continue;
                }

                bool hasReplace = items.Any(x => x.HasPatternReplace);
                string sampleName = items.First().Name;

                // Case C2-A: Pure private-namespace classifiers (e.g. SlotClassifier.ini / ASlot_xxxxx CrossIBClassifier.ini)
                bool allPrivateNsClassifiers = !hasReplace &&
                    items.All(x => x.IsPrivateNamespaceClassifier) &&
                    AreNamespacesDistinctAcrossMods(items);

                // Case C2-B: Tag-isolated Pattern.Replace (e.g. standalone CutoutMask.ini with unique eq + if_nz constant per Mod)
                var replaceItems = items.Where(x => x.HasPatternReplace).ToList();
                bool allTagIsolatedReplace = hasReplace &&
                    items.All(x => x.IsTagIsolatedReplace) &&
                    AreReplaceSectionsUniquelyTagIsolated(replaceItems);

                if (hasReplace)
                {
                    foreach (var ri in replaceItems)
                    {
                        c2ReportedReplaceSections.Add(ri);
                    }
                }

                string severity;
                string titleZh;
                string titleEn;
                string subtitleZh;
                string subtitleEn;
                string reasonZh;
                string reasonEn;
                string recommendZh;
                string recommendEn;

                if (allPrivateNsClassifiers)
                {
                    var nsList = items.Select(x => x.Namespace).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    severity = "INFO";
                    titleZh = $"正则模板复用 · 私有命名空间隔离 ([{sampleName}])";
                    titleEn = $"Shared Regex Template · Private Namespace Isolated ([{sampleName}])";
                    subtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  独立 namespace ({nsList.Count}): {string.Join(", ", nsList.Take(4))}{(nsList.Count > 4 ? " ..." : "")}";
                    subtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  Private namespaces ({nsList.Count}): {string.Join(", ", nsList.Take(4))}{(nsList.Count > 4 ? " ..." : "")}";
                    reasonZh = $"多个 Mod 使用了相同的着色器阶段识别正则模板，但各自均声明了独立的私有 namespace 且未写入公共 filter_index。各 Mod 状态变量彼此完全隔离，不会产生冲突（仅在首次加载着色器时有轻微重复正则匹配开销）。";
                    reasonEn = $"Multiple Mods reuse the same shader classification regex template, but each declares its own private namespace without writing shared filter_index. State variables are strictly isolated with zero conflict risk (only minor startup regex matching overhead).";
                    recommendZh = "这些文件并非公共插件的冗余副本，而是各 Mod 自身必需的私有分类器——切勿删除各 Mod 内部的该文件（如 SlotClassifier.ini 或带独立 namespace 的 CrossIBClassifier.ini），否则会导致该 Mod 渲染异常！正常共存即可，无需处理。";
                    recommendEn = "These are NOT redundant copies of a public plugin, but private classifiers required by each Mod — do NOT delete them, or the Mod's pass detection will fail! Safe to leave as-is.";
                }
                else if (allTagIsolatedReplace)
                {
                    var tags = replaceItems.Select(x => x.IsolatedGuardTag).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    string tagSummary = string.Join(", ", tags.Take(5)) + (tags.Count > 5 ? $" ... (+{tags.Count - 5})" : "");
                    bool heavyStacking = mods.Count >= 6;
                    severity = heavyStacking ? "HIGH" : "WARNING";
                    titleZh = heavyStacking
                        ? $"正则字节码叠加过多 · 唯一 ID 隔离 ([{sampleName}])"
                        : $"正则字节码注入叠加 · 已做唯一 ID 隔离 ([{sampleName}])";
                    titleEn = heavyStacking
                        ? $"Heavy Tag-Isolated Regex ASM Stacking ([{sampleName}])"
                        : $"Stacked Regex ASM Injection · Tag-Isolated ([{sampleName}])";
                    subtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  唯一隔离 ID: [{tagSummary}]";
                    subtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  Unique Guard IDs: [{tagSummary}]";
                    reasonZh = $"检测到 {mods.Count} 个 Mod 对同一类着色器注入了字节码（如独立 CutoutMask.ini 透明遮罩），但各自均使用独立 namespace 与互不相同的唯一常量 ID（{tagSummary}）包裹在 if_nz 条件内，运行时逻辑互不污染。每增加一份此类正则会在命中的着色器上叠加约 4 个临时寄存器与十余条指令{(heavyStacking ? "，当前同时开启数量较多，可能引起首次切图编译卡顿或寄存器压力。" : "，当前数量较少可稳定共存。")}";
                    reasonEn = $"Detected {mods.Count} Mods injecting ASM bytecode into the same shader pattern (e.g. standalone CutoutMask.ini), each isolated by a distinct namespace and unique if_nz constant ID ({tagSummary}) so runtime behavior does not cross-contaminate. Each copy adds ~4 temp registers and ~15 instructions per matched shader{(heavyStacking ? "; with 6+ active copies, shader compilation stalls or register pressure may occur." : ", which coexists safely at low counts.")}";
                    recommendZh = heavyStacking
                        ? "请勿直接删除角色目录内的 CutoutMask.ini（否则透明遮罩失效）；若遇到切图卡顿或材质异常，建议减少同时开启的同类型独立 CutoutMask Mod 数量，或优先选用基于 EFMITTL 共享插件的版本。"
                        : "已通过唯一常量 ID 自保护隔离，当前数量下可正常兼容共存——请勿删除 CutoutMask.ini 或停用上述 Mod。";
                    recommendEn = heavyStacking
                        ? "Do NOT delete CutoutMask.ini inside active Mods (or alpha masks will break); if you experience shader compilation stutter, reduce the number of simultaneously enabled standalone CutoutMask Mods or prefer EFMITTL-based versions."
                        : "Safely isolated via unique constant IDs — compatible in normal gameplay. Do NOT delete CutoutMask.ini or disable these Mods.";
                }
                else
                {
                    severity = hasReplace ? "HIGH" : "WARNING";
                    titleZh = hasReplace
                        ? $"正则匹配器重复 · 未隔离字节码注入 ([{sampleName}])"
                        : $"正则匹配器重复 ([{sampleName}])";
                    titleEn = hasReplace
                        ? $"Duplicate Unguarded Regex ASM Injection ([{sampleName}])"
                        : $"Duplicate ShaderRegex Pattern ([{sampleName}])";
                    subtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  含 Pattern.Replace: {(hasReplace ? "是 (未隔离/ID冲突)" : "否")}";
                    subtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  Has Pattern.Replace: {(hasReplace ? "Yes (Unguarded/Collision)" : "No")}";
                    reasonZh = hasReplace
                        ? $"多个 Mod 对相同的着色器结构执行了 [ShaderRegex.*.Pattern.Replace] 动态字节码替换，且未检测到互不冲突的唯一常量 if_nz 隔离保护（或存在相同 ID 撞号）。同时开启极易导致着色器指令互相覆写、贴图错乱甚至游戏崩溃！"
                        : $"多个 Mod 内置了相同的未隔离 [ShaderRegex] 正则匹配模式，在加载着色器时会产生重复匹配开销，若共享命名空间则可能互相覆盖状态。";
                    reasonEn = hasReplace
                        ? $"Multiple Mods apply [ShaderRegex.*.Pattern.Replace] ASM patching to the same shader pattern without mutually distinct if_nz constant tag isolation (or have colliding IDs). Enabling them together risks instruction clobbering, broken materials, or crashes!"
                        : $"Multiple Mods bundle identical unisolated [ShaderRegex] patterns, adding redundant matching overhead and risking state overwrite if namespaces are shared.";
                    recommendZh = hasReplace
                        ? "若游戏内出现材质发黑、透明失效或崩溃，请避免同时开启上述存在字节码冲突的 Mod，或将其迁移至 EFMITTL / [CustomShader] 方案。"
                        : "请检查其中是否包含与根目录公共分类器完全一致的无 namespace 冗余拷贝；若文件顶部声明了私有 namespace 则请勿删除。";
                    recommendEn = hasReplace
                        ? "If material glitches or crashes occur, avoid enabling these conflicting bytecode-patching Mods simultaneously, or migrate them to EFMITTL / [CustomShader]."
                        : "Check whether any file is a non-namespaced duplicate of a root shared classifier; do NOT delete files that declare their own private namespace.";
                }

                var ci = new ConflictItem
                {
                    Id = $"C_Pat_{idCounter++}",
                    Category = "C",
                    Severity = severity,
                    TitleZh = titleZh,
                    TitleEn = titleEn,
                    SubtitleZh = subtitleZh,
                    SubtitleEn = subtitleEn,
                    ReasonZh = reasonZh,
                    ReasonEn = reasonEn,
                    RecommendZh = recommendZh,
                    RecommendEn = recommendEn,
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                categoryCItems.Add(ci);
            }

            // C3: Cross-Mod [ShaderRegex.*.Pattern.Replace] targeting the same shader signature bucket
            //     (excludes sections already reported in C2, and compares normalized CB/output constraints rather than bare shader_model)
            var remainingReplaceItems = allSR
                .Where(x => x.HasPatternReplace && x.ModType != ModArchType.ShaderPatch && !c2ReportedReplaceSections.Contains(x))
                .ToList();

            var replaceByTargetBucket = remainingReplaceItems
                .GroupBy(x => ExtractShaderTargetSignature(x.ShaderModel, x.PatternText), StringComparer.OrdinalIgnoreCase);

            foreach (var g in replaceByTargetBucket)
            {
                var items = g.ToList();
                var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (mods.Count < 2) continue;

                bool allTagIsolated = items.All(x => x.IsTagIsolatedReplace) &&
                                      AreReplaceSectionsUniquelyTagIsolated(items);
                bool heavyStacking = mods.Count >= 6;
                string severity = (!allTagIsolated || heavyStacking) ? "HIGH" : "WARNING";

                var tags = items
                    .Where(x => !string.IsNullOrEmpty(x.IsolatedGuardTag))
                    .Select(x => x.IsolatedGuardTag)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                string tagSummary = tags.Count > 0 ? string.Join(", ", tags.Take(5)) : "-";

                var ci = new ConflictItem
                {
                    Id = $"C_ReplaceStack_{idCounter++}",
                    Category = "C",
                    Severity = severity,
                    TitleZh = allTagIsolated
                        ? $"正则字节码注入叠加 · 已做唯一 ID 隔离 ({g.Key})"
                        : $"正则匹配器重复 · 多重字节码注入叠加 ({g.Key})",
                    TitleEn = allTagIsolated
                        ? $"Stacked Regex ASM Injection · Tag-Isolated ({g.Key})"
                        : $"Duplicate ShaderRegex.Pattern.Replace Stacking ({g.Key})",
                    SubtitleZh = allTagIsolated
                        ? $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  唯一隔离 ID: [{tagSummary}]"
                        : $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  共 {items.Count} 个未完全隔离字节码节",
                    SubtitleEn = allTagIsolated
                        ? $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  Unique Guard IDs: [{tagSummary}]"
                        : $"Involved Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  {items.Count} unisolated replace sections",
                    ReasonZh = allTagIsolated
                        ? $"检测到 {mods.Count} 个不同 Mod 对同一着色器特征族（{g.Key}）注入了字节码，但各自均通过独立 namespace 与唯一常量 ID（{tagSummary}）包裹在 if_nz 内实现了运行时逻辑隔离。{(heavyStacking ? "当前叠加份数较多，可能引起首次加载编译卡顿。" : "当前开启份数较少，可正常兼容运行。")}"
                        : $"检测到 {mods.Count} 个不同 Mod 同时对同一着色器特征族（{g.Key}）使用了未做唯一 ID 隔离保护的 [ShaderRegex.*.Pattern.Replace] 动态字节码替换，可能导致多命名空间指令冲突、渲染异常或崩溃。",
                    ReasonEn = allTagIsolated
                        ? $"Detected {mods.Count} Mods injecting ASM into the same shader target family ({g.Key}), each isolated via private namespace and unique if_nz constant ID ({tagSummary}). {(heavyStacking ? "High active count may cause compilation stalls." : "Coexists normally at low counts.")}"
                        : $"Detected {mods.Count} Mods applying unisolated [ShaderRegex.*.Pattern.Replace] patching to the same shader family ({g.Key}), risking ASM instruction conflicts, visual glitches, or crashes.",
                    RecommendZh = allTagIsolated
                        ? (heavyStacking
                            ? "请勿删除角色目录内的 CutoutMask.ini；若切图卡顿可减少同时开启的同类 Mod 数量。"
                            : "已通过唯一常量 ID 隔离，常规游玩可正常共存，请勿删除内部功能文件或停用 Mod。")
                        : "若出现材质异常或崩溃，建议避免同时开启上述存在未隔离字节码重叠的 Mod。",
                    RecommendEn = allTagIsolated
                        ? (heavyStacking
                            ? "Do NOT delete CutoutMask.ini; reduce simultaneously active standalone CutoutMask Mods if stutter occurs."
                            : "Isolated via unique constant IDs — safe in normal gameplay. Do NOT delete internal INI files or disable these Mods.")
                        : "If visual glitches or crashes occur, avoid enabling these overlapping unguarded bytecode-patching Mods together.",
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                categoryCItems.Add(ci);
            }

            // C4: KatFXMatcap + RabbitFX simultaneous active conflict detection
            var katFxItems = allSR
                .Where(x =>
                    string.Equals(x.Name, "ShaderRegexMatcap", StringComparison.OrdinalIgnoreCase) ||
                    x.Mod.IndexOf("KatFXMatcap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (x.Text != null && x.Text.IndexOf("matcap_norm", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            var rabbitFxItems = allSR
                .Where(x =>
                    x.Mod.IndexOf("RabbitFX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    x.RelativeFile.IndexOf("RabbitFX.ini", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (string.Equals(x.Name, "ShaderRegexMain", StringComparison.OrdinalIgnoreCase) &&
                     x.Filter.HasValue && Math.Abs(x.Filter.Value - 1718.1) < 1e-4))
                .ToList();

            bool hasActiveKatFx = katFxItems.Any(x => !x.ModIsDisabled);
            bool hasActiveRabbitFx = rabbitFxItems.Any(x => !x.ModIsDisabled);
            if (hasActiveKatFx && hasActiveRabbitFx)
            {
                var activeKatFx = katFxItems.Where(x => !x.ModIsDisabled).ToList();
                var activeRabbitFx = rabbitFxItems.Where(x => !x.ModIsDisabled).ToList();

                var previewRabbitFx = activeRabbitFx
                    .Where(x => string.Equals(x.Name, "ShaderRegexMain", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (previewRabbitFx.Count == 0)
                {
                    previewRabbitFx = activeRabbitFx.Take(1).ToList();
                }

                var combinedPreviewSections = activeKatFx.Concat(previewRabbitFx).ToList();
                var allInvolvedSections = activeKatFx.Concat(activeRabbitFx).ToList();
                var involvedMods = allInvolvedSections
                    .Select(x => x.Mod)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var ci = new ConflictItem
                {
                    Id = $"C_KatFX_RabbitFX_{idCounter++}",
                    Category = "C",
                    Severity = "HIGH",
                    TitleZh = "前置插件冲突: KatFXMatcap ↔ RabbitFX",
                    TitleEn = "FX Plugin Conflict: KatFXMatcap ↔ RabbitFX",
                    SubtitleZh = $"涉及 Mod ({involvedMods.Count}): {string.Join(" ↔ ", involvedMods)}  |  同时开启存在着色器正则冲突",
                    SubtitleEn = $"Involved Mods ({involvedMods.Count}): {string.Join(" ↔ ", involvedMods)}  |  Incompatible when enabled together",
                    ReasonZh = "检测到 KatFXMatcap 与 RabbitFX 同时处于启用状态。由于 3DMigoto 按字母顺序执行 [ShaderRegex]，KatFXMatcap 会在 RabbitFX 之前向角色像素着色器注入输出混合指令，导致 RabbitFX 的正则匹配器误劫持 KatFXMatcap 的指令分支，引发角色贴图失效、材质发黑或光效异常。",
                    ReasonEn = "Detected both KatFXMatcap and RabbitFX enabled simultaneously. Because 3DMigoto executes [ShaderRegex] sections in alphabetical order, KatFXMatcap injects output blending instructions before RabbitFX runs, causing RabbitFX's regex pattern to hijack KatFXMatcap's branch and resulting in broken textures, black materials, or lighting glitches.",
                    RecommendZh = "这两个前置插件无法同时正常运作，请关闭其中一个（在不需要的前置插件文件夹名前加上 DISABLED_ 前缀）。",
                    RecommendEn = "These two FX plugins conflict with each other when enabled together. Please disable one of them (add the DISABLED_ prefix to its folder name).",
                    Sections = combinedPreviewSections
                };
                PopulateModMetadata(ci, allInvolvedSections);
                categoryCItems.Add(ci);
            }

            foreach (var ci in categoryCItems.OrderBy(x => SeverityRank(x.Severity)))
            {
                result.AllFindings.Add(ci);
            }

            // =========================================================================
            // D) Same-Character Duplicate Active Mod Detection (同角色重复开启检测)
            //    ONLY checked and recorded when includeDisabled == false!
            // =========================================================================
            if (!includeDisabled)
            {
                var realTO = allTO.Where(x => !string.IsNullOrEmpty(x.Hash) && !x.PureClassifier).ToList();
                var modHashSets = realTO
                    .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => new HashSet<string>(g.Select(x => x.Hash), StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase);

                var dualFormBuckets = new Dictionary<string, (string modA, string modB, List<string> hashes, List<IniSectionItem> sections)>(StringComparer.OrdinalIgnoreCase);

                var toGroups = realTO.GroupBy(x => x.Hash, StringComparer.OrdinalIgnoreCase);

                foreach (var g in toGroups)
                {
                    var items = g.ToList();
                    var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
                    if (mods.Count < 2) continue;

                    // Detect complementary Normal + Ultimate/Transformation dual-form companion pair
                    if (mods.Count == 2 && IsDualFormCompanionPair(mods[0], mods[1], modHashSets))
                    {
                        string pairKey = $"{mods[0]}||{mods[1]}";
                        if (!dualFormBuckets.TryGetValue(pairKey, out var bucket))
                        {
                            bucket = (mods[0], mods[1], new List<string>(), new List<IniSectionItem>());
                            dualFormBuckets[pairKey] = bucket;
                        }
                        bucket.hashes.Add(g.Key);
                        bucket.sections.AddRange(items);
                        continue;
                    }

                    var ci = new ConflictItem
                    {
                        Id = $"D_{idCounter++}",
                        Category = "D",
                        Severity = "INFO",
                        TitleZh = $"同角色重复开启检测: {g.Key}",
                        TitleEn = $"Same-Character Multi-Mod Overlap: {g.Key}",
                        SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}",
                        SubtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}",
                        ReasonZh = $"多个已启用的 Mod 同时覆写了相同的模型缓冲区 (IB/VB) 或贴图 Hash ({g.Key})。若这些 Mod 属于同一个角色的不同皮肤，同时开启会导致模型破损或互相重叠。",
                        ReasonEn = $"Multiple active Mods override the same IB/VB/Texture resource hash ({g.Key}). If these Mods are different skins for the same character, enabling them simultaneously causes mesh overlap or glitches.",
                        RecommendZh = "请人工确认涉及的 Mod 是否为同一角色的不同皮肤：若是重复开启的多余皮肤，请为其中一个文件夹加上 DISABLED_ 前缀禁用；若属于不同角色，或属于同一角色的「常态 + 大招 / 独立配件」配套拆分包，则无需处理。",
                        RecommendEn = "Verify whether these Mods are duplicate skins for the same character (if so, disable one with the DISABLED_ prefix); if they belong to different characters or are complementary normal/ultimate/accessory sub-packages, no action is needed.",
                        Sections = items
                    };
                    PopulateModMetadata(ci, items);
                    result.AllFindings.Add(ci);
                }

                foreach (var kv in dualFormBuckets.Values)
                {
                    string hashPreview = kv.hashes.Count <= 5
                        ? string.Join(", ", kv.hashes)
                        : $"{string.Join(", ", kv.hashes.Take(5))} ... (+{kv.hashes.Count - 5})";

                    var ci = new ConflictItem
                    {
                        Id = $"D_DualForm_{idCounter++}",
                        Category = "D",
                        Severity = "INFO",
                        TitleZh = $"同角色双形态配套提示 (共享 {kv.hashes.Count} 个局部 Hash)",
                        TitleEn = $"Dual-Form Companion Mods Notice ({kv.hashes.Count} shared sub-hashes)",
                        SubtitleZh = $"常态与变身配套: {kv.modA} ↔ {kv.modB}  |  共享局部 Hash: {hashPreview}",
                        SubtitleEn = $"Companion Pair: {kv.modA} ↔ {kv.modB}  |  Shared sub-hashes: {hashPreview}",
                        ReasonZh = $"检测到同一角色的「常态形态」与「大招/变身形态」配套 Mod 同时启用。两者拥有各自独立的主体服饰网格 Hash，仅在面部/五官等 {kv.hashes.Count} 个通用局部组件（{hashPreview}）上存在正常交集。",
                        ReasonEn = $"Detected complementary Normal-form and Ultimate/Transformation-form Mods enabled together. Each targets its own distinct primary outfit meshes and only shares {kv.hashes.Count} common sub-component hash(es) ({hashPreview}, such as face/eyelashes).",
                        RecommendZh = "此为正常的「常态 + 大招变身」双形态拆分设计，游玩时需同时保持启用——请勿禁用其中任何一个文件夹，否则对应形态将退回原版模型。",
                        RecommendEn = "This is an intentional Normal + Ultimate dual-form split setup and both folders should remain enabled — do NOT disable either one, or that form will revert to the vanilla model.",
                        Sections = kv.sections
                    };
                    PopulateModMetadata(ci, kv.sections);
                    result.AllFindings.Add(ci);
                }
            }

            return result;
        }

        private static int SeverityRank(string severity)
        {
            return (severity?.ToUpperInvariant()) switch
            {
                "CRITICAL" => 0,
                "HIGH" => 1,
                "WARNING" => 2,
                "MEDIUM" => 2,
                "INFO" => 3,
                _ => 4
            };
        }

        private static bool AreNamespacesDistinctAcrossMods(IEnumerable<IniSectionItem> items)
        {
            var list = items.ToList();
            if (list.Any(x => string.IsNullOrWhiteSpace(x.Namespace)))
                return false;

            return list
                .GroupBy(x => x.Namespace.Trim(), StringComparer.OrdinalIgnoreCase)
                .All(g => g.Select(x => x.File).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1);
        }

        private static bool AreReplaceSectionsUniquelyTagIsolated(IEnumerable<IniSectionItem> replaceItems)
        {
            var list = replaceItems.ToList();
            if (list.Count == 0)
                return false;

            if (list.Any(x => !x.IsTagIsolatedReplace || string.IsNullOrWhiteSpace(x.IsolatedGuardTag) || string.IsNullOrWhiteSpace(x.Namespace)))
                return false;

            bool distinctTagsAcrossFiles = list
                .GroupBy(x => x.IsolatedGuardTag.Trim(), StringComparer.OrdinalIgnoreCase)
                .All(g => g.Select(x => x.File).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1);

            if (!distinctTagsAcrossFiles)
                return false;

            return AreNamespacesDistinctAcrossMods(list);
        }

        private static bool IsDualFormCompanionPair(
            string modA,
            string modB,
            Dictionary<string, HashSet<string>> modHashSets)
        {
            bool aUlt = IsDualFormModName(modA);
            bool bUlt = IsDualFormModName(modB);
            if (aUlt == bUlt)
                return false;

            if (!modHashSets.TryGetValue(modA, out var setA) || !modHashSets.TryGetValue(modB, out var setB))
                return false;

            int uniqueA = setA.Count(h => !setB.Contains(h));
            int uniqueB = setB.Count(h => !setA.Contains(h));

            // Both forms must have distinct primary outfit/body component hashes (>= 2 unique hashes each)
            return uniqueA >= 2 && uniqueB >= 2;
        }

        private static string NormalizePatternForComparison(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return string.Empty;
            string norm = Regex.Replace(pattern.Trim(), @"\r\n", "\n");
            return norm;
        }

        public static string GenerateReportText(AuditResult audit)
        {
            var sb = new StringBuilder();
            bool zh = I18n.IsZh;

            sb.AppendLine("================================================================================");
            sb.AppendLine(zh
                ? " EFMI MOD 冲突检测工具 v1.0.3 - 检测报告"
                : " EFMI Mods Conflict Auditor v1.0.3 - Audit Report");
            sb.AppendLine("================================================================================");
            sb.AppendLine(zh ? $"扫描时间: {audit.ScanTime:yyyy-MM-dd HH:mm:ss}" : $"Scan Time: {audit.ScanTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(zh ? $"目标路径: {audit.TargetPath}" : $"Target: {audit.TargetPath}");
            sb.AppendLine(zh ? $"扫描根目录: {audit.ScanRoot}" : $"Scan Root: {audit.ScanRoot}");
            sb.AppendLine(zh
                ? $"包含 DISABLED_ 禁用 Mod: {(audit.IncludedDisabled ? "是 (Yes)" : "否 (No - 仅启用中的 Mod)")}"
                : $"Include DISABLED_ Mods: {(audit.IncludedDisabled ? "Yes" : "No (Active Mods Only)")}");
            sb.AppendLine(zh
                ? $"已扫描 Mod 总数: {audit.TotalMods} (启用: {audit.ActiveMods}, 禁用: {audit.DisabledMods})"
                : $"Total Scanned Mods: {audit.TotalMods} (Active: {audit.ActiveMods}, Disabled: {audit.DisabledMods})");
            sb.AppendLine(zh
                ? $"架构类型统计: 统一骨骼 (MergedSkeleton) = {audit.MergedSkeletonModCount} | CrossIB = {audit.CrossIBModCount} | 简易 MOD (Simple) = {audit.SimpleModCount} | 前置插件 (FX Plugin) = {audit.ShaderPatchModCount}"
                : $"Mod Architecture Breakdown: MergedSkeleton = {audit.MergedSkeletonModCount} | CrossIB = {audit.CrossIBModCount} | Simple = {audit.SimpleModCount} | FX Plugin = {audit.ShaderPatchModCount}");
            sb.AppendLine(zh
                ? $"ShaderFixes 目录状态: {(audit.ShaderFixesFileCount > 0 ? $"⚠️ 检测到 {audit.ShaderFixesFileCount} 个文件 (需注意全局着色器干扰)" : "空 (安全)")}"
                : $"ShaderFixes Status: {(audit.ShaderFixesFileCount > 0 ? $"WARNING - {audit.ShaderFixesFileCount} file(s) present" : "Empty (Clean)")}");
            if (audit.CacheStatus != null && audit.CacheStatus.IsValidEfmiRoot)
            {
                var cs = audit.CacheStatus;
                string d3dxDescZh = cs.D3dxState switch
                {
                    D3dxCacheSettingState.Enabled => "开启 (1)",
                    D3dxCacheSettingState.Disabled => "关闭 (0)",
                    D3dxCacheSettingState.NotSet => "未配置",
                    _ => "未找到 d3dx.ini"
                };
                string d3dxDescEn = cs.D3dxState switch
                {
                    D3dxCacheSettingState.Enabled => "ON (1)",
                    D3dxCacheSettingState.Disabled => "OFF (0)",
                    D3dxCacheSettingState.NotSet => "Not Set",
                    _ => "d3dx.ini Not Found"
                };
                sb.AppendLine(zh
                    ? $"着色器缓存开关: {(cs.IsEffectivelyEnabled ? "全局已开启" : "未开启")} (d3dx.ini: {d3dxDescZh}, RabbitFX ShaderCacheSettings.ini: {(cs.HasActiveModCacheIni ? "已启用" : "未启用")})"
                    : $"Shader Cache Setting: {(cs.IsEffectivelyEnabled ? "Enabled (Global)" : "Disabled")} (d3dx.ini: {d3dxDescEn}, RabbitFX ShaderCacheSettings.ini: {(cs.HasActiveModCacheIni ? "Active" : "Disabled")})");
                sb.AppendLine(zh
                    ? $"ShaderCache 目录状态: {(cs.CacheFileCount > 0 ? $"{cs.CacheFileCount} 个缓存文件 ({FormatByteSize(cs.CacheTotalBytes)})" : "空 (0 个文件)")}"
                    : $"ShaderCache Directory: {(cs.CacheFileCount > 0 ? $"{cs.CacheFileCount} cached file(s) ({FormatByteSize(cs.CacheTotalBytes)})" : "Empty (0 files)")}");
            }
            sb.AppendLine(zh ? $"已解析 INI 文件数: {audit.IniCount}" : $"Parsed INI Files: {audit.IniCount}");
            sb.AppendLine($"ShaderOverride Sections: {audit.ShaderOverrideCount}");
            sb.AppendLine($"ShaderRegex Sections: {audit.ShaderRegexCount}");
            sb.AppendLine($"TextureOverride Sections: {audit.TextureOverrideCount}");
            sb.AppendLine();

            string[] cats = audit.IncludedDisabled
                ? new[] { "A", "B", "C" }
                : new[] { "A", "B", "C", "D" };

            foreach (string cat in cats)
            {
                string header = cat switch
                {
                    "A" => zh ? "=== A. ShaderOverride 警告 (含 ShaderOverride 的 Mod 名单) ===" : "=== A. ShaderOverride Warnings (Mods containing ShaderOverride) ===",
                    "B" => zh ? "=== B. ShaderRegex 警告 (非前置插件含 ShaderRegex 的 Mod 名单) ===" : "=== B. ShaderRegex Warnings (Non-FX-Plugin Mods containing ShaderRegex) ===",
                    "C" => zh ? "=== C. 正则匹配器重复 (ShaderRegex 冲突) ===" : "=== C. Duplicate ShaderRegex Conflicts ===",
                    "D" => zh ? "=== D. 同角色重复开启检测 (TextureOverride Hash 重叠) ===" : "=== D. Same-Character Multi-Mod Check (TextureOverride Overlaps) ===",
                    _ => cat
                };

                sb.AppendLine(header);
                var items = audit.AllFindings.Where(x => x.Category == cat).ToList();
                if (items.Count == 0)
                {
                    sb.AppendLine(zh ? "无 (None)." : "None.");
                }
                else
                {
                    foreach (var item in items)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"[{item.Severity}] {item.Title}");
                        sb.AppendLine($"  {item.Subtitle}");
                        sb.AppendLine(zh ? $"  原因: {item.Reason}" : $"  Reason: {item.Reason}");
                        sb.AppendLine(zh ? $"  建议: {item.Recommendation}" : $"  Recommendation: {item.Recommendation}");
                        foreach (var s in item.Sections)
                        {
                            string disTag = s.ModIsDisabled ? "[DISABLED] " : "";
                            string filterStr = s.Filter.HasValue ? s.Filter.Value.ToString(CultureInfo.InvariantCulture) : "-";
                            string nsStr = !string.IsNullOrEmpty(s.Namespace) ? $" ns={s.Namespace}" : "";
                            string tagStr = s.IsTagIsolatedReplace && !string.IsNullOrEmpty(s.IsolatedGuardTag) ? $" guard_id={s.IsolatedGuardTag}" : "";
                            sb.AppendLine($"    - {disTag}{s.Mod} :: {s.RelativeFile} (Line {s.Line}) :: [{s.Name}]{nsStr} hash={s.Hash ?? "-"} filter={filterStr}{tagStr} overrule={s.Overrule}");
                        }
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
