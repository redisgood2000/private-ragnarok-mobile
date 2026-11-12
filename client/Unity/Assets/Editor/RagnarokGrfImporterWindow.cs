#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Offline importer for user-supplied Ragnarok GRF archives.
/// It extracts selected source files into Assets/RemoteContent/GRFImport;
/// it never uploads or commits the original GRF archives.
/// </summary>
public sealed class RagnarokGrfImporterWindow : EditorWindow
{
    private const string OutputRoot = "Assets/RemoteContent/GRFImport";
    private const int MaxFilesPerBatch = 2000;
    private const long MaxBytesPerBatch = 512L * 1024L * 1024L;
    private const uint MaxEntryBytes = 64U * 1024U * 1024U;

    private string archivePath = string.Empty;
    private string search = "texture/";
    private bool includeRawRagnarokFiles = true;
    private Grf archive;
    private string loadedArchivePath;
    private Vector2 scroll;
    private string status = "Choose data.grf or rdata.grf to begin.";

    private static readonly HashSet<string> UnityImportExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".bmp", ".tga", ".png", ".jpg", ".jpeg", ".wav", ".mp3", ".ogg" };

    private static readonly HashSet<string> RawExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".spr", ".act", ".pal", ".rsw", ".gnd", ".gat", ".rsm", ".str" };

    [MenuItem("Private Ragnarok/Import GRF Resources")]
    public static void Open()
    {
        var window = GetWindow<RagnarokGrfImporterWindow>("GRF Resource Importer");
        window.minSize = new Vector2(540, 430);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Ragnarok GRF Resource Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select your own data.grf or rdata.grf. The importer extracts selected files locally; " +
            "it does not upload the archive. PNG/BMP/TGA/JPG and audio files become Unity assets. " +
            "SPR/ACT/PAL and map/model files are preserved as raw source files for a later conversion step.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.TextField("GRF archive", archivePath);
            if (GUILayout.Button("Browse", GUILayout.Width(78)))
            {
                var selected = EditorUtility.OpenFilePanel("Select Ragnarok GRF archive", "", "grf");
                if (!string.IsNullOrEmpty(selected))
                {
                    archivePath = selected;
                    OpenArchive();
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            search = EditorGUILayout.TextField("Path filter", search);
            includeRawRagnarokFiles = EditorGUILayout.ToggleLeft("Include SPR/ACT/maps", includeRawRagnarokFiles, GUILayout.Width(155));
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !string.IsNullOrEmpty(archivePath);
            if (GUILayout.Button("Open / Refresh Archive")) OpenArchive();
            if (GUILayout.Button("Extract All Matching Files"))
                ExtractMatchingFiles();
            GUI.enabled = true;
        }

        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
        if (archive == null)
            return;

        var matches = GetMatchingEntries();
        EditorGUILayout.LabelField(
            "Matching entries: " + matches.Count + " (showing first 200)",
            EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var entryName in matches.Take(200))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(entryName);
                if (GUILayout.Button("Extract", GUILayout.Width(70)))
                    ExtractOne(entryName);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void OpenArchive()
    {
        archive = null;
        loadedArchivePath = null;
        if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
        {
            status = "Archive file was not found.";
            return;
        }

        try
        {
            archive = Grf.FromFile(archivePath, LoadingMode.Deferred);
            loadedArchivePath = archivePath;
            status = "Opened " + Path.GetFileName(archivePath) + " — " +
                archive.Count + " entries. GRF signature: " + archive.Signature;
        }
        catch (Exception ex)
        {
            status = "Could not open GRF: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private List<string> GetMatchingEntries()
    {
        if (archive == null)
            return new List<string>();

        var filter = (search ?? string.Empty).Trim().Replace('\\', '/');
        return archive.EntryNames
            .Where(IsSupportedEntry)
            .Where(name => string.IsNullOrEmpty(filter) ||
                name.Replace('\\', '/').IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool IsSupportedEntry(string entryName)
    {
        var extension = Path.GetExtension(entryName);
        return UnityImportExtensions.Contains(extension) ||
            (includeRawRagnarokFiles && RawExtensions.Contains(extension));
    }

    private void ExtractMatchingFiles()
    {
        if (archive == null)
            return;

        var matches = GetMatchingEntries();
        if (matches.Count == 0)
        {
            status = "No supported entries match this filter.";
            return;
        }

        if (matches.Count > MaxFilesPerBatch)
        {
            EditorUtility.DisplayDialog("GRF import limit",
                "This filter matches " + matches.Count + " files. Narrow the path filter to 2,000 files or fewer.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Extract GRF resources",
            "Extract " + matches.Count + " matching files from " + Path.GetFileName(loadedArchivePath) +
            " into " + OutputRoot + "?\n\nThe original GRF will not be modified.",
            "Extract", "Cancel"))
            return;

        var extracted = 0;
        long totalBytes = 0;
        try
        {
            for (var i = 0; i < matches.Count; i++)
            {
                var name = matches[i];
                EditorUtility.DisplayProgressBar("Extracting GRF resources", name,
                    (float)i / matches.Count);
                var size = ExtractEntry(name, ref totalBytes);
                if (size >= 0) extracted++;
            }
            AssetDatabase.Refresh();
            status = "Extracted " + extracted + " files (" + FormatBytes(totalBytes) +
                "). Unity-importable images/audio are now available under " + OutputRoot + ".";
        }
        catch (Exception ex)
        {
            status = "Extraction stopped: " + ex.Message;
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void ExtractOne(string entryName)
    {
        try
        {
            long totalBytes = 0;
            var size = ExtractEntry(entryName, ref totalBytes);
            if (size >= 0)
            {
                AssetDatabase.Refresh();
                status = "Extracted " + entryName + " (" + FormatBytes(size) + ").";
            }
        }
        catch (Exception ex)
        {
            status = "Could not extract " + entryName + ": " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private long ExtractEntry(string entryName, ref long totalBytes)
    {
        if (archive == null || loadedArchivePath != archivePath)
            throw new InvalidOperationException("Open the selected GRF archive again.");

        if (!archive.Find(entryName, out GrfEntry entry) || entry == null)
            throw new FileNotFoundException("Entry disappeared from archive: " + entryName);

        if (entry.Size == 0 || entry.Size > MaxEntryBytes)
        {
            Debug.LogWarning("Skipped unusually sized GRF entry: " + entryName + " (" + entry.Size + " bytes)");
            return -1;
        }

        if (totalBytes + entry.Size > MaxBytesPerBatch)
            throw new InvalidOperationException("The 512 MB extraction safety limit was reached. Narrow the path filter.");

        var relative = entryName.Replace('\\', '/').TrimStart('/');
        if (relative.Split('/').Any(part => part == ".." || part == ".") ||
            Path.IsPathRooted(relative))
            throw new InvalidDataException("Unsafe archive path rejected: " + entryName);

        var archiveFolder = Path.GetFileNameWithoutExtension(loadedArchivePath);
        foreach (var invalid in Path.GetInvalidFileNameChars())
            archiveFolder = archiveFolder.Replace(invalid, '_');

        var outputDirectory = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), OutputRoot, archiveFolder));
        var outputPath = Path.GetFullPath(Path.Combine(outputDirectory, relative));
        var rootPrefix = outputDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!outputPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Archive entry escaped the output folder.");

        var bytes = entry.GetUncompressedData();
        if (bytes == null || bytes.Length == 0 || bytes.Length > MaxEntryBytes)
            throw new InvalidDataException("Extracted data size is invalid: " + entryName);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, bytes);
        totalBytes += bytes.Length;
        return bytes.Length;
    }

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024L * 1024L
            ? (bytes / (1024f * 1024f)).ToString("0.0") + " MB"
            : (bytes / 1024f).ToString("0.0") + " KB";
    }
}
#endif
