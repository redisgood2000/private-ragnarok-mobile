#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PrivateRagnarok.Client;

public static class RagnarokSpriteCatalogCreator
{
    private const string Folder = "Assets/Resources/PrivateRagnarok";
    private const string AssetPath = Folder + "/RagnarokSpriteCatalog.asset";

    [MenuItem("Private Ragnarok/Create Reference Sprite Catalog")]
    public static void CreateOrRefresh()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/PrivateRagnarok"))
                AssetDatabase.CreateFolder("Assets/Resources", "PrivateRagnarok");
        }

        var catalog = AssetDatabase.LoadAssetAtPath<RagnarokSpriteCatalog>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RagnarokSpriteCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }

        var names = new[]
        {
            "4_F_CLOCKDOLL","4_F_FAIRY2","4_F_PINKWOMAN","4_FAIRYDEADLEAF","4_FROG",
            "4_M_BLACKMAN","4_M_BLUEMAN","4_M_FAIRYANG","4_M_FAIRYAVANT","4_M_FAIRYFREAK",
            "4_M_FAIRYKID6","4_M_FAIRYSCHOLAR","4_M_FAIRYSCHOLAR_DIRTY","4_M_FARIY_HISIE","4_M_FARIYKING",
            "4_M_NEWOZ","4_M_OLIVER","4_M_PROFESSORWORM","4_M_REDMAN","4_F_GELKA",
            "4_M_ROTERT","4_BLACKDRAGON","4_M_GUNSLINGER","4_F_GUNSLINGER","4_M_ARCHER",
            "4_M_SWORDMAN","4_M_NINJA_RED","4_M_NINJA_BLUE","4_M_THIEF_RUMIN","4_M_NOV_RUMIN",
            "4_F_MAYSEL","4_F_ACOLYTE","4_M_NOV_HUNT","4_F_GENETIC","4_F_TAEKWON",
            "4_F_SWORDMAN","4_F_IU","4_M_RAGI","4_M_MELODY","4_TRACE"
        };

        var entries = new List<RagnarokSpriteDefinition>(names.Length);
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var isJob = name.Contains("GUNSLINGER") || name.Contains("ARCHER") ||
                name.Contains("SWORDMAN") || name.Contains("NINJA") ||
                name.Contains("ACOLYTE") || name.Contains("GENETIC") ||
                name.Contains("TAEKWON");
            entries.Add(new RagnarokSpriteDefinition
            {
                id = 610 + i,
                spriteName = name,
                kind = isJob ? RagnarokSpriteKind.JobClass : RagnarokSpriteKind.Npc,
                directionalSprites = new Sprite[8]
            });
        }

        Undo.RecordObject(catalog, "Refresh Ragnarok sprite reference catalog");
        catalog.ReplaceEntries(entries);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        EditorGUIUtility.PingObject(catalog);
        Debug.Log("[Private Ragnarok] Created reference catalog with " + entries.Count +
            " metadata entries (IDs 610–649). No copyrighted sprite images were copied.");
    }
}
#endif
