using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections.Generic;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Test runner skills.
    /// </summary>
    public static class TestSkills
    {
        private static readonly Dictionary<string, TestRunInfo> _runningTests = new Dictionary<string, TestRunInfo>();
        private static TestRunnerApi _api;

        private class TestRunInfo
        {
            public string JobId;
            public string Status = "running";
            public int TotalTests;
            public int PassedTests;
            public int FailedTests;
            public List<string> FailedTestNames = new List<string>();
            public System.DateTime StartTime;
        }

        [UnitySkill("test_run", "Run Unity tests (returns job ID for polling)")]
        public static object TestRun(string testMode = "EditMode", string filter = null)
        {
            if (_api == null)
                _api = ScriptableObject.CreateInstance<TestRunnerApi>();

            var mode = testMode.ToLower() == "playmode" ? TestMode.PlayMode : TestMode.EditMode;
            var jobId = System.Guid.NewGuid().ToString("N").Substring(0, 8);

            var runInfo = new TestRunInfo
            {
                JobId = jobId,
                StartTime = System.DateTime.Now
            };
            _runningTests[jobId] = runInfo;

            var callbacks = new TestCallbacks(runInfo);
            _api.RegisterCallbacks(callbacks);

            var filterObj = new Filter { testMode = mode };
            if (!string.IsNullOrEmpty(filter))
                filterObj.testNames = new[] { filter };

            _api.Execute(new ExecutionSettings(filterObj));

            return new
            {
                success = true,
                jobId,
                testMode,
                message = "Tests started. Use test_get_result to poll for results."
            };
        }

        [UnitySkill("test_get_result", "Get the result of a test run")]
        public static object TestGetResult(string jobId)
        {
            if (!_runningTests.TryGetValue(jobId, out var runInfo))
                return new { error = $"Test job not found: {jobId}" };

            return new
            {
                jobId,
                status = runInfo.Status,
                totalTests = runInfo.TotalTests,
                passedTests = runInfo.PassedTests,
                failedTests = runInfo.FailedTests,
                failedTestNames = runInfo.FailedTestNames.ToArray(),
                elapsedSeconds = (System.DateTime.Now - runInfo.StartTime).TotalSeconds
            };
        }

        [UnitySkill("test_list", "List available tests")]
        public static object TestList(string testMode = "EditMode", int limit = 100)
        {
            if (_api == null)
                _api = ScriptableObject.CreateInstance<TestRunnerApi>();

            var mode = testMode.ToLower() == "playmode" ? TestMode.PlayMode : TestMode.EditMode;
            var tests = new List<object>();

            _api.RetrieveTestList(mode, (testRoot) =>
            {
                CollectTests(testRoot, tests, limit);
            });

            return new { testMode, count = tests.Count, tests };
        }

        [UnitySkill("test_cancel", "Cancel a running test")]
        public static object TestCancel(string jobId = null)
        {
            if (_api == null)
                return new { error = "No test runner available" };

            // Note: Unity's TestRunnerApi doesn't have a direct cancel method
            // This is a placeholder that clears the job status
            if (!string.IsNullOrEmpty(jobId) && _runningTests.ContainsKey(jobId))
            {
                _runningTests[jobId].Status = "cancelled";
                return new { success = true, cancelled = jobId };
            }

            return new { error = "Cannot cancel tests directly. Wait for completion." };
        }

        private static void CollectTests(ITestAdaptor test, List<object> tests, int limit)
        {
            if (tests.Count >= limit) return;

            if (!test.HasChildren)
            {
                tests.Add(new
                {
                    name = test.Name,
                    fullName = test.FullName,
                    runState = test.RunState.ToString()
                });
            }
            else
            {
                foreach (var child in test.Children)
                {
                    CollectTests(child, tests, limit);
                }
            }
        }

        private class TestCallbacks : ICallbacks
        {
            private readonly TestRunInfo _runInfo;

            public TestCallbacks(TestRunInfo runInfo)
            {
                _runInfo = runInfo;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _runInfo.TotalTests = CountTests(testsToRun);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                _runInfo.Status = "completed";
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.Test.HasChildren)
                {
                    if (result.TestStatus == TestStatus.Passed)
                        _runInfo.PassedTests++;
                    else if (result.TestStatus == TestStatus.Failed)
                    {
                        _runInfo.FailedTests++;
                        _runInfo.FailedTestNames.Add(result.Test.FullName);
                    }
                }
            }

            private int CountTests(ITestAdaptor test)
            {
                if (!test.HasChildren) return 1;
                return test.Children.Sum(c => CountTests(c));
            }
        }
    }
}
