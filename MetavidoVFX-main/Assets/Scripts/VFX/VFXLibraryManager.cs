// VFXLibraryManager - Auto-populates ALL_VFX parent with VFX assets sorted by category
// Each VFX gets: VisualEffect, VFXPropertyBinder, VFXARDataBinder, VFXCategory
// Attach to ALL_VFX parent GameObject
//
// VFX persist across play/stop when created via "Populate Library (Editor)" context menu

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using MetavidoVFX.VFX.Binders;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MetavidoVFX.VFX
{
    /// <summary>
    /// Manages a library of VFX assets, auto-populating them as child GameObjects
    /// with proper bindings for AR data. Sorted by category.
    /// VFX created in Editor mode persist across play/stop transitions.
    /// </summary>
    public class VFXLibraryManager : MonoBehaviour
    {
        [Header("VFX Sources")]
        [SerializeField] private string[] resourceFolders = { "VFX" };
        [Tooltip("On Start, rebuild runtime lists from existing children instead of creating new VFX")]
        [SerializeField] private bool useExistingChildren = true;
        [SerializeField] private bool includeSubfolders = true;

        [Header("Category Organization")]
        [SerializeField] private bool organizeByCategory = true;
        [SerializeField] private bool createCategoryContainers = true;

        [Header("Default Bindings")]
        [SerializeField] private bool addVFXPropertyBinder = true;
        [SerializeField] private bool addVFXARDataBinder = true;
        [SerializeField] private bool addVFXCategory = true;

        [Header("Initial State")]
        [SerializeField] private bool startAllDisabled = true;
        [SerializeField] private int maxActiveVFX = 3; // Limit for performance

        // Runtime state
        private Dictionary<VFXCategoryType, List<VFXEntry>> _vfxByCategory = new();
        private List<VFXEntry> _allVFX = new();
        private HashSet<VFXEntry> _activeVFX = new();

        /// <summary>
        /// Entry for each VFX instance
        /// </summary>
        public class VFXEntry
        {
            public GameObject GameObject;
            public VisualEffect VFX;
            public VFXCategory Category;
            public VFXPropertyBinder PropertyBinder;
            public VFXARDataBinder ARBinder;
            public string AssetName;
            public VFXCategoryType CategoryType;

            public bool IsActive => VFX != null && VFX.enabled;
        }

        // Events
        public event System.Action<VFXEntry> OnVFXCreated;
        public event System.Action<VFXEntry, bool> OnVFXToggled;
        public event System.Action OnLibraryPopulated;

        // Public API
        public IReadOnlyList<VFXEntry> AllVFX => _allVFX;
        public IReadOnlyDictionary<VFXCategoryType, List<VFXEntry>> VFXByCategory => _vfxByCategory;
        public IReadOnlyCollection<VFXEntry> ActiveVFX => _activeVFX;
        public int ActiveCount => _activeVFX.Count;
        public int MaxActiveVFX { get => maxActiveVFX; set => maxActiveVFX = value; }

        void Start()
        {
            // If we have existing children (created in Editor), rebuild lists from them
            if (useExistingChildren && transform.childCount > 0)
            {
                RebuildFromChildren();
            }
            else
            {
                // No existing children - populate fresh (runtime-only, won't persist)
                PopulateLibraryRuntime();
            }
        }

        /// <summary>
        /// Rebuild runtime lists from existing child VFX (for persistent VFX)
        /// </summary>
        [ContextMenu("Rebuild From Children")]
        public void RebuildFromChildren()
        {
            _allVFX.Clear();
            _vfxByCategory.Clear();
            _activeVFX.Clear();

            // Initialize category dictionary
            foreach (VFXCategoryType cat in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                _vfxByCategory[cat] = new List<VFXEntry>();
            }

            // Scan all children for VisualEffect components
            var allVfxComponents = GetComponentsInChildren<VisualEffect>(true);
            Debug.Log($"[VFXLibrary] Found {allVfxComponents.Length} existing VFX in children");

            foreach (var vfx in allVfxComponents)
            {
                var entry = new VFXEntry
                {
                    GameObject = vfx.gameObject,
                    VFX = vfx,
                    AssetName = vfx.visualEffectAsset != null ? vfx.visualEffectAsset.name : vfx.gameObject.name,
                    PropertyBinder = vfx.GetComponent<VFXPropertyBinder>(),
                    ARBinder = vfx.GetComponent<VFXARDataBinder>(),
                    Category = vfx.GetComponent<VFXCategory>()
                };

                // Detect category from VFXCategory component or name
                if (entry.Category != null)
                {
                    entry.CategoryType = entry.Category.Category;
                }
                else
                {
                    entry.CategoryType = DetectCategory(entry.AssetName);
                }

                _allVFX.Add(entry);
                _vfxByCategory[entry.CategoryType].Add(entry);

                // Track active state
                if (entry.IsActive)
                {
                    _activeVFX.Add(entry);
                }
            }

            // Sort each category by name
            foreach (var cat in _vfxByCategory.Keys.ToList())
            {
                _vfxByCategory[cat] = _vfxByCategory[cat].OrderBy(e => e.AssetName).ToList();
            }

            Debug.Log($"[VFXLibrary] Rebuilt {_allVFX.Count} VFX entries from existing children");
            OnLibraryPopulated?.Invoke();
        }

        /// <summary>
        /// Populate library at runtime (VFX won't persist after stopping play mode)
        /// </summary>
        [ContextMenu("Populate Library (Runtime)")]
        public void PopulateLibraryRuntime()
        {
            ClearLibraryRuntime();
            CreateVFXFromResources(persistent: false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Populate library in Editor mode (VFX WILL persist after stopping play mode)
        /// </summary>
        [ContextMenu("Populate Library (Editor - Persistent)")]
        public void PopulateLibraryEditor()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[VFXLibrary] Use 'Populate Library (Runtime)' during play mode");
                return;
            }

            ClearLibraryEditor();
            CreateVFXFromResources(persistent: true);

            // Mark scene dirty so changes are saved
            EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        /// <summary>
        /// Clear library in Editor mode (with Undo support)
        /// </summary>
        [ContextMenu("Clear Library (Editor)")]
        public void ClearLibraryEditor()
        {
            if (Application.isPlaying)
            {
                ClearLibraryRuntime();
                return;
            }

            Undo.SetCurrentGroupName("Clear VFX Library");
            int group = Undo.GetCurrentGroup();

            // Destroy all children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            }

            _allVFX.Clear();
            _vfxByCategory.Clear();
            _activeVFX.Clear();

            Undo.CollapseUndoOperations(group);
            Debug.Log("[VFXLibrary] Library cleared (Editor)");
        }
#endif

        /// <summary>
        /// Clear library at runtime
        /// </summary>
        public void ClearLibraryRuntime()
        {
            foreach (var entry in _allVFX)
            {
                if (entry.GameObject != null)
                {
                    if (Application.isPlaying)
                        Destroy(entry.GameObject);
                    else
                        DestroyImmediate(entry.GameObject);
                }
            }

            // Clear category containers
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            _allVFX.Clear();
            _vfxByCategory.Clear();
            _activeVFX.Clear();
        }

        /// <summary>
        /// Legacy method - calls appropriate clear based on context
        /// </summary>
        [ContextMenu("Clear Library")]
        public void ClearLibrary()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ClearLibraryEditor();
                return;
            }
#endif
            ClearLibraryRuntime();
        }

        /// <summary>
        /// Legacy method - calls appropriate populate based on context
        /// </summary>
        public void PopulateLibrary()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                PopulateLibraryEditor();
                return;
            }
