// PhysicsTestHarness - Runtime test harness for Spec 007 physics VFX
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;
using XRRAI.VFXBinders;

namespace XRRAI.Testing
{
    public class PhysicsTestHarness : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private float cycleInterval = 5f;

        [Header("Physics Settings")]
        [SerializeField] private Vector3 gravityDirection = new Vector3(0, -9.8f, 0);
        [SerializeField] private float cameraSpeed = 5f;

        [Header("Status")]
        [SerializeField] private string currentVFXName;
        [SerializeField] private int currentIndex;
        [SerializeField] private Vector3 cameraVelocity;

        private List<VisualEffect> _loadedVFX = new();
        private float _cycleTimer;
        private Vector3 _lastCameraPos;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null) _lastCameraPos = _mainCamera.transform.position;
            LoadPhysicsVFX();
            if (_loadedVFX.Count > 0) SetActiveVFX(0);
            Debug.Log($"[PhysicsTestHarness] Loaded {_loadedVFX.Count} VFX. WASD=Move, G=Gravity, Space=Cycle");
        }

        private void Update()
        {
            HandleInput();
            UpdateCameraVelocity();
            UpdateGlobalShaderProps();

            if (cycleInterval > 0)
            {
                _cycleTimer += Time.deltaTime;
                if (_cycleTimer >= cycleInterval) { _cycleTimer = 0; CycleToNextVFX(); }
            }
        }

        private void HandleInput()
        {
            if (_mainCamera == null) return;

            // WASD camera movement
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += _mainCamera.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= _mainCamera.transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= _mainCamera.transform.right;
            if (Input.GetKey(KeyCode.D)) move += _mainCamera.transform.right;
            if (Input.GetKey(KeyCode.Q)) move += Vector3.up;
            if (Input.GetKey(KeyCode.E)) move -= Vector3.up;

            _mainCamera.transform.position += move.normalized * cameraSpeed * Time.deltaTime;

            // Toggle gravity direction
            if (Input.GetKeyDown(KeyCode.G))
            {
                gravityDirection = gravityDirection.y < 0 
                    ? new Vector3(0, 9.8f, 0) 
                    : new Vector3(0, -9.8f, 0);
                Debug.Log($"[PhysicsTestHarness] Gravity: {gravityDirection}");
            }

            if (Input.GetKeyDown(KeyCode.Space)) CycleToNextVFX();
        }

        private void UpdateCameraVelocity()
        {
            if (_mainCamera == null) return;
            cameraVelocity = (_mainCamera.transform.position - _lastCameraPos) / Time.deltaTime;
            _lastCameraPos = _mainCamera.transform.position;
        }

        private void UpdateGlobalShaderProps()
        {
            Shader.SetGlobalVector("_CameraVelocity", cameraVelocity);
            Shader.SetGlobalVector("_GravityDirection", gravityDirection);
            Shader.SetGlobalFloat("_CameraSpeed", cameraVelocity.magnitude);
        }

        private void LoadPhysicsVFX()
        {
            string[] names = { "swarm", "warp", "particles", "trails", "ribbons" };
            foreach (var name in names)
            {
                var asset = Resources.Load<VisualEffectAsset>($"VFX/Environment/{name}")
                         ?? Resources.Load<VisualEffectAsset>($"VFX/People/{name}");
                if (asset != null)
                {
                    var go = new GameObject($"VFX_{name}");
                    go.transform.SetParent(transform);
                    var vfx = go.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = asset;
                    go.AddComponent<VFXARBinder>();
                    go.AddComponent<VFXPhysicsBinder>();
                    vfx.Stop(); go.SetActive(false);
                    _loadedVFX.Add(vfx);
                }
            }
        }

        private void SetActiveVFX(int index)
        {
            foreach (var vfx in _loadedVFX) { vfx.gameObject.SetActive(false); vfx.Stop(); }
            currentIndex = index % Mathf.Max(1, _loadedVFX.Count);
            if (_loadedVFX.Count > 0)
            {
                var active = _loadedVFX[currentIndex];
                active.gameObject.SetActive(true); active.Play();
                currentVFXName = active.name;
            }
        }

        private void CycleToNextVFX() => SetActiveVFX(currentIndex + 1);

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 140));
            GUILayout.Label($"=== Physics Test ===");
            GUILayout.Label($"VFX: {currentVFXName}");
            GUILayout.Label($"Velocity: {cameraVelocity.magnitude:F1} m/s");
            GUILayout.Label($"Gravity: {gravityDirection.y:F1}");
            GUILayout.Label($"WASD=Move, G=Gravity, Space=Cycle");
            GUILayout.EndArea();
        }
    }
}
