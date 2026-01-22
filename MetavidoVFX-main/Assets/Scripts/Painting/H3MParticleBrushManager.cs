using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;
using XRRAI.VFXBinders;

namespace XRRAI.BrushPainting
{
    public class H3MParticleBrushManager : MonoBehaviour
    {
        [Header("Configuration")]
        public H3MBrushCatalog brushCatalog;
        public Transform spawnTransform; // Cursor position

        [Header("Current State")]
        private H3MBrushDescriptor currentBrush;
        private GameObject activeShurikenBrush;
        private VisualEffect activeVFXBrush;
        private bool isEmitting = false;
        private float currentPressure = 1f;

        // Brush selection
        public void SelectBrush(string brushName)
        {
            // Disable current brush
            if (isEmitting) StopPainting();
            CleanupCurrentBrush();

            // Load new brush
            currentBrush = brushCatalog.GetBrush(brushName);
            if (currentBrush == null)
            {
                Debug.LogError($"Brush not found: {brushName}");
                return;
            }

            // Instantiate based on type
            switch (currentBrush.brushType)
            {
                case BrushType.Shuriken:
                    InstantiateShurikenBrush();
                    break;
                case BrushType.VFXGraph:
                    InstantiateVFXBrush();
                    break;
            }
        }

        private void InstantiateShurikenBrush()
        {
            activeShurikenBrush = Instantiate(
                currentBrush.shurikenPrefab,
                spawnTransform.position,
                spawnTransform.rotation,
                spawnTransform
            );

            // Disable emission initially
            var particleSystems = activeShurikenBrush.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }
        }

        private void InstantiateVFXBrush()
        {
            GameObject vfxGO = new GameObject($"VFX_{currentBrush.brushName}");
            vfxGO.transform.SetParent(spawnTransform, false);
            activeVFXBrush = vfxGO.AddComponent<VisualEffect>();
            activeVFXBrush.visualEffectAsset = currentBrush.vfxAsset;

            // Set initial parameters
            activeVFXBrush.SetFloat("EmissionRate", 0f);
            activeVFXBrush.SetVector3("TintColor", new Vector3(currentBrush.tintColor.r, currentBrush.tintColor.g, currentBrush.tintColor.b));
            activeVFXBrush.SetFloat("SizeScale", currentBrush.sizeScale);

            // Auto-detect and attach appropriate data binders for runtime VFX
            // This ensures spawned VFX receive AR depth, audio, and hand data
            VFXBinderUtility.SetupVFXAuto(activeVFXBrush);
        }

        // Emission control
        public void StartPainting()
        {
            if (currentBrush == null) return;
            isEmitting = true;

            if (currentBrush.brushType == BrushType.Shuriken)
            {
                EnableShurikenEmission();
            }
            else if (currentBrush.brushType == BrushType.VFXGraph)
            {
                EnableVFXEmission();
            }
        }

        public void StopPainting()
        {
            isEmitting = false;

            if (currentBrush.brushType == BrushType.Shuriken)
            {
                DisableShurikenEmission();
            }
            else if (currentBrush.brushType == BrushType.VFXGraph)
            {
                DisableVFXEmission();
            }
        }

        private void EnableShurikenEmission()
        {
            var particleSystems = activeShurikenBrush.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                emission.enabled = true;

                // Apply pressure-sensitive rate
                if (currentBrush.pressureSensitive)
                {
                    var main = ps.main;
                    main.startLifetime = currentPressure * currentBrush.pressureMultiplier;
                }
            }

            // Enable trails
            var trails = activeShurikenBrush.GetComponentsInChildren<TrailRenderer>();
            foreach (var trail in trails)
            {
                trail.enabled = true;
                trail.time = currentBrush.trailTime * 10f; // Painting mode
            }
        }

        private void DisableShurikenEmission()
        {
            var particleSystems = activeShurikenBrush.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }

            // Reduce trail time
            var trails = activeShurikenBrush.GetComponentsInChildren<TrailRenderer>();
            foreach (var trail in trails)
            {
                trail.time = currentBrush.trailTime; // Idle mode
            }
        }

        private void EnableVFXEmission()
        {
            float emissionRate = currentBrush.baseSpawnRate;
            if (currentBrush.pressureSensitive)
            {
                emissionRate *= currentPressure * currentBrush.pressureMultiplier;
            }
            activeVFXBrush.SetFloat("EmissionRate", emissionRate);
        }

        private void DisableVFXEmission()
        {
            activeVFXBrush.SetFloat("EmissionRate", 0f);
        }

        // Pressure modulation
        public void UpdatePressure(float pressure01)
        {
            currentPressure = Mathf.Clamp01(pressure01);

            if (isEmitting && currentBrush != null && currentBrush.pressureSensitive)
            {
                if (currentBrush.brushType == BrushType.Shuriken)
                {
                    UpdateShurikenPressure();
                }
                else if (currentBrush.brushType == BrushType.VFXGraph)
                {
                    activeVFXBrush.SetFloat("Pressure", currentPressure);
                }
            }
        }

        private void UpdateShurikenPressure()
        {
            var particleSystems = activeShurikenBrush.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var emission = ps.emission;
                var rateOverTime = emission.rateOverTime;
                rateOverTime.constant = currentBrush.baseSpawnRate * currentPressure;
                emission.rateOverTime = rateOverTime;
            }
        }

        private void CleanupCurrentBrush()
        {
            if (activeShurikenBrush != null)
            {
                Destroy(activeShurikenBrush);
                activeShurikenBrush = null;
            }
            if (activeVFXBrush != null)
            {
                Destroy(activeVFXBrush.gameObject);
                activeVFXBrush = null;
            }
        }

        void OnDestroy()
        {
            CleanupCurrentBrush();
        }
    }
}
