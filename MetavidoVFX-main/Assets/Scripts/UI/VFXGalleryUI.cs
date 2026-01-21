// VisionOS-style VFX Gallery UI with Gaze Interaction
// Automatically populates with available VFX assets and supports HoloKit gaze selection
//
// HoloKit Integration:
// - Each card has VFXCardInteractable for IGazeGestureInteractable support
// - Gaze at a card and pinch to select (HoloKit hand tracking)
// - Falls back to dwell selection when HoloKit is not available
//
// Setup: Use H3M > HoloKit > Setup Camera Rig with Hand Tracking menu

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using TMPro;

#if HOLOKIT_AVAILABLE
using HoloKit.iOS;
#endif

namespace MetavidoVFX.UI
{
    /// <summary>
    /// VisionOS-style floating gallery UI for VFX selection.
    /// Auto-populates from a folder of VFX assets.
    /// Supports gaze-and-dwell selection via HoloKit or touch fallback.
    /// </summary>
    public class VFXGalleryUI : MonoBehaviour
    {
        [Header("VFX Sources")]
        [SerializeField] private VisualEffectAsset[] vfxAssets;
        [SerializeField] private string vfxResourceFolder = "VFX"; // Resources folder to scan
        [SerializeField] private bool autoPopulateFromResources = true;

        [Header("Target VFX Managers")]
        [SerializeField] private PeopleOcclusionVFXManager peopleVFXManager;
        [SerializeField] private VisualEffect directVFXTarget; // Direct VFX to swap

        [Header("UI Layout")]
        [SerializeField] private float cardWidth = 0.06f;       // Smaller cards
        [SerializeField] private float cardHeight = 0.05f;
        [SerializeField] private float cardSpacing = 0.01f;
        [SerializeField] private float galleryDistance = 1.2f;  // Further away
        [SerializeField] private float galleryHeight = -0.35f;  // Lower, out of main view
        [SerializeField] private int cardsPerRow = 6;           // More per row

        [Header("Gaze Interaction")]
        [SerializeField] private float dwellTime = 1.0f; // Time to gaze to select
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 0.4f, 0.9f);

        [Header("Visual Style")]
        [SerializeField] private Material cardMaterial;
        [SerializeField] private float cornerRadius = 0.01f;

        // Runtime state
        private List<VFXCard> cards = new List<VFXCard>();
        private VFXCard hoveredCard;
        private VFXCard selectedCard;
        private float hoverTime;
        private Transform cameraTransform;
        private bool isVisible = true;

        private class VFXCard
        {
            public GameObject gameObject;
            public VisualEffectAsset asset;
            public MeshRenderer renderer;
            public TextMeshPro label;
            public Transform progressBar;
            public int index;
        }

