#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace XRRAI.Editor
{
    /// <summary>
    /// Automated AR Remote PlayMode test runner.
    /// Launches companion app on device and runs hand tracking tests.
    /// </summary>
    public static class ARRemotePlayModeTestRunner
    {
        private const string AR_COMPANION_BUNDLE_ID = "com.imclab.metavidovfxARCompanion";
        private const string DEVICE_ID = "93485B6C-D0DD-5535-BD87-A80D0FC9FB54"; // IMClab 15

        [MenuItem("H3M/Testing/AR Remote/Launch Companion App on Device")]
        public static void LaunchCompanionApp()
        {
            Debug.Log("[AR Remote] Launching AR Companion app on device...");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xcrun",
                    Arguments = $"devicectl device process launch --device {DEVICE_ID} {AR_COMPANION_BUNDLE_ID}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Debug.Log($"[AR Remote] Companion app launched successfully!\n{output}");
                    Debug.Log("[AR Remote] Open Window > AR Foundation Remote > Connection to connect");
                }
                else
                {
                    Debug.LogWarning($"[AR Remote] Launch may have failed:\n{error}\n{output}");
                    // App might already be running
                    Debug.Log("[AR Remote] If app is already running, you can proceed with testing");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AR Remote] Failed to launch companion app: {e.Message}");
            }
        }

        [MenuItem("H3M/Testing/AR Remote/Run Hand Tracking PlayMode Tests")]
        public static void RunHandTrackingPlayModeTests()
        {
            Debug.Log("[AR Remote] Starting hand tracking PlayMode tests...");

            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = new[] { "MetavidoVFX.Tests.HandTrackingPlayModeTests" }
            };

            testRunnerApi.Execute(new ExecutionSettings(filter));
            Debug.Log("[AR Remote] PlayMode tests started. Check Test Runner window for results.");
        }

        [MenuItem("H3M/Testing/AR Remote/Full AR Test Sequence")]
        public static async void RunFullARTestSequence()
        {
            Debug.Log("[AR Remote] Starting full AR test sequence...");

            // 1. Setup optimal config
            ARRemoteTestingSetup.SetupOptimalConfig();

            // 2. Launch companion app
            LaunchCompanionApp();

            // 3. Wait for app to start
            Debug.Log("[AR Remote] Waiting 5 seconds for companion app to start...");
            await Task.Delay(5000);

            // 4. Verify scene setup
            ARRemoteTestingSetup.VerifySceneSetup();

            // 5. Instructions for manual connection
            Debug.Log("=== NEXT STEPS ===");
            Debug.Log("1. Open Window > AR Foundation Remote > Connection");
            Debug.Log("2. Enter device IP shown in companion app");
            Debug.Log("3. Click Connect");
            Debug.Log("4. Press Play to test, or run H3M > Testing > AR Remote > Run Hand Tracking PlayMode Tests");
        }

        [MenuItem("H3M/Testing/AR Remote/Enter Play Mode with AR")]
        public static void EnterPlayModeWithAR()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[AR Remote] Already in Play Mode");
                return;
            }

            // Setup optimal config first
            ARRemoteTestingSetup.SetupOptimalConfig();

            // Enter play mode
            EditorApplication.isPlaying = true;
            Debug.Log("[AR Remote] Entering Play Mode. Ensure AR Remote is connected.");
        }
    }

    /// <summary>
    /// Test result callback for PlayMode tests.
    /// </summary>
    public class ARTestResultCallback : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"[AR Tests] Starting {testsToRun.TestCaseCount} tests...");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"[AR Tests] Finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped");

            if (result.FailCount > 0)
            {
                Debug.LogWarning("[AR Tests] Some tests failed. Check Test Runner for details.");
            }
            else
            {
                Debug.Log("[AR Tests] All tests passed!");
            }
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.TestStatus == TestStatus.Failed)
            {
                Debug.LogError($"[AR Tests] FAILED: {result.Test.Name}\n{result.Message}");
            }
        }
    }
}
#endif
