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
        public string Name { get; set; }
        public string Hash { get; set; }
        public double? Filter { get; set; }
        public string FilterRaw { get; set; }
        public bool Overrule { get; set; }
        public string AllowDuplicate { get; set; }
        public bool PureClassifier { get; set; }
        public string ShaderModel { get; set; }
        public string PatternText { get; set; }
        public bool HasPatternReplace { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public int Line { get; set; }
        public string Text { get; set; }
    }

    public class ParsedIniFile
    {
        public string FilePath { get; set; }
        public string RelativePath { get; set; }
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
                if (Regex.IsMatch(trim, @"^(?i)(hash|filter_index|allow_duplicate_hash)\s*="))
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

        public static ParsedIniFile ParseIni(FileInfo file, ModIdentity modIdentity, string scanRoot)
        {
            EncodedText loaded = ReadTextPreserveEncoding(file.FullName);
            string text = loaded.Text;
            string relPath = GetRelativePath(file.FullName, scanRoot);

            var parsed = new ParsedIniFile
            {
                FilePath = file.FullName,
                RelativePath = relPath,
                ModId = modIdentity.ModId,
                ModIsDisabled = modIdentity.IsDisabled,
                ModRootPath = modIdentity.ModRootPath,
                Loaded = loaded
            };

            var regexRootSections = new Dictionary<string, IniSectionItem>(StringComparer.OrdinalIgnoreCase);
            var regexPatterns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                    }
                    else if (name.EndsWith(".InsertDeclarations", StringComparison.OrdinalIgnoreCase))
                    {
                        // Sub-section of ShaderRegex, attach info via root
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
                            Name = name,
                            Hash = hash,
                            Index = m.Index,
                            Length = m.Length,
                            Line = line,
                            Text = section
                        });
                    }
                }
            }

            // Link ShaderRegex patterns & replace flags to their root ShaderRegex sections
            foreach (var sr in parsed.ShaderRegexes)
            {
                if (regexPatterns.TryGetValue(sr.Name, out string pat))
                {
                    sr.PatternText = pat;
                }
                if (regexHasReplace.Contains(sr.Name))
                {
                    sr.HasPatternReplace = true;
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

            // Standalone Shader Patch / FX Plugin (e.g. RabbitFX, KatFXMatcap, root CrossIBClassifier.ini):
            // Contains active [ShaderRegex] or [ShaderOverride] sections, and 0 [TextureOverride] mesh/texture hooks
            int totalTO = fileList.Sum(f => f.TextureOverrides.Count);
            int totalSR = fileList.Sum(f => f.ShaderRegexes.Count);
            int totalSO = fileList.Sum(f => f.ShaderOverrides.Count);
            if (totalTO == 0 && (totalSR > 0 || totalSO > 0))
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

                string severity = (hasFilter && !isStandardCrossIB) ? "HIGH" : "WARNING";

                string reasonZh;
                string reasonEn;
                if (isStandardCrossIB)
                {
                    reasonZh = $"CrossIB 架构 Mod「{modId}」包含 {items.Count} 个用于跨骨骼阶段识别的 [ShaderOverride] 节（filter_index: {filterDescZh}）。这是旧版 CrossIB 正常运行所需的顶点着色器标记，通常不会影响 RabbitFX，仅在跨版本 Shader Hash 变动或与其他特殊 Mod 撞 Hash 时可能产生冲突。";
                    reasonEn = $"CrossIB Mod '{modId}' contains {items.Count} [ShaderOverride] section(s) for cross-skeleton pass detection (filter_index: {filterDescEn}). These vertex shader markers are required for legacy CrossIB to function and normally do not affect RabbitFX, only risking conflicts if shader hashes change across game updates or collide with custom Mods.";
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
            var srByMod = allSR
                .Where(x => x.ModType != ModArchType.ShaderPatch)
                .GroupBy(x => x.Mod, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var g in srByMod)
            {
                string modId = g.Key;
                var items = g.ToList();

                var filters = items
                    .Where(x => x.Filter.HasValue)
                    .Select(x => x.Filter.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                bool hasReplace = items.Any(x => x.HasPatternReplace);
                var secNames = items.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                string secPreview = secNames.Count <= 3
                    ? string.Join(", ", secNames)
                    : $"{string.Join(", ", secNames.Take(3))} ... (+{secNames.Count - 3})";

                string filterDesc = filters.Count > 0
                    ? string.Join(", ", filters.Select(f => f.ToString(CultureInfo.InvariantCulture)))
                    : "-";

                string severity = (hasReplace || filters.Count > 0) ? "HIGH" : "WARNING";

                var ci = new ConflictItem
                {
                    Id = $"B_{idCounter++}",
                    Category = "B",
                    Severity = severity,
                    TitleZh = $"ShaderRegex 警告: {modId} ({items.Count} 个节)",
                    TitleEn = $"ShaderRegex Warning: {modId} ({items.Count} section(s))",
                    SubtitleZh = $"包含节: {secPreview}  |  filter_index: [{filterDesc}]  |  含 Pattern.Replace: {(hasReplace ? "是" : "否")}",
                    SubtitleEn = $"Sections: {secPreview}  |  filter_index: [{filterDesc}]  |  Has Pattern.Replace: {(hasReplace ? "Yes" : "No")}",
                    ReasonZh = $"非前置插件 Mod「{modId}」包含 {items.Count} 个 [ShaderRegex] 正则匹配节。易与公共前置插件（如 RabbitFX / CrossIBClassifier）发生冲突。",
                    ReasonEn = $"Non-FX-Plugin Mod '{modId}' contains {items.Count} [ShaderRegex] section(s). May conflict with shared FX plugins (such as RabbitFX / CrossIBClassifier).",
                    RecommendZh = "若游戏内出现画面异常，请直接停用该 Mod（在文件夹名前加上 DISABLED_ 前缀）。",
                    RecommendEn = "If visual glitches occur in-game, please disable this Mod (add the DISABLED_ prefix to its folder).",
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                result.AllFindings.Add(ci);
            }

            // =========================================================================
            // C) Duplicate ShaderRegex Conflict Detection (正则匹配器重复)
            // =========================================================================
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
                var ci = new ConflictItem
                {
                    Id = $"C_Filter_{idCounter++}",
                    Category = "C",
                    Severity = "HIGH",
                    TitleZh = $"正则匹配器重复 (filter_index = {fVal})",
                    TitleEn = $"Duplicate ShaderRegex (filter_index = {fVal})",
                    SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}",
                    SubtitleEn = $"Involved Mods ({mods.Count}): {string.Join(" ↔ ", mods)}",
                    ReasonZh = $"多个不同的 Mod 同时定义了分配相同 filter_index ({fVal}) 的 [ShaderRegex]！这通常意味着某个角色 Mod 私自内置了一份全局分类器副本，与公共组件或其他 Mod 发生重复匹配，会导致着色器分类覆盖、掉帧或渲染异常。",
                    ReasonEn = $"Multiple Mods define [ShaderRegex] assigning the same filter_index ({fVal})! Usually caused by a character Mod bundling its own private copy of global regex classifiers, causing redundant regex evaluation, classification override, or FPS drops.",
                    RecommendZh = "建议保留唯一的公共 ShaderRegex 提供者（如 RabbitFX），将角色 Mod 内部重复夹带的冗余 ShaderRegex 节删除或禁用。",
                    RecommendEn = "Keep a single shared ShaderRegex provider (such as RabbitFX) and remove/disable duplicate bundled ShaderRegex sections inside individual character Mods.",
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                result.AllFindings.Add(ci);
            }

            // C2: Identical ShaderRegex .Pattern across multiple Mods (especially with .Pattern.Replace ASM injection!)
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
                string severity = hasReplace ? "HIGH" : "WARNING";
                string sampleName = items.First().Name;

                var ci = new ConflictItem
                {
                    Id = $"C_Pat_{idCounter++}",
                    Category = "C",
                    Severity = severity,
                    TitleZh = hasReplace
                        ? $"正则匹配器重复 · 字节码注入 ([{sampleName}])"
                        : $"正则匹配器重复 ([{sampleName}])",
                    TitleEn = hasReplace
                        ? $"Duplicate ShaderRegex ASM Injection ([{sampleName}])"
                        : $"Duplicate ShaderRegex Pattern ([{sampleName}])",
                    SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  含 Pattern.Replace: {(hasReplace ? "是 (高危)" : "否")}",
                    SubtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  Has Pattern.Replace: {(hasReplace ? "Yes (High Risk)" : "No")}",
                    ReasonZh = hasReplace
                        ? $"多个 Mod 对相同的着色器结构使用了 [ShaderRegex.*.Pattern.Replace] 动态字节码注入（如独立 CutoutMask / 透明遮罩正则）。根据 AGMF社区规范，多个 Mod 同时对同一类 Shader 执行 Regex 字节码修补会导致自重匹配、多命名空间指令堆叠、严重掉帧甚至游戏崩溃！"
                        : $"多个 Mod 各自内置了完全相同的 [ShaderRegex] 正则匹配模式（如 CrossIBClassifier.ini）。虽然各自写入私有命名空间变量，但多份相同正则在加载着色器时会产生重复匹配开销。",
                    ReasonEn = hasReplace
                        ? $"Multiple Mods apply [ShaderRegex.*.Pattern.Replace] ASM bytecode patching to the same shader pattern (e.g. standalone CutoutMask). Per AGMF community standard, multi-Mod ShaderRegex bytecode replacement causes self-rematching, multi-namespace instruction stacking, severe stutter, or crashes!"
                        : $"Multiple Mods bundle identical [ShaderRegex] patterns (e.g. CrossIBClassifier.ini). Even with private namespaces, duplicate regex definitions add redundant shader compilation/matching overhead.",
                    RecommendZh = hasReplace
                        ? "强烈建议将 [ShaderRegex.*.Pattern.Replace] 迁移为按具体 Shader Hash 绑定的 [CustomShader] 或统一交由单个共享组件处理，避免多个角色 Mod 叠加注入。"
                        : "如果已使用 EFMI Core 原生骨骼合并 (MergedSkeleton) 或统一分类器，可清理多余的重复 CrossIBClassifier.ini。",
                    RecommendEn = hasReplace
                        ? "Migrate [ShaderRegex.*.Pattern.Replace] to targeted [CustomShader] sections or consolidate into a single shared component."
                        : "Consider consolidating duplicate CrossIBClassifier.ini files or migrating to EFMI Core MergedSkeleton.",
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                result.AllFindings.Add(ci);
            }

            // C3: Multiple Mods using [ShaderRegex.*.Pattern.Replace] on the same shader_model (AGMF community standard ASM stacking risk)
            var replaceByModelGroups = allSR
                .Where(x => x.HasPatternReplace)
                .GroupBy(x => (x.ShaderModel ?? "unknown").ToLowerInvariant());

            foreach (var g in replaceByModelGroups)
            {
                var items = g.ToList();
                var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (mods.Count < 2) continue;

                // Skip if all items were already reported as identical pattern in C2
                bool alreadyCovered = result.AllFindings.Any(f =>
                    f.Category == "C" &&
                    f.InvolvedMods.Count == mods.Count &&
                    !f.InvolvedMods.Except(mods, StringComparer.OrdinalIgnoreCase).Any());
                if (alreadyCovered) continue;

                var ci = new ConflictItem
                {
                    Id = $"C_ReplaceStack_{idCounter++}",
                    Category = "C",
                    Severity = "HIGH",
                    TitleZh = $"正则匹配器重复 · 多重字节码注入叠加 ({g.Key})",
                    TitleEn = $"Duplicate ShaderRegex.Pattern.Replace Bytecode Stacking ({g.Key})",
                    SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}  |  共 {items.Count} 个字节码修补节",
                    SubtitleEn = $"Involved Mods ({mods.Count}): {string.Join(" ↔ ", mods)}  |  {items.Count} bytecode replace sections",
                    ReasonZh = $"检测到 {mods.Count} 个不同 Mod 同时对 {g.Key} 着色器使用了 [ShaderRegex.*.Pattern.Replace] 动态字节码替换（例如各自的 CutoutMask.ini 透明遮罩正则）。根据 AGMF社区规范，多个 Mod 同时对同类着色器进行 Regex 字节码注入会导致自重匹配、多命名空间指令堆叠、性能下降甚至崩溃。",
                    ReasonEn = $"Detected {mods.Count} different Mods applying [ShaderRegex.*.Pattern.Replace] bytecode patching to {g.Key} shaders (e.g. standalone CutoutMask.ini). Per AGMF community standard, multi-Mod regex bytecode replacement causes self-rematching, multi-namespace ASM stacking, performance loss, or crashes.",
                    RecommendZh = "建议避免同时开启多个自带独立 CutoutMask.ini (ShaderRegex.Pattern.Replace) 的角色 Mod，或将其改造为基于 [CustomShader] 的定向材质路由。",
                    RecommendEn = "Avoid enabling multiple Mods with standalone CutoutMask.ini (ShaderRegex.Pattern.Replace) simultaneously, or convert them to targeted [CustomShader] sections.",
                    Sections = items
                };
                PopulateModMetadata(ci, items);
                result.AllFindings.Add(ci);
            }

            // =========================================================================
            // D) Same-Character Duplicate Active Mod Detection (同角色重复开启检测)
            //    ONLY checked and recorded when includeDisabled == false!
            // =========================================================================
            if (!includeDisabled)
            {
                var toGroups = allTO
                    .Where(x => !string.IsNullOrEmpty(x.Hash))
                    .GroupBy(x => x.Hash, StringComparer.OrdinalIgnoreCase);

                foreach (var g in toGroups)
                {
                    var items = g.ToList();
                    var mods = items.Select(x => x.Mod).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (mods.Count < 2) continue;

                    var ci = new ConflictItem
                    {
                        Id = $"D_{idCounter++}",
                        Category = "D",
                        Severity = "INFO",
                        TitleZh = $"同角色重复开启检测: {g.Key}",
                        TitleEn = $"Same-Character Multi-Mod Overlap: {g.Key}",
                        SubtitleZh = $"涉及 Mod ({mods.Count}): {string.Join(" ↔ ", mods)}",
                        SubtitleEn = $"Mods ({mods.Count}): {string.Join(" ↔ ", mods)}",
                        ReasonZh = $"多个已启用的 Mod 同时覆写了相同的角色模型缓冲区 (IB/VB) 或贴图 Hash ({g.Key})。如果这两个 Mod 属于同一个角色的不同皮肤，同时开启会导致模型破损或互相重叠。",
                        ReasonEn = $"Multiple active Mods override the same character IB/VB/Texture resource hash ({g.Key}). Enabling multiple skins for the same character simultaneously causes mesh overlap or glitches.",
                        RecommendZh = "请检查是否为同一角色同时开启了多个皮肤 Mod（若是，请将其中一个文件夹加上 DISABLED_ 前缀禁用）。",
                        RecommendEn = "Verify whether you accidentally left multiple skins for the same character enabled at the same time.",
                        Sections = items
                    };
                    PopulateModMetadata(ci, items);
                    result.AllFindings.Add(ci);
                }
            }

            return result;
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
                ? " EFMI MOD 冲突检测工具 v1.0 - 检测报告"
                : " EFMI Mods Conflict Auditor v1.0 - Audit Report");
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
                            sb.AppendLine($"    - {disTag}{s.Mod} :: {s.RelativeFile} (Line {s.Line}) :: [{s.Name}] hash={s.Hash ?? "-"} filter={filterStr} overrule={s.Overrule}");
                        }
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