        void Start()
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("[VFXGallery] Camera.main not found, gallery positioning disabled");
            }

            if (autoPopulateFromResources)
            {
                PopulateFromResources();
            }

            CreateGallery();

            // Try to find managers if not assigned
            if (peopleVFXManager == null)
                peopleVFXManager = FindFirstObjectByType<PeopleOcclusionVFXManager>();

            // Setup spawn control mode
            if (useSpawnControlMode)
            {
                SetupSpawnControlVFX();
            }

            // Start auto-cycling if enabled
            if (enableAutoCycle)
            {
                StartAutoCycle();
            }
        }

        /// <summary>
        /// Setup VFX instances for spawn control mode
        /// </summary>
        void SetupSpawnControlVFX()
        {
            if (vfxAssets == null || vfxAssets.Length == 0) return;

            // Auto-find existing VFX instances or create new ones
            if (autoFindVFXInstances && (spawnControlVFXList == null || spawnControlVFXList.Length == 0))
            {
                // Create VFX instances for each asset
                spawnControlVFXList = new VisualEffect[vfxAssets.Length];

                GameObject vfxContainer = new GameObject("SpawnControlVFX");
                vfxContainer.transform.SetParent(transform.parent);

                for (int i = 0; i < vfxAssets.Length; i++)
                {
                    if (vfxAssets[i] == null) continue;

                    GameObject vfxObj = new GameObject($"VFX_{vfxAssets[i].name}");
                    vfxObj.transform.SetParent(vfxContainer.transform);

                    VisualEffect vfx = vfxObj.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = vfxAssets[i];

                    // IMPORTANT: Disable component AND set Spawn=false to prevent scene freeze
                    vfx.enabled = false;
                    if (vfx.HasBool("Spawn"))
                    {
                        vfx.SetBool("Spawn", false);
                    }

                    spawnControlVFXList[i] = vfx;

                    Debug.Log($"[VFXGallery] Created spawn-control VFX: {vfxAssets[i].name} (disabled)");
                }

                // Activate ONLY the first one
                if (spawnControlVFXList.Length > 0 && spawnControlVFXList[0] != null)
                {
                    spawnControlVFXList[0].enabled = true;
                    if (spawnControlVFXList[0].HasBool("Spawn"))
                    {
                        spawnControlVFXList[0].SetBool("Spawn", true);
                    }
                    spawnControlVFXList[0].Reinit();
                }
            }

            Debug.Log($"[VFXGallery] Spawn control mode ready with {spawnControlVFXList?.Length ?? 0} VFX instances");
        }

        void PopulateFromResources()
        {
            var loadedAssets = Resources.LoadAll<VisualEffectAsset>(vfxResourceFolder);
            if (loadedAssets.Length > 0)
            {
                vfxAssets = loadedAssets.OrderBy(a => a.name).ToArray();
                Debug.Log($"[VFXGallery] Loaded {loadedAssets.Length} VFX assets from Resources/{vfxResourceFolder} (sorted)");
            }
        }

        void CreateGallery()
        {
            if (vfxAssets == null || vfxAssets.Length == 0)
            {
                Debug.LogWarning("[VFXGallery] No VFX assets configured");
                return;
            }

            // Clear existing cards
            foreach (var card in cards)
            {
                if (card.gameObject != null)
                    Destroy(card.gameObject);
            }
            cards.Clear();

            // Create card for each VFX asset
            for (int i = 0; i < vfxAssets.Length; i++)
            {
                if (vfxAssets[i] == null) continue;
                CreateCard(vfxAssets[i], i);
            }

            PositionGallery();
        }

        void CreateCard(VisualEffectAsset asset, int index)
        {
            var card = new VFXCard
            {
                asset = asset,
                index = index
            };

            // Create card GameObject
            card.gameObject = new GameObject($"VFXCard_{asset.name}");
            card.gameObject.transform.SetParent(transform);

            // Add VFXCardInteractable for HoloKit gaze+gesture support
            var interactable = card.gameObject.AddComponent<VFXCardInteractable>();

            // Create quad mesh for card background
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(card.gameObject.transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(cardWidth, cardHeight, 1f);

            // Remove collider from quad, add to parent
            Destroy(quad.GetComponent<Collider>());
            var collider = card.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(cardWidth, cardHeight, 0.02f);

            card.renderer = quad.GetComponent<MeshRenderer>();
            if (cardMaterial != null)
            {
                card.renderer.material = new Material(cardMaterial);
            }
            else
            {
                // Create default visionOS-style material
                card.renderer.material = CreateDefaultCardMaterial();
            }
            card.renderer.material.color = normalColor;

            // Create label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(card.gameObject.transform);
            labelObj.transform.localPosition = new Vector3(0, -cardHeight * 0.35f, -0.001f);

            card.label = labelObj.AddComponent<TextMeshPro>();
            card.label.text = asset.name;
            card.label.fontSize = 0.3f;  // Smaller for compact cards
            card.label.alignment = TextAlignmentOptions.Center;
            card.label.color = Color.white;
            card.label.rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight * 0.3f);

            // Create progress bar for dwell
            var progressObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            progressObj.name = "ProgressBar";
            progressObj.transform.SetParent(card.gameObject.transform);
            progressObj.transform.localPosition = new Vector3(-cardWidth * 0.5f, cardHeight * 0.45f, -0.001f);
            progressObj.transform.localScale = new Vector3(0, cardHeight * 0.05f, 1f);
            Destroy(progressObj.GetComponent<Collider>());

            var progressRenderer = progressObj.GetComponent<MeshRenderer>();
            var unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                progressRenderer.material = new Material(unlitShader);
                progressRenderer.material.color = Color.white;
            }

            card.progressBar = progressObj.transform;

            // Initialize the VFXCardInteractable for HoloKit gaze+gesture
            interactable.Initialize(asset, index, card.renderer, card.progressBar, cardWidth);
            interactable.OnCardSelected.AddListener(OnCardInteractableSelected);

            cards.Add(card);
        }

        /// <summary>
        /// Called when a VFXCardInteractable is selected via HoloKit gaze+gesture
        /// </summary>
        private void OnCardInteractableSelected(VFXCardInteractable interactable)
        {
            // Find the matching VFXCard
            foreach (var card in cards)
            {
                if (card.index == interactable.cardIndex)
                {
                    SelectCard(card);
                    break;
                }
            }
        }

        Material CreateDefaultCardMaterial()
        {
            // Create a simple unlit transparent material with fallback
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[VFXGallery] Could not find unlit shader, using default material");
                return new Material(Shader.Find("Standard"));
            }
            var mat = new Material(shader);
            mat.color = normalColor;
            return mat;
        }

        void PositionGallery()
        {
            if (cameraTransform == null) return;

            // Position gallery in front of camera
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();

            transform.position = cameraTransform.position + forward * galleryDistance + Vector3.up * galleryHeight;
            transform.rotation = Quaternion.LookRotation(forward);

            // Layout cards in grid
            float totalWidth = (cardsPerRow - 1) * (cardWidth + cardSpacing);
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < cards.Count; i++)
            {
                int row = i / cardsPerRow;
                int col = i % cardsPerRow;

                float x = startX + col * (cardWidth + cardSpacing);
                float y = -row * (cardHeight + cardSpacing);

                cards[i].gameObject.transform.localPosition = new Vector3(x, y, 0);
            }
        }

        void Update()
        {
            if (!isVisible) return;

            // Update gallery position to follow camera (smoothly)
            if (cameraTransform != null)
            {
                Vector3 targetForward = cameraTransform.forward;
                targetForward.y = 0;
                targetForward.Normalize();

                Vector3 targetPos = cameraTransform.position + targetForward * galleryDistance + Vector3.up * galleryHeight;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetForward), Time.deltaTime * 2f);
            }

            // Handle gaze/touch interaction
            HandleInteraction();
        }

        void HandleInteraction()
        {
            VFXCard hitCard = null;

            // Raycast from camera center (gaze) or touch position
            Ray ray;
            if (Input.touchCount > 0)
            {
                ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            }
            else
            {
                ray = new Ray(cameraTransform.position, cameraTransform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, galleryDistance * 2f))
            {
                foreach (var card in cards)
                {
                    if (hit.collider.gameObject == card.gameObject)
                    {
                        hitCard = card;
                        break;
                    }
                }
            }

            // Update hover state
            if (hitCard != hoveredCard)
            {
                // Exit previous hover
                if (hoveredCard != null)
                {
                    hoveredCard.renderer.material.color = (hoveredCard == selectedCard) ? selectedColor : normalColor;
                    hoveredCard.progressBar.localScale = new Vector3(0, hoveredCard.progressBar.localScale.y, 1);
                }

                // Enter new hover
                hoveredCard = hitCard;
                hoverTime = 0;

                if (hoveredCard != null)
                {
                    hoveredCard.renderer.material.color = hoverColor;
                }
            }

            // Update dwell progress
            if (hoveredCard != null)
            {
                hoverTime += Time.deltaTime;
                float progress = Mathf.Clamp01(hoverTime / dwellTime);
                hoveredCard.progressBar.localScale = new Vector3(cardWidth * progress, hoveredCard.progressBar.localScale.y, 1);
                hoveredCard.progressBar.localPosition = new Vector3(-cardWidth * 0.5f + cardWidth * progress * 0.5f, cardHeight * 0.45f, -0.001f);

                // Check for selection (dwell complete or touch)
                if (hoverTime >= dwellTime || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
                {
                    SelectCard(hoveredCard);
                }
            }
        }

        void SelectCard(VFXCard card)
        {
            if (card == null) return;

            Debug.Log($"[VFXGallery] Selected: {card.asset.name}");

            // Reset previous selection
            if (selectedCard != null)
            {
                selectedCard.renderer.material.color = normalColor;
                // Update VFXCardInteractable selection state
                var prevInteractable = selectedCard.gameObject.GetComponent<VFXCardInteractable>();
                if (prevInteractable != null) prevInteractable.SetSelected(false);
            }

            // Mark new selection
            selectedCard = card;
            selectedCard.renderer.material.color = selectedColor;
            // Update VFXCardInteractable selection state
            var interactable = selectedCard.gameObject.GetComponent<VFXCardInteractable>();
            if (interactable != null) interactable.SetSelected(true);

            // Use spawn control mode (VfxSwitcher pattern) or asset swapping
            if (useSpawnControlMode && spawnControlVFXList != null && spawnControlVFXList.Length > 0)
            {
                SelectBySpawnControl(card.index);
            }
            else
            {
                // Asset swapping mode
                if (peopleVFXManager != null)
                {
                    peopleVFXManager.SwapVFX(card.asset);
                }

                if (directVFXTarget != null)
                {
                    directVFXTarget.visualEffectAsset = card.asset;
                    directVFXTarget.Reinit();
                    directVFXTarget.Play();
                }
            }

            // Haptic feedback (iOS)
            #if UNITY_IOS && !UNITY_EDITOR
            // iOS haptic could be added here via native plugin
            #endif

            // Reset hover
            hoverTime = 0;
        }

        /// <summary>
        /// Toggle gallery visibility
        /// </summary>
        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// Show the gallery
        /// </summary>
        public void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);
            PositionGallery();
        }

        /// <summary>
        /// Hide the gallery
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Refresh the gallery with new VFX assets
        /// </summary>
        public void Refresh()
        {
            if (autoPopulateFromResources)
            {
                PopulateFromResources();
            }
            CreateGallery();
        }

        /// <summary>
        /// Add VFX assets programmatically
        /// </summary>
        public void SetVFXAssets(VisualEffectAsset[] assets)
        {
            vfxAssets = assets;
            CreateGallery();
        }

        #region Auto-Cycling (like VfxSwitcher pattern)

        [Header("Auto-Cycling")]
        [SerializeField] private bool enableAutoCycle = false;
        [SerializeField] private float autoCycleInterval = 5.0f;

        private bool isAutoCycling = false;

        /// <summary>
        /// Start auto-cycling through VFX effects
        /// </summary>
        public async void StartAutoCycle()
        {
            if (isAutoCycling || cards.Count == 0) return;

            isAutoCycling = true;
            int currentIndex = selectedCard?.index ?? 0;

            while (isAutoCycling && cards.Count > 0)
            {
                SelectCard(cards[currentIndex]);
                currentIndex = (currentIndex + 1) % cards.Count;

                await Awaitable.WaitForSecondsAsync(autoCycleInterval);
            }
        }

        /// <summary>
        /// Stop auto-cycling
        /// </summary>
        public void StopAutoCycle()
        {
            isAutoCycling = false;
        }

        #endregion

        #region Spawn Control Mode (alternative to asset swapping)

        [Header("Spawn Control Mode")]
        [Tooltip("If enabled, uses SetBool('Spawn', true/false) instead of swapping VFX assets")]
        [SerializeField] private bool useSpawnControlMode = true; // Default to spawn control
        [SerializeField] private VisualEffect[] spawnControlVFXList;
        [SerializeField] private bool autoFindVFXInstances = true; // Auto-find VFX in scene

        /// <summary>
        /// Select VFX by index using spawn control (VfxSwitcher pattern)
        /// Disables ALL other VFX to prevent scene freeze
        /// </summary>
        public void SelectBySpawnControl(int index)
        {
            if (spawnControlVFXList == null || spawnControlVFXList.Length == 0) return;

            for (int i = 0; i < spawnControlVFXList.Length; i++)
            {
                if (spawnControlVFXList[i] != null)
                {
                    bool isActive = (i == index);

                    // Set Spawn bool if property exists
                    if (spawnControlVFXList[i].HasBool("Spawn"))
                    {
                        spawnControlVFXList[i].SetBool("Spawn", isActive);
                    }

                    // ALSO disable the VisualEffect component for non-active VFX
                    // This prevents VFX without Spawn property from running
                    spawnControlVFXList[i].enabled = isActive;

                    // Reinit the active VFX to ensure clean start
                    if (isActive)
                    {
                        spawnControlVFXList[i].Reinit();
                    }
                }
            }

            Debug.Log($"[VFXGallery] Spawn control: activated index {index}, disabled {spawnControlVFXList.Length - 1} others");
        }

        /// <summary>
        /// Enable multiple VFX at once (use at runtime only, not in editor)
        /// </summary>
        public void EnableMultipleVFX(int[] indices)
        {
            if (spawnControlVFXList == null || spawnControlVFXList.Length == 0) return;

            // First disable all
            for (int i = 0; i < spawnControlVFXList.Length; i++)
            {
                if (spawnControlVFXList[i] != null)
                {
                    spawnControlVFXList[i].enabled = false;
                    if (spawnControlVFXList[i].HasBool("Spawn"))
                    {
                        spawnControlVFXList[i].SetBool("Spawn", false);
                    }
                }
            }

            // Then enable requested ones
            foreach (int index in indices)
            {
                if (index >= 0 && index < spawnControlVFXList.Length && spawnControlVFXList[index] != null)
                {
                    spawnControlVFXList[index].enabled = true;
                    if (spawnControlVFXList[index].HasBool("Spawn"))
                    {
                        spawnControlVFXList[index].SetBool("Spawn", true);
                    }
                    spawnControlVFXList[index].Reinit();
                }
            }

            Debug.Log($"[VFXGallery] Multi-select: enabled {indices.Length} VFX");
        }

        /// <summary>
        /// Disable all VFX (safe for editor)
        /// </summary>
        public void DisableAllVFX()
        {
            if (spawnControlVFXList == null) return;

            for (int i = 0; i < spawnControlVFXList.Length; i++)
            {
                if (spawnControlVFXList[i] != null)
                {
                    spawnControlVFXList[i].enabled = false;
                    if (spawnControlVFXList[i].HasBool("Spawn"))
                    {
                        spawnControlVFXList[i].SetBool("Spawn", false);
                    }
                }
            }

            Debug.Log("[VFXGallery] All VFX disabled");
        }

        /// <summary>
        /// Get the current selected index
        /// </summary>
        public int CurrentSelectedIndex => selectedCard?.index ?? -1;

        /// <summary>
        /// Get the current selected VFX asset
        /// </summary>
        public VisualEffectAsset CurrentSelectedAsset => selectedCard?.asset;

        #endregion
    }
}
