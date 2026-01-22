// HandTrackingPlayModeTests - PlayMode tests for hand tracking (spec-012)
// Self-contained tests that don't require assembly references to main project

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MetavidoVFX.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for hand tracking functionality.
    /// These tests are self-contained and don't depend on main project asmdefs.
    ///
    /// For full integration testing with AR Remote:
    /// 1. Use H3M > Testing > AR Remote > Full AR Test Sequence
    /// 2. Manual verification in Play Mode with device connected
    /// </summary>
    [TestFixture]
    public class HandTrackingPlayModeTests
    {
        #region Framework Validation Tests

        [UnityTest]
        public IEnumerator PlayMode_FrameAdvances()
        {
            int startFrame = Time.frameCount;
            yield return null;
            yield return null;

            Assert.Greater(Time.frameCount, startFrame, "Frame should advance in PlayMode");
        }

        [UnityTest]
        public IEnumerator PlayMode_TimeProgresses()
        {
            float startTime = Time.time;
            yield return new WaitForSeconds(0.1f);

            Assert.Greater(Time.time, startTime, "Time should progress in PlayMode");
        }

        #endregion

        #region Gesture Detection Algorithm Tests

        [UnityTest]
        public IEnumerator GestureHysteresis_PreventsFlickering()
        {
            // Test hysteresis pattern: different thresholds for start vs end
            float pinchStartThreshold = 0.02f;  // Start pinch when distance < 2cm
            float pinchEndThreshold = 0.04f;    // End pinch when distance > 4cm

            bool isPinching = false;

            // Simulate finger distance oscillating between thresholds
            float[] distances = { 0.05f, 0.03f, 0.015f, 0.025f, 0.035f, 0.025f, 0.05f };

            foreach (float distance in distances)
            {
                // Apply hysteresis
                if (!isPinching && distance < pinchStartThreshold)
                    isPinching = true;
                else if (isPinching && distance > pinchEndThreshold)
                    isPinching = false;

                yield return null;
            }

            // After sequence, should NOT be pinching (ended at 0.05f > endThreshold)
            Assert.IsFalse(isPinching, "Hysteresis should prevent pinch at 0.05f");
        }

        [UnityTest]
        public IEnumerator GestureVelocity_CalculatesCorrectly()
        {
            Vector3 prevPos = Vector3.zero;
            Vector3 currPos = new Vector3(0.1f, 0, 0);
            float deltaTime = 0.016f; // ~60fps

            Vector3 velocity = (currPos - prevPos) / deltaTime;
            float speed = velocity.magnitude;

            yield return null;

            Assert.AreEqual(6.25f, speed, 0.01f, "Velocity magnitude should be ~6.25 m/s");
        }

        #endregion

        #region Provider Priority Tests

        [UnityTest]
        public IEnumerator ProviderPriority_HigherWins()
        {
            // Test priority-based selection (higher priority = preferred)
            int[] priorities = { 10, 80, 60, 100, 40 };
            bool[] available = { true, true, false, true, true };

            int bestPriority = -1;
            int bestIndex = -1;

            for (int i = 0; i < priorities.Length; i++)
            {
                if (available[i] && priorities[i] > bestPriority)
                {
                    bestPriority = priorities[i];
                    bestIndex = i;
                }
            }

            yield return null;

            Assert.AreEqual(3, bestIndex, "Should select provider at index 3 (priority 100)");
            Assert.AreEqual(100, bestPriority, "Best priority should be 100");
        }

        #endregion

        #region Transform Tests (Common Hand Tracking Patterns)

        [UnityTest]
        public IEnumerator JointTransform_ParentChild_Preserved()
        {
            // Create test hierarchy (simulating hand joints)
            var wrist = new GameObject("Wrist");
            var palm = new GameObject("Palm");
            var indexTip = new GameObject("IndexTip");

            palm.transform.SetParent(wrist.transform);
            indexTip.transform.SetParent(palm.transform);

            // Set local positions
            wrist.transform.position = new Vector3(0.5f, 1f, 0.3f);
            palm.transform.localPosition = new Vector3(0, 0.05f, 0);
            indexTip.transform.localPosition = new Vector3(0, 0.08f, 0);

            yield return null;

            // Verify world positions maintain hierarchy
            Vector3 expectedIndexWorld = wrist.transform.position +
                                         new Vector3(0, 0.05f, 0) +
                                         new Vector3(0, 0.08f, 0);

            Assert.AreEqual(expectedIndexWorld.y, indexTip.transform.position.y, 0.001f,
                "Index tip Y should be wrist + palm offset + index offset");

            // Cleanup
            Object.Destroy(wrist);
        }

        [UnityTest]
        public IEnumerator PinchDistance_CalculatesCorrectly()
        {
            Vector3 thumbTip = new Vector3(0.52f, 1.05f, 0.31f);
            Vector3 indexTip = new Vector3(0.53f, 1.06f, 0.32f);

            float pinchDistance = Vector3.Distance(thumbTip, indexTip);

            yield return null;

            // Distance should be small (~1.7cm)
            Assert.Less(pinchDistance, 0.02f, "Pinch distance should be < 2cm for a pinch");
        }

        #endregion

        #region Color Picker Algorithm Tests

        [UnityTest]
        public IEnumerator ColorPicker_HSBtoRGB_Converts()
        {
            // Test HSB to RGB conversion (used in ColorPicker)
            float hue = 0f;        // Red
            float saturation = 1f;
            float brightness = 1f;

            Color color = Color.HSVToRGB(hue, saturation, brightness);

            yield return null;

            Assert.AreEqual(1f, color.r, 0.01f, "Red channel should be 1");
            Assert.AreEqual(0f, color.g, 0.01f, "Green channel should be 0");
            Assert.AreEqual(0f, color.b, 0.01f, "Blue channel should be 0");
        }

        [UnityTest]
        public IEnumerator ColorPicker_PalmProjection_Calculates()
        {
            // Test projecting finger position onto palm plane
            Vector3 palmCenter = new Vector3(0.5f, 1f, 0.3f);
            Vector3 palmNormal = Vector3.up;
            Vector3 fingerPos = new Vector3(0.55f, 1.02f, 0.32f);

            // Project onto palm plane
            Vector3 toFinger = fingerPos - palmCenter;
            float distToPlane = Vector3.Dot(toFinger, palmNormal);
            Vector3 projected = fingerPos - distToPlane * palmNormal;

            yield return null;

            // Projected point should be on palm plane (same Y as palm)
            Assert.AreEqual(palmCenter.y, projected.y, 0.001f,
                "Projected point should be on palm plane");
        }

        #endregion

        #region Brush Selector Algorithm Tests

        [UnityTest]
        public IEnumerator BrushSelector_CircularLayout_Correct()
        {
            // Test circular brush layout (8 brushes around palm)
            int brushCount = 8;
            float radius = 0.06f;
            Vector3 center = Vector3.zero;

            Vector3[] positions = new Vector3[brushCount];

            for (int i = 0; i < brushCount; i++)
            {
                float angle = i * (360f / brushCount) * Mathf.Deg2Rad;
                positions[i] = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
            }

            yield return null;

            // First position should be at (radius, 0, 0)
            Assert.AreEqual(radius, positions[0].x, 0.001f, "First brush at X=radius");
            Assert.AreEqual(0f, positions[0].z, 0.001f, "First brush at Z=0");

            // Fourth position (180°) should be at (-radius, 0, 0)
            Assert.AreEqual(-radius, positions[4].x, 0.001f, "Fifth brush at X=-radius");
        }

        [UnityTest]
        public IEnumerator BrushSelector_NearestBrush_Found()
        {
            // Test finding nearest brush to finger position
            Vector3[] brushPositions = {
                new Vector3(0.06f, 0, 0),
                new Vector3(0, 0, 0.06f),
                new Vector3(-0.06f, 0, 0),
                new Vector3(0, 0, -0.06f)
            };

            Vector3 fingerPos = new Vector3(0.05f, 0, 0.01f);

            int nearest = -1;
            float minDist = float.MaxValue;

            for (int i = 0; i < brushPositions.Length; i++)
            {
                float dist = Vector3.Distance(fingerPos, brushPositions[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }

            yield return null;

            Assert.AreEqual(0, nearest, "Should select brush 0 (closest to finger)");
        }

        #endregion
    }
}
