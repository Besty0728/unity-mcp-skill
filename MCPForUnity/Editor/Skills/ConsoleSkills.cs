using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnitySkills
{
    /// <summary>
    /// Console and debug skills.
    /// </summary>
    public static class ConsoleSkills
    {
        private static readonly List<LogEntry> _logs = new List<LogEntry>();
        private static bool _capturing;

        private class LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
            public System.DateTime time;
        }

        [UnitySkill("console_start_capture", "Start capturing console logs")]
        public static object ConsoleStartCapture()
        {
            if (!_capturing)
            {
                Application.logMessageReceived += OnLogMessage;
                _capturing = true;
            }
            _logs.Clear();
            return new { success = true, message = "Console capture started" };
        }

        [UnitySkill("console_stop_capture", "Stop capturing console logs")]
        public static object ConsoleStopCapture()
        {
            if (_capturing)
            {
                Application.logMessageReceived -= OnLogMessage;
                _capturing = false;
            }
            return new { success = true, message = "Console capture stopped", capturedCount = _logs.Count };
        }

        [UnitySkill("console_get_logs", "Get captured console logs")]
        public static object ConsoleGetLogs(string filter = null, int limit = 100)
        {
            IEnumerable<LogEntry> results = _logs;

            if (!string.IsNullOrEmpty(filter))
                results = results.Where(l => l.message.Contains(filter));

            var logs = results.TakeLast(limit).Select(l => new
            {
                message = l.message,
                type = l.type.ToString(),
                time = l.time.ToString("HH:mm:ss.fff")
            }).ToArray();

            return new { count = logs.Length, logs };
        }

        [UnitySkill("console_clear", "Clear the Unity console")]
        public static object ConsoleClear()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
            var logEntries = assembly.GetType("UnityEditor.LogEntries");
            var clearMethod = logEntries.GetMethod("Clear");
            clearMethod.Invoke(null, null);

            _logs.Clear();
            return new { success = true, message = "Console cleared" };
        }

        [UnitySkill("console_log", "Write a message to the console")]
        public static object ConsoleLog(string message, string type = "log")
        {
            switch (type.ToLower())
            {
                case "warning":
                    Debug.LogWarning(message);
                    break;
                case "error":
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
            return new { success = true, logged = message };
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            _logs.Add(new LogEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
                time = System.DateTime.Now
            });

            // Keep only last 1000 entries
            if (_logs.Count > 1000)
                _logs.RemoveAt(0);
        }
    }
}
