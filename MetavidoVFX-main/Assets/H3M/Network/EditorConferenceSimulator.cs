// EditorConferenceSimulator.cs - Mock conferencing for Editor testing (spec-003)
// Simulates multiple remote users without network connectivity
// Useful for testing layout, spatial audio, and stress testing
//
// Features:
// - Spawn N mock holograms with animated movement
// - Simulate voice activity with random patterns
// - Stress testing with 20+ users
// - Works in Editor and build

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Simulates a hologram conferencing session for Editor testing.
    /// Creates mock remote holograms with simulated movement and audio.
    /// </summary>
    public class EditorConferenceSimulator : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Simulation Settings")]
        [SerializeField] private bool _autoStartSimulation = false;
        [SerializeField] private int _initialUserCount = 3;
        [SerializeField] private float _userJoinInterval = 1f;
        [SerializeField] private bool _simulateVoiceActivity = true;
        [SerializeField] private bool _simulateMovement = true;

        [Header("Mock Hologram")]
        [SerializeField] private GameObject _mockHologramPrefab;
        [SerializeField] private VisualEffectAsset _fallbackVFX;
        [SerializeField] private float _hologramScale = 0.15f;

        [Header("Movement Simulation")]
        [SerializeField] private float _idleMovementRange = 0.05f;
        [SerializeField] private float _idleMovementSpeed = 0.5f;
        [SerializeField] private float _headBobAmount = 0.02f;
        [SerializeField] private float _headBobSpeed = 1f;

        [Header("Voice Simulation")]
        [SerializeField] private float _voiceActivityChance = 0.1f;
        [SerializeField] private float _voiceMinDuration = 1f;
        [SerializeField] private float _voiceMaxDuration = 5f;
        [SerializeField] private float _voiceCooldown = 2f;

        [Header("Stress Test")]
        [SerializeField] private int _stressTestUserCount = 20;
        [SerializeField] private float _stressTestJoinRate = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugUI = true;
        [SerializeField] private bool _logSimulationEvents = true;

        #endregion

        #region Private Fields

        private ConferenceLayoutManager _layoutManager;
        private SpatialAudioController _audioController;
        private readonly Dictionary<string, MockUser> _mockUsers = new();
        private bool _isSimulating = false;
        private int _nextUserId = 1;

        #endregion

        #region Properties

        public bool IsSimulating => _isSimulating;
        public int MockUserCount => _mockUsers.Count;

        #endregion

        #region MonoBehaviour

        private void Awake()
        {
            _layoutManager = GetComponent<ConferenceLayoutManager>();
            if (_layoutManager == null)
            {
                _layoutManager = gameObject.AddComponent<ConferenceLayoutManager>();
            }

            _audioController = GetComponent<SpatialAudioController>();
        }

        private void Start()
        {
            if (_autoStartSimulation)
            {
                StartSimulation(_initialUserCount);
            }
        }

        private void Update()
        {
            if (!_isSimulating) return;

            UpdateMockUsers();
        }

        private void OnGUI()
        {
            if (!_showDebugUI) return;
            DrawDebugUI();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start the conference simulation with N users.
        /// </summary>
        public void StartSimulation(int userCount)
        {
            if (_isSimulating)
            {
                StopSimulation();
            }

            _isSimulating = true;
            StartCoroutine(SpawnUsersRoutine(userCount, _userJoinInterval));

            if (_logSimulationEvents)
                Debug.Log($"[ConferenceSim] Starting simulation with {userCount} users");
        }

        /// <summary>
        /// Stop the simulation and remove all mock users.
        /// </summary>
        public void StopSimulation()
        {
            _isSimulating = false;
            StopAllCoroutines();

            // Remove all mock users
            var userIds = new List<string>(_mockUsers.Keys);
            foreach (var userId in userIds)
            {
                RemoveMockUser(userId);
            }
            _mockUsers.Clear();

            if (_logSimulationEvents)
                Debug.Log("[ConferenceSim] Simulation stopped");
        }

        /// <summary>
        /// Add a single mock user to the conference.
        /// </summary>
        public string AddMockUser()
        {
            string peerId = $"mock_{_nextUserId:D4}";
            _nextUserId++;

            // Create mock hologram
            GameObject hologramGO = CreateMockHologram(peerId);

            // Register with layout manager
            var seat = _layoutManager.RegisterHologram(peerId, hologramGO.transform);

            // Create mock user state
            var mockUser = new MockUser
            {
                PeerId = peerId,
                GameObject = hologramGO,
                BasePosition = seat.Position,
                VoiceState = VoiceState.Silent,
                LastVoiceTime = Time.time - _voiceCooldown // Allow immediate voice
            };
            _mockUsers[peerId] = mockUser;

            if (_logSimulationEvents)
                Debug.Log($"[ConferenceSim] Added mock user: {peerId}");

            return peerId;
        }

        /// <summary>
        /// Remove a mock user from the conference.
        /// </summary>
        public void RemoveMockUser(string peerId)
        {
            if (_mockUsers.TryGetValue(peerId, out var user))
            {
                _layoutManager.UnregisterHologram(peerId);
                if (user.GameObject != null)
                {
                    Destroy(user.GameObject);
                }
                _mockUsers.Remove(peerId);

                if (_logSimulationEvents)
                    Debug.Log($"[ConferenceSim] Removed mock user: {peerId}");
            }
        }

        /// <summary>
        /// Run stress test with many users.
        /// </summary>
        public void StartStressTest()
        {
            StartSimulation(_stressTestUserCount);
        }

        /// <summary>
        /// Change layout mode for testing.
        /// </summary>
        public void SetLayoutMode(ConferenceLayoutMode mode)
        {
            _layoutManager.LayoutMode = mode;
        }

        #endregion

        #region Simulation Logic

        private IEnumerator SpawnUsersRoutine(int count, float interval)
        {
            for (int i = 0; i < count; i++)
            {
                AddMockUser();
                yield return new WaitForSeconds(interval);
            }
        }

        private GameObject CreateMockHologram(string peerId)
        {
            GameObject hologramGO;

            if (_mockHologramPrefab != null)
            {
                hologramGO = Instantiate(_mockHologramPrefab);
            }
            else
            {
                // Create default hologram with VFX
                hologramGO = new GameObject($"Hologram_{peerId}");

                // Add VFX if available
                if (_fallbackVFX != null)
                {
                    var vfx = hologramGO.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = _fallbackVFX;
                }
                else
                {
                    // Add placeholder sphere
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(hologramGO.transform);
                    sphere.transform.localScale = Vector3.one * 0.3f;
                    sphere.GetComponent<Collider>().enabled = false;

                    // Color by user ID
                    var renderer = sphere.GetComponent<Renderer>();
                    renderer.material.color = GetUserColor(peerId);
                }
            }

            hologramGO.name = $"Hologram_{peerId}";
            hologramGO.transform.localScale = Vector3.one * _hologramScale;

            return hologramGO;
        }

        private void UpdateMockUsers()
        {
            float time = Time.time;

            foreach (var kvp in _mockUsers)
            {
                var user = kvp.Value;
                if (user.GameObject == null) continue;

                // Idle movement
                if (_simulateMovement)
                {
                    UpdateIdleMovement(ref user, time);
                }

                // Voice simulation
                if (_simulateVoiceActivity)
                {
                    UpdateVoiceSimulation(ref user, time);
                }

                _mockUsers[kvp.Key] = user;
            }
        }

        private void UpdateIdleMovement(ref MockUser user, float time)
        {
            // Get seat position
            var seat = _layoutManager.GetSeatForPeer(user.PeerId);
            Vector3 basePos = seat.Position;

            // Subtle idle sway
            float swayX = Mathf.PerlinNoise(time * _idleMovementSpeed, 0) * 2 - 1;
            float swayZ = Mathf.PerlinNoise(0, time * _idleMovementSpeed) * 2 - 1;
            Vector3 sway = new Vector3(swayX, 0, swayZ) * _idleMovementRange;

            // Head bob
            float bob = Mathf.Sin(time * _headBobSpeed * Mathf.PI * 2) * _headBobAmount;
            Vector3 bobOffset = Vector3.up * bob;

            // Apply (don't override layout manager's positioning)
            user.GameObject.transform.position = basePos + sway + bobOffset;
        }

        private void UpdateVoiceSimulation(ref MockUser user, float time)
        {
            switch (user.VoiceState)
            {
                case VoiceState.Silent:
                    // Random chance to start speaking
                    if (time - user.LastVoiceTime > _voiceCooldown)
                    {
                        if (UnityEngine.Random.value < _voiceActivityChance * Time.deltaTime)
                        {
                            user.VoiceState = VoiceState.Speaking;
                            user.VoiceDuration = UnityEngine.Random.Range(_voiceMinDuration, _voiceMaxDuration);
                            user.VoiceStartTime = time;

                            // Simulate voice activity
                            SimulateVoiceActivity(user.PeerId, 0.5f + UnityEngine.Random.value * 0.5f);

                            if (_logSimulationEvents)
                                Debug.Log($"[ConferenceSim] {user.PeerId} started speaking");
                        }
                    }
                    break;

                case VoiceState.Speaking:
                    // Check if done speaking
                    if (time - user.VoiceStartTime > user.VoiceDuration)
                    {
                        user.VoiceState = VoiceState.Silent;
                        user.LastVoiceTime = time;

                        // Stop voice activity
                        SimulateVoiceActivity(user.PeerId, 0f);

                        if (_logSimulationEvents)
                            Debug.Log($"[ConferenceSim] {user.PeerId} stopped speaking");
                    }
                    else
                    {
                        // Vary volume during speech
                        float volume = 0.3f + Mathf.PerlinNoise(time * 5f, 0) * 0.7f;
                        SimulateVoiceActivity(user.PeerId, volume);
                    }
                    break;
            }
        }

        private void SimulateVoiceActivity(string peerId, float level)
        {
            if (_audioController != null)
            {
                // Generate mock audio samples (simple sine wave)
                float[] samples = new float[1024];
                float freq = 200 + level * 300; // 200-500 Hz
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = Mathf.Sin(i * freq / 44100f * Mathf.PI * 2) * level;
                }
                _audioController.FeedAudio(peerId, samples, 1, 44100);
            }
        }

        private Color GetUserColor(string peerId)
        {
            // Generate consistent color from peer ID
            int hash = peerId.GetHashCode();
            float hue = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        #endregion

        #region Debug UI

        private void DrawDebugUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"Conference Simulator", GUI.skin.box);
            GUILayout.Label($"Simulating: {_isSimulating}");
            GUILayout.Label($"Users: {_mockUsers.Count}");
            GUILayout.Label($"Layout: {_layoutManager.LayoutMode}");

            GUILayout.Space(10);

            if (!_isSimulating)
            {
                if (GUILayout.Button($"Start ({_initialUserCount} users)"))
                {
                    StartSimulation(_initialUserCount);
                }
                if (GUILayout.Button($"Stress Test ({_stressTestUserCount} users)"))
                {
                    StartStressTest();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Simulation"))
                {
                    StopSimulation();
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Add User"))
                {
                    AddMockUser();
                }

                if (_mockUsers.Count > 0 && GUILayout.Button("Remove User"))
                {
                    var firstUser = new List<string>(_mockUsers.Keys)[0];
                    RemoveMockUser(firstUser);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Layout Mode:");

            if (GUILayout.Button("Table (Semi-circle)"))
                SetLayoutMode(ConferenceLayoutMode.Table);
            if (GUILayout.Button("Theater (Side-by-side)"))
                SetLayoutMode(ConferenceLayoutMode.Theater);
            if (GUILayout.Button("Grid (Stress Test)"))
                SetLayoutMode(ConferenceLayoutMode.Grid);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region Internal Types

        private enum VoiceState
        {
            Silent,
            Speaking
        }

        private struct MockUser
        {
            public string PeerId;
            public GameObject GameObject;
            public Vector3 BasePosition;
            public VoiceState VoiceState;
            public float LastVoiceTime;
            public float VoiceStartTime;
            public float VoiceDuration;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Start Simulation (3 users)")]
        private void StartSimulation3() => StartSimulation(3);

        [ContextMenu("Start Simulation (10 users)")]
        private void StartSimulation10() => StartSimulation(10);

        [ContextMenu("Stress Test (20 users)")]
        private void StartStressTest20() => StartStressTest();

        [ContextMenu("Stop Simulation")]
        private void StopSim() => StopSimulation();

        #endregion
    }
}
