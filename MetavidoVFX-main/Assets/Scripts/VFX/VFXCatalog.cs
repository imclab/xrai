// VFXCatalog - ScriptableObject catalog of VFX assets
// Replaces Resources.LoadAll pattern for faster builds
// VFX in this catalog are included in builds; others are excluded

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.VFXBinders
{
    /// <summary>
    /// Catalog of VFX assets with category organization.
    /// Use instead of Resources.LoadAll for build optimization.
    /// </summary>
    [CreateAssetMenu(fileName = "VFXCatalog", menuName = "MetavidoVFX/VFX Catalog")]
    public class VFXCatalog : ScriptableObject
    {
        [Serializable]
        public class VFXCatalogEntry
        {
            public string name;
            public VisualEffectAsset asset;
            public VFXCategoryType category;
            public string[] tags;
            public bool includedInBuild = true;
        }

        [Header("VFX Assets")]
        [SerializeField] private List<VFXCatalogEntry> _entries = new();

        [Header("Default Settings")]
        [SerializeField] private VisualEffectAsset _defaultVFX;
        [SerializeField] private VisualEffectAsset _hologramVFX;

        // Runtime cache
        private Dictionary<VFXCategoryType, List<VFXCatalogEntry>> _byCategory;
        private Dictionary<string, VFXCatalogEntry> _byName;
        private bool _initialized;

        public IReadOnlyList<VFXCatalogEntry> Entries => _entries;
        public VisualEffectAsset DefaultVFX => _defaultVFX;
        public VisualEffectAsset HologramVFX => _hologramVFX;
        public int Count => _entries.Count;

        void OnEnable() => _initialized = false;

        void EnsureInitialized()
        {
            if (_initialized) return;

            _byCategory = new Dictionary<VFXCategoryType, List<VFXCatalogEntry>>();
            _byName = new Dictionary<string, VFXCatalogEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in _entries)
            {
                if (entry.asset == null) continue;

                // By category
                if (!_byCategory.TryGetValue(entry.category, out var list))
                {
                    list = new List<VFXCatalogEntry>();
                    _byCategory[entry.category] = list;
                }
                list.Add(entry);

                // By name
                _byName[entry.name] = entry;
                if (entry.asset != null)
                    _byName[entry.asset.name] = entry;
            }

            _initialized = true;
        }

        /// <summary>Get all VFX assets (replaces Resources.LoadAll)</summary>
        public VisualEffectAsset[] GetAllAssets(bool includedInBuildOnly = true)
        {
            EnsureInitialized();
            var result = new List<VisualEffectAsset>();
            foreach (var entry in _entries)
            {
                if (entry.asset != null && (!includedInBuildOnly || entry.includedInBuild))
                    result.Add(entry.asset);
            }
            return result.ToArray();
        }

        /// <summary>Get VFX by category</summary>
        public VisualEffectAsset[] GetByCategory(VFXCategoryType category)
        {
            EnsureInitialized();
            if (!_byCategory.TryGetValue(category, out var entries))
                return Array.Empty<VisualEffectAsset>();

            var result = new List<VisualEffectAsset>();
            foreach (var e in entries)
                if (e.asset != null && e.includedInBuild)
                    result.Add(e.asset);
            return result.ToArray();
        }

        /// <summary>Get VFX by name</summary>
        public VisualEffectAsset GetByName(string name)
        {
            EnsureInitialized();
            return _byName.TryGetValue(name, out var entry) ? entry.asset : null;
        }

        /// <summary>Get entry by name (includes metadata)</summary>
        public VFXCatalogEntry GetEntry(string name)
        {
            EnsureInitialized();
            return _byName.TryGetValue(name, out var entry) ? entry : null;
        }

        /// <summary>Get all categories that have VFX</summary>
        public VFXCategoryType[] GetActiveCategories()
        {
            EnsureInitialized();
            var result = new List<VFXCategoryType>();
            foreach (var kvp in _byCategory)
                if (kvp.Value.Count > 0)
                    result.Add(kvp.Key);
            return result.ToArray();
        }

#if UNITY_EDITOR
        /// <summary>Add or update entry (Editor only)</summary>
        public void AddOrUpdateEntry(VisualEffectAsset asset, VFXCategoryType category, string[] tags = null)
        {
            if (asset == null) return;

            var existing = _entries.Find(e => e.asset == asset);
            if (existing != null)
            {
                existing.category = category;
                if (tags != null) existing.tags = tags;
            }
            else
            {
                _entries.Add(new VFXCatalogEntry
                {
                    name = asset.name,
                    asset = asset,
                    category = category,
                    tags = tags ?? Array.Empty<string>(),
                    includedInBuild = true
                });
            }
            _initialized = false;
        }

        /// <summary>Remove entry (Editor only)</summary>
        public void RemoveEntry(VisualEffectAsset asset)
        {
            _entries.RemoveAll(e => e.asset == asset);
            _initialized = false;
        }

        /// <summary>Clear all entries (Editor only)</summary>
        public void Clear()
        {
            _entries.Clear();
            _initialized = false;
        }

        /// <summary>Remove null entries (Editor only)</summary>
        public int CleanupNullEntries()
        {
            int removed = _entries.RemoveAll(e => e.asset == null);
            _initialized = false;
            return removed;
        }
#endif
    }
}
