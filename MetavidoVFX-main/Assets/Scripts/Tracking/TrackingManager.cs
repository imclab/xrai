// TrackingManager - Central orchestrator for tracking providers (spec-008)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MetavidoVFX.Tracking
{
    /// <summary>
    /// Central manager for discovering, initializing, and routing tracking data.
    /// Singleton pattern with auto-discovery of providers via attributes.
    /// </summary>
    public class TrackingManager : MonoBehaviour
    {
        public static TrackingManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Initialize providers automatically on Awake")]
        [SerializeField] bool _autoInitialize = true;
        [Tooltip("Log provider discovery and status changes")]
        [SerializeField] bool _logProviders = true;
        [Tooltip("Preferred provider ID (leave empty for auto-select by priority)")]
        [SerializeField] string _preferredProviderId = "";
        [Tooltip("Required capabilities mask (leave at None for any)")]
        [SerializeField] TrackingCap _requiredCapabilities = TrackingCap.None;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField, Tooltip("Detected platform")]
        string _platformDisplay = "Unknown";
        [SerializeField, Tooltip("Number of discovered providers")]
        int _providerCount = 0;
        [SerializeField, Tooltip("Number of registered consumers")]
        int _consumerCount = 0;
        [SerializeField, Tooltip("Currently available tracking capabilities")]
        TrackingCap _availableCapabilitiesDisplay = TrackingCap.None;
        [SerializeField, Tooltip("List of active provider IDs")]
        string[] _activeProviderIds = new string[0];

        // Provider registry
        readonly List<ITrackingProvider> _providers = new();
        readonly Dictionary<TrackingCap, ITrackingProvider> _capabilityMap = new();

        // Consumer registry
        readonly List<ITrackingConsumer> _consumers = new();

        // Current platform
        Platform _currentPlatform;

        // Public accessors for configuration
        public bool AutoInitialize { get => _autoInitialize; set => _autoInitialize = value; }
        public bool LogProviders { get => _logProviders; set => _logProviders = value; }
        public string PreferredProviderId { get => _preferredProviderId; set => _preferredProviderId = value; }
        public TrackingCap RequiredCapabilities { get => _requiredCapabilities; set => _requiredCapabilities = value; }
        public Platform CurrentPlatform => _currentPlatform;

        public IReadOnlyList<ITrackingProvider> Providers => _providers;
        public TrackingCap AvailableCapabilities { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _currentPlatform = GetCurrentPlatform();
            _platformDisplay = _currentPlatform.ToString();

            if (_autoInitialize)
            {
                DiscoverProviders();
                InitializeProviders();
            }

            UpdateRuntimeStatus();
        }

        void Update()
        {
            // Update all active providers
            foreach (var provider in _providers)
            {
                if (provider.IsAvailable)
                {
                    provider.Update();
                }
            }

            // Notify consumers
            foreach (var consumer in _consumers)
            {
                var provider = GetProviderForCapabilities(consumer.RequiredCapabilities);
                if (provider != null && provider.IsAvailable)
                {
                    consumer.OnTrackingData(provider);
                }
            }

            // Update runtime status display
            UpdateRuntimeStatus();
        }

        void UpdateRuntimeStatus()
        {
            _providerCount = _providers.Count;
            _consumerCount = _consumers.Count;
            _availableCapabilitiesDisplay = AvailableCapabilities;
            _activeProviderIds = _providers
                .Where(p => p.IsAvailable)
                .Select(p => p.Id)
                .ToArray();
        }

        void OnDestroy()
        {
            foreach (var provider in _providers)
            {
                provider.Shutdown();
                provider.Dispose();
            }
            _providers.Clear();
            _capabilityMap.Clear();

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Discovers providers via [TrackingProvider] attribute.
        /// </summary>
        public void DiscoverProviders()
        {
            var providerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(TrackingProviderAttribute), false).Length > 0)
                .Where(t => typeof(ITrackingProvider).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in providerTypes)
            {
                var attr = (TrackingProviderAttribute)type.GetCustomAttributes(typeof(TrackingProviderAttribute), false)[0];

                // Check platform compatibility
                if ((attr.Platforms & _currentPlatform) == 0) continue;

                try
                {
                    var provider = (ITrackingProvider)Activator.CreateInstance(type);
                    RegisterProvider(provider);

                    if (_logProviders)
                        Debug.Log($"[TrackingManager] Discovered: {attr.Id} (caps: {attr.Capabilities})");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TrackingManager] Failed to create {type.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Manually register a provider instance.
        /// </summary>
        public void RegisterProvider(ITrackingProvider provider)
        {
            if (_providers.Any(p => p.Id == provider.Id)) return;

            _providers.Add(provider);
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Map capabilities to best provider
            foreach (TrackingCap cap in Enum.GetValues(typeof(TrackingCap)))
            {
                if (cap == TrackingCap.None || cap == TrackingCap.All) continue;

                if ((provider.Capabilities & cap) != 0)
                {
                    if (!_capabilityMap.ContainsKey(cap) ||
                        provider.Priority > _capabilityMap[cap].Priority)
                    {
                        _capabilityMap[cap] = provider;
                    }
                }
            }

            UpdateAvailableCapabilities();
        }

        /// <summary>
        /// Register a tracking consumer for automatic updates.
        /// </summary>
        public void RegisterConsumer(ITrackingConsumer consumer)
        {
            if (!_consumers.Contains(consumer))
                _consumers.Add(consumer);
        }

        public void UnregisterConsumer(ITrackingConsumer consumer)
        {
            _consumers.Remove(consumer);
        }

        /// <summary>
        /// Initialize all registered providers.
        /// </summary>
        public void InitializeProviders()
        {
            foreach (var provider in _providers)
            {
                try
                {
                    provider.Initialize();
                    provider.OnCapabilitiesChanged += OnProviderCapabilitiesChanged;
                    provider.OnTrackingLost += () => OnProviderTrackingLost(provider);
                    provider.OnTrackingFound += () => OnProviderTrackingFound(provider);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TrackingManager] Failed to init {provider.Id}: {e.Message}");
                }
            }

            UpdateAvailableCapabilities();
        }

        /// <summary>
        /// Get best provider for requested capabilities.
        /// </summary>
        public ITrackingProvider GetProviderForCapabilities(TrackingCap required)
        {
            // Find provider that supports ALL required capabilities
            return _providers.FirstOrDefault(p =>
                p.IsAvailable && (p.Capabilities & required) == required);
        }

        /// <summary>
        /// Try to get tracking data of specific type.
        /// </summary>
        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            foreach (var provider in _providers)
            {
                if (provider.IsAvailable && provider.TryGetData(out data))
                    return true;
            }
            data = default;
            return false;
        }

        void UpdateAvailableCapabilities()
        {
            AvailableCapabilities = TrackingCap.None;
            foreach (var provider in _providers)
            {
                if (provider.IsAvailable)
                    AvailableCapabilities |= provider.Capabilities;
            }
        }

        void OnProviderCapabilitiesChanged(TrackingCap newCaps)
        {
            UpdateAvailableCapabilities();
        }

        void OnProviderTrackingLost(ITrackingProvider provider)
        {
            foreach (var consumer in _consumers)
            {
                if ((consumer.RequiredCapabilities & provider.Capabilities) != 0)
                    consumer.OnTrackingLost();
            }
        }

        void OnProviderTrackingFound(ITrackingProvider provider)
        {
            UpdateAvailableCapabilities();
        }

        Platform GetCurrentPlatform()
        {
#if UNITY_EDITOR
            return Platform.Editor;
#elif UNITY_IOS
            return Platform.iOS;
#elif UNITY_ANDROID && !UNITY_EDITOR
    #if UNITY_XR_OCULUS
            return Platform.Quest;
    #else
            return Platform.Android;
    #endif
#elif UNITY_WEBGL
            return Platform.WebGL;
#else
            return Platform.None;
#endif
        }
    }
}
