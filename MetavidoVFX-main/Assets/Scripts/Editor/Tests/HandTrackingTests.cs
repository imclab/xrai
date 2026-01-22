// HandTrackingTests - EditMode tests for hand tracking (spec-012 T5.1)
// Tests joint mapping, pinch hysteresis, gesture detection

#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using XRRAI.HandTracking;

namespace MetavidoVFX.Editor.Tests
{
    [TestFixture]
    public class HandTrackingTests
    {
        #region Joint ID Tests

        [Test]
        public void HandJointID_HasAllRequiredJoints()
        {
            // Verify all expected joints exist
            var joints = new[]
            {
                HandJointID.Wrist,
                HandJointID.Palm,
                HandJointID.ThumbTip,
                HandJointID.IndexTip,
                HandJointID.MiddleTip,
                HandJointID.RingTip,
                HandJointID.PinkyTip
            };

            foreach (var joint in joints)
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(HandJointID), joint),
                    $"Joint {joint} should be defined");
            }
        }

        [Test]
        public void HandJointID_FingertipsHaveCorrectIndices()
        {
            // Verify fingertip indices for VFX binding
            Assert.AreEqual(5, (int)HandJointID.ThumbTip);
            Assert.AreEqual(10, (int)HandJointID.IndexTip);
            Assert.AreEqual(15, (int)HandJointID.MiddleTip);
            Assert.AreEqual(20, (int)HandJointID.RingTip);
            Assert.AreEqual(25, (int)HandJointID.PinkyTip);
        }

        [Test]
        public void HandJointID_HasCorrectCount()
        {
            Assert.AreEqual(26, (int)HandJointID.Count,
                "HandJointID should have 26 joints");
        }

        #endregion

        #region Gesture Detector Tests

        [Test]
        public void GestureDetector_PinchHysteresis_PreventFlickering()
        {
            var detector = new GestureDetector(Hand.Right, 0.02f, 0.04f);

            // Track state changes
            int startCount = 0;
            int endCount = 0;
            detector.OnGestureStart += (h, g) => { if (g == GestureType.Pinch) startCount++; };
            detector.OnGestureEnd += (h, g) => { if (g == GestureType.Pinch) endCount++; };

            // Simulate pinch with distance oscillating around threshold
            float[] distances = { 0.05f, 0.03f, 0.019f, 0.025f, 0.021f, 0.039f, 0.041f };

            foreach (float dist in distances)
            {
                detector.UpdateSimple(dist, 0f);
            }

            // With hysteresis (start=0.02, end=0.04):
            // 0.05 - not pinching
            // 0.03 - not pinching (above start threshold)
            // 0.019 - START pinching (below 0.02)
            // 0.025 - still pinching (below end threshold 0.04)
            // 0.021 - still pinching
            // 0.039 - still pinching (below end threshold)
            // 0.041 - END pinching (above 0.04)

            Assert.AreEqual(1, startCount, "Should have exactly 1 pinch start");
            Assert.AreEqual(1, endCount, "Should have exactly 1 pinch end");
        }

        [Test]
        public void GestureDetector_PinchStrength_CalculatedCorrectly()
        {
            var detector = new GestureDetector(Hand.Right, 0.02f, 0.04f);

            // Fully pinched (distance = 0)
            detector.UpdateSimple(0f, 0f);
            Assert.Greater(detector.CurrentPinchStrength, 0.9f, "Fully pinched should have high strength");

            // Fully open (distance > threshold)
            detector.UpdateSimple(0.1f, 0f);
            Assert.Less(detector.CurrentPinchStrength, 0.1f, "Fully open should have low strength");
        }

        [Test]
        public void GestureDetector_GrabDetection_RequiresMultipleFingers()
        {
            var detector = new GestureDetector(Hand.Right);

            bool grabStarted = false;
            detector.OnGestureStart += (h, g) => { if (g == GestureType.Grab) grabStarted = true; };

            // 3 fingers curled - not enough
            detector.UpdateSimple(0.1f, 0.75f); // 75% = 3 fingers
            Assert.IsFalse(grabStarted, "3 fingers should not trigger grab");

            // 4 fingers curled - should grab
            detector.UpdateSimple(0.1f, 1.0f); // 100% = 4 fingers
            Assert.IsTrue(grabStarted, "4 fingers should trigger grab");
        }

        [Test]
        public void GestureDetector_QueryMethods_Work()
        {
            var detector = new GestureDetector(Hand.Right, 0.02f, 0.04f);

            // Start pinching
            detector.UpdateSimple(0.01f, 0f);

            Assert.IsTrue(detector.IsGestureActive(GestureType.Pinch));
            Assert.IsFalse(detector.IsGestureActive(GestureType.Grab));
            Assert.AreEqual(0f, detector.GetGestureHoldDuration(GestureType.Grab));
        }

        #endregion

        #region GestureConfig Tests

        [Test]
        public void GestureConfig_PrecisePreset_HasTightThresholds()
        {
            var config = GestureConfig.CreatePrecisePreset();

            Assert.Less(config.PinchStartThreshold, 0.02f, "Precise preset should have tight start threshold");
            Assert.Less(config.PinchEndThreshold, 0.03f, "Precise preset should have tight end threshold");

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GestureConfig_CasualPreset_HasForgivingThresholds()
        {
            var config = GestureConfig.CreateCasualPreset();

            Assert.Greater(config.PinchStartThreshold, 0.025f, "Casual preset should have forgiving start threshold");
            Assert.Greater(config.PinchEndThreshold, 0.05f, "Casual preset should have forgiving end threshold");

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GestureConfig_AppliesTo_Detector()
        {
            var config = GestureConfig.CreatePrecisePreset();
            var detector = new GestureDetector(Hand.Left);

            config.ApplyTo(detector);

            Assert.AreEqual(config.PinchStartThreshold, detector.PinchStartThreshold);
            Assert.AreEqual(config.PinchEndThreshold, detector.PinchEndThreshold);

            Object.DestroyImmediate(config);
        }

        #endregion

        #region Hand Enum Tests

        [Test]
        public void Hand_HasLeftAndRight()
        {
            Assert.AreEqual(0, (int)Hand.Left);
            Assert.AreEqual(1, (int)Hand.Right);
        }

        [Test]
        public void GestureType_HasAllExpectedGestures()
        {
            var expected = new[]
            {
                GestureType.None,
                GestureType.Pinch,
                GestureType.Grab,
                GestureType.Point,
                GestureType.OpenPalm,
                GestureType.ThumbsUp
            };

            foreach (var gesture in expected)
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(GestureType), gesture),
                    $"Gesture {gesture} should be defined");
            }
        }

        #endregion

        #region Velocity Tests

        [Test]
        public void VelocityCalculation_ZeroWhenStationary()
        {
            Vector3 pos1 = new Vector3(1, 2, 3);
            Vector3 pos2 = new Vector3(1, 2, 3);
            float dt = 0.016f;

            Vector3 velocity = (pos2 - pos1) / dt;

            Assert.AreEqual(Vector3.zero, velocity);
        }

        [Test]
        public void VelocityCalculation_CorrectMagnitude()
        {
            Vector3 pos1 = new Vector3(0, 0, 0);
            Vector3 pos2 = new Vector3(0.1f, 0, 0); // 10cm movement
            float dt = 0.1f; // 100ms

            Vector3 velocity = (pos2 - pos1) / dt;
            float speed = velocity.magnitude;

            Assert.AreEqual(1f, speed, 0.001f, "Speed should be 1 m/s (10cm in 100ms)");
        }

        [Test]
        public void VelocitySmoothing_ReducesJitter()
        {
            // Simulate velocity smoothing with exponential moving average
            float smoothingFactor = 0.2f;
            float[] rawVelocities = { 1.0f, 0.5f, 1.5f, 0.8f, 1.2f, 0.7f, 1.1f, 0.9f };

            float smoothed = rawVelocities[0];
            float maxChange = 0f;

            for (int i = 1; i < rawVelocities.Length; i++)
            {
                float prevSmoothed = smoothed;
                smoothed = Mathf.Lerp(smoothed, rawVelocities[i], smoothingFactor);
                maxChange = Mathf.Max(maxChange, Mathf.Abs(smoothed - prevSmoothed));
            }

            // Smoothed max change should be much less than raw
            Assert.Less(maxChange, 0.5f, "Smoothing should reduce jitter");
        }

        #endregion

        #region Provider Attribute Tests

        [Test]
        public void HandTrackingProviderAttribute_StoresIdAndPriority()
        {
            var attr = new HandTrackingProviderAttribute("test-provider", 50);

            Assert.AreEqual("test-provider", attr.Id);
            Assert.AreEqual(50, attr.Priority);
        }

        [Test]
        public void HandTrackingProviderAttribute_DefaultPriorityIsZero()
        {
            var attr = new HandTrackingProviderAttribute("test");

            Assert.AreEqual(0, attr.Priority);
        }

        #endregion
    }
}
#endif
