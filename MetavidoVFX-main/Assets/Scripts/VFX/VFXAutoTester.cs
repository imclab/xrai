// VFXAutoTester - Automatically cycles through all VFX in a category container
// Attach to a parent GameObject (e.g., VFX_ALL, [People], [Environment])
// Each VFX is enabled for the specified duration, then moves to the next

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace MetavidoVFX.VFX
{
    /// <summary>
    /// Auto-tests VFX by cycling through each child VFX for a specified duration.
    /// Attach to a container GameObject like VFX_ALL or any category container.
    /// </summary>
    public class VFXAutoTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Duration each VFX is active before switching to next")]
        [SerializeField] private float displayDuration = 5f;

        [Tooltip("Start testing automatically on Start")]
        [SerializeField] private bool autoStart = true;

        [Tooltip("Loop back to beginning when reaching the end")]
        [SerializeField] private bool loopContinuously = true;

        [Tooltip("Include VFX in child containers (recursive)")]
        [SerializeField] private bool includeNestedVFX = true;

        [Header("State")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int currentIndex = 0;
        [SerializeField] private float timeRemaining = 0f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool showOnScreenInfo = true;

        // Cached VFX list
        private List<VisualEffect> _vfxList = new List<VisualEffect>();
        private VisualEffect _currentVFX;
        private Coroutine _testCoroutine;

        // Events
        public event System.Action<VisualEffect, int, int> OnVFXChanged;
        public event System.Action OnTestComplete;
        public event System.Action OnTestStarted;

        // Public API
        public bool IsRunning => isRunning;
        public int CurrentIndex => currentIndex;
        public int TotalCount => _vfxList.Count;
        public VisualEffect CurrentVFX => _currentVFX;
        public float TimeRemaining => timeRemaining;
        public float DisplayDuration { get => displayDuration; set => displayDuration = value; }

        void Start()
        {
            RefreshVFXList();

            if (autoStart && _vfxList.Count > 0)
            {
                StartTest();
            }
        }

        void OnDestroy()
        {
            StopTest();
        }

        /// <summary>
        /// Refresh the list of VFX from children
        /// </summary>
        [ContextMenu("Refresh VFX List")]
        public void RefreshVFXList()
        {
            _vfxList.Clear();

            if (includeNestedVFX)
            {
                // Get all VFX in children (recursive)
                var allVFX = GetComponentsInChildren<VisualEffect>(true);
                _vfxList.AddRange(allVFX);
            }
            else
            {
                // Get only direct children
                foreach (Transform child in transform)
                {
                    var vfx = child.GetComponent<VisualEffect>();
                    if (vfx != null)
                    {
                        _vfxList.Add(vfx);
                    }
                }
            }

            // Sort alphabetically by name
            _vfxList.Sort((a, b) => a.name.CompareTo(b.name));

            if (verboseLogging)
            {
                Debug.Log($"[VFXAutoTester] Found {_vfxList.Count} VFX to test");
                for (int i = 0; i < _vfxList.Count; i++)
                {
                    Debug.Log($"  [{i + 1}] {_vfxList[i].name}");
                }
            }
        }

        /// <summary>
        /// Start the auto-test cycle
        /// </summary>
        [ContextMenu("Start Test")]
        public void StartTest()
        {
            if (isRunning)
            {
                Debug.LogWarning("[VFXAutoTester] Test already running");
                return;
            }

            if (_vfxList.Count == 0)
            {
                RefreshVFXList();
                if (_vfxList.Count == 0)
                {
                    Debug.LogWarning("[VFXAutoTester] No VFX found to test");
                    return;
                }
            }

            currentIndex = 0;
            isRunning = true;
            _testCoroutine = StartCoroutine(TestCycleCoroutine());

            if (verboseLogging)
            {
                Debug.Log($"[VFXAutoTester] Starting test cycle: {_vfxList.Count} VFX, {displayDuration}s each");
            }

            OnTestStarted?.Invoke();
        }

        /// <summary>
        /// Stop the auto-test cycle
        /// </summary>
        [ContextMenu("Stop Test")]
        public void StopTest()
        {
            if (!isRunning) return;

            isRunning = false;

            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
                _testCoroutine = null;
            }

            // Re-enable all VFX
            foreach (var vfx in _vfxList)
            {
                if (vfx != null)
                {
                    vfx.enabled = true;
                    vfx.gameObject.SetActive(true);
                }
            }

            if (verboseLogging)
            {
                Debug.Log("[VFXAutoTester] Test stopped");
            }
        }

        /// <summary>
        /// Pause the current test
        /// </summary>
        public void PauseTest()
        {
            if (_testCoroutine != null)
            {
                StopCoroutine(_testCoroutine);
                _testCoroutine = null;
            }
            if (verboseLogging)
            {
                Debug.Log($"[VFXAutoTester] Paused at index {currentIndex}: {_currentVFX?.name}");
            }
        }

        /// <summary>
        /// Resume a paused test
        /// </summary>
        public void ResumeTest()
        {
            if (isRunning && _testCoroutine == null)
            {
                _testCoroutine = StartCoroutine(TestCycleCoroutine());
                if (verboseLogging)
                {
                    Debug.Log("[VFXAutoTester] Resumed");
                }
            }
        }

        /// <summary>
        /// Skip to next VFX immediately
        /// </summary>
        [ContextMenu("Skip to Next")]
        public void SkipToNext()
        {
            if (!isRunning || _vfxList.Count == 0) return;

            currentIndex = (currentIndex + 1) % _vfxList.Count;
            timeRemaining = displayDuration;

            ActivateVFX(currentIndex);
        }

        /// <summary>
        /// Skip to previous VFX immediately
        /// </summary>
        [ContextMenu("Skip to Previous")]
        public void SkipToPrevious()
        {
            if (!isRunning || _vfxList.Count == 0) return;

            currentIndex = (currentIndex - 1 + _vfxList.Count) % _vfxList.Count;
            timeRemaining = displayDuration;

            ActivateVFX(currentIndex);
        }

        /// <summary>
        /// Jump to specific VFX by index
        /// </summary>
        public void JumpToIndex(int index)
        {
            if (index < 0 || index >= _vfxList.Count) return;

            currentIndex = index;
            timeRemaining = displayDuration;

            ActivateVFX(currentIndex);
        }

        private IEnumerator TestCycleCoroutine()
        {
            while (isRunning)
            {
                // Activate current VFX
                ActivateVFX(currentIndex);
                timeRemaining = displayDuration;

                // Wait for duration
                while (timeRemaining > 0 && isRunning)
                {
                    yield return null;
                    timeRemaining -= Time.deltaTime;
                }

                if (!isRunning) break;

                // Move to next
                currentIndex++;

                // Check if we've completed the cycle
                if (currentIndex >= _vfxList.Count)
                {
                    if (loopContinuously)
                    {
                        currentIndex = 0;
                        if (verboseLogging)
                        {
                            Debug.Log("[VFXAutoTester] Cycle complete, looping back to start");
                        }
                    }
                    else
                    {
                        isRunning = false;
                        OnTestComplete?.Invoke();
                        if (verboseLogging)
                        {
                            Debug.Log("[VFXAutoTester] Test complete!");
                        }
                        break;
                    }
                }
            }
        }

        private void ActivateVFX(int index)
        {
            if (index < 0 || index >= _vfxList.Count) return;

            // Disable all VFX
            foreach (var vfx in _vfxList)
            {
                if (vfx != null)
                {
                    vfx.enabled = false;
                    vfx.gameObject.SetActive(false);
                }
            }

            // Enable the current VFX
            _currentVFX = _vfxList[index];
            if (_currentVFX != null)
            {
                _currentVFX.gameObject.SetActive(true);
                _currentVFX.enabled = true;
                _currentVFX.Reinit();

                if (verboseLogging)
                {
                    Debug.Log($"[VFXAutoTester] [{index + 1}/{_vfxList.Count}] Now testing: {_currentVFX.name}");
                }

                OnVFXChanged?.Invoke(_currentVFX, index, _vfxList.Count);
            }
        }

        void OnGUI()
        {
            if (!showOnScreenInfo || !isRunning) return;

            // Draw info panel
            float panelWidth = 350f;
            float panelHeight = 100f;
            float margin = 20f;

            Rect panelRect = new Rect(
                Screen.width - panelWidth - margin,
                margin,
                panelWidth,
                panelHeight
            );

            GUI.Box(panelRect, "");

            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 5, panelRect.width - 20, panelRect.height - 10));

            GUILayout.Label("<b>VFX Auto Tester</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });

            string vfxName = _currentVFX != null ? _currentVFX.name : "None";
            if (vfxName.Length > 35)
            {
                vfxName = vfxName.Substring(0, 32) + "...";
            }
            GUILayout.Label($"Current: {vfxName}");
            GUILayout.Label($"Progress: {currentIndex + 1} / {_vfxList.Count}");
            GUILayout.Label($"Time: {timeRemaining:F1}s / {displayDuration}s");

            // Progress bar
            float progress = 1f - (timeRemaining / displayDuration);
            GUI.HorizontalSlider(new Rect(10, panelHeight - 25, panelWidth - 20, 20), progress, 0f, 1f);

            GUILayout.EndArea();
        }

        #region Editor Helpers

        [ContextMenu("List All VFX")]
        private void LogAllVFX()
        {
            RefreshVFXList();
            Debug.Log($"[VFXAutoTester] VFX List ({_vfxList.Count} total):");
            for (int i = 0; i < _vfxList.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {_vfxList[i].name}");
            }
        }

        #endregion
    }
}
