// NNCam VFX Switcher
// Keyboard and InputAction-based VFX effect switching
// Adapted from NNCam2's VfxSwitcher with MIDI-ready InputAction support

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.InputSystem;

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// Switches between multiple VFX effects using keyboard or InputAction.
    ///
    /// Keyboard shortcuts (when useKeyboardShortcuts is true):
    /// - 1-9: Select VFX 1-9
    /// - 0: Select VFX 10
    /// - Left/Right arrows: Cycle through VFX
    /// - Space: Toggle current VFX on/off
    ///
    /// For MIDI control:
    /// - Add Minis package (jp.keijiro.minis)
    /// - Configure InputAction with MIDI bindings like "<MIDI>/CC/1"
    /// </summary>
    public class NNCamVFXSwitcher : MonoBehaviour
    {
        [Header("VFX Effects")]
        [Tooltip("List of VFX to switch between. Only one is active at a time.")]
        [SerializeField] private List<VisualEffect> _vfx = new List<VisualEffect>();

        [Header("Input")]
        [Tooltip("Use keyboard shortcuts (1-9, 0, arrows, space)")]
        [SerializeField] private bool useKeyboardShortcuts = true;

        [Tooltip("InputAction for cycling to next VFX (MIDI-ready)")]
        [SerializeField] private InputAction nextAction;

        [Tooltip("InputAction for cycling to previous VFX (MIDI-ready)")]
        [SerializeField] private InputAction prevAction;

        [Tooltip("InputAction for toggling current VFX (MIDI-ready)")]
        [SerializeField] private InputAction toggleAction;

        [Header("State")]
        [SerializeField] private int currentIndex = 0;
        [SerializeField] private bool isEnabled = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        void OnEnable()
        {
            // Enable InputActions
            if (nextAction != null)
            {
                nextAction.performed += OnNextPerformed;
                nextAction.Enable();
            }
            if (prevAction != null)
            {
                prevAction.performed += OnPrevPerformed;
                prevAction.Enable();
            }
            if (toggleAction != null)
            {
                toggleAction.performed += OnTogglePerformed;
                toggleAction.Enable();
            }

            // Initialize VFX state
            SelectVFX(currentIndex);
        }

        void OnDisable()
        {
            if (nextAction != null)
            {
                nextAction.performed -= OnNextPerformed;
                nextAction.Disable();
            }
            if (prevAction != null)
            {
                prevAction.performed -= OnPrevPerformed;
                prevAction.Disable();
            }
            if (toggleAction != null)
            {
                toggleAction.performed -= OnTogglePerformed;
                toggleAction.Disable();
            }
        }

        void Update()
        {
            if (!useKeyboardShortcuts) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Number keys 1-9 select VFX directly
            if (_vfx.Count > 0 && keyboard.digit1Key.wasPressedThisFrame) { SelectVFX(0); return; }
            if (_vfx.Count > 1 && keyboard.digit2Key.wasPressedThisFrame) { SelectVFX(1); return; }
            if (_vfx.Count > 2 && keyboard.digit3Key.wasPressedThisFrame) { SelectVFX(2); return; }
            if (_vfx.Count > 3 && keyboard.digit4Key.wasPressedThisFrame) { SelectVFX(3); return; }
            if (_vfx.Count > 4 && keyboard.digit5Key.wasPressedThisFrame) { SelectVFX(4); return; }
            if (_vfx.Count > 5 && keyboard.digit6Key.wasPressedThisFrame) { SelectVFX(5); return; }
            if (_vfx.Count > 6 && keyboard.digit7Key.wasPressedThisFrame) { SelectVFX(6); return; }
            if (_vfx.Count > 7 && keyboard.digit8Key.wasPressedThisFrame) { SelectVFX(7); return; }
            if (_vfx.Count > 8 && keyboard.digit9Key.wasPressedThisFrame) { SelectVFX(8); return; }

            // Key 0 selects VFX 10 (index 9)
            if (_vfx.Count >= 10 && keyboard.digit0Key.wasPressedThisFrame)
            {
                SelectVFX(9);
                return;
            }

            // Arrow keys cycle
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                NextVFX();
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                PrevVFX();
            }

            // Space toggles
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                ToggleVFX();
            }
        }

        // === InputAction Callbacks ===

        void OnNextPerformed(InputAction.CallbackContext ctx) => NextVFX();
        void OnPrevPerformed(InputAction.CallbackContext ctx) => PrevVFX();
        void OnTogglePerformed(InputAction.CallbackContext ctx) => ToggleVFX();

        // === Public API ===

        /// <summary>
        /// Select VFX by index (disables all others)
        /// </summary>
        public void SelectVFX(int index)
        {
            if (_vfx.Count == 0) return;

            currentIndex = Mathf.Clamp(index, 0, _vfx.Count - 1);

            for (int i = 0; i < _vfx.Count; i++)
            {
                if (_vfx[i] != null)
                {
                    bool shouldBeActive = (i == currentIndex) && isEnabled;
                    _vfx[i].enabled = shouldBeActive;
                }
            }

            if (verboseLogging)
            {
                string vfxName = _vfx[currentIndex]?.name ?? "null";
                Debug.Log($"[NNCamVFXSwitcher] Selected: {vfxName} (index {currentIndex})");
            }
        }

        /// <summary>
        /// Cycle to next VFX
        /// </summary>
        public void NextVFX()
        {
            if (_vfx.Count == 0) return;
            SelectVFX((currentIndex + 1) % _vfx.Count);
        }

        /// <summary>
        /// Cycle to previous VFX
        /// </summary>
        public void PrevVFX()
        {
            if (_vfx.Count == 0) return;
            SelectVFX((currentIndex + _vfx.Count - 1) % _vfx.Count);
        }

        /// <summary>
        /// Toggle current VFX on/off
        /// </summary>
        public void ToggleVFX()
        {
            isEnabled = !isEnabled;
            SelectVFX(currentIndex); // Re-apply state

            if (verboseLogging)
            {
                Debug.Log($"[NNCamVFXSwitcher] Toggle: {(isEnabled ? "ON" : "OFF")}");
            }
        }

        /// <summary>
        /// Get currently active VFX
        /// </summary>
        public VisualEffect CurrentVFX
        {
            get
            {
                if (_vfx.Count == 0 || currentIndex < 0 || currentIndex >= _vfx.Count)
                    return null;
                return _vfx[currentIndex];
            }
        }

        /// <summary>
        /// Get current VFX index
        /// </summary>
        public int CurrentIndex => currentIndex;

        /// <summary>
        /// Get number of VFX in list
        /// </summary>
        public int VFXCount => _vfx.Count;

        /// <summary>
        /// Add a VFX to the list at runtime
        /// </summary>
        public void AddVFX(VisualEffect vfx)
        {
            if (vfx != null && !_vfx.Contains(vfx))
            {
                _vfx.Add(vfx);
                vfx.enabled = false; // Start disabled
            }
        }

        /// <summary>
        /// Remove a VFX from the list at runtime
        /// </summary>
        public void RemoveVFX(VisualEffect vfx)
        {
            _vfx.Remove(vfx);
            if (currentIndex >= _vfx.Count)
            {
                currentIndex = Mathf.Max(0, _vfx.Count - 1);
            }
            SelectVFX(currentIndex);
        }
    }
}
