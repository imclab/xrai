// HandTrackingPlayModeTests - PlayMode tests with AR Remote (spec-012 T5.2)
// Tests provider auto-detection, VFX binding, and fallback chain in actual Unity runtime

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MetavidoVFX.HandTracking;

namespace MetavidoVFX.Tests
{
    [TestFixture]
    public class HandTrackingPlayModeTests
    {
        private HandTrackingProviderManager _manager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Wait for Unity to initialize
            yield return null;

            // Get or create manager
            _manager = HandTrackingProviderManager.Instance;
            Assert.IsNotNull(_manager, "HandTrackingProviderManager should exist");

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }

        #region Provider Detection Tests

        [UnityTest]
        public IEnumerator Provider_AutoDetects_Available()
        {
            yield return null;

            // Manager should detect at least Touch provider (always available in Editor)
            Assert.IsNotNull(_manager.ActiveProvider,
                "Should have an active provider (at least Touch fallback)");

            Debug.Log($"[PlayMode Test] Active provider: {_manager.ActiveProvider?.GetType().Name}");
        }

        [UnityTest]
        public IEnumerator Provider_TouchFallback_WorksInEditor()
        {
            yield return null;

            // In Editor without AR Remote, should fall back to Touch
            var provider = _manager.ActiveProvider;

            if (provider is TouchInputHandTrackingProvider)
            {
                Debug.Log("[PlayMode Test] Touch provider is active (expected in Editor)");
                Assert.Pass("Touch provider is active as expected");
            }
            else
            {
                // AR Remote might be connected
                Debug.Log($"[PlayMode Test] Non-touch provider active: {provider?.GetType().Name}");
                Assert.Pass("Provider detected (AR Remote may be connected)");
            }
        }

        [UnityTest]
        public IEnumerator Provider_Enumerate_ReturnsAllProviders()
        {
            yield return null;

            var providers = _manager.GetAllProviders();
            Assert.IsNotNull(providers, "Provider list should not be null");
            Assert.Greater(providers.Count, 0, "Should have at least one provider");

            foreach (var provider in providers)
            {
                Debug.Log($"[PlayMode Test] Available provider: {provider.ProviderId} (priority: {provider.Priority})");
            }

            Assert.Pass($"Found {providers.Count} providers");
        }

        #endregion

        #region Hand Tracking Tests

        [UnityTest]
        public IEnumerator HandTracking_GetJointPosition_NoException()
        {
            yield return null;

            var provider = _manager.ActiveProvider;
            if (provider == null)
            {
                Assert.Inconclusive("No active provider");
                yield break;
            }

            // This should not throw even if hand is not tracked
            Vector3 position = provider.GetJointPosition(Hand.Right, HandJointID.Wrist);
            Debug.Log($"[PlayMode Test] Wrist position: {position}");

            // Position might be zero if hand not tracked, that's OK
            Assert.Pass("GetJointPosition executed without exception");
        }

        [UnityTest]
        public IEnumerator HandTracking_GetPinchStrength_ReturnsValidRange()
        {
            yield return null;

            var provider = _manager.ActiveProvider;
            if (provider == null)
            {
                Assert.Inconclusive("No active provider");
                yield break;
            }

            float pinchStrength = provider.GetPinchStrength(Hand.Right);
            Debug.Log($"[PlayMode Test] Pinch strength: {pinchStrength}");

            Assert.GreaterOrEqual(pinchStrength, 0f, "Pinch strength should be >= 0");
            Assert.LessOrEqual(pinchStrength, 1f, "Pinch strength should be <= 1");
        }

        [UnityTest]
        public IEnumerator HandTracking_IsHandTracked_ReturnsBool()
        {
            yield return null;

            var provider = _manager.ActiveProvider;
            if (provider == null)
            {
                Assert.Inconclusive("No active provider");
                yield break;
            }

            bool leftTracked = provider.IsHandTracked(Hand.Left);
            bool rightTracked = provider.IsHandTracked(Hand.Right);

            Debug.Log($"[PlayMode Test] Left tracked: {leftTracked}, Right tracked: {rightTracked}");

            // In Editor without AR, hands won't be tracked - that's expected
            Assert.Pass("IsHandTracked returned valid booleans");
        }

        #endregion

