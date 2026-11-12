// The GRF package currently referenced by this project does not expose the
// FromFile/LoadingMode/Find/Size API used by the original prototype importer.
// Keep the prototype out of Unity compilation until its adapter is rewritten
// against the exact installed package API. This does not affect gameplay or APK
// builds; the importer will be restored after API-level validation.
#if UNITY_EDITOR && RAGNAROK_GRF_IMPORTER_EXPERIMENTAL
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF;
using UnityEditor;
using UnityEngine;

// EXPERIMENTAL ONLY: this prototype is intentionally excluded from normal builds.
// Do not enable RAGNAROK_GRF_IMPORTER_EXPERIMENTAL until the GRF API is corrected.
public sealed class RagnarokGrfImporterWindow : EditorWindow
{
    // Original implementation retained below for API migration reference.
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
    private string status = "Experimental GRF importer is disabled pending API compatibility work.";

    private static readonly HashSet<string> UnityImportExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".bmp", ".tga", ".png", ".jpg", ".jpeg", ".wav", ".mp3", ".ogg" };
    private static readonly HashSet<string> RawExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".spr", ".act", ".pal", ".rsw", ".gnd", ".gat", ".rsm", ".str" };

    [MenuItem("Private Ragnarok/Import GRF Resources (Experimental)")]
    public static void Open()
    {
        var window = GetWindow<RagnarokGrfImporterWindow>("GRF Resource Importer");
        window.minSize = new Vector2(540, 430);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(status + "\nThe GRF package API must be adapted and tested before this tool can be enabled.", MessageType.Warning);
    }
}
#endif
