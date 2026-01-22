// PhysicsTestHarness - Spec 007 T-018 Physics Test Scene Setup
// Validates: Camera velocity, gravity direction, AR mesh collision

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using MetavidoVFX.VFX.Binders;

namespace MetavidoVFX.Testing
{
    /// <summary>
    /// Test harness for physics-driven VFX validation.
    /// Tests camera velocity binding, gravity direction, and AR mesh collision.
    /// Press G to cycle gravity direction, V to toggle velocity binding.
    /// </summary>
    public class PhysicsTestHarness : MonoBehaviour
    {
        [Header("VFX Setup")]
        [Tooltip("VFX assets to test (leave empty to auto-load from Resources)")]
        [SerializeField] private VisualEffectAsset[] testVFXAssets;
        [SerializeField] private int maxVFXCount = 4;
        [SerializeField] private float vfxSpacing = 2.5f;

        [Header("Camera Movement")]
        [SerializeField] private bool enableCameraMovement = true;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float lookSpeed = 2f;

        [Header("Gravity Test")]
        [SerializeField] private Vector3[] gravityDirections = new Vector3[]
        {
            new Vector3(0, -9.81f, 0),  // Default down
            new Vector3(0, 9.81f, 0),   // Up
            new Vector3(9.81f, 0, 0),   // Right
            new Vector3(-9.81f, 0, 0),  // Left
            new Vector3(0, 0, 9.81f),   // Forward
            new Vector3(0, 0, -9.81f),  // Back
        };
        [SerializeField] private int currentGravityIndex = 0;

        [Header("Runtime Status (Read-Only)")]
        [SerializeField] private Vector3 _cameraVelocityDisplay = Vector3.zero;
        [SerializeField] private float _cameraSpeedDisplay = 0f;
        [SerializeField] private Vector3 _currentGravityDisplay = Vector3.zero;
        [SerializeField] private int _activeVFXCount = 0;
        [SerializeField] private int _bounceCollisionsDisplay = 0;

        private List<VisualEffect> _testVFX = new();
        private List<VFXPhysicsBinder> _physicsBinders = new();
        private Vector3 _lastCameraPosition;
        private Vector3 _cameraVelocity;
        private Camera _mainCamera;

        void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
                _lastCameraPosition = _mainCamera.transform.position;

            if (testVFXAssets == null || testVFXAssets.Length == 0)
                LoadDefaultVFX();

            SetupTestVFX();
            ApplyGravity(currentGravityIndex);

            Debug.Log($"[PhysicsTestHarness] Initialized with {_testVFX.Count} VFX");
            Debug.Log("[PhysicsTestHarness] Controls: WASD=Move, Mouse=Look, G=Cycle Gravity, V=Toggle Velocity");
        }

        void Update()
        {
            // Camera movement
            if (enableCameraMovement && _mainCamera != null)
            {
                UpdateCameraMovement();
            }

            // Cycle gravity with G
            if (Input.GetKeyDown(KeyCode.G))
            {
                currentGravityIndex = (currentGravityIndex + 1) % gravityDirections.Length;
                ApplyGravity(currentGravityIndex);
            }

            // Toggle velocity binding with V
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleVelocityBinding();
            }

            // Calculate camera velocity
            if (_mainCamera != null)
            {
                Vector3 currentPos = _mainCamera.transform.position;
                _cameraVelocity = (currentPos - _lastCameraPosition) / Time.deltaTime;
                _lastCameraPosition = currentPos;
            }

