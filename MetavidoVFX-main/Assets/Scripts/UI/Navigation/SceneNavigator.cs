// SceneNavigator.cs - Singleton for scene navigation between spec demos
// Provides consistent scene loading with transition support

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MetavidoVFX.UI.Navigation
{
    /// <summary>
    /// Singleton for navigating between spec demo scenes.
    /// Persists across scene loads for consistent navigation.
    /// </summary>
    public class SceneNavigator : MonoBehaviour
    {
        #region Singleton

        private static SceneNavigator _instance;
        public static SceneNavigator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SceneNavigator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[SceneNavigator]");
                        _instance = go.AddComponent<SceneNavigator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Scene Paths

        public const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        public const string SpecDemoFolder = "Assets/Scenes/SpecDemos/";

        // Spec scene names (without path/extension)
        public static readonly string[] SpecScenes = new[]
        {
            "Spec002_H3M_Foundation",
            "Spec003_Hologram_Conferencing",
            "Spec004_MetavidoVFX_Systems",
            "Spec005_AR_Texture_Safety",
            "Spec006_VFX_Library_Pipeline",
            "Spec007_VFX_Multi_Mode",
            "Spec008_ML_Foundations",
            "Spec009_Icosa_Sketchfab",
            "Spec010_Normcore_Multiuser",
            "Spec011_OpenBrush_Integration",
            "Spec012_Hand_Tracking",
            "Spec013_UI_UX_Conferencing",
            "Spec014_HiFi_Hologram_VFX"
        };

        // Human-readable names for UI
        public static readonly string[] SpecDisplayNames = new[]
        {
            "002: H3M Foundation",
            "003: Hologram Conferencing",
            "004: MetavidoVFX Systems",
            "005: AR Texture Safety",
            "006: VFX Library Pipeline",
            "007: VFX Multi-Mode",
            "008: ML Foundations",
            "009: Icosa/Sketchfab",
            "010: Normcore Multiuser",
            "011: Open Brush Integration",
            "012: Hand Tracking",
            "013: UI/UX Conferencing",
            "014: HiFi Hologram VFX"
        };

        #endregion

        #region Events

        public event Action<string> OnSceneLoadStart;
        public event Action<string> OnSceneLoadComplete;

        #endregion

        #region Properties

        public string CurrentScene => SceneManager.GetActiveScene().name;
        public bool IsLoading { get; private set; }

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Load the main menu scene.
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }

        /// <summary>
        /// Load a spec demo scene by index (0-12).
        /// </summary>
        public void LoadSpecScene(int index)
        {
            if (index < 0 || index >= SpecScenes.Length)
            {
                Debug.LogError($"[SceneNavigator] Invalid spec index: {index}");
                return;
            }
            LoadScene(SpecScenes[index]);
        }

        /// <summary>
        /// Load a scene by name.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneNavigator] Already loading a scene");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            IsLoading = true;
            OnSceneLoadStart?.Invoke(sceneName);

            Debug.Log($"[SceneNavigator] Loading: {sceneName}");

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"[SceneNavigator] Failed to load scene: {sceneName}");
                IsLoading = false;
                yield break;
            }

            while (!asyncOp.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            OnSceneLoadComplete?.Invoke(sceneName);
            Debug.Log($"[SceneNavigator] Loaded: {sceneName}");
        }

        /// <summary>
        /// Get the display name for the current scene.
        /// </summary>
        public string GetCurrentSceneDisplayName()
        {
            string current = CurrentScene;

            for (int i = 0; i < SpecScenes.Length; i++)
            {
                if (SpecScenes[i] == current)
                    return SpecDisplayNames[i];
            }

            return current;
        }

        /// <summary>
        /// Get the spec index for the current scene (-1 if not a spec scene).
        /// </summary>
        public int GetCurrentSpecIndex()
        {
            string current = CurrentScene;
            for (int i = 0; i < SpecScenes.Length; i++)
            {
                if (SpecScenes[i] == current)
                    return i;
            }
            return -1;
        }

        #endregion
    }
}
