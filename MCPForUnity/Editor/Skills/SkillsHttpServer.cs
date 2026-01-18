using System;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace MCPForUnity.Editor.Skills
{
    /// <summary>
    /// Embedded HTTP server for UnitySkills.
    /// Provides REST API for AI to directly invoke skills without MCP overhead.
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

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true
                };
                _listenerThread.Start();

                Debug.Log($"[UnitySkills] REST Server started at {_prefix}");
                Debug.Log("[UnitySkills] Endpoints: GET /skills, POST /skill/{name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnitySkills] Failed to start server: {ex.Message}");
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
            _listenerThread?.Join(1000);

            Debug.Log("[UnitySkills] REST Server stopped");
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
                catch (HttpListenerException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Debug.LogError($"[UnitySkills] Listener error: {ex.Message}");
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string responseString;

            try
            {
                // CORS headers for browser-based tools
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
                    // Return skill manifest
                    responseString = GetSkillManifest();
                }
                else if (path.StartsWith("/skill/") && request.HttpMethod == "POST")
                {
                    // Execute skill
                    var skillName = path.Substring(7); // Remove "/skill/"
                    var body = ReadRequestBody(request);
                    responseString = ExecuteSkillOnMainThread(skillName, body);
                }
                else if (path == "/" || path == "/health")
                {
                    responseString = JsonConvert.SerializeObject(new
                    {
                        status = "ok",
                        service = "UnitySkills",
                        version = "1.0.0"
                    });
                }
                else
                {
                    response.StatusCode = 404;
                    responseString = JsonConvert.SerializeObject(new
                    {
                        error = "Not found",
                        availableEndpoints = new[] { "GET /skills", "POST /skill/{name}" }
                    });
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                responseString = JsonConvert.SerializeObject(new
                {
                    error = ex.Message,
                    type = ex.GetType().Name
                });
            }

            response.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        private static string GetSkillManifest()
        {
            return SkillRouter.GetSkillManifestJson();
        }

        private static string ExecuteSkillOnMainThread(string skillName, string jsonArgs)
        {
            string result = null;
            var waitHandle = new ManualResetEvent(false);

            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = SkillRouter.ExecuteSkill(skillName, jsonArgs);
                }
                catch (Exception ex)
                {
                    result = JsonConvert.SerializeObject(new
                    {
                        status = "error",
                        error = ex.Message
                    });
                }
                finally
                {
                    waitHandle.Set();
                }
            };

            // Wait for main thread execution (max 30 seconds)
            waitHandle.WaitOne(30000);
            return result ?? JsonConvert.SerializeObject(new { status = "error", error = "Timeout" });
        }
    }
}
