using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace UnitySkills
{
    /// <summary>
    /// Robust HTTP server for UnitySkills REST API.
    /// 
    /// Architecture: Strict Producer-Consumer Pattern
    /// - HTTP Thread (Producer): ONLY receives requests and enqueues them. NO Unity API calls.
    /// - Main Thread (Consumer): Processes ALL logic including routing, rate limiting, and skill execution.
    /// 
    /// This ensures 100% thread safety with Unity's single-threaded architecture.
    /// </summary>
    [InitializeOnLoad]
    public static class SkillsHttpServer
    {
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static Thread _keepAliveThread;
        private static volatile bool _isRunning;
        private static readonly string _prefix = "http://localhost:8090/";
        
        // Job queue - HTTP thread enqueues, Main thread dequeues and processes
        private static readonly Queue<RequestJob> _jobQueue = new Queue<RequestJob>();
        private static readonly object _queueLock = new object();
        private static bool _updateHooked = false;
        
        // Rate limiting (processed on main thread only)
        private static int _requestsThisSecond = 0;
        private static double _lastRateLimitReset = 0;
        private const int MaxRequestsPerSecond = 100;
        
        // Keep-alive interval (ms)
        private const int KeepAliveIntervalMs = 50;
        
        // Statistics
        private static long _totalRequestsProcessed = 0;
        private static long _totalRequestsReceived = 0;

        public static bool IsRunning => _isRunning;
        public static string Url => _prefix;
        public static int QueuedRequests { get { lock (_queueLock) { return _jobQueue.Count; } } }
        public static long TotalProcessed => _totalRequestsProcessed;

        /// <summary>
        /// Represents a pending HTTP request job.
        /// Created by HTTP thread, processed by Main thread.
        /// </summary>
        private class RequestJob
        {
            // Raw HTTP data (set by HTTP thread)
            public HttpListenerContext Context;
            public string HttpMethod;
            public string Path;
            public string Body;
            public long EnqueueTimeTicks;
            
            // Result (set by Main thread)
            public string ResponseJson;
            public int StatusCode;
            public bool IsProcessed;
            public ManualResetEventSlim CompletionSignal;
        }

        static SkillsHttpServer()
        {
            EditorApplication.quitting += Stop;
            HookUpdateLoop();
        }
        
        private static void HookUpdateLoop()
        {
            if (_updateHooked) return;
            EditorApplication.update += ProcessJobQueue;
            _updateHooked = true;
        }
        
        private static void UnhookUpdateLoop()
        {
            if (!_updateHooked) return;
            EditorApplication.update -= ProcessJobQueue;
            _updateHooked = false;
        }

        public static void Start()
        {
            if (_isRunning)
            {
                Debug.Log("[UnitySkills] Server already running at " + _prefix);
                return;
            }

            try
            {
                HookUpdateLoop();
                
                _listener = new HttpListener();
                _listener.Prefixes.Add(_prefix);
                _listener.Start();
                _isRunning = true;

                // Start listener thread (Producer - ONLY enqueues, no Unity API)
                _listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "UnitySkills-Listener" };
                _listenerThread.Start();
                
                // Start keep-alive thread (forces Unity to update when not focused)
                _keepAliveThread = new Thread(KeepAliveLoop) { IsBackground = true, Name = "UnitySkills-KeepAlive" };
                _keepAliveThread.Start();

                // These calls are safe here because Start() is called from Main thread
                var skillCount = SkillRouter.GetManifest().Split('\n').Length;
                Debug.Log($"[UnitySkills] REST Server started at {_prefix}");
                Debug.Log($"[UnitySkills] {skillCount} skills available");
                Debug.Log($"[UnitySkills] Architecture: Strict Producer-Consumer (Thread-Safe)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnitySkills] Failed to start: {ex.Message}");
                _isRunning = false;
            }
        }

        public static void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            
            // Signal all pending jobs to complete with error
            lock (_queueLock)
            {
                while (_jobQueue.Count > 0)
                {
                    var job = _jobQueue.Dequeue();
                    job.StatusCode = 503;
                    job.ResponseJson = JsonConvert.SerializeObject(new { error = "Server stopped" });
                    job.IsProcessed = true;
                    job.CompletionSignal?.Set();
                }
            }
            
            Debug.Log("[UnitySkills] Server stopped");
        }
        
        /// <summary>
        /// Keep-alive loop - forces Unity to update when not focused.
        /// Does NOT call any Unity API directly (uses thread-safe QueuePlayerLoopUpdate).
        /// </summary>
        private static void KeepAliveLoop()
        {
            while (_isRunning)
            {
                try
                {
                    Thread.Sleep(KeepAliveIntervalMs);
                    
                    bool hasPendingJobs;
                    lock (_queueLock)
                    {
                        hasPendingJobs = _jobQueue.Count > 0;
                    }
                    
                    if (hasPendingJobs)
                    {
                        // Thread-safe call to wake up Unity's main thread
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                }
                catch (ThreadAbortException) { break; }
                catch { /* Ignore */ }
            }
        }

        /// <summary>
        /// HTTP Listener loop (Producer).
        /// CRITICAL: This runs on a background thread. NO Unity API calls allowed.
        /// Only enqueues raw request data for main thread processing.
        /// </summary>
        private static void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    
                    // Immediately capture raw data (no Unity API)
                    var request = context.Request;
                    string body = "";
                    
                    if (request.HttpMethod == "POST" && request.ContentLength64 > 0)
                    {
                        using (var reader = new System.IO.StreamReader(request.InputStream, Encoding.UTF8))
                        {
                            body = reader.ReadToEnd();
                        }
                    }
                    
                    // Create job with raw data only - use DateTime (thread-safe) instead of Unity time
                    var job = new RequestJob
                    {
                        Context = context,
                        HttpMethod = request.HttpMethod,
                        Path = request.Url.AbsolutePath,
                        Body = body,
                        EnqueueTimeTicks = DateTime.UtcNow.Ticks,
                        StatusCode = 200,
                        ResponseJson = null,
                        IsProcessed = false,
                        CompletionSignal = new ManualResetEventSlim(false)
                    };
                    
                    Interlocked.Increment(ref _totalRequestsReceived);
                    
                    // Enqueue for main thread processing
                    lock (_queueLock)
                    {
                        _jobQueue.Enqueue(job);
                    }
                    
                    // Wait for main thread to process (with timeout)
                    // This is thread-safe - just waiting on a signal
                    ThreadPool.QueueUserWorkItem(_ => WaitAndRespond(job));
                }
                catch (HttpListenerException) { /* Expected when stopping */ }
                catch (ObjectDisposedException) { /* Expected when stopping */ }
                catch (Exception)
                {
                    // Can't use Debug.Log here (not main thread safe for logging)
                    // Errors will be visible if Unity console is monitoring
                }
            }
        }
        
        /// <summary>
        /// Waits for job completion and sends HTTP response.
        /// Runs on ThreadPool thread - NO Unity API calls.
        /// </summary>
        private static void WaitAndRespond(RequestJob job)
        {
            try
            {
                // Wait up to 60 seconds for main thread to process
                bool completed = job.CompletionSignal.Wait(60000);
                
                if (!completed)
                {
                    job.StatusCode = 504;
                    job.ResponseJson = JsonConvert.SerializeObject(new {
                        error = "Gateway Timeout: Main thread did not respond within 60 seconds",
                        suggestion = "Unity Editor may be paused or showing a modal dialog"
                    });
                }
                
                // Send HTTP response (thread-safe)
                SendResponse(job);
            }
            catch (Exception)
            {
                // Best effort - try to send error response
                try
                {
                    job.StatusCode = 500;
                    job.ResponseJson = JsonConvert.SerializeObject(new { error = "Internal server error" });
                    SendResponse(job);
                }
                catch { }
            }
            finally
            {
                job.CompletionSignal?.Dispose();
            }
        }
        
        /// <summary>
        /// Sends HTTP response. Thread-safe (no Unity API).
        /// </summary>
        private static void SendResponse(RequestJob job)
        {
            HttpListenerResponse response = null;
            try
            {
                response = job.Context.Response;
                
                // CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                response.StatusCode = job.StatusCode;
                
                if (!string.IsNullOrEmpty(job.ResponseJson))
                {
                    response.ContentType = "application/json";
                    byte[] buffer = Encoding.UTF8.GetBytes(job.ResponseJson);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch { /* Ignore write errors */ }
            finally
            {
                try { response?.Close(); } catch { }
            }
        }

        /// <summary>
        /// Main thread job processor (Consumer).
        /// Runs via EditorApplication.update - ALL Unity API calls are safe here.
        /// </summary>
        private static void ProcessJobQueue()
        {
            int processed = 0;
            const int maxPerFrame = 20; // Process more per frame for high throughput
            
            while (processed < maxPerFrame)
            {
                RequestJob job = null;
                
                lock (_queueLock)
                {
                    if (_jobQueue.Count > 0)
                    {
                        job = _jobQueue.Dequeue();
                    }
                }
                
                if (job == null) break;
                
                try
                {
                    ProcessJob(job);
                }
                catch (Exception ex)
                {
                    job.StatusCode = 500;
                    job.ResponseJson = JsonConvert.SerializeObject(new {
                        error = ex.Message,
                        type = ex.GetType().Name
                    });
                    Debug.LogWarning($"[UnitySkills] Job processing error: {ex.Message}");
                }
                finally
                {
                    job.IsProcessed = true;
                    job.CompletionSignal?.Set();
                    Interlocked.Increment(ref _totalRequestsProcessed);
                }
                
                processed++;
            }
        }

        /// <summary>
        /// Processes a single job. Runs on MAIN THREAD - all Unity API safe.
        /// </summary>
        private static void ProcessJob(RequestJob job)
        {
            // Handle OPTIONS (CORS preflight)
            if (job.HttpMethod == "OPTIONS")
            {
                job.StatusCode = 204;
                job.ResponseJson = "";
                return;
            }
            
            string path = job.Path.ToLower();
            
            // Health check
            if (path == "/" || path == "/health")
            {
                job.StatusCode = 200;
                job.ResponseJson = JsonConvert.SerializeObject(new {
                    status = "ok",
                    service = "UnitySkills",
                    version = "1.3.0",
                    serverRunning = _isRunning,
                    queuedRequests = QueuedRequests,
                    totalProcessed = _totalRequestsProcessed,
                    architecture = "Producer-Consumer (Thread-Safe)"
                });
                return;
            }
            
            // Get skills manifest
            if (path == "/skills" && job.HttpMethod == "GET")
            {
                job.StatusCode = 200;
                job.ResponseJson = SkillRouter.GetManifest();
                return;
            }
            
            // Execute skill
            if (path.StartsWith("/skill/") && job.HttpMethod == "POST")
            {
                // Rate limiting (now safe - on main thread)
                if (!CheckRateLimit())
                {
                    job.StatusCode = 429;
                    job.ResponseJson = JsonConvert.SerializeObject(new {
                        error = "Rate limit exceeded",
                        limit = MaxRequestsPerSecond,
                        suggestion = "Please slow down requests"
                    });
                    return;
                }
                
                // Extract skill name (preserve original case)
                string skillName = job.Path.Substring(7);
                
                // Execute skill (safe - on main thread)
                try
                {
                    job.StatusCode = 200;
                    job.ResponseJson = SkillRouter.Execute(skillName, job.Body);
                }
                catch (Exception ex)
                {
                    job.StatusCode = 500;
                    job.ResponseJson = JsonConvert.SerializeObject(new {
                        error = ex.Message,
                        type = ex.GetType().Name,
                        skill = skillName
                    });
                    Debug.LogWarning($"[UnitySkills] Skill '{skillName}' error: {ex.Message}");
                }
                return;
            }
            
            // Not found
            job.StatusCode = 404;
            job.ResponseJson = JsonConvert.SerializeObject(new {
                error = "Not found",
                endpoints = new[] { "GET /skills", "POST /skill/{name}", "GET /health" }
            });
        }

        /// <summary>
        /// Rate limiting check. MUST be called from main thread only.
        /// Uses DateTime for consistent timing (NOT EditorApplication.timeSinceStartup).
        /// </summary>
        private static bool CheckRateLimit()
        {
            // Use DateTime for thread-safe timing
            double now = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            
            if (now - _lastRateLimitReset >= 1.0)
            {
                _requestsThisSecond = 0;
                _lastRateLimitReset = now;
            }
            
            _requestsThisSecond++;
            return _requestsThisSecond <= MaxRequestsPerSecond;
        }
    }
}

