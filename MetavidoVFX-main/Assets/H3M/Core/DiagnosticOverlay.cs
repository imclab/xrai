using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;

namespace H3M.Core
{
    /// <summary>
    /// Standaloee diagnostic overlay - doesn't depend on any other scripts
    /// </summary>
    public class DiagnosticOverlay : MonoBehaviour
    {
        [SerializeField] bool _spawnTestParticles = true;

        ParticleSystem _testPS;
        int _frame = 0;
        string _log = "";

        void Start()
        {
            Log("DiagnosticOverlay Start");

            if (_spawnTestParticles)
                CreateTestParticleSystem();
        }

        void CreateTestParticleSystem()
        {
            // Create a simple legacy particle system for testing
            var go = new GameObject("TestParticles");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0, 0, 2); // 2m in front

            _testPS = go.AddComponent<ParticleSystem>();
            var main = _testPS.main;
            main.startColor = Color.cyan;
            main.startSize = 0.05f;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.maxParticles = 1000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _testPS.emission;
            emission.rateOverTime = 50;

            var shape = _testPS.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Simple unlit material
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = Color.cyan;

            Log("Created test ParticleSystem");
        }

        void Update()
        {
            _frame++;

            if (_frame % 60 == 0)
            {
                try { GatherDiagnostics(); } catch { }
            }
        }

        void GatherDiagnostics()
        {
            _log = $"Frame: {_frame}\n";

            // Check AR components
            var session = FindFirstObjectByType<ARSession>();
            _log += $"ARSession: {(session != null ? "OK" : "NULL")}\n";

            var occlusion = FindFirstObjectByType<AROcclusionManager>();
            if (occlusion != null)
            {
                var envDepth = occlusion.environmentDepthTexture;
                var humanDepth = occlusion.humanDepthTexture;
                _log += $"EnvDepth: {(envDepth != null ? $"{envDepth.width}x{envDepth.height}" : "NULL")}\n";
                _log += $"HumanDepth: {(humanDepth != null ? $"{humanDepth.width}x{humanDepth.height}" : "NULL")}\n";
            }
            else
            {
                _log += "AROcclusionManager: NULL\n";
            }

            // Check VFX
            var vfx = FindFirstObjectByType<VisualEffect>();
            if (vfx != null)
            {
                _log += $"VFX: {vfx.name}\n";
                _log += $"VFX Enabled: {vfx.enabled}\n";
                _log += $"VFX Alive: {vfx.aliveParticleCount}\n";
                _log += $"VFX Asset: {(vfx.visualEffectAsset != null ? vfx.visualEffectAsset.name : "NULL")}\n";
            }
            else
            {
                _log += "VFX: NULL\n";
            }

            // Check test particles
            if (_testPS != null)
            {
                _log += $"TestPS Alive: {_testPS.particleCount}\n";
            }

            // Camera
            var cam = Camera.main;
            if (cam != null)
            {
                _log += $"Cam FOV: {cam.fieldOfView:F1}\n";
                _log += $"Cam Pos: {cam.transform.position}\n";
            }
        }

        void Log(string msg)
        {
            Debug.Log($"[Diagnostic] {msg}");
            _log += msg + "\n";
        }

        void OnGUI()
        {
            // Large, visible text
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 28;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            // Black background for readability
            GUI.backgroundColor = new Color(0, 0, 0, 0.8f);

            GUILayout.BeginArea(new Rect(20, 550, Screen.width - 40, Screen.height - 100));

            GUILayout.Label("=== H3M DIAGNOSTIC ===", style);
            GUILayout.Space(10);

            style.fontSize = 22;
            GUILayout.Label(_log, style);

            GUILayout.Space(20);

            // Test buttons
            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 32;

            if (GUILayout.Button("Spawn More Particles", buttonStyle, GUILayout.Height(60)))
            {
                if (_testPS != null)
                    _testPS.Emit(100);
                Log("Emitted 100 particles");
            }

            if (GUILayout.Button("Toggle VFX", buttonStyle, GUILayout.Height(60)))
            {
                var vfx = FindFirstObjectByType<VisualEffect>();
                if (vfx != null)
                {
                    vfx.enabled = !vfx.enabled;
                    Log($"VFX enabled: {vfx.enabled}");
                }
            }

            GUILayout.EndArea();
        }
    }
}