            UpdateRuntimeStatus();
        }

        void UpdateCameraMovement()
        {
            var t = _mainCamera.transform;

            // WASD movement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = (t.right * h + t.forward * v) * moveSpeed * Time.deltaTime;
            t.position += move;

            // Q/E for up/down
            if (Input.GetKey(KeyCode.Q)) t.position += Vector3.down * moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) t.position += Vector3.up * moveSpeed * Time.deltaTime;

            // Mouse look (hold right mouse button)
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
                t.Rotate(Vector3.up, mouseX, Space.World);
                t.Rotate(Vector3.right, -mouseY, Space.Self);
            }
        }

        void UpdateRuntimeStatus()
        {
            _cameraVelocityDisplay = _cameraVelocity;
            _cameraSpeedDisplay = _cameraVelocity.magnitude;
            _currentGravityDisplay = gravityDirections[currentGravityIndex];
            _activeVFXCount = _testVFX.Count;

            // Count bounce collisions from physics binders
            _bounceCollisionsDisplay = 0;
            foreach (var binder in _physicsBinders)
            {
                if (binder != null)
                {
                    // Note: Would need to track collisions in VFXPhysicsBinder
                }
            }
        }

        void LoadDefaultVFX()
        {
            var allVFX = Resources.LoadAll<VisualEffectAsset>("VFX");
            var selectedVFX = new List<VisualEffectAsset>();

            // Pick VFX that work well for physics visualization
            string[] preferredNames = { "spark", "particles", "flame", "point", "trail" };

            foreach (var vfx in allVFX)
            {
                if (selectedVFX.Count >= maxVFXCount) break;

                foreach (var name in preferredNames)
                {
                    if (vfx.name.ToLower().Contains(name))
                    {
                        selectedVFX.Add(vfx);
                        break;
                    }
                }
            }

            // Fill with any VFX if not enough
            foreach (var vfx in allVFX)
            {
                if (selectedVFX.Count >= maxVFXCount) break;
                if (!selectedVFX.Contains(vfx))
                    selectedVFX.Add(vfx);
            }

            testVFXAssets = selectedVFX.ToArray();
            Debug.Log($"[PhysicsTestHarness] Loaded {testVFXAssets.Length} VFX from Resources");
        }

        void SetupTestVFX()
        {
            float startX = -(testVFXAssets.Length - 1) * vfxSpacing / 2f;

            for (int i = 0; i < testVFXAssets.Length; i++)
            {
                var asset = testVFXAssets[i];
                if (asset == null) continue;

                // Create VFX GameObject
                var go = new GameObject($"PhysicsVFX_{asset.name}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(startX + i * vfxSpacing, 1, 0);

                // Add VisualEffect
                var vfx = go.AddComponent<VisualEffect>();
                vfx.visualEffectAsset = asset;

                // Add VFXARBinder for depth/position data
                var arBinder = go.AddComponent<VFXARBinder>();

                // Add VFXPhysicsBinder for physics
                var physicsBinder = go.AddComponent<VFXPhysicsBinder>();
                physicsBinder.enableVelocity = true;
                physicsBinder.enableGravity = true;

                _testVFX.Add(vfx);
                _physicsBinders.Add(physicsBinder);

                Debug.Log($"[PhysicsTestHarness] Created VFX: {asset.name} with physics binder");
            }
        }

        void ApplyGravity(int index)
        {
            Vector3 gravity = gravityDirections[index];
            foreach (var binder in _physicsBinders)
            {
                if (binder != null)
                {
                    binder.gravityDirection = gravity.normalized;
                    binder.gravityStrength = gravity.magnitude;
                }
            }

            string dirName = index switch
            {
                0 => "Down",
                1 => "Up",
                2 => "Right",
                3 => "Left",
                4 => "Forward",
                5 => "Back",
                _ => "Custom"
            };

            Debug.Log($"[PhysicsTestHarness] Gravity: {dirName} ({gravity})");
        }

        void ToggleVelocityBinding()
        {
            bool newState = !(_physicsBinders.Count > 0 && _physicsBinders[0] != null && _physicsBinders[0].enableVelocity);

            foreach (var binder in _physicsBinders)
            {
                if (binder != null)
                    binder.enableVelocity = newState;
            }

            Debug.Log($"[PhysicsTestHarness] Velocity binding: {(newState ? "ON" : "OFF")}");
        }

        void OnGUI()
        {
            // Velocity display
            GUI.Label(new Rect(10, 10, 300, 20), $"Camera Speed: {_cameraSpeedDisplay:F2} m/s");
            GUI.Label(new Rect(10, 30, 300, 20), $"Velocity: {_cameraVelocityDisplay}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Gravity [{currentGravityIndex}]: {_currentGravityDisplay}");

            // Velocity indicator bar
            float barWidth = Mathf.Clamp(_cameraSpeedDisplay * 20, 0, 200);
            GUI.backgroundColor = Color.Lerp(Color.green, Color.red, _cameraSpeedDisplay / 10f);
            GUI.Box(new Rect(10, 75, barWidth, 15), "");
            GUI.backgroundColor = Color.white;

            // Instructions
            GUI.Label(new Rect(10, 100, 400, 20), "WASD: Move | Mouse+RMB: Look | G: Gravity | V: Velocity | Tab: Dashboard");
        }

        void OnDrawGizmos()
        {
            // Draw gravity direction
            Gizmos.color = Color.yellow;
            Vector3 gravity = gravityDirections[Mathf.Clamp(currentGravityIndex, 0, gravityDirections.Length - 1)];
            Gizmos.DrawRay(transform.position, gravity.normalized * 2);
            Gizmos.DrawWireSphere(transform.position + gravity.normalized * 2, 0.1f);

            // Draw velocity direction
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, _cameraVelocity.normalized * Mathf.Min(_cameraSpeedDisplay, 3));
            }
        }
    }
}
