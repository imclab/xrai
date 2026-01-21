// HandTrackingProviderManager - Auto-discovers and orchestrates hand tracking (spec-012)
// Priority: HoloKit > XRHands > BodyPix > Touch

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MetavidoVFX.HandTracking
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
        [SerializeField] bool _autoInitialize = true;
        [SerializeField] bool _enableFallback = true;

        List<IHandTrackingProvider> _providers = new();
        IHandTrackingProvider _activeProvider;
        bool _initialized;

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
        }

        public void Initialize()
        {
            if (_initialized) return;

            DiscoverProviders();
            InitializeAllProviders();
            SelectBestProvider();

            _initialized = true;
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
                    Debug.LogWarning($"[HandTrackingManager] Failed to init {provider.Id}: {e.Message}");
                }
            }
        }

        void SelectBestProvider()
        {
            var newProvider = _providers.FirstOrDefault(p => p.IsAvailable);

            if (newProvider != _activeProvider)
            {
                if (_activeProvider != null)
                    UnsubscribeProvider(_activeProvider);

                _activeProvider = newProvider;

                if (_activeProvider != null)
                    SubscribeProvider(_activeProvider);

                OnProviderChanged?.Invoke(_activeProvider);
                Debug.Log($"[HandTrackingManager] Switched to: {_activeProvider?.Id ?? "none"}");
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
