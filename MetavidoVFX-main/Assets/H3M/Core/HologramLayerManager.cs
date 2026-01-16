using UnityEngine;
using UnityEngine.VFX;
using System;

namespace H3M.Core
{
    /// <summary>
    /// Manages multiple VFX layers for the hologram, allowing toggling between
    /// body, background, hands, and face visualization modes.
    /// </summary>
    public class HologramLayerManager : MonoBehaviour
    {
        [Serializable]
        public class VFXLayer
        {
            public string Name;
            public VisualEffect VFX;
            public bool DefaultVisible = true;
            [Tooltip("Spawn bool name in VFX Graph")]
            public string SpawnProperty = "Spawn";
        }

        [Header("VFX Layers")]
        [SerializeField] VFXLayer[] _layers;

        [Header("Default Mode")]
        [SerializeField] LayerMode _defaultMode = LayerMode.BodyOnly;

        public enum LayerMode
        {
            BodyOnly,       // Just the segmented body
            FullScene,      // Body + background
            HandsOnly,      // Just hands (for hand tracking focus)
            FaceOnly,       // Just face (for facial tracking)
            Custom          // Individual layer control via UI
        }

        LayerMode _currentMode;
        bool[] _customVisibility;

        void Start()
        {
            _customVisibility = new bool[_layers.Length];
            for (int i = 0; i < _layers.Length; i++)
                _customVisibility[i] = _layers[i].DefaultVisible;

            SetMode(_defaultMode);
        }

        /// <summary>
        /// Set the visualization mode
        /// </summary>
        public void SetMode(LayerMode mode)
        {
            _currentMode = mode;
            ApplyMode();
        }

        void ApplyMode()
        {
            switch (_currentMode)
            {
                case LayerMode.BodyOnly:
                    SetLayersByName(body: true, background: false, hands: false, face: false);
                    break;
                case LayerMode.FullScene:
                    SetLayersByName(body: true, background: true, hands: true, face: true);
                    break;
                case LayerMode.HandsOnly:
                    SetLayersByName(body: false, background: false, hands: true, face: false);
                    break;
                case LayerMode.FaceOnly:
                    SetLayersByName(body: false, background: false, hands: false, face: true);
                    break;
                case LayerMode.Custom:
                    ApplyCustomVisibility();
                    break;
            }
        }

        void SetLayersByName(bool body, bool background, bool hands, bool face)
        {
            foreach (var layer in _layers)
            {
                if (layer.VFX == null) continue;

                bool visible = layer.Name.ToLower() switch
                {
                    "body" => body,
                    "background" => background,
                    "hands" or "hand" => hands,
                    "face" => face,
                    _ => true // Unknown layers default to visible
                };

                layer.VFX.SetBool(layer.SpawnProperty, visible);
            }
        }

        void ApplyCustomVisibility()
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                if (_layers[i].VFX != null)
                    _layers[i].VFX.SetBool(_layers[i].SpawnProperty, _customVisibility[i]);
            }
        }

        /// <summary>
        /// Toggle a specific layer's visibility (for Custom mode)
        /// </summary>
        public void ToggleLayer(int index)
        {
            if (index < 0 || index >= _layers.Length) return;
            _customVisibility[index] = !_customVisibility[index];
            if (_currentMode == LayerMode.Custom)
                ApplyCustomVisibility();
        }

        /// <summary>
        /// Set a specific layer's visibility (for Custom mode)
        /// </summary>
        public void SetLayerVisible(int index, bool visible)
        {
            if (index < 0 || index >= _layers.Length) return;
            _customVisibility[index] = visible;
            if (_currentMode == LayerMode.Custom)
                ApplyCustomVisibility();
        }

        /// <summary>
        /// Get layer visibility
        /// </summary>
        public bool IsLayerVisible(int index)
        {
            if (index < 0 || index >= _layers.Length) return false;
            return _customVisibility[index];
        }

        /// <summary>
        /// Get all layer names
        /// </summary>
        public string[] GetLayerNames()
        {
            var names = new string[_layers.Length];
            for (int i = 0; i < _layers.Length; i++)
                names[i] = _layers[i].Name;
            return names;
        }

        // Debug UI for on-device testing
        void OnGUI()
        {
            if (_layers == null || _layers.Length == 0) return;

            GUI.skin.button.fontSize = 28;
            GUI.skin.toggle.fontSize = 24;

            float buttonWidth = 180;
            float buttonHeight = 50;
            float startY = Screen.height - 300;

            GUILayout.BeginArea(new Rect(20, startY, buttonWidth * 2 + 20, 280));

            GUILayout.Label("Layer Mode:", GUI.skin.label);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Body", GUILayout.Height(buttonHeight)))
                SetMode(LayerMode.BodyOnly);
            if (GUILayout.Button("Full", GUILayout.Height(buttonHeight)))
                SetMode(LayerMode.FullScene);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hands", GUILayout.Height(buttonHeight)))
                SetMode(LayerMode.HandsOnly);
            if (GUILayout.Button("Face", GUILayout.Height(buttonHeight)))
                SetMode(LayerMode.FaceOnly);
            GUILayout.EndHorizontal();

            GUILayout.Label($"Current: {_currentMode}");

            GUILayout.EndArea();
        }
    }
}
