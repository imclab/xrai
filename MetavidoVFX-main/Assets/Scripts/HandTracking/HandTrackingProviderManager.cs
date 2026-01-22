// HandTrackingProviderManager - Auto-discovers and orchestrates hand tracking (spec-012)
// Priority: HoloKit > XRHands > BodyPix > Touch

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace XRRAI.HandTracking
{
    /// <summary>
    /// Singleton manager for hand tracking providers.
    /// Auto-discovers providers via [HandTrackingProvider] attribute.
    /// Selects highest-priority available provider at runtime.
    /// </summary>
    public class HandTrackingProviderManager : MonoBehaviour
    {
        public static HandTrackingProviderManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Initialize providers automatically on Awake")]
        [SerializeField] bool _autoInitialize = true;
        [Tooltip("Automatically switch to next available provider if current becomes unavailable")]
        [SerializeField] bool _enableFallback = true;
        [Tooltip("Preferred provider ID (leave empty for auto-select by priority)")]
        [SerializeField] string _preferredProviderId = "";
        [Tooltip("Minimum acceptable priority level (0 = accept any)")]
        [SerializeField] int _minimumPriority = 0;

        [Header("Debug")]
        [Tooltip("Log provider discovery and switching")]
        [SerializeField] bool _debugLogging = true;
        [Tooltip("Show debug gizmos for hand positions")]
        [SerializeField] bool _showDebugGizmos = false;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Currently active provider")]
        string _activeProviderDisplay = "None";
        [SerializeField, Tooltip("Number of discovered providers")]
        int _providerCount = 0;
        [SerializeField, Tooltip("Is hand tracking currently active")]
        bool _isTrackingActive = false;
        [SerializeField, Tooltip("Left hand tracked")]
        bool _leftHandTracked = false;
        [SerializeField, Tooltip("Right hand tracked")]
        bool _rightHandTracked = false;

        List<IHandTrackingProvider> _providers = new();
        IHandTrackingProvider _activeProvider;
        bool _initialized;

        // Public accessors for configuration
        public bool AutoInitialize { get => _autoInitialize; set => _autoInitialize = value; }
        public bool EnableFallback { get => _enableFallback; set => _enableFallback = value; }
        public string PreferredProviderId { get => _preferredProviderId; set => _preferredProviderId = value; }
        public int MinimumPriority { get => _minimumPriority; set => _minimumPriority = value; }
        public bool DebugLogging { get => _debugLogging; set => _debugLogging = value; }
        public bool ShowDebugGizmos { get => _showDebugGizmos; set => _showDebugGizmos = value; }

        public IHandTrackingProvider ActiveProvider => _activeProvider;
        public IReadOnlyList<IHandTrackingProvider> AllProviders => _providers;
        public bool IsTracking => _activeProvider?.IsAvailable == true;

        public event Action<IHandTrackingProvider> OnProviderChanged;
        public event Action<Hand> OnHandTrackingGained;
        public event Action<Hand> OnHandTrackingLost;
        public event Action<Hand, GestureType> OnGestureStart;
        public event Action<Hand, GestureType> OnGestureEnd;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_autoInitialize)
                Initialize();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Shutdown();
                Instance = null;
            }
        }

        void Update()
        {
            if (!_initialized) return;

            // Update active provider
            _activeProvider?.Update();

            // Check for provider switch if current unavailable
            if (_enableFallback && (_activeProvider == null || !_activeProvider.IsAvailable))
            {
                SelectBestProvider();
            }

            // Update runtime status display
            UpdateRuntimeStatus();
        }

        void UpdateRuntimeStatus()
        {
            _activeProviderDisplay = _activeProvider?.Id ?? "None";
            _providerCount = _providers.Count;
            _isTrackingActive = _activeProvider?.IsAvailable == true;
            _leftHandTracked = _activeProvider?.IsHandTracked(Hand.Left) ?? false;
            _rightHandTracked = _activeProvider?.IsHandTracked(Hand.Right) ?? false;
        }

        void OnDrawGizmos()
        {
            if (!_showDebugGizmos || _activeProvider == null) return;

            // Draw hand positions
            if (_leftHandTracked)
            {
                Gizmos.color = Color.cyan;
                var wrist = GetJointPosition(Hand.Left, HandJointID.Wrist);
                Gizmos.DrawWireSphere(wrist, 0.02f);
            }

            if (_rightHandTracked)
            {
                Gizmos.color = Color.magenta;
                var wrist = GetJointPosition(Hand.Right, HandJointID.Wrist);
                Gizmos.DrawWireSphere(wrist, 0.02f);
            }
        }

        public void Initialize()
        {
            if (_initialized) return;

            DiscoverProviders();
            InitializeAllProviders();

            // Try preferred provider first if specified
            if (!string.IsNullOrEmpty(_preferredProviderId))
            {
                SetActiveProvider(_preferredProviderId);
            }

            // Fall back to best available if preferred not set or unavailable
            if (_activeProvider == null)
            {
                SelectBestProvider();
            }

            _initialized = true;
            UpdateRuntimeStatus();

            if (_debugLogging)
                Debug.Log($"[HandTrackingManager] Initialized with {_providers.Count} providers, active: {_activeProvider?.Id ?? "none"}");
        }

        public void Shutdown()
        {
            foreach (var provider in _providers)
            {
                UnsubscribeProvider(provider);
                provider.Shutdown();
                provider.Dispose();
            }
            _providers.Clear();
            _activeProvider = null;
            _initialized = false;
        }

        void DiscoverProviders()
        {
            var providerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(IHandTrackingProvider).IsAssignableFrom(t) &&
                           !t.IsInterface && !t.IsAbstract &&
                           t.GetCustomAttribute<HandTrackingProviderAttribute>() != null);

            foreach (var type in providerTypes)
            {
                try
                {
                    var provider = (IHandTrackingProvider)Activator.CreateInstance(type);
                    _providers.Add(provider);
                }
                catch (Exception e)
                {
                    if (_debugLogging)
                        Debug.LogWarning($"[HandTrackingManager] Failed to create provider {type.Name}: {e.Message}");
                }
            }

            // Sort by priority (highest first)
            _providers = _providers.OrderByDescending(p => p.Priority).ToList();
        }

        void InitializeAllProviders()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.Initialize();
                }
                catch (Exception e)
                {
                    if (_debugLogging)
                        Debug.LogWarning($"[HandTrackingManager] Failed to init {provider.Id}: {e.Message}");
                }
            }
        }

        void SelectBestProvider()
        {
            var newProvider = _providers.FirstOrDefault(p =>
                p.IsAvailable && p.Priority >= _minimumPriority);

            if (newProvider != _activeProvider)
            {
                if (_activeProvider != null)
                    UnsubscribeProvider(_activeProvider);

                _activeProvider = newProvider;

                if (_activeProvider != null)
                    SubscribeProvider(_activeProvider);

                OnProviderChanged?.Invoke(_activeProvider);

                if (_debugLogging)
                    Debug.Log($"[HandTrackingManager] Switched to: {_activeProvider?.Id ?? "none"} (priority: {_activeProvider?.Priority ?? 0})");
            }
        }

        void SubscribeProvider(IHandTrackingProvider provider)
        {
            provider.OnHandTrackingGained += ForwardHandGained;
            provider.OnHandTrackingLost += ForwardHandLost;
            provider.OnGestureStart += ForwardGestureStart;
            provider.OnGestureEnd += ForwardGestureEnd;
        }

        void UnsubscribeProvider(IHandTrackingProvider provider)
        {
            provider.OnHandTrackingGained -= ForwardHandGained;
            provider.OnHandTrackingLost -= ForwardHandLost;
            provider.OnGestureStart -= ForwardGestureStart;
            provider.OnGestureEnd -= ForwardGestureEnd;
        }

        void ForwardHandGained(Hand h) => OnHandTrackingGained?.Invoke(h);
        void ForwardHandLost(Hand h) => OnHandTrackingLost?.Invoke(h);
        void ForwardGestureStart(Hand h, GestureType g) => OnGestureStart?.Invoke(h, g);
        void ForwardGestureEnd(Hand h, GestureType g) => OnGestureEnd?.Invoke(h, g);

        // Convenience methods that delegate to active provider

        public bool IsHandTracked(Hand hand) =>
            _activeProvider?.IsHandTracked(hand) ?? false;

        public Vector3 GetJointPosition(Hand hand, HandJointID joint) =>
            _activeProvider?.GetJointPosition(hand, joint) ?? Vector3.zero;

        public Quaternion GetJointRotation(Hand hand, HandJointID joint) =>
            _activeProvider?.GetJointRotation(hand, joint) ?? Quaternion.identity;

        public bool IsGestureActive(Hand hand, GestureType gesture) =>
            _activeProvider?.IsGestureActive(hand, gesture) ?? false;

        public float GetPinchStrength(Hand hand) =>
            _activeProvider?.GetPinchStrength(hand) ?? 0f;

        public float GetGrabStrength(Hand hand) =>
            _activeProvider?.GetGrabStrength(hand) ?? 0f;

        /// <summary>
        /// Force switch to a specific provider by ID.
        /// </summary>
        public bool SetActiveProvider(string providerId)
        {
            var provider = _providers.FirstOrDefault(p => p.Id == providerId && p.IsAvailable);
            if (provider == null) return false;

            if (_activeProvider != null)
                UnsubscribeProvider(_activeProvider);

            _activeProvider = provider;
            SubscribeProvider(_activeProvider);
            OnProviderChanged?.Invoke(_activeProvider);

            return true;
        }
    }
}
