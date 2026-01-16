// VFXLibraryManager - Hybrid Bridge Pipeline Integration
// Auto-populates VFX with ARDepthSource + VFXARBinder (new lightweight pipeline)
// Removes legacy components (VFXARDataBinder, VFXPropertyBinder, VFXBinderManager)
//
// Key features:
// - One-click pipeline setup: ensures ARDepthSource exists, adds VFXARBinder to all VFX
// - Auto-detect bindings per VFX based on exposed properties
// - Removes legacy components automatically
// - Category organization for UI
// - Runtime-compatible via Resources folder
//
// Usage:
// 1. Attach to parent GameObject (e.g., "VFX_Container")
// 2. Context menu: "Setup Complete Pipeline" - does everything
// 3. Or use Editor menu: H3M > VFX Pipeline Master > Setup Complete Pipeline

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MetavidoVFX.VFX
{
    /// <summary>
    /// Manages a library of VFX with the Hybrid Bridge Pipeline.
    /// Uses ARDepthSource (singleton compute) + VFXARBinder (lightweight per-VFX binding).
    /// Automatically removes legacy components and sets up proper bindings.
    /// </summary>
    public class VFXLibraryManager : MonoBehaviour
    {
        [Header("VFX Sources - Resources (Runtime Compatible)")]
        [Tooltip("Folders inside Resources/ to load VFX from at runtime")]
        [SerializeField] private string[] resourceFolders = { "VFX" };

        [Header("VFX Sources - Direct References")]
        [Tooltip("Directly referenced VFX assets (drag & drop)")]
        [SerializeField] private VisualEffectAsset[] directVFXAssets;

#if UNITY_EDITOR
        [Header("VFX Sources - Project Search (Editor Only)")]
        [Tooltip("Search these paths for VFX assets")]
        [SerializeField] private string[] searchPaths = { "Assets/VFX", "Assets/Resources/VFX" };
        [Tooltip("Include VFX from all subfolders")]
        [SerializeField] private bool includeSubfolders = true;
        [Tooltip("Use project search in Editor")]
        [SerializeField] private bool useProjectSearch = true;
#endif

        [Header("Behavior")]
        [Tooltip("On Start, rebuild runtime lists from existing children")]
        [SerializeField] private bool useExistingChildren = true;
        [Tooltip("Remove legacy components (VFXARDataBinder, VFXPropertyBinder) automatically")]
        [SerializeField] private bool removeLegacyComponents = true;

        [Header("Category Organization")]
        [SerializeField] private bool organizeByCategory = true;
        [SerializeField] private bool createCategoryContainers = true;

        [Header("Initial State")]
        [SerializeField] private bool startAllDisabled = true;
        [SerializeField] private int maxActiveVFX = 3;

        [Header("Pipeline Reference")]
        [Tooltip("ARDepthSource instance (auto-found if null)")]
        [SerializeField] private ARDepthSource _arDepthSource;

        // Runtime state
        private Dictionary<VFXCategoryType, List<VFXEntry>> _vfxByCategory = new();
        private List<VFXEntry> _allVFX = new();
        private HashSet<VFXEntry> _activeVFX = new();

        /// <summary>
        /// Entry for each VFX instance - uses new pipeline components
        /// </summary>
        public class VFXEntry
        {
            public GameObject GameObject;
            public VisualEffect VFX;
            public VFXARBinder ARBinder;        // New lightweight binder
            public VFXCategory Category;
            public string AssetName;
            public VFXCategoryType CategoryType;

            public bool IsActive => VFX != null && VFX.enabled && GameObject.activeInHierarchy;
            public bool IsBound => ARBinder != null && ARBinder.IsBound;
            public int BoundCount => ARBinder?.BoundCount ?? 0;
        }

        // Events
        public event System.Action<VFXEntry> OnVFXCreated;
        public event System.Action<VFXEntry, bool> OnVFXToggled;
        public event System.Action OnLibraryPopulated;
        public event System.Action OnPipelineSetupComplete;

        // Public API
        public IReadOnlyList<VFXEntry> AllVFX => _allVFX;
        public IReadOnlyDictionary<VFXCategoryType, List<VFXEntry>> VFXByCategory => _vfxByCategory;
        public IReadOnlyCollection<VFXEntry> ActiveVFX => _activeVFX;
        public int ActiveCount => _activeVFX.Count;
        public int TotalCount => _allVFX.Count;
        public int MaxActiveVFX { get => maxActiveVFX; set => maxActiveVFX = value; }
        public ARDepthSource DepthSource => _arDepthSource != null ? _arDepthSource : ARDepthSource.Instance;
        public bool IsPipelineReady => DepthSource != null && DepthSource.IsReady;

        void Start()
        {
            // Ensure ARDepthSource exists
            EnsureARDepthSource();

            // If we have existing children (created in Editor), rebuild lists from them
            if (useExistingChildren && transform.childCount > 0)
            {
                RebuildFromChildren();
            }
            else
            {
                PopulateLibraryRuntime();
            }
        }

        #region Pipeline Setup

        /// <summary>
        /// One-click complete pipeline setup
        /// </summary>
        [ContextMenu("Setup Complete Pipeline")]
        public void SetupCompletePipeline()
        {
            Debug.Log("[VFXLibrary] === Setting up Complete Pipeline ===");

            // 1. Ensure ARDepthSource exists
            EnsureARDepthSource();

            // 2. Remove legacy components from scene
            if (removeLegacyComponents)
            {
                RemoveAllLegacyComponents();
            }

            // 3. Populate library (creates VFX with VFXARBinder)
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                PopulateLibraryEditor();
            }
            else
#endif
            {
                PopulateLibraryRuntime();
            }

            // 4. Auto-detect bindings for all VFX
            AutoDetectAllBindings();

            Debug.Log($"[VFXLibrary] Pipeline setup complete: {_allVFX.Count} VFX ready");
            OnPipelineSetupComplete?.Invoke();
        }

        /// <summary>
        /// Ensure ARDepthSource exists in scene (creates if missing)
        /// </summary>
        [ContextMenu("Ensure ARDepthSource")]
        public void EnsureARDepthSource()
        {
            if (_arDepthSource == null)
            {
                _arDepthSource = FindFirstObjectByType<ARDepthSource>();
            }

            if (_arDepthSource == null)
            {
                Debug.Log("[VFXLibrary] Creating ARDepthSource...");
                var go = new GameObject("ARDepthSource");
                _arDepthSource = go.AddComponent<ARDepthSource>();

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(go, "Create ARDepthSource");
                }
#endif
                Debug.Log("[VFXLibrary] ARDepthSource created");
            }
            else
            {
                Debug.Log($"[VFXLibrary] ARDepthSource found: {_arDepthSource.name}");
            }
        }

        /// <summary>
        /// Remove all legacy pipeline components from scene
        /// </summary>
        [ContextMenu("Remove All Legacy Components")]
        public void RemoveAllLegacyComponents()
        {
            int removed = 0;

            // Find and remove VFXBinderManager (legacy centralized manager)
            var binderManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m.GetType().Name == "VFXBinderManager")
                .ToArray();
            foreach (var mgr in binderManagers)
            {
                Debug.Log($"[VFXLibrary] Removing legacy VFXBinderManager from {mgr.gameObject.name}");
                RemoveComponent(mgr);
                removed++;
            }

            // Find and remove VFXARDataBinder (legacy per-VFX binder with compute)
            var oldBinders = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m.GetType().Name == "VFXARDataBinder")
                .ToArray();
            foreach (var binder in oldBinders)
            {
                Debug.Log($"[VFXLibrary] Removing legacy VFXARDataBinder from {binder.gameObject.name}");
                RemoveComponent(binder);
                removed++;
            }

            // Find and remove orphaned VFXPropertyBinder (Unity's built-in, but we don't need it)
            var propertyBinders = FindObjectsByType<UnityEngine.VFX.Utility.VFXPropertyBinder>(FindObjectsSortMode.None);
            foreach (var binder in propertyBinders)
            {
                // Only remove if it has no active bindings
                var bindings = binder.GetPropertyBinders<UnityEngine.VFX.Utility.VFXBinderBase>();
                if (bindings == null || !bindings.Any())
                {
                    Debug.Log($"[VFXLibrary] Removing empty VFXPropertyBinder from {binder.gameObject.name}");
                    RemoveComponent(binder);
                    removed++;
                }
            }

            Debug.Log($"[VFXLibrary] Removed {removed} legacy components");
        }

        /// <summary>
        /// Remove legacy components from a specific GameObject
        /// </summary>
        public void RemoveLegacyComponentsFrom(GameObject go)
        {
            if (go == null) return;

            // Remove VFXARDataBinder
            var oldBinder = go.GetComponent("VFXARDataBinder") as MonoBehaviour;
            if (oldBinder != null)
            {
                Debug.Log($"[VFXLibrary] Removing VFXARDataBinder from {go.name}");
                RemoveComponent(oldBinder);
            }

            // Remove empty VFXPropertyBinder
            var propertyBinder = go.GetComponent<UnityEngine.VFX.Utility.VFXPropertyBinder>();
            if (propertyBinder != null)
            {
                var bindings = propertyBinder.GetPropertyBinders<UnityEngine.VFX.Utility.VFXBinderBase>();
                if (bindings == null || !bindings.Any())
                {
                    Debug.Log($"[VFXLibrary] Removing empty VFXPropertyBinder from {go.name}");
                    RemoveComponent(propertyBinder);
                }
            }
        }

        /// <summary>
        /// Auto-detect bindings for all VFX in library
        /// </summary>
        [ContextMenu("Auto-Detect All Bindings")]
        public void AutoDetectAllBindings()
        {
            int count = 0;
            foreach (var entry in _allVFX)
            {
                if (entry.ARBinder != null)
                {
                    entry.ARBinder.AutoDetectBindings();
                    count++;
                }
            }
            Debug.Log($"[VFXLibrary] Auto-detected bindings for {count} VFX");
        }

        private void RemoveComponent(Component component)
        {
            if (component == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(component);
            }
            else
#endif
            {
                Destroy(component);
            }
        }

        #endregion

        #region Library Management

        /// <summary>
        /// Rebuild runtime lists from existing child VFX
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
                // Remove legacy components if configured
                if (removeLegacyComponents)
                {
                    RemoveLegacyComponentsFrom(vfx.gameObject);
                }

                // Ensure VFXARBinder exists
                var arBinder = vfx.GetComponent<VFXARBinder>();
                if (arBinder == null)
                {
                    arBinder = vfx.gameObject.AddComponent<VFXARBinder>();
                    arBinder.AutoDetectBindings();
                    Debug.Log($"[VFXLibrary] Added VFXARBinder to {vfx.name}");
                }

                var entry = new VFXEntry
                {
                    GameObject = vfx.gameObject,
                    VFX = vfx,
                    AssetName = vfx.visualEffectAsset != null ? vfx.visualEffectAsset.name : vfx.gameObject.name,
                    ARBinder = arBinder,
                    Category = vfx.GetComponent<VFXCategory>()
                };

                // Detect category
                entry.CategoryType = entry.Category != null
                    ? entry.Category.Category
                    : DetectCategory(entry.AssetName);

                _allVFX.Add(entry);
                _vfxByCategory[entry.CategoryType].Add(entry);

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

            if (startAllDisabled)
            {
                DisableAll();
                Debug.Log($"[VFXLibrary] All VFX disabled at start");
            }

            OnLibraryPopulated?.Invoke();
        }

        /// <summary>
        /// Populate library (chooses Editor or Runtime mode automatically)
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
        /// Clear library (chooses Editor or Runtime mode automatically)
        /// </summary>
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
        /// Populate library at runtime
        /// </summary>
        [ContextMenu("Populate Library (Runtime)")]
        public void PopulateLibraryRuntime()
        {
            ClearLibraryRuntime();
            CreateVFXFromResources(persistent: false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Populate library in Editor mode (persists after stopping play mode)
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
        /// Create VFX instances from all configured sources
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

            // Collect all VFX assets
            var allAssets = new List<VisualEffectAsset>();

            // Source 1: Direct references
            if (directVFXAssets != null)
            {
                foreach (var asset in directVFXAssets)
                {
                    if (asset != null) allAssets.Add(asset);
                }
            }

            // Source 2: Resources folders
            if (resourceFolders != null)
            {
                foreach (var folder in resourceFolders)
                {
                    if (string.IsNullOrEmpty(folder)) continue;
                    var assets = Resources.LoadAll<VisualEffectAsset>(folder);
                    allAssets.AddRange(assets);
                    if (assets.Length > 0)
                    {
                        Debug.Log($"[VFXLibrary] Loaded {assets.Length} VFX from Resources/{folder}");
                    }
                }
            }

#if UNITY_EDITOR
            // Source 3: Project search (Editor only)
            if (useProjectSearch && searchPaths != null && !Application.isPlaying)
            {
                var projectAssets = FindVFXInProject();
                allAssets.AddRange(projectAssets);
                if (projectAssets.Count > 0)
                {
                    Debug.Log($"[VFXLibrary] Found {projectAssets.Count} VFX via project search");
                }
            }
#endif

            // Remove duplicates
            allAssets = allAssets.GroupBy(a => a.name).Select(g => g.First()).ToList();
            Debug.Log($"[VFXLibrary] Total unique VFX assets: {allAssets.Count}");

            // Create VFX instances
            foreach (var asset in allAssets)
            {
                var entry = CreateVFXEntry(asset, persistent);
                if (entry == null) continue;

                // Parent to category container
                if (createCategoryContainers && categoryContainers.TryGetValue(entry.CategoryType, out var parent))
                {
                    entry.GameObject.transform.SetParent(parent);
                }
                else
                {
                    entry.GameObject.transform.SetParent(transform);
                }

                _allVFX.Add(entry);
                _vfxByCategory[entry.CategoryType].Add(entry);

                if (startAllDisabled)
                {
                    SetVFXActive(entry, false);
                }

                OnVFXCreated?.Invoke(entry);
            }

            // Sort by name
            foreach (var cat in _vfxByCategory.Keys.ToList())
            {
                _vfxByCategory[cat] = _vfxByCategory[cat].OrderBy(e => e.AssetName).ToList();
            }

            string modeStr = persistent ? "persistent" : "runtime";
            Debug.Log($"[VFXLibrary] Created {_allVFX.Count} VFX instances ({modeStr})");

            OnLibraryPopulated?.Invoke();
        }

        /// <summary>
        /// Create a VFX entry with new pipeline components
        /// </summary>
        private VFXEntry CreateVFXEntry(VisualEffectAsset asset, bool persistent)
        {
            var entry = new VFXEntry
            {
                AssetName = asset.name,
                CategoryType = DetectCategory(asset.name)
            };

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

            // Add VFXARBinder (new lightweight pipeline)
            entry.ARBinder = entry.GameObject.AddComponent<VFXARBinder>();
            entry.ARBinder.AutoDetectBindings();

            // Add VFXCategory for organization
            entry.Category = entry.GameObject.AddComponent<VFXCategory>();
            entry.Category.SetCategory(entry.CategoryType);

            return entry;
        }

        #endregion

        #region Category Detection

        private VFXCategoryType DetectCategory(string assetName)
        {
            string name = assetName.ToLower();

            // Check folder naming convention: xxx_category_xxx
            if (name.Contains("_hand"))
                return VFXCategoryType.Hands;
            if (name.Contains("_face"))
                return VFXCategoryType.Face;
            if (name.Contains("_audio") || name.Contains("_sound") || name.Contains("_wave"))
                return VFXCategoryType.Audio;
            if (name.Contains("_environment") || name.Contains("_env") || name.Contains("_grid") || name.Contains("_world"))
                return VFXCategoryType.Environment;
            if (name.Contains("_people") || name.Contains("_body") || name.Contains("_depth") || name.Contains("_stencil"))
                return VFXCategoryType.People;

            // Fallback: check keywords anywhere
            if (name.Contains("hand"))
                return VFXCategoryType.Hands;
            if (name.Contains("face"))
                return VFXCategoryType.Face;
            if (name.Contains("audio") || name.Contains("sound"))
                return VFXCategoryType.Audio;
            if (name.Contains("environment") || name.Contains("grid") || name.Contains("world"))
                return VFXCategoryType.Environment;

            return VFXCategoryType.People; // Default
        }

        #endregion

        #region VFX Control

        public bool ToggleVFX(VFXEntry entry)
        {
            if (entry == null) return false;
            bool newState = !entry.IsActive;
            SetVFXActive(entry, newState);
            return newState;
        }

        public void SetVFXActive(VFXEntry entry, bool active)
        {
            if (entry?.VFX == null) return;

            if (active && _activeVFX.Count >= maxActiveVFX && !_activeVFX.Contains(entry))
            {
                Debug.LogWarning($"[VFXLibrary] Max active VFX limit ({maxActiveVFX}) reached");
                return;
            }

            entry.GameObject.SetActive(active);
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

        public void SetSoloVFX(VFXEntry entry)
        {
            DisableAll();
            if (entry != null)
            {
                SetVFXActive(entry, true);
            }
        }

        public void DisableAll()
        {
            foreach (var entry in _allVFX)
            {
                SetVFXActive(entry, false);
            }
        }

        public void EnableCategory(VFXCategoryType category)
        {
            if (!_vfxByCategory.TryGetValue(category, out var entries)) return;
            foreach (var entry in entries)
            {
                SetVFXActive(entry, true);
            }
        }

        public void DisableCategory(VFXCategoryType category)
        {
            if (!_vfxByCategory.TryGetValue(category, out var entries)) return;
            foreach (var entry in entries)
            {
                SetVFXActive(entry, false);
            }
        }

        public IReadOnlyList<VFXEntry> GetCategory(VFXCategoryType category)
        {
            return _vfxByCategory.TryGetValue(category, out var entries) ? entries : new List<VFXEntry>();
        }

        public VFXEntry FindByName(string name)
        {
            return _allVFX.FirstOrDefault(e => e.AssetName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public VFXEntry GetNextVFX(VFXEntry current = null)
        {
            if (_allVFX.Count == 0) return null;
            if (current == null) return _allVFX[0];
            int idx = _allVFX.IndexOf(current);
            return _allVFX[(idx + 1) % _allVFX.Count];
        }

        public VFXEntry GetPreviousVFX(VFXEntry current = null)
        {
            if (_allVFX.Count == 0) return null;
            if (current == null) return _allVFX[_allVFX.Count - 1];
            int idx = _allVFX.IndexOf(current);
            return _allVFX[(idx - 1 + _allVFX.Count) % _allVFX.Count];
        }

        #endregion

        #region Debug / Utility

        [ContextMenu("Debug Pipeline Status")]
        public void DebugPipelineStatus()
        {
            Debug.Log("=== VFXLibraryManager Pipeline Status ===");
            Debug.Log($"ARDepthSource: {(DepthSource != null ? DepthSource.name : "NOT FOUND")}");
            Debug.Log($"  IsReady: {DepthSource?.IsReady}");
            Debug.Log($"  UsingMockData: {DepthSource?.UsingMockData}");
            Debug.Log($"Total VFX: {_allVFX.Count}");
            Debug.Log($"Active VFX: {_activeVFX.Count}/{maxActiveVFX}");

            Debug.Log("--- VFX by Category ---");
            foreach (var kvp in _vfxByCategory.Where(k => k.Value.Count > 0))
            {
                Debug.Log($"  [{kvp.Key}]: {kvp.Value.Count} VFX");
                foreach (var entry in kvp.Value)
                {
                    string status = entry.IsActive ? "ACTIVE" : "inactive";
                    string binding = entry.IsBound ? $"bound:{entry.BoundCount}" : "not bound";
                    Debug.Log($"    - {entry.AssetName} ({status}, {binding})");
                }
            }

            // Check for legacy components
            var legacyBinderManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Count(m => m.GetType().Name == "VFXBinderManager");
            var legacyARDataBinders = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Count(m => m.GetType().Name == "VFXARDataBinder");

            Debug.Log("--- Legacy Components ---");
            Debug.Log($"  VFXBinderManager instances: {legacyBinderManagers}");
            Debug.Log($"  VFXARDataBinder instances: {legacyARDataBinders}");

            if (legacyBinderManagers > 0 || legacyARDataBinders > 0)
            {
                Debug.LogWarning("[VFXLibrary] Legacy components found! Use 'Remove All Legacy Components' to clean up.");
            }
        }

        [ContextMenu("List All VFX")]
        public void ListAllVFX()
        {
            Debug.Log($"=== VFX Library ({_allVFX.Count} total) ===");
            foreach (var kvp in _vfxByCategory.Where(k => k.Value.Count > 0).OrderBy(k => k.Key.ToString()))
            {
                Debug.Log($"\n[{kvp.Key}] ({kvp.Value.Count}):");
                foreach (var entry in kvp.Value.OrderBy(e => e.AssetName))
                {
                    Debug.Log($"  - {entry.AssetName}");
                }
            }
        }

#if UNITY_EDITOR
        private List<VisualEffectAsset> FindVFXInProject()
        {
            var results = new List<VisualEffectAsset>();
            string[] guids = AssetDatabase.FindAssets("t:VisualEffectAsset", searchPaths);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                bool matchesSearchPath = false;
                foreach (var searchPath in searchPaths)
                {
                    if (string.IsNullOrEmpty(searchPath)) continue;

                    if (includeSubfolders)
                    {
                        if (path.StartsWith(searchPath + "/") || path.StartsWith(searchPath))
                        {
                            matchesSearchPath = true;
                            break;
                        }
                    }
                    else
                    {
                        string directory = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
                        if (directory == searchPath || directory == searchPath.TrimEnd('/'))
                        {
                            matchesSearchPath = true;
                            break;
                        }
                    }
                }

                if (matchesSearchPath)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
                    if (asset != null) results.Add(asset);
                }
            }

            return results;
        }

        [ContextMenu("Add VFXARBinder to All Scene VFX")]
        public void AddBindersToAllSceneVFX()
        {
            var allVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);
            int added = 0;

            foreach (var vfx in allVFX)
            {
                if (vfx.GetComponent<VFXARBinder>() == null)
                {
                    var binder = vfx.gameObject.AddComponent<VFXARBinder>();
                    binder.AutoDetectBindings();
                    added++;
                    Debug.Log($"[VFXLibrary] Added VFXARBinder to {vfx.name}");
                }

                if (removeLegacyComponents)
                {
                    RemoveLegacyComponentsFrom(vfx.gameObject);
                }
            }

            Debug.Log($"[VFXLibrary] Added VFXARBinder to {added} VFX in scene");
        }
#endif

        #endregion
    }
}
