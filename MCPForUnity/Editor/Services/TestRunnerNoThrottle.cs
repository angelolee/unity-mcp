// TestRunnerNoThrottle.cs
// Sets Unity Editor to "No Throttling" mode during test runs.
// This helps tests that don't trigger compilation run smoothly in the background.
// Note: Tests that trigger mid-run compilation may still stall due to OS-level throttling.

using System;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
#if !UNITY_2021_1_OR_NEWER
using Unity.Collections;
#endif
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MCPForUnity.Editor.Services
{
    /// <summary>
    /// Automatically sets the editor to "No Throttling" mode during test runs.
    /// 
    /// This helps prevent background stalls for normal tests. However, tests that trigger
    /// script compilation mid-run may still stall because:
    /// - Internal Unity coroutine waits rely on editor ticks
    /// - OS-level throttling affects the main thread when Unity is backgrounded
    /// - No amount of internal nudging can overcome OS thread scheduling
    /// 
    /// The MCP workflow is unaffected because socket messages provide external stimulus
    /// that wakes Unity's main thread.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunnerNoThrottle
    {
        private const string ApplicationIdleTimeKey = "ApplicationIdleTime";
        private const string InteractionModeKey = "InteractionMode";

        // SessionState keys to persist across domain reload
        private const string SessionKey_TestRunActive = "TestRunnerNoThrottle_TestRunActive";
        private const string SessionKey_PrevIdleTime = "TestRunnerNoThrottle_PrevIdleTime";
        private const string SessionKey_PrevInteractionMode = "TestRunnerNoThrottle_PrevInteractionMode";
        private const string SessionKey_SettingsCaptured = "TestRunnerNoThrottle_SettingsCaptured";
#if !UNITY_2021_1_OR_NEWER
        private const string SessionKey_PrevNativeLeakDetectionMode = "TestRunnerNoThrottle_PrevNativeLeakDetectionMode";
        private const string SessionKey_NativeLeakDetectionCaptured = "TestRunnerNoThrottle_NativeLeakDetectionCaptured";
        private const string SessionKey_PostRunCleanupActive = "TestRunnerNoThrottle_PostRunCleanupActive";
        private const string SessionKey_PostRunCleanupRestoreSettings = "TestRunnerNoThrottle_PostRunCleanupRestoreSettings";
#endif

        private static readonly int[] PostRunConsoleClearFrames = { 1, 5, 15, 30, 60, 120 };
        private static int _postRunConsoleClearFrame;
        private static int _postRunConsoleClearIndex;
#if !UNITY_2021_1_OR_NEWER
        private static bool _restoreTestSettingsAfterPostRunCleanup;
#endif

        // Keep reference to avoid GC and set HideFlags to avoid serialization issues
        private static TestRunnerApi _api;

        static TestRunnerNoThrottle()
        {
            try
            {
                _api = ScriptableObject.CreateInstance<TestRunnerApi>();
                _api.hideFlags = HideFlags.HideAndDontSave;
                _api.RegisterCallbacks(new TestCallbacks());

                // Check if recovering from domain reload during an active test run
                if (IsTestRunActive())
                {
                    McpLog.Info("[TestRunnerNoThrottle] Recovered from domain reload - reapplying No Throttling.");
                    ApplyNoThrottling();
                }
#if !UNITY_2021_1_OR_NEWER
                else if (IsPostRunCleanupActive())
                {
                    ResumePostRunConsoleCleanup();
                }
#endif
            }
            catch (Exception e)
            {
                McpLog.Warn($"[TestRunnerNoThrottle] Failed to register callbacks: {e}");
            }
        }

        #region State Persistence

        private static bool IsTestRunActive() => SessionState.GetBool(SessionKey_TestRunActive, false);
        private static void SetTestRunActive(bool active) => SessionState.SetBool(SessionKey_TestRunActive, active);
        private static bool AreSettingsCaptured() => SessionState.GetBool(SessionKey_SettingsCaptured, false);
        private static void SetSettingsCaptured(bool captured) => SessionState.SetBool(SessionKey_SettingsCaptured, captured);
        private static int GetPrevIdleTime() => SessionState.GetInt(SessionKey_PrevIdleTime, 4);
        private static void SetPrevIdleTime(int value) => SessionState.SetInt(SessionKey_PrevIdleTime, value);
        private static int GetPrevInteractionMode() => SessionState.GetInt(SessionKey_PrevInteractionMode, 0);
        private static void SetPrevInteractionMode(int value) => SessionState.SetInt(SessionKey_PrevInteractionMode, value);
#if !UNITY_2021_1_OR_NEWER
        private static bool IsNativeLeakDetectionCaptured() => SessionState.GetBool(SessionKey_NativeLeakDetectionCaptured, false);
        private static void SetNativeLeakDetectionCaptured(bool captured) => SessionState.SetBool(SessionKey_NativeLeakDetectionCaptured, captured);
        private static int GetPrevNativeLeakDetectionMode() => SessionState.GetInt(SessionKey_PrevNativeLeakDetectionMode, (int)NativeLeakDetectionMode.Enabled);
        private static void SetPrevNativeLeakDetectionMode(int value) => SessionState.SetInt(SessionKey_PrevNativeLeakDetectionMode, value);
        private static bool IsPostRunCleanupActive() => SessionState.GetBool(SessionKey_PostRunCleanupActive, false);
        private static void SetPostRunCleanupActive(bool active) => SessionState.SetBool(SessionKey_PostRunCleanupActive, active);
        private static bool ShouldPostRunCleanupRestoreSettings() => SessionState.GetBool(SessionKey_PostRunCleanupRestoreSettings, false);
        private static void SetPostRunCleanupRestoreSettings(bool restore) => SessionState.SetBool(SessionKey_PostRunCleanupRestoreSettings, restore);
#endif

        #endregion

        /// <summary>
        /// Apply no-throttling preemptively before tests start.
        /// Call this before Execute() for PlayMode tests to ensure Unity isn't throttled
        /// during the Play mode transition (before RunStarted fires).
        /// </summary>
        public static void ApplyNoThrottlingPreemptive()
        {
            SetTestRunActive(true);
            ApplyNoThrottling();
        }

        private static void ApplyNoThrottling()
        {
            if (!AreSettingsCaptured())
            {
                SetPrevIdleTime(EditorPrefs.GetInt(ApplicationIdleTimeKey, 4));
                SetPrevInteractionMode(EditorPrefs.GetInt(InteractionModeKey, 0));
                SetSettingsCaptured(true);
            }

            // 0ms idle + InteractionMode=1 (No Throttling)
            EditorPrefs.SetInt(ApplicationIdleTimeKey, 0);
            EditorPrefs.SetInt(InteractionModeKey, 1);

            ForceEditorToApplyInteractionPrefs();
#if !UNITY_2021_1_OR_NEWER
            CaptureAndDisableNativeLeakDetection();
#endif
            McpLog.Info("[TestRunnerNoThrottle] Applied No Throttling for test run.");
        }

        private static void RestoreInteractionSettings()
        {
            if (AreSettingsCaptured())
            {
                EditorPrefs.SetInt(ApplicationIdleTimeKey, GetPrevIdleTime());
                EditorPrefs.SetInt(InteractionModeKey, GetPrevInteractionMode());
                ForceEditorToApplyInteractionPrefs();
                SetSettingsCaptured(false);
            }

            SetTestRunActive(false);
        }

        private static void RestoreThrottling(bool restoreNativeLeakDetection)
        {
            RestoreInteractionSettings();
#if !UNITY_2021_1_OR_NEWER
            if (restoreNativeLeakDetection)
                RestoreNativeLeakDetection();
            else
                ClearNativeLeakDetectionCapture();
#endif
            McpLog.Info("[TestRunnerNoThrottle] Restored Interaction Mode after test run.");
        }

#if !UNITY_2021_1_OR_NEWER
        private static bool ShouldSuspendNativeLeakDetectionDuringRun(bool isUnity2020)
        {
            return isUnity2020;
        }

        private static bool ShouldRestoreNativeLeakDetectionImmediatelyAfterRun(bool isUnity2020, int failCount)
        {
            return !isUnity2020 || failCount > 0;
        }

        private static bool ShouldRestoreTestSettingsAfterPostRunCleanup(bool isUnity2020, int failCount)
        {
            return isUnity2020 && failCount == 0;
        }

        private static void CaptureAndDisableNativeLeakDetection()
        {
            if (!ShouldSuspendNativeLeakDetectionDuringRun(true))
                return;

            try
            {
                if (!IsNativeLeakDetectionCaptured())
                {
                    SetPrevNativeLeakDetectionMode((int)NativeLeakDetection.Mode);
                    SetNativeLeakDetectionCaptured(true);
                }

                NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
            }
            catch
            {
                // Native leak detection is a Unity-editor diagnostic; test execution should not depend on it.
            }
        }

        private static void RestoreNativeLeakDetection()
        {
            if (!IsNativeLeakDetectionCaptured())
                return;

            try
            {
                NativeLeakDetection.Mode = (NativeLeakDetectionMode)GetPrevNativeLeakDetectionMode();
            }
            catch
            {
                // Keep restoration best-effort for the same reason capture is best-effort.
            }
            finally
            {
                SetNativeLeakDetectionCaptured(false);
            }
        }

        private static void ClearNativeLeakDetectionCapture()
        {
            SetNativeLeakDetectionCaptured(false);
        }
#endif

        private static bool ShouldClearConsoleAfterRun(bool isUnity2020, int failCount)
        {
            return isUnity2020 && failCount == 0;
        }

        private static void SchedulePostRunConsoleCleanup(bool restoreTestSettingsAfterCleanup)
        {
            _postRunConsoleClearFrame = 0;
            _postRunConsoleClearIndex = 0;
#if !UNITY_2021_1_OR_NEWER
            _restoreTestSettingsAfterPostRunCleanup = restoreTestSettingsAfterCleanup;
            SetPostRunCleanupActive(true);
            SetPostRunCleanupRestoreSettings(restoreTestSettingsAfterCleanup);
#endif
            EditorApplication.update -= ClearConsoleDuringPostRunDrain;
            EditorApplication.update += ClearConsoleDuringPostRunDrain;
        }

#if !UNITY_2021_1_OR_NEWER
        private static void ResumePostRunConsoleCleanup()
        {
            _postRunConsoleClearFrame = 0;
            _postRunConsoleClearIndex = 0;
            _restoreTestSettingsAfterPostRunCleanup = ShouldPostRunCleanupRestoreSettings();
            EditorApplication.update -= ClearConsoleDuringPostRunDrain;
            EditorApplication.update += ClearConsoleDuringPostRunDrain;
        }
#endif

        private static void ClearConsoleDuringPostRunDrain()
        {
            if (_postRunConsoleClearIndex >= PostRunConsoleClearFrames.Length)
            {
#if !UNITY_2021_1_OR_NEWER
                if (_restoreTestSettingsAfterPostRunCleanup)
                {
                    RestoreInteractionSettings();
                    ClearNativeLeakDetectionCapture();
                    ClearConsole();
                    _restoreTestSettingsAfterPostRunCleanup = false;
                }
                SetPostRunCleanupActive(false);
                SetPostRunCleanupRestoreSettings(false);
#endif
                EditorApplication.update -= ClearConsoleDuringPostRunDrain;
                return;
            }

            _postRunConsoleClearFrame++;
            if (_postRunConsoleClearFrame < PostRunConsoleClearFrames[_postRunConsoleClearIndex])
                return;

            ClearConsole();
            _postRunConsoleClearIndex++;
        }

        private static void ClearConsole()
        {
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                var clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                clearMethod?.Invoke(null, null);
            }
            catch
            {
                // Best-effort cleanup only; never let console cleanup affect test result handling.
            }
        }

        private static void ForceEditorToApplyInteractionPrefs()
        {
            try
            {
                var method = typeof(EditorApplication).GetMethod(
                    "UpdateInteractionModeSettings",
                    BindingFlags.Static | BindingFlags.NonPublic
                );
                method?.Invoke(null, null);
            }
            catch
            {
                // Ignore reflection errors
            }
        }

        private sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                SetTestRunActive(true);
                ApplyNoThrottling();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var failCount = result?.FailCount ?? 0;
#if !UNITY_2021_1_OR_NEWER
                if (ShouldClearConsoleAfterRun(true, failCount))
                {
                    SetTestRunActive(false);
                    // Unity 2020.3 can continue emitting false native TLS allocator warnings after clean test runs if leak detection is restored.
                    SchedulePostRunConsoleCleanup(ShouldRestoreTestSettingsAfterPostRunCleanup(true, failCount));
                    return;
                }

                RestoreThrottling(ShouldRestoreNativeLeakDetectionImmediatelyAfterRun(true, failCount));
#else
                RestoreThrottling(true);
#endif
            }

            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
