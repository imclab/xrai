// AudioTestHarness - Runtime test harness for Spec 007 audio reactive VFX
using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

namespace MetavidoVFX.Testing
{
    public class AudioTestHarness : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private float cycleInterval = 5f;

        [Header("Status")]
        [SerializeField] private string currentVFXName;
        [SerializeField] private int currentIndex;
        [SerializeField] private float bassLevel;
        [SerializeField] private float beatPulse;

        private List<VisualEffect> _loadedVFX = new();
        private float _cycleTimer;
        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            LoadAudioReactiveVFX();
            if (_loadedVFX.Count > 0) SetActiveVFX(0);
            Debug.Log($"[AudioTestHarness] Loaded {_loadedVFX.Count} VFX. Space=Cycle, T=Tone");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) CycleToNextVFX();
            if (Input.GetKeyDown(KeyCode.T)) PlayTestTone();

            if (cycleInterval > 0)
            {
                _cycleTimer += Time.deltaTime;
                if (_cycleTimer >= cycleInterval) { _cycleTimer = 0; CycleToNextVFX(); }
            }
            UpdateStatus();
        }

        private void LoadAudioReactiveVFX()
        {
            string[] names = { "bubbles", "particles", "trails", "swarm", "warp" };
            foreach (var name in names)
            {
                var asset = Resources.Load<VisualEffectAsset>($"VFX/People/{name}") 
                         ?? Resources.Load<VisualEffectAsset>($"VFX/Environment/{name}");
                if (asset != null)
                {
                    var go = new GameObject($"VFX_{name}");
                    go.transform.SetParent(transform);
                    var vfx = go.AddComponent<VisualEffect>();
                    vfx.visualEffectAsset = asset;
                    go.AddComponent<VFXARBinder>();
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

        private void PlayTestTone()
        {
            int rate = 44100; float dur = 2f;
            var clip = AudioClip.Create("TestTone", (int)(rate * dur), 1, rate, false);
            float[] samples = new float[(int)(rate * dur)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = 0.5f * Mathf.Sin(2 * Mathf.PI * 100f * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * 1000f * t);
            }
            clip.SetData(samples, 0);
            _audioSource.clip = clip; _audioSource.Play();
        }

        private void UpdateStatus()
        {
            var bands = Shader.GetGlobalVector("_AudioBands");
            bassLevel = bands.x;
            beatPulse = Shader.GetGlobalFloat("_BeatPulse");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 250, 120));
            GUILayout.Label($"=== Audio Test ===");
            GUILayout.Label($"VFX: {currentVFXName}");
            GUILayout.Label($"Bass: {bassLevel:F2} | Beat: {beatPulse:F2}");
            GUILayout.Label($"Space=Cycle, T=Tone");
            GUILayout.EndArea();
        }
    }
}
