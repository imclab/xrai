// VFX Card with HoloKit Gaze Gesture Interaction
// Implements IGazeGestureInteractable for pinch-to-select on HoloKit
// Falls back to dwell selection when HoloKit is not available

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

#if HOLOKIT_AVAILABLE
using HoloKit.iOS;
#endif

namespace XRRAI.UI
{
    /// <summary>
    /// Individual VFX card that supports HoloKit gaze+gesture interaction.
    /// Attach to each card GameObject created by VFXGalleryUI.
    /// </summary>
    public class VFXCardInteractable : MonoBehaviour
#if HOLOKIT_AVAILABLE
        , IGazeGestureInteractable
#endif
    {
        [Header("Card Data")]
        public VisualEffectAsset vfxAsset;
        public int cardIndex;

        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer cardRenderer;
        [SerializeField] private Transform progressBar;
        [SerializeField] private GameObject hoverIndicator;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 0.4f, 0.9f);

        [Header("Dwell Selection (Fallback)")]
        [SerializeField] private float dwellTime = 1.5f;
        [SerializeField] private bool useDwellFallback = true;

        [Header("Events")]
        public UnityEvent<VFXCardInteractable> OnCardSelected;

        // State
        private bool isHovered;
        private bool isSelected;
        private float currentDwellTime;
        private float cardWidth;

        public bool IsSelected => isSelected;

        void Awake()
        {
            if (cardRenderer == null)
                cardRenderer = GetComponentInChildren<MeshRenderer>();

            // Initialize UnityEvent if not already done
            if (OnCardSelected == null)
                OnCardSelected = new UnityEvent<VFXCardInteractable>();
        }

        /// <summary>
        /// Initialize the card with VFX asset and visual references
        /// </summary>
        public void Initialize(VisualEffectAsset asset, int index, MeshRenderer renderer, Transform progress, float width)
        {
            vfxAsset = asset;
            cardIndex = index;
            cardRenderer = renderer;
            progressBar = progress;
            cardWidth = width;

            if (cardRenderer != null)
                cardRenderer.material.color = normalColor;

            ResetProgress();
        }

        /// <summary>
        /// Set this card as the selected card
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (cardRenderer != null)
            {
                cardRenderer.material.color = selected ? selectedColor : normalColor;
            }
        }

        /// <summary>
        /// Reset the dwell progress bar
        /// </summary>
        private void ResetProgress()
        {
            currentDwellTime = 0;
            if (progressBar != null)
            {
                progressBar.localScale = new Vector3(0, progressBar.localScale.y, 1);
            }
        }

        /// <summary>
        /// Update dwell progress (called by VFXGalleryUI for non-HoloKit fallback)
        /// </summary>
        public bool UpdateDwell(float deltaTime)
        {
            if (!useDwellFallback) return false;

            currentDwellTime += deltaTime;
            float progress = Mathf.Clamp01(currentDwellTime / dwellTime);

            if (progressBar != null)
            {
                progressBar.localScale = new Vector3(cardWidth * progress, progressBar.localScale.y, 1);
                progressBar.localPosition = new Vector3(
                    -cardWidth * 0.5f + cardWidth * progress * 0.5f,
                    progressBar.localPosition.y,
                    progressBar.localPosition.z
                );
            }

            if (currentDwellTime >= dwellTime)
            {
                TriggerSelection();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Trigger the selection event
        /// </summary>
        private void TriggerSelection()
        {
            Debug.Log($"[VFXCard] Selected: {vfxAsset?.name ?? "null"}");
            OnCardSelected?.Invoke(this);

            // Haptic feedback on iOS
#if UNITY_IOS && !UNITY_EDITOR
            // iOS haptic feedback could be added here
#endif
        }

        #region HoloKit IGazeGestureInteractable Implementation

#if HOLOKIT_AVAILABLE
        /// <summary>
        /// Called when pinch gesture is detected while gazing at this card
        /// </summary>
        public void OnGestureSelected()
        {
            Debug.Log($"[VFXCard] Gesture selected: {vfxAsset?.name}");
            TriggerSelection();
        }

        /// <summary>
        /// Called when gaze enters this card
        /// </summary>
        public void OnSelectionEntered()
        {
            isHovered = true;
            if (cardRenderer != null && !isSelected)
            {
                cardRenderer.material.color = hoverColor;
            }

            if (hoverIndicator != null)
            {
                hoverIndicator.SetActive(true);
            }

            ResetProgress();
        }

        /// <summary>
        /// Called when gaze exits this card
        /// </summary>
        public void OnSelectionExited()
        {
            isHovered = false;
            if (cardRenderer != null && !isSelected)
            {
                cardRenderer.material.color = normalColor;
            }

            if (hoverIndicator != null)
            {
                hoverIndicator.SetActive(false);
            }

            ResetProgress();
        }

        /// <summary>
        /// Called every frame while gazing at this card
        /// </summary>
        public void OnSelected(float deltaTime)
        {
            // Use dwell selection as fallback if enabled
            if (useDwellFallback)
            {
                UpdateDwell(deltaTime);
            }
        }
#endif

        #endregion

        #region Non-HoloKit Fallback (called by VFXGalleryUI)

        /// <summary>
        /// Enter hover state (non-HoloKit fallback)
        /// </summary>
        public void EnterHover()
        {
#if !HOLOKIT_AVAILABLE
            isHovered = true;
            if (cardRenderer != null && !isSelected)
            {
                cardRenderer.material.color = hoverColor;
            }
            ResetProgress();
#endif
        }

        /// <summary>
        /// Exit hover state (non-HoloKit fallback)
        /// </summary>
        public void ExitHover()
        {
#if !HOLOKIT_AVAILABLE
            isHovered = false;
            if (cardRenderer != null && !isSelected)
            {
                cardRenderer.material.color = normalColor;
            }
            ResetProgress();
#endif
        }

        #endregion
    }
}
