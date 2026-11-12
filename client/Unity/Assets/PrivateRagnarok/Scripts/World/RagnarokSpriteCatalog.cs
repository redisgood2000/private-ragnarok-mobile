using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateRagnarok.Client
{
    public enum RagnarokSpriteKind { Npc, JobClass, Monster, Unknown }

    [Serializable]
    public sealed class RagnarokSpriteDefinition
    {
        [Min(0)] public int id;
        public string spriteName;
        public RagnarokSpriteKind kind;
        [Tooltip("Optional sprites in clockwise order: south, south-west, west, north-west, north, north-east, east, south-east.")]
        public Sprite[] directionalSprites = new Sprite[8];
        public RuntimeAnimatorController animatorController;

        public bool HasVisuals
        {
            get
            {
                if (directionalSprites != null)
                    for (var i = 0; i < directionalSprites.Length; i++)
                        if (directionalSprites[i] != null) return true;
                return animatorController != null;
            }
        }
    }

    [CreateAssetMenu(fileName = "RagnarokSpriteCatalog", menuName = "Private Ragnarok/Sprite Catalog")]
    public sealed class RagnarokSpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<RagnarokSpriteDefinition> entries = new List<RagnarokSpriteDefinition>();
        private Dictionary<int, RagnarokSpriteDefinition> byId;
        private Dictionary<string, RagnarokSpriteDefinition> byName;

        public IReadOnlyList<RagnarokSpriteDefinition> Entries => entries;

        public RagnarokSpriteDefinition FindById(int id)
        {
            EnsureIndex();
            return byId.TryGetValue(id, out var value) ? value : null;
        }

        public RagnarokSpriteDefinition FindByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            EnsureIndex();
            return byName.TryGetValue(name.Trim(), out var value) ? value : null;
        }

        private void OnEnable() { byId = null; byName = null; }

        private void EnsureIndex()
        {
            if (byId != null && byName != null) return;
            byId = new Dictionary<int, RagnarokSpriteDefinition>();
            byName = new Dictionary<string, RagnarokSpriteDefinition>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || entry.id <= 0 || string.IsNullOrWhiteSpace(entry.spriteName)) continue;
                if (!byId.ContainsKey(entry.id)) byId.Add(entry.id, entry);
                if (!byName.ContainsKey(entry.spriteName.Trim())) byName.Add(entry.spriteName.Trim(), entry);
            }
        }

#if UNITY_EDITOR
        public void ReplaceEntries(List<RagnarokSpriteDefinition> value)
        {
            entries = value ?? new List<RagnarokSpriteDefinition>();
            byId = null;
            byName = null;
        }
#endif
    }
}
