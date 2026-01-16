// Simple VFX UI - Original Metavido Style
// Minimal screen-space UI with tap-to-switch and auto-cycle
// Dark theme with Inconsolata-style monospace aesthetic

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using MetavidoVFX.VFX;

namespace MetavidoVFX.UI
{
    /// <summary>
    /// Simple IMGUI-based VFX selector matching original Metavido style.
    /// - Tap screen to cycle VFX
    /// - Small stats overlay (FPS, particles)
    /// - Optional auto-cycle mode
    /// - Minimal, non-intrusive design
    /// </summary>
    public class SimpleVFXUI : MonoBehaviour
    {
        void Log(string msg) { if (!VFXBinderManager.SuppressUILogs) Debug.Log(msg); }

        [Header("VFX")]
        [SerializeField] private VisualEffect[] vfxList;
        [SerializeField] private string vfxResourceFolder = "VFX";
        [SerializeField] private bool autoPopulateFromResources = true;

        [Header("Auto Cycle")]
        [SerializeField] private bool autoCycleEnabled = true;
        [SerializeField] private float autoCycleInterval = 5f;

        [Header("UI Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private bool showStats = true;
        [SerializeField] private bool showVFXName = true;
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.6f);
        [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Color accentColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Header("Touch")]
        [SerializeField] private bool tapToSwitch = true;
        [SerializeField] private float doubleTapTime = 0.3f;

        // State
        private int currentVFXIndex = 0;
        private float autoCycleTimer = 0f;
        private float lastTapTime = 0f;
        private string currentVFXName = "";

        // Stats
        private float fps = 0f;
        private float[] fpsBuffer = new float[30];
        private int fpsIndex = 0;
        private int particleCount = 0;

        // UI
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle boxStyle;
        private Texture2D backgroundTex;
        private bool stylesInitialized = false;

        // Name display fade
        private float nameDisplayTimer = 0f;
        private float nameDisplayDuration = 2f;

        void Start()
        {
            // Enable Enhanced Touch for new Input System
            EnhancedTouchSupport.Enable();

            if (autoPopulateFromResources)
            {
                PopulateFromResources();
            }

            // Use existing SpawnControlVFX if available
            if ((vfxList == null || vfxList.Length == 0) && transform.parent != null)
            {
                FindExistingVFX();
            }

            // Enable first VFX
            if (vfxList != null && vfxList.Length > 0)
            {
                SelectVFX(0);
            }
        }

        void OnDestroy()
        {
            if (backgroundTex != null)
            {
                Destroy(backgroundTex);
            }
            // Disable Enhanced Touch when destroyed
            if (EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();
            }
        }

        void FindExistingVFX()
        {
            // Look for SpawnControlVFX_Container
            var container = GameObject.Find("SpawnControlVFX_Container");
            if (container != null)
            {
                var vfxComponents = container.GetComponentsInChildren<VisualEffect>(true);
                if (vfxComponents.Length > 0)
                {
                    vfxList = vfxComponents;
                    Log($"[SimpleVFXUI] Found {vfxList.Length} VFX in SpawnControlVFX_Container");
                }
            }
        }

        void PopulateFromResources()
        {
            var assets = Resources.LoadAll<VisualEffectAsset>(vfxResourceFolder);
            if (assets.Length == 0)
            {
                Debug.LogWarning($"[SimpleVFXUI] No VFX assets found in Resources/{vfxResourceFolder}");
                return;
            }

            // Check if SpawnControlVFX_Container already exists
            var existingContainer = GameObject.Find("SpawnControlVFX_Container");
            if (existingContainer != null)
            {
                vfxList = existingContainer.GetComponentsInChildren<VisualEffect>(true);
                Log($"[SimpleVFXUI] Using existing SpawnControlVFX_Container with {vfxList.Length} VFX");
                return;
            }

            // Create new container
            var container = new GameObject("SpawnControlVFX_Container");
            var vfxInstances = new List<VisualEffect>();

            foreach (var asset in assets)
            {
                var vfxObj = new GameObject($"VFX_{asset.name}");
                vfxObj.transform.SetParent(container.transform);

                var vfx = vfxObj.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = asset;
                vfx.enabled = false;

                if (vfx.HasBool("Spawn"))
                {
                    vfx.SetBool("Spawn", false);
                }

                vfxInstances.Add(vfx);
            }

            vfxList = vfxInstances.ToArray();
            Log($"[SimpleVFXUI] Created {vfxList.Length} VFX from Resources/{vfxResourceFolder}");
        }

        void Update()
        {
            UpdateStats();
            UpdateInput();
            UpdateAutoCycle();
            UpdateNameDisplay();
        }

        void UpdateStats()
        {
            // FPS
            fpsBuffer[fpsIndex] = 1f / Time.unscaledDeltaTime;
            fpsIndex = (fpsIndex + 1) % fpsBuffer.Length;

            float total = 0f;
            foreach (float f in fpsBuffer) total += f;
            fps = total / fpsBuffer.Length;

            // Particle count
            particleCount = 0;
            if (vfxList != null)
            {
                foreach (var vfx in vfxList)
                {
                    if (vfx != null && vfx.enabled)
                    {
                        particleCount += vfx.aliveParticleCount;
                    }
                }
            }
        }

        void UpdateInput()
        {
            if (!tapToSwitch) return;

            // New Input System - Enhanced Touch
            if (Touch.activeTouches.Count > 0)
            {
                var touch = Touch.activeTouches[0];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    HandleTap(touch.screenPosition);
                }
            }

            // Mouse fallback (Editor) - using new Input System
            #if UNITY_EDITOR
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                // Ignore if clicking on UI area
                if (mousePos.y > Screen.height * 0.15f)
                {
                    HandleTap(mousePos);
                }
            }

