using System;
using System.Collections.Generic;
using System.IO;

public struct RuneSpellSaveInfo {
    public string FileName { get; set; }
    public string DisplayName { get; set; }
    public string UpdatedAtUtc { get; set; }
    public DateTime LastWriteTimeUtc { get; set; }
    public bool IsSelectedForLoadout { get; set; }
}

public class RuneSpellGridData {
    public int radius;
    public List<RunePlacedGlyph> glyphs = new List<RunePlacedGlyph>();
}

public class RuneSpellMetadata {
    public string name;
    public string words;
    public string updatedAtUtc;
    public bool isSelectedForLoadout;
}

public class RuneSpellLocalStorage {
    private readonly string _rootPath;
    private const string GridFileName = "grid.json";
    private const string MetadataFileName = "metadata.json";

    public RuneSpellLocalStorage() {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _rootPath = Path.Combine(documents, "My Games", "Battle Magi");
    }

    public string RootPath => _rootPath;

    public bool Save(string spellName, RuneSpellDraft draft, string sourceFileName, out string savedFileName, out string error) {
        savedFileName = SanitizeFileName(spellName);
        if (string.IsNullOrWhiteSpace(savedFileName)) {
            error = "Spell name is empty";
            return false;
        }

        try {
            Directory.CreateDirectory(_rootPath);
            var sourceSanitized = SanitizeFileName(sourceFileName);
            var sourcePath = string.IsNullOrWhiteSpace(sourceSanitized)
                ? string.Empty
                : Path.Combine(_rootPath, sourceSanitized);
            var spellPath = Path.Combine(_rootPath, savedFileName);
            if (!string.IsNullOrWhiteSpace(sourcePath) &&
                !string.Equals(sourcePath, spellPath, StringComparison.OrdinalIgnoreCase) &&
                Directory.Exists(sourcePath) &&
                !Directory.Exists(spellPath)) {
                Directory.Move(sourcePath, spellPath);
            }

            Directory.CreateDirectory(spellPath);

            draft.name = spellName;
            var metadata = ReadMetadata(spellPath);
            if (metadata == null && !string.IsNullOrWhiteSpace(sourcePath))
                metadata = ReadMetadata(sourcePath);
            if (metadata == null)
                metadata = new RuneSpellMetadata();
            metadata.name = spellName;
            metadata.words = draft.words;
            metadata.updatedAtUtc = string.IsNullOrWhiteSpace(draft.updatedAtUtc)
                ? DateTime.UtcNow.ToString("dd MMMM yy HH:mm")
                : draft.updatedAtUtc;

            var grid = CreateGridData(draft.board);
            var gridPath = Path.Combine(spellPath, GridFileName);
            var metadataPath = Path.Combine(spellPath, MetadataFileName);

            File.WriteAllText(gridPath, SpellJsonSerializer.ToJson(grid, true));
            File.WriteAllText(metadataPath, SpellJsonSerializer.ToJson(metadata, true));

            if (!string.IsNullOrWhiteSpace(sourcePath) &&
                !string.Equals(sourcePath, spellPath, StringComparison.OrdinalIgnoreCase)) {
                TryDeleteLegacyFile(sourcePath);
                TryDeleteFolder(sourcePath);
            }

            error = string.Empty;
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    public bool TryLoad(string fileName, out RuneSpellDraft draft, out string error) {
        draft = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName)) {
            error = "Save file name is empty";
            return false;
        }

        try {
            var sanitized = SanitizeFileName(fileName);
            var folderPath = Path.Combine(_rootPath, sanitized);
            if (Directory.Exists(folderPath)) {
                var loaded = TryLoadFromFolder(folderPath, out draft, out error);
                if (loaded)
                    return true;
            }

            var legacyFilePath = Path.Combine(_rootPath, sanitized);
            if (!File.Exists(legacyFilePath)) {
                error = "Save file not found";
                return false;
            }

            var legacyJson = File.ReadAllText(legacyFilePath);
            draft = SpellJsonSerializer.FromJson<RuneSpellDraft>(legacyJson);
            if (draft == null) {
                error = "Failed to parse save file";
                return false;
            }

            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    public List<RuneSpellSaveInfo> List() {
        var saves = new List<RuneSpellSaveInfo>();
        if (!Directory.Exists(_rootPath))
            return saves;

        var directories = Directory.GetDirectories(_rootPath);
        for (var i = 0; i < directories.Length; i++) {
            var directoryPath = directories[i];
            var folderName = Path.GetFileName(directoryPath);
            var metadata = ReadMetadata(directoryPath);
            var displayName = metadata != null && !string.IsNullOrWhiteSpace(metadata.name)
                ? metadata.name
                : folderName;
            var updatedAtUtc = metadata != null ? metadata.updatedAtUtc ?? string.Empty : string.Empty;
            var isSelectedForLoadout = metadata != null && metadata.isSelectedForLoadout;

            saves.Add(new RuneSpellSaveInfo {
                FileName = folderName,
                DisplayName = displayName,
                UpdatedAtUtc = updatedAtUtc,
                LastWriteTimeUtc = Directory.GetLastWriteTimeUtc(directoryPath),
                IsSelectedForLoadout = isSelectedForLoadout,
            });
        }

        saves.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
        return saves;
    }

    public bool SetSelectedForLoadout(string fileName, bool isSelectedForLoadout, out string error) {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName)) {
            error = "Save file name is empty";
            return false;
        }

        try {
            var sanitized = SanitizeFileName(fileName);
            var folderPath = Path.Combine(_rootPath, sanitized);
            if (!Directory.Exists(folderPath)) {
                error = "Save folder not found";
                return false;
            }

            var metadata = ReadMetadata(folderPath) ?? new RuneSpellMetadata {
                name = sanitized,
                updatedAtUtc = DateTime.UtcNow.ToString("dd MMMM yy HH:mm"),
            };
            metadata.isSelectedForLoadout = isSelectedForLoadout;

            var metadataPath = Path.Combine(folderPath, MetadataFileName);
            File.WriteAllText(metadataPath, SpellJsonSerializer.ToJson(metadata, true));
            return true;
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    private bool TryLoadFromFolder(string folderPath, out RuneSpellDraft draft, out string error) {
        draft = null;
        error = string.Empty;

        var gridPath = Path.Combine(folderPath, GridFileName);
        if (!File.Exists(gridPath)) {
            error = "grid.json not found";
            return false;
        }

        var gridJson = File.ReadAllText(gridPath);
        var gridData = SpellJsonSerializer.FromJson<RuneSpellGridData>(gridJson);
        if (gridData == null) {
            error = "Failed to parse grid.json";
            return false;
        }

        var metadata = ReadMetadata(folderPath);
        var name = metadata != null && !string.IsNullOrWhiteSpace(metadata.name)
            ? metadata.name
            : Path.GetFileName(folderPath);
        var updatedAtUtc = metadata != null ? metadata.updatedAtUtc ?? string.Empty : string.Empty;
        var words = metadata != null ? metadata.words ?? string.Empty : string.Empty;

        draft = new RuneSpellDraft {
            name = name,
            words = words,
            version = 1,
            updatedAtUtc = updatedAtUtc,
            board = new RuneBoardData {
                radius = gridData.radius,
                glyphs = gridData.glyphs ?? new List<RunePlacedGlyph>(),
                links = new List<RuneLink>(),
            }
        };

        return true;
    }

    private RuneSpellMetadata ReadMetadata(string folderPath) {
        try {
            var metadataPath = Path.Combine(folderPath, MetadataFileName);
            if (!File.Exists(metadataPath))
                return null;

            var metadataJson = File.ReadAllText(metadataPath);
            return SpellJsonSerializer.FromJson<RuneSpellMetadata>(metadataJson);
        } catch (Exception) {
            return null;
        }
    }

    private static RuneSpellGridData CreateGridData(RuneBoardData board) {
        var sourceBoard = board ?? new RuneBoardData();
        var glyphs = sourceBoard.glyphs ?? new List<RunePlacedGlyph>();
        var result = new RuneSpellGridData {
            radius = sourceBoard.radius,
            glyphs = new List<RunePlacedGlyph>(glyphs.Count)
        };

        for (var i = 0; i < glyphs.Count; i++) {
            var g = glyphs[i];
            result.glyphs.Add(new RunePlacedGlyph {
                placementId = g.placementId,
                glyphId = g.glyphId,
                anchor = g.anchor,
                rotation = g.rotation,
            });
        }

        return result;
    }

    private static string SanitizeFileName(string value) {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        for (var i = 0; i < invalidChars.Length; i++) {
            text = text.Replace(invalidChars[i], '_');
        }

        return text;
    }

    private static void TryDeleteFolder(string folderPath) {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;
        if (!Directory.Exists(folderPath))
            return;

        Directory.Delete(folderPath, true);
    }

    private static void TryDeleteLegacyFile(string folderPath) {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        var directory = Path.GetDirectoryName(folderPath);
        var fileName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            return;

        var legacyPath = Path.Combine(directory, fileName);
        if (File.Exists(legacyPath))
            File.Delete(legacyPath);
    }
}