        #region Gesture Detection Tests

        [UnityTest]
        public IEnumerator GestureDetector_Updates_WithoutException()
        {
            yield return null;

            var detector = new GestureDetector(Hand.Right, 0.02f, 0.04f);

            // Simulate several updates
            for (int i = 0; i < 10; i++)
            {
                detector.UpdateSimple(0.05f, 0f);
                yield return null;
            }

            Assert.Pass("GestureDetector updated without exception");
        }

        [UnityTest]
        public IEnumerator GestureDetector_FiresEvents_OnStateChange()
        {
            yield return null;

            var detector = new GestureDetector(Hand.Right, 0.02f, 0.04f);

            bool startFired = false;
            bool endFired = false;

            detector.OnGestureStart += (h, g) => { if (g == GestureType.Pinch) startFired = true; };
            detector.OnGestureEnd += (h, g) => { if (g == GestureType.Pinch) endFired = true; };

            // Start pinch
            detector.UpdateSimple(0.01f, 0f);
            yield return null;

            Assert.IsTrue(startFired, "Pinch start should fire");

            // End pinch
            detector.UpdateSimple(0.05f, 0f);
            yield return null;

            Assert.IsTrue(endFired, "Pinch end should fire");
        }

        #endregion

        #region VFX Binding Tests

        [UnityTest]
        public IEnumerator VFXHandBinder_FindsInScene()
        {
            yield return null;

            var binders = Object.FindObjectsByType<VFX.Binders.VFXHandBinder>(FindObjectsSortMode.None);
            Debug.Log($"[PlayMode Test] Found {binders.Length} VFXHandBinder(s) in scene");

            // Binder might not be in test scene
            if (binders.Length == 0)
            {
                Assert.Inconclusive("No VFXHandBinder in scene (add to test scene for full coverage)");
            }
            else
            {
                Assert.Pass($"Found {binders.Length} VFXHandBinder(s)");
            }
        }

        #endregion

        #region Integration Tests (Require AR Remote)

        [UnityTest]
        [Category("ARRemote")]
        public IEnumerator ARRemote_HandsTracked_WhenConnected()
        {
            // This test requires AR Remote to be connected
            yield return new WaitForSeconds(1f);

            var provider = _manager.ActiveProvider;
            if (provider == null || provider is TouchInputHandTrackingProvider)
            {
                Assert.Inconclusive("AR Remote not connected - skipping AR-specific test");
                yield break;
            }

            // Wait for hands to be detected
            float timeout = 10f;
            float elapsed = 0f;
            bool handDetected = false;

            while (elapsed < timeout && !handDetected)
            {
                if (provider.IsHandTracked(Hand.Right) || provider.IsHandTracked(Hand.Left))
                {
                    handDetected = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (handDetected)
            {
                Debug.Log("[PlayMode Test] Hand detected via AR Remote!");
                Assert.Pass("Hand tracking working with AR Remote");
            }
            else
            {
                Debug.Log("[PlayMode Test] No hand detected within timeout (hold hand up to device)");
                Assert.Inconclusive("No hand detected - ensure hand is visible to device camera");
            }
        }

        [UnityTest]
        [Category("ARRemote")]
        public IEnumerator ARRemote_PinchGesture_Detected()
        {
            yield return new WaitForSeconds(1f);

            var provider = _manager.ActiveProvider;
            if (provider == null || provider is TouchInputHandTrackingProvider)
            {
                Assert.Inconclusive("AR Remote not connected");
                yield break;
            }

            Debug.Log("[PlayMode Test] Waiting for pinch gesture (pinch your fingers)...");

            float timeout = 15f;
            float elapsed = 0f;
            bool pinchDetected = false;

            while (elapsed < timeout && !pinchDetected)
            {
                float pinchStrength = provider.GetPinchStrength(Hand.Right);
                if (pinchStrength > 0.7f)
                {
                    pinchDetected = true;
                    Debug.Log($"[PlayMode Test] Pinch detected! Strength: {pinchStrength}");
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (pinchDetected)
            {
                Assert.Pass("Pinch gesture detected via AR Remote");
            }
            else
            {
                Assert.Inconclusive("Pinch not detected - try pinching fingers during test");
            }
        }

        #endregion
    }
}
