// VFX Selector UI - UI Toolkit based VFX switcher
// Matches original MetavidoVFX style with Inconsolata font and dark theme

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using XRRAI.VFXBinders;

namespace XRRAI.UI
{
    /// <summary>
    /// UI Toolkit-based VFX selector matching original MetavidoVFX style.
    /// Displays VFX options as radio buttons with FPS/particle stats.
    /// </summary>
    public class VFXSelectorUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private PanelSettings panelSettings;

        [Header("VFX")]
        [SerializeField] private VisualEffect[] vfxList;
        [SerializeField] private VFXCatalog vfxCatalog;
        [SerializeField] private string vfxResourceFolder = "VFX";
        [SerializeField] private bool autoPopulateFromResources = true;

        [Header("Auto Cycle")]
        [SerializeField] private float autoCycleInterval = 5f;

        // UI Elements
        private RadioButtonGroup vfxSelector;
        private Toggle autoCycleToggle;
        private Label fpsLabel;
        private Label particleLabel;

        // State
        private int currentVFXIndex = 0;
        private bool isAutoCycling = false;
        private float autoCycleTimer = 0f;
        private float[] fpsBuffer = new float[30];
        private int fpsBufferIndex = 0;

        void Start()
        {
            if (autoPopulateFromResources)
            {
                PopulateFromResources();
            }

            SetupUI();
        }

        void PopulateFromResources()
        {
            var assets = vfxCatalog != null
                ? vfxCatalog.GetAllAssets()
                : Resources.LoadAll<VisualEffectAsset>(vfxResourceFolder);
            if (assets.Length > 0)
            {
                // Create VFX instances from assets
                var container = new GameObject("VFX_Container");
                container.transform.SetParent(transform);

                var vfxInstances = new List<VisualEffect>();
                foreach (var asset in assets)
                {
                    var vfxObj = new GameObject($"VFX_{asset.name}");
                    vfxObj.transform.SetParent(container.transform);

                    var vfx = vfxObj.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = asset;
                    vfx.enabled = false; // Start disabled

                    vfxInstances.Add(vfx);
                }

                vfxList = vfxInstances.ToArray();

                // Enable first VFX
                if (vfxList.Length > 0)
                {
                    vfxList[0].enabled = true;
                }

                Debug.Log($"[VFXSelectorUI] Loaded {assets.Length} VFX from Resources/{vfxResourceFolder}");
            }
        }

        void SetupUI()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    uiDocument = gameObject.AddComponent<UIDocument>();
                }
            }

            // Load UXML
            var visualTree = Resources.Load<VisualTreeAsset>("VFXSelector");
#if UNITY_EDITOR
            if (visualTree == null)
            {
                // Try loading from Assets/UI (Editor fallback)
                visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/VFXSelector.uxml");
            }
#endif

            if (visualTree != null)
            {
                uiDocument.visualTreeAsset = visualTree;
            }

            // Get UI elements
            var root = uiDocument.rootVisualElement;

            vfxSelector = root.Q<RadioButtonGroup>("vfx-selector");
            autoCycleToggle = root.Q<Toggle>("auto-cycle-toggle");
            fpsLabel = root.Q<Label>("fps-label");
            particleLabel = root.Q<Label>("particle-label");

            // Populate VFX choices
            if (vfxSelector != null && vfxList != null)
            {
                var choices = new List<string>();
                foreach (var vfx in vfxList)
                {
                    if (vfx != null && vfx.visualEffectAsset != null)
                    {
                        choices.Add(vfx.visualEffectAsset.name);
                    }
                }

                vfxSelector.choices = choices;
                vfxSelector.value = 0;
                vfxSelector.RegisterValueChangedCallback(OnVFXSelected);
            }

            // Auto cycle toggle
            if (autoCycleToggle != null)
            {
                autoCycleToggle.RegisterValueChangedCallback(evt =>
                {
                    isAutoCycling = evt.newValue;
                    autoCycleTimer = 0f;
                });
            }
        }

        void OnVFXSelected(ChangeEvent<int> evt)
        {
            SelectVFX(evt.newValue);
        }

        void SelectVFX(int index)
        {
            if (vfxList == null || index < 0 || index >= vfxList.Length) return;

            // Disable all, enable selected
            for (int i = 0; i < vfxList.Length; i++)
            {
                if (vfxList[i] != null)
                {
                    bool isActive = (i == index);
                    vfxList[i].enabled = isActive;

                    if (vfxList[i].HasBool("Spawn"))
                    {
                        vfxList[i].SetBool("Spawn", isActive);
                    }

                    if (isActive)
                    {
                        vfxList[i].Reinit();
                    }
                }
            }

            currentVFXIndex = index;
            Debug.Log($"[VFXSelectorUI] Selected: {vfxList[index]?.visualEffectAsset?.name}");
        }

        void Update()
        {
            UpdateStats();
            UpdateAutoCycle();
        }

        void UpdateStats()
        {
            // FPS calculation
            fpsBuffer[fpsBufferIndex] = 1f / Time.unscaledDeltaTime;
            fpsBufferIndex = (fpsBufferIndex + 1) % fpsBuffer.Length;

            float avgFps = 0f;
            foreach (float fps in fpsBuffer)
            {
                avgFps += fps;
            }
            avgFps /= fpsBuffer.Length;

            if (fpsLabel != null)
            {
                fpsLabel.text = $"FPS: {avgFps:F0}";
            }

            // Particle count
            int totalParticles = 0;
            if (vfxList != null)
            {
                foreach (var vfx in vfxList)
                {
                    if (vfx != null && vfx.enabled)
                    {
                        totalParticles += vfx.aliveParticleCount;
                    }
                }
            }

            if (particleLabel != null)
            {
                particleLabel.text = $"Particles: {totalParticles:N0}";
            }
        }

        void UpdateAutoCycle()
        {
            if (!isAutoCycling || vfxList == null || vfxList.Length <= 1) return;

            autoCycleTimer += Time.deltaTime;
            if (autoCycleTimer >= autoCycleInterval)
            {
                autoCycleTimer = 0f;
                int nextIndex = (currentVFXIndex + 1) % vfxList.Length;
                SelectVFX(nextIndex);

                // Update radio button
                if (vfxSelector != null)
                {
                    vfxSelector.SetValueWithoutNotify(nextIndex);
                }
            }
        }

        /// <summary>
        /// Programmatically select a VFX by index
        /// </summary>
        public void SetVFXIndex(int index)
        {
            SelectVFX(index);
            if (vfxSelector != null)
            {
                vfxSelector.SetValueWithoutNotify(index);
            }
        }

        /// <summary>
        /// Get current VFX index
        /// </summary>
        public int CurrentIndex => currentVFXIndex;

        /// <summary>
        /// Toggle auto-cycle mode
        /// </summary>
        public void SetAutoCycle(bool enabled)
        {
            isAutoCycling = enabled;
            if (autoCycleToggle != null)
            {
                autoCycleToggle.SetValueWithoutNotify(enabled);
            }
        }
    }
}
