using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace UnitySkills
{
    /// <summary>
    /// Embedded HTTP server for UnitySkills REST API.
    /// </summary>
    [InitializeOnLoad]
    public static class SkillsHttpServer
    {
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static bool _isRunning;
        private static string _prefix = "http://localhost:8090/";

        public static bool IsRunning => _isRunning;
        public static string Url => _prefix;

        static SkillsHttpServer()
        {
            EditorApplication.quitting += Stop;
        }

        [MenuItem("Window/UnitySkills/Start REST Server")]
        public static void Start()
        {
            if (_isRunning)
            {
                Debug.Log("[UnitySkills] Server already running at " + _prefix);
                return;
            }

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_prefix);
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenLoop) { IsBackground = true };
                _listenerThread.Start();

                Debug.Log($"[UnitySkills] REST Server started at {_prefix}");
                Debug.Log($"[UnitySkills] {SkillRouter.GetManifest().Split('\n').Length} skills available");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnitySkills] Failed to start: {ex.Message}");
                _isRunning = false;
            }
        }

        [MenuItem("Window/UnitySkills/Stop REST Server")]
        public static void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            Debug.Log("[UnitySkills] Server stopped");
        }

        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException) { }
                catch (Exception ex)
                {
                    if (_isRunning) Debug.LogError($"[UnitySkills] Error: {ex.Message}");
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string result;

            try
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath.ToLower();

                if (path == "/skills" && request.HttpMethod == "GET")
                {
                    result = SkillRouter.GetManifest();
                }
                else if (path.StartsWith("/skill/") && request.HttpMethod == "POST")
                {
                    var skillName = request.Url.AbsolutePath.Substring(7); // Keep original case
                    var body = new System.IO.StreamReader(request.InputStream).ReadToEnd();
                    result = ExecuteOnMainThread(skillName, body);
                }
                else if (path == "/" || path == "/health")
                {
                    SkillRouter.Initialize();
                    result = JsonConvert.SerializeObject(new { status = "ok", service = "UnitySkills", version = "1.1.0" });
                }
                else
                {
                    response.StatusCode = 404;
                    result = JsonConvert.SerializeObject(new { error = "Not found", endpoints = new[] { "GET /skills", "POST /skill/{name}" } });
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                result = JsonConvert.SerializeObject(new { error = ex.Message });
            }

            response.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(result);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private static string ExecuteOnMainThread(string skillName, string args)
        {
            string result = null;
            var wait = new ManualResetEvent(false);

            EditorApplication.delayCall += () =>
            {
                try { result = SkillRouter.Execute(skillName, args); }
                catch (Exception ex) { result = JsonConvert.SerializeObject(new { error = ex.Message }); }
                finally { wait.Set(); }
            };

            wait.WaitOne(30000);
            return result ?? JsonConvert.SerializeObject(new { error = "Timeout" });
        }
    }
}