#endif
            PopulateLibraryRuntime();
        }

        /// <summary>
        /// Create VFX instances from Resources folders
        /// </summary>
        private void CreateVFXFromResources(bool persistent)
        {
            // Initialize category dictionary
            foreach (VFXCategoryType cat in System.Enum.GetValues(typeof(VFXCategoryType)))
            {
                _vfxByCategory[cat] = new List<VFXEntry>();
            }

            // Create category containers if needed
            Dictionary<VFXCategoryType, Transform> categoryContainers = new();
            if (createCategoryContainers)
            {
                foreach (VFXCategoryType cat in System.Enum.GetValues(typeof(VFXCategoryType)))
                {
                    var container = new GameObject($"[{cat}]");
                    container.transform.SetParent(transform);
                    container.transform.localPosition = Vector3.zero;
                    categoryContainers[cat] = container.transform;

#if UNITY_EDITOR
                    if (persistent && !Application.isPlaying)
                    {
                        Undo.RegisterCreatedObjectUndo(container, "Create VFX Category");
                    }
#endif
                }
            }

            // Load all VFX assets
            var allAssets = new List<VisualEffectAsset>();
            foreach (var folder in resourceFolders)
            {
                var assets = Resources.LoadAll<VisualEffectAsset>(folder);
                allAssets.AddRange(assets);
                Debug.Log($"[VFXLibrary] Loaded {assets.Length} VFX from Resources/{folder}");
            }

            // Remove duplicates by name
            allAssets = allAssets.GroupBy(a => a.name).Select(g => g.First()).ToList();
            Debug.Log($"[VFXLibrary] Total unique VFX assets: {allAssets.Count}");

            // Create VFX instances
            foreach (var asset in allAssets)
            {
                var entry = CreateVFXEntry(asset, persistent);
                if (entry == null) continue;

                // Parent to category container or this object
                if (createCategoryContainers && categoryContainers.TryGetValue(entry.CategoryType, out var parent))
                {
                    entry.GameObject.transform.SetParent(parent);
                }
                else
                {
                    entry.GameObject.transform.SetParent(transform);
                }

                // Add to collections
                _allVFX.Add(entry);
                _vfxByCategory[entry.CategoryType].Add(entry);

                // Initial state
                if (startAllDisabled)
                {
                    SetVFXActive(entry, false);
                }

                OnVFXCreated?.Invoke(entry);
            }

            // Sort each category by name
            foreach (var cat in _vfxByCategory.Keys.ToList())
            {
                _vfxByCategory[cat] = _vfxByCategory[cat].OrderBy(e => e.AssetName).ToList();
            }

            string modeStr = persistent ? "persistent" : "runtime";
            Debug.Log($"[VFXLibrary] Created {_allVFX.Count} VFX instances ({modeStr}) across {_vfxByCategory.Count(kvp => kvp.Value.Count > 0)} categories");
            OnLibraryPopulated?.Invoke();
        }

        /// <summary>
        /// Create a VFX entry from an asset
        /// </summary>
        private VFXEntry CreateVFXEntry(VisualEffectAsset asset, bool persistent)
        {
            var entry = new VFXEntry
            {
                AssetName = asset.name,
                CategoryType = DetectCategory(asset.name)
            };

            // Create GameObject
            entry.GameObject = new GameObject(asset.name);
            entry.GameObject.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
            if (persistent && !Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(entry.GameObject, $"Create VFX {asset.name}");
            }
#endif

            // Add VisualEffect
            entry.VFX = entry.GameObject.AddComponent<VisualEffect>();
            entry.VFX.visualEffectAsset = asset;

            // Add VFXPropertyBinder
            if (addVFXPropertyBinder)
            {
                entry.PropertyBinder = entry.GameObject.AddComponent<VFXPropertyBinder>();
            }

            // Add VFXARDataBinder
            if (addVFXARDataBinder)
            {
                entry.ARBinder = entry.GameObject.AddComponent<VFXARDataBinder>();
                ConfigureARBinder(entry.ARBinder, entry.CategoryType);
            }

            // Add VFXCategory
            if (addVFXCategory)
            {
                entry.Category = entry.GameObject.AddComponent<VFXCategory>();
                entry.Category.SetCategory(entry.CategoryType);
            }

            return entry;
        }

        /// <summary>
        /// Detect category from asset name
        /// </summary>
        private VFXCategoryType DetectCategory(string assetName)
        {
            string name = assetName.ToLower();

            if (name.Contains("hand"))
                return VFXCategoryType.Hands;
            if (name.Contains("face"))
                return VFXCategoryType.Face;
            if (name.Contains("audio") || name.Contains("sound") || name.Contains("wave"))
                return VFXCategoryType.Audio;
            if (name.Contains("environment") || name.Contains("env") || name.Contains("grid") || name.Contains("world"))
                return VFXCategoryType.Environment;
            if (name.Contains("people") || name.Contains("body") || name.Contains("depth") || name.Contains("stencil"))
                return VFXCategoryType.People;

            return VFXCategoryType.People; // Default
        }

        /// <summary>
        /// Configure AR binder based on category
        /// </summary>
        private void ConfigureARBinder(VFXARDataBinder binder, VFXCategoryType category)
        {
            switch (category)
            {
                case VFXCategoryType.People:
                    binder.bindDepthMap = true;
                    binder.bindStencilMap = true;
                    binder.bindColorMap = true;
                    binder.bindPositionMap = true;
                    binder.maskDepthWithStencil = true;
                    break;
                case VFXCategoryType.Hands:
                    binder.bindDepthMap = false;
                    binder.bindStencilMap = false;
                    binder.bindColorMap = true;
                    binder.bindPositionMap = false;
                    break;
                case VFXCategoryType.Environment:
                    binder.bindDepthMap = true;
                    binder.bindStencilMap = false;
                    binder.bindColorMap = true;
                    binder.bindPositionMap = false;
                    binder.maskDepthWithStencil = false;
                    break;
                case VFXCategoryType.Audio:
                    binder.bindDepthMap = false;
                    binder.bindStencilMap = false;
                    binder.bindColorMap = false;
                    binder.bindPositionMap = false;
                    break;
                default:
                    // Default bindings are fine
                    break;
            }
        }

        /// <summary>
        /// Toggle a VFX on/off
        /// </summary>
        public bool ToggleVFX(VFXEntry entry)
        {
            if (entry == null) return false;
            bool newState = !entry.IsActive;
            SetVFXActive(entry, newState);
            return newState;
        }

        /// <summary>
        /// Set VFX active state
        /// </summary>
        public void SetVFXActive(VFXEntry entry, bool active)
        {
            if (entry?.VFX == null) return;

            // Check max active limit
            if (active && _activeVFX.Count >= maxActiveVFX && !_activeVFX.Contains(entry))
            {
                Debug.LogWarning($"[VFXLibrary] Max active VFX limit ({maxActiveVFX}) reached");
                return;
            }

            entry.VFX.enabled = active;
            if (entry.VFX.HasBool("Spawn"))
            {
                entry.VFX.SetBool("Spawn", active);
            }

            if (active)
            {
                entry.VFX.Reinit();
                _activeVFX.Add(entry);
            }
            else
            {
                _activeVFX.Remove(entry);
            }

            OnVFXToggled?.Invoke(entry, active);
        }

        /// <summary>
        /// Enable only one VFX (disable all others)
        /// </summary>
        public void SetSoloVFX(VFXEntry entry)
        {
            DisableAll();
            if (entry != null)
            {
                SetVFXActive(entry, true);
            }
        }

        /// <summary>
        /// Disable all VFX
        /// </summary>
        public void DisableAll()
        {
            foreach (var entry in _allVFX)
            {
                SetVFXActive(entry, false);
            }
        }

        /// <summary>
        /// Enable all VFX in a category
        /// </summary>
        public void EnableCategory(VFXCategoryType category)
        {
            if (!_vfxByCategory.TryGetValue(category, out var entries)) return;
            foreach (var entry in entries)
            {
                SetVFXActive(entry, true);
            }
        }

        /// <summary>
        /// Disable all VFX in a category
        /// </summary>
        public void DisableCategory(VFXCategoryType category)
        {
            if (!_vfxByCategory.TryGetValue(category, out var entries)) return;
            foreach (var entry in entries)
            {
                SetVFXActive(entry, false);
            }
        }

        /// <summary>
        /// Get entries in a category
        /// </summary>
        public IReadOnlyList<VFXEntry> GetCategory(VFXCategoryType category)
        {
            return _vfxByCategory.TryGetValue(category, out var entries) ? entries : new List<VFXEntry>();
        }

        /// <summary>
        /// Find entry by name
        /// </summary>
        public VFXEntry FindByName(string name)
        {
            return _allVFX.FirstOrDefault(e => e.AssetName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