            // Keyboard shortcuts (Editor) - using new Input System
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    NextVFX();
                }
                if (keyboard.aKey.wasPressedThisFrame)
                {
                    autoCycleEnabled = !autoCycleEnabled;
                }
            }
            #endif
        }

        void HandleTap(Vector2 position)
        {
            float now = Time.time;

            // Double tap to toggle auto-cycle
            if (now - lastTapTime < doubleTapTime)
            {
                autoCycleEnabled = !autoCycleEnabled;
                Log($"[SimpleVFXUI] Auto-cycle: {(autoCycleEnabled ? "ON" : "OFF")}");
            }
            else
            {
                // Single tap to switch VFX
                NextVFX();
            }

            lastTapTime = now;
        }

        void UpdateAutoCycle()
        {
            if (!autoCycleEnabled || vfxList == null || vfxList.Length <= 1) return;

            autoCycleTimer += Time.deltaTime;
            if (autoCycleTimer >= autoCycleInterval)
            {
                autoCycleTimer = 0f;
                NextVFX();
            }
        }

        void UpdateNameDisplay()
        {
            if (nameDisplayTimer > 0)
            {
                nameDisplayTimer -= Time.deltaTime;
            }
        }

        public void NextVFX()
        {
            if (vfxList == null || vfxList.Length == 0) return;
            int next = (currentVFXIndex + 1) % vfxList.Length;
            SelectVFX(next);
        }

        public void PreviousVFX()
        {
            if (vfxList == null || vfxList.Length == 0) return;
            int prev = (currentVFXIndex - 1 + vfxList.Length) % vfxList.Length;
            SelectVFX(prev);
        }

        public void SelectVFX(int index)
        {
            if (vfxList == null || index < 0 || index >= vfxList.Length) return;

            // Disable all
            for (int i = 0; i < vfxList.Length; i++)
            {
                if (vfxList[i] != null)
                {
                    vfxList[i].enabled = false;
                    if (vfxList[i].HasBool("Spawn"))
                    {
                        vfxList[i].SetBool("Spawn", false);
                    }
                }
            }

            // Enable selected
            if (vfxList[index] != null)
            {
                vfxList[index].enabled = true;
                if (vfxList[index].HasBool("Spawn"))
                {
                    vfxList[index].SetBool("Spawn", true);
                }
                vfxList[index].Reinit();

                currentVFXName = vfxList[index].visualEffectAsset?.name ?? $"VFX {index}";
                nameDisplayTimer = nameDisplayDuration;
            }

            currentVFXIndex = index;
            autoCycleTimer = 0f; // Reset timer on manual selection
        }

        void InitStyles()
        {
            if (stylesInitialized) return;

            // Background texture
            backgroundTex = new Texture2D(1, 1);
            backgroundTex.SetPixel(0, 0, backgroundColor);
            backgroundTex.Apply();

            // Label style
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * uiScale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = textColor;

            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(12 * uiScale),
                fontStyle = FontStyle.Normal
            };
            buttonStyle.normal.textColor = textColor;

            // Box style
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = backgroundTex;

            stylesInitialized = true;
        }

        void OnGUI()
        {
            if (!showUI) return;

            InitStyles();

            float scale = uiScale * (Screen.dpi > 0 ? Screen.dpi / 160f : 1f);
            float padding = 10 * scale;
            float lineHeight = 20 * scale;

            // Stats overlay (top-left)
            if (showStats)
            {
                DrawStats(padding, padding, scale);
            }

            // VFX name (center, fading)
            if (showVFXName && nameDisplayTimer > 0)
            {
                DrawVFXName(scale);
            }

            // Bottom controls
            DrawBottomControls(padding, scale);
        }

        void DrawStats(float x, float y, float scale)
        {
            float width = 120 * scale;
            float height = 50 * scale;

            GUI.Box(new Rect(x - 5, y - 5, width + 10, height + 10), "", boxStyle);

            GUI.Label(new Rect(x, y, width, 20 * scale),
                $"FPS: {fps:F0}", labelStyle);
            GUI.Label(new Rect(x, y + 22 * scale, width, 20 * scale),
                $"Particles: {particleCount:N0}", labelStyle);
        }

        void DrawVFXName(float scale)
        {
            // Center of screen, fading out
            float alpha = Mathf.Clamp01(nameDisplayTimer / 0.5f);
            var nameStyle = new GUIStyle(labelStyle)
            {
                fontSize = Mathf.RoundToInt(24 * scale),
                alignment = TextAnchor.MiddleCenter
            };
            nameStyle.normal.textColor = new Color(textColor.r, textColor.g, textColor.b, alpha);

            float width = 300 * scale;
            float height = 40 * scale;
            float x = (Screen.width - width) / 2;
            float y = Screen.height * 0.3f;

            GUI.Label(new Rect(x, y, width, height), currentVFXName, nameStyle);
        }

        void DrawBottomControls(float padding, float scale)
        {
            float buttonWidth = 60 * scale;
            float buttonHeight = 40 * scale;
            float y = Screen.height - buttonHeight - padding;

            // Background
            float totalWidth = buttonWidth * 4 + padding * 3;
            float startX = (Screen.width - totalWidth) / 2;

            GUI.Box(new Rect(startX - padding, y - padding,
                totalWidth + padding * 2, buttonHeight + padding * 2), "", boxStyle);

            // Previous button
            if (GUI.Button(new Rect(startX, y, buttonWidth, buttonHeight), "◀", buttonStyle))
            {
                PreviousVFX();
            }

            // Index display
            var indexStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16 * scale)
            };
            GUI.Label(new Rect(startX + buttonWidth + padding, y, buttonWidth, buttonHeight),
                $"{currentVFXIndex + 1}/{vfxList?.Length ?? 0}", indexStyle);

            // Next button
            if (GUI.Button(new Rect(startX + buttonWidth * 2 + padding * 2, y, buttonWidth, buttonHeight), "▶", buttonStyle))
            {
                NextVFX();
            }

            // Auto-cycle toggle
            string autoLabel = autoCycleEnabled ? "⏸" : "▶▶";
            var autoStyle = new GUIStyle(buttonStyle);
            if (autoCycleEnabled)
            {
                autoStyle.normal.textColor = accentColor;
            }
            if (GUI.Button(new Rect(startX + buttonWidth * 3 + padding * 3, y, buttonWidth, buttonHeight), autoLabel, autoStyle))
            {
                autoCycleEnabled = !autoCycleEnabled;
            }
        }

        // Public API
        public int CurrentIndex => currentVFXIndex;
        public bool AutoCycleEnabled
        {
            get => autoCycleEnabled;
            set => autoCycleEnabled = value;
        }
        public float AutoCycleInterval
        {
            get => autoCycleInterval;
            set => autoCycleInterval = value;
        }
    }
}
