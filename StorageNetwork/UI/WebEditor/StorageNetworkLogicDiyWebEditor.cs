using Newtonsoft.Json;
using StorageNetwork.Components;
using StorageNetwork.LogicDiy.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace StorageNetwork.UI.WebEditor
{
    internal static class StorageNetworkLogicDiyWebEditor
    {
        private const int FirstPort = 17845;
        private const int LastPort = 17864;
        private const double LaunchSuppressSeconds = 20d;

        private static readonly object sync = new object();
        private static readonly Dictionary<int, WeakReference<StorageNetworkLogicDiy>> registered = new Dictionary<int, WeakReference<StorageNetworkLogicDiy>>();
        private static readonly LogicDiyEditorSessionRegistry<StorageNetworkLogicDiy, object, WebEditorSaveRequest> sessions =
            new LogicDiyEditorSessionRegistry<StorageNetworkLogicDiy, object, WebEditorSaveRequest>();
        private static readonly Dictionary<int, System.DateTime> activePages = new Dictionary<int, System.DateTime>();
        private static readonly Dictionary<int, System.DateTime> recentLaunches = new Dictionary<int, System.DateTime>();
        private static readonly Dictionary<int, WebEditorState> cachedStates = new Dictionary<int, WebEditorState>();
        private static readonly HashSet<int> pendingRuntimeResets = new HashSet<int>();
        private static readonly Dictionary<int, int> pendingBuildingFocusTargets = new Dictionary<int, int>();
        private static readonly Dictionary<int, float> lastFullStateRefreshTime = new Dictionary<int, float>();
        private static readonly Dictionary<int, IntPtr> editorWindowHandles = new Dictionary<int, IntPtr>();

        private static HttpListener listener;
        private static Thread listenerThread;
        private static int activePort;
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);
        private static readonly IntPtr HwndNotopmost = new IntPtr(-2);
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpShowWindow = 0x0040;
        private const int SwShownormal = 1;

        public static void Open(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            EnsureServer();
            if (activePort <= 0)
            {
                Debug.LogWarning("StorageNetwork LogicDiy web editor could not start local server.");
                return;
            }

            int id = logic.GetInstanceID();
            WebEditorState state = BuildFullState(logic);
            string windowTitle = GetExpectedWindowTitle(state);
            lock (sync)
            {
                registered[id] = new WeakReference<StorageNetworkLogicDiy>(logic);
                cachedStates[id] = state;
                lastFullStateRefreshTime[id] = Time.unscaledTime;
            }

            if (TryActivateExistingEditorWindow(id, windowTitle))
            {
                return;
            }

            string url = $"http://127.0.0.1:{activePort}/?id={id}";
            lock (sync)
            {
                activePages[id] = System.DateTime.UtcNow;
                recentLaunches[id] = System.DateTime.UtcNow;
            }

            if (!TryOpenTopmostBrowserWindow(url, id, windowTitle))
            {
                try { System.Diagnostics.Process.Start(url); } catch { Application.OpenURL(url); }
            }
        }

        public static void RefreshCachedStateIfActive(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            int id = logic.GetInstanceID();
            lock (sync)
            {
                if (!IsPageActiveLocked(id))
                {
                    return;
                }

                float now = Time.unscaledTime;
                if (lastFullStateRefreshTime.TryGetValue(id, out float lastRefresh) && now - lastRefresh < 1f)
                {
                    return;
                }

                lastFullStateRefreshTime[id] = now;
            }

            WebEditorState state = BuildFullState(logic);
            lock (sync)
            {
                cachedStates[id] = state;
            }
            sessions.Register(id, logic, null);
            sessions.Prune(TimeSpan.FromMinutes(10));
        }

        public static void RefreshRuntimeSignalsIfActive(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            int id = logic.GetInstanceID();
            int outputSignalValue = logic.OutputSignalValue;
            Dictionary<string, float> nodeOutputValues = logic.GetRuntimeEvalSnapshot();
            lock (sync)
            {
                if (!IsPageActiveLocked(id) || !cachedStates.TryGetValue(id, out WebEditorState state) || state == null)
                {
                    return;
                }

                state.OutputSignalValue = outputSignalValue;
                state.NodeOutputValues = nodeOutputValues;
            }
        }

        public static void ApplyPending(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            int id = logic.GetInstanceID();
            int focusTargetId = 0;
            lock (sync)
            {
                if (pendingBuildingFocusTargets.TryGetValue(id, out focusTargetId))
                {
                    pendingBuildingFocusTargets.Remove(id);
                }
                if (pendingRuntimeResets.Remove(id))
                {
                    logic.ResetRuntimeStateForEditor();
                    if (cachedStates.TryGetValue(id, out WebEditorState resetState) && resetState != null)
                    {
                        resetState.OutputSignalValue = 0;
                        resetState.NodeOutputValues = new Dictionary<string, float>();
                    }
                }
            }
            if (focusTargetId > 0)
            {
                FocusBuilding(focusTargetId);
            }
            WebEditorSaveRequest request = sessions.TakeSave(id);

            if (request == null)
            {
                return;
            }

            logic.ApplyWebEditorState(
                request.RuntimeBlueprintJson,
                request.OutputModeValue,
                request.SourceModeValue,
                request.ConditionThresholdKg,
                request.ConditionItemKey,
                request.RuntimeLayoutJson);

            WebEditorState state = BuildFullState(logic);
            lock (sync)
            {
                cachedStates[id] = state;
                lastFullStateRefreshTime[id] = Time.unscaledTime;
            }
        }

        private static void EnsureServer()
        {
            if (listener != null && listener.IsListening)
            {
                return;
            }

            for (int port = FirstPort; port <= LastPort; port++)
            {
                try
                {
                    HttpListener candidate = new HttpListener();
                    candidate.Prefixes.Add($"http://127.0.0.1:{port}/");
                    candidate.Start();
                    listener = candidate;
                    activePort = port;
                    listenerThread = new Thread(ListenLoop)
                    {
                        IsBackground = true,
                        Name = "StorageNetworkLogicDiyWebEditor"
                    };
                    listenerThread.Start();
                    Debug.Log($"StorageNetwork LogicDiy web editor listening on http://127.0.0.1:{port}/");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"StorageNetwork LogicDiy web editor skipped port {port}: {ex.Message}");
                }
            }
        }

        private static void ListenLoop()
        {
            while (listener != null && listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"StorageNetwork LogicDiy web editor request failed: {ex.Message}");
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                if (path == "/api/state")
                {
                    WriteJson(context, BuildState(context));
                    return;
                }

                if (path == "/api/signals")
                {
                    WriteJson(context, BuildSignalState(context));
                    return;
                }

                if (path == "/api/reset" && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    bool queued = QueueRuntimeReset(context);
                    WriteJson(context, new { ok = queued }, queued ? 200 : 404);
                    return;
                }

                if (path == "/api/focus-building" && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    bool queued = QueueBuildingFocus(context);
                    WriteJson(context, new { ok = queued }, queued ? 200 : 404);
                    return;
                }

                if (path == "/api/heartbeat")
                {
                    MarkHeartbeat(context);
                    WriteJson(context, new { ok = true });
                    return;
                }

                if (path == "/api/save" && string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    bool saved = SaveState(context);
                    WriteJson(context, new { ok = saved }, saved ? 200 : 404);
                    return;
                }

                string ext = Path.GetExtension(path)?.ToLowerInvariant();
                if (ext == ".js" || ext == ".css" || ext == ".png" || ext == ".svg")
                {
                    ServeStaticFile(context, path.TrimStart('/'));
                    return;
                }

                ServeEditor(context);
            }
            catch (Exception ex)
            {
                WriteJson(context, new { ok = false, error = ex.Message }, 500);
            }
        }

        private static WebEditorState BuildState(HttpListenerContext context)
        {
            int id = GetId(context);
            if (id != 0)
            {
                lock (sync)
                {
                    activePages[id] = System.DateTime.UtcNow;
                }
            }

            lock (sync)
            {
                if (cachedStates.TryGetValue(id, out WebEditorState state) && state != null)
                {
                    sessions.MarkSeen(id);
                    return state;
                }
            }

            return new WebEditorState();
        }

        private static WebEditorSignalState BuildSignalState(HttpListenerContext context)
        {
            int id = GetId(context);
            lock (sync)
            {
                if (id != 0)
                {
                    activePages[id] = System.DateTime.UtcNow;
                }

                if (cachedStates.TryGetValue(id, out WebEditorState state) && state != null)
                {
                    return new WebEditorSignalState
                    {
                        OutputSignalValue = state.OutputSignalValue,
                        NodeOutputValues = state.NodeOutputValues == null
                            ? new Dictionary<string, float>()
                            : new Dictionary<string, float>(state.NodeOutputValues)
                    };
                }
            }

            return new WebEditorSignalState();
        }

        private static bool QueueRuntimeReset(HttpListenerContext context)
        {
            int id = GetId(context);
            if (id == 0 || ResolveLogic(id) == null)
            {
                return false;
            }

            lock (sync)
            {
                pendingRuntimeResets.Add(id);
                if (cachedStates.TryGetValue(id, out WebEditorState state) && state != null)
                {
                    state.OutputSignalValue = 0;
                    state.NodeOutputValues = new Dictionary<string, float>();
                }
            }
            return true;
        }

        private static bool QueueBuildingFocus(HttpListenerContext context)
        {
            int id = GetId(context);
            Dictionary<string, string> query = ParseQuery(context.Request.Url.Query);
            if (id == 0 || ResolveLogic(id) == null || !query.TryGetValue("targetId", out string rawTargetId) ||
                !int.TryParse(rawTargetId, out int targetId) || targetId <= 0)
            {
                return false;
            }

            lock (sync)
            {
                pendingBuildingFocusTargets[id] = targetId;
            }
            return true;
        }

        private static void FocusBuilding(int targetInstanceId)
        {
            if (!StorageNetwork.Services.StorageNetworkBuildingRegistry.TryGetBuilding(targetInstanceId, out GameObject target) ||
                target == null || target.GetComponent<PixelPack>() == null || SelectTool.Instance == null)
            {
                return;
            }

            int worldId = target.GetMyWorldId();
            if (ClusterManager.Instance != null && worldId >= 0 && ClusterManager.Instance.activeWorldId != worldId)
            {
                ClusterManager.Instance.SetActiveWorld(worldId);
            }

            KSelectable selectable = target.GetComponent<KSelectable>();
            if (selectable != null)
            {
                SelectTool.Instance.SelectAndFocus(target.transform.position, selectable, Vector3.zero);
            }
        }

        private static WebEditorState BuildLightState(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return new WebEditorState();
            }

            KPrefabID prefabId = logic.gameObject != null ? logic.gameObject.GetComponent<KPrefabID>() : null;
            return new WebEditorState
            {
                Id = logic.GetInstanceID(),
                CurrentBuildingInstanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID,
                BuildingName = StripWebEditorRichText(logic.gameObject != null ? logic.gameObject.GetProperName() : string.Empty),
                RuntimeBlueprintJson = logic.RuntimeBlueprintJson ?? string.Empty,
                RuntimeLayoutJson = logic.RuntimeLayoutJson ?? string.Empty,
                OutputModeValue = logic.OutputModeValue,
                SourceModeValue = logic.SourceModeValue,
                ConditionThresholdKg = logic.ConditionThresholdKg,
                ConditionItemKey = logic.ConditionItemKey ?? string.Empty
            };
        }

        private static WebEditorState BuildFullState(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return new WebEditorState();
            }

            StorageNetworkLogicDiy.WebEditorNetworkMetrics metrics = logic.GetWebEditorNetworkMetrics();
            KPrefabID prefabId = logic.gameObject != null ? logic.gameObject.GetComponent<KPrefabID>() : null;
            return new WebEditorState
            {
                Id = logic.GetInstanceID(),
                CurrentBuildingInstanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID,
                BuildingName = StripWebEditorRichText(logic.gameObject != null ? logic.gameObject.GetProperName() : string.Empty),
                RuntimeBlueprintJson = logic.RuntimeBlueprintJson ?? string.Empty,
                RuntimeLayoutJson = logic.RuntimeLayoutJson ?? string.Empty,
                OutputModeValue = logic.OutputModeValue,
                SourceModeValue = logic.SourceModeValue,
                ConditionThresholdKg = logic.ConditionThresholdKg,
                ConditionItemKey = logic.ConditionItemKey ?? string.Empty,
                SelectedMaterialAmountKg = logic.GetSelectedMaterialAmountKgForWebEditor(),
                TotalStoredKg = metrics.TotalStoredKg,
                TotalCapacityKg = metrics.TotalCapacityKg,
                PowerStoredJoules = metrics.PowerStoredJoules,
                PowerCapacityJoules = metrics.PowerCapacityJoules,
                PowerRemainingJoules = metrics.PowerRemainingJoules,
                PowerJoulesLostPerCycle = metrics.PowerJoulesLostPerCycle,
                OutputSignalValue = logic.OutputSignalValue,
                NodeOutputValues = logic.GetRuntimeEvalSnapshot(),
                Materials = logic.GetWebEditorMaterialOptions(),
                Buildings = logic.GetWebEditorBuildingOptions()
            };
        }

        private static bool SaveState(HttpListenerContext context)
        {
            int id = GetId(context);
            if (id == 0 || ResolveLogic(id) == null || context.Request.ContentLength64 > 1024 * 1024)
            {
                return false;
            }

            using (StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                WebEditorSaveRequest request = JsonConvert.DeserializeObject<WebEditorSaveRequest>(reader.ReadToEnd()) ?? new WebEditorSaveRequest();
                return sessions.QueueSave(id, request);
            }
        }

        private static void MarkHeartbeat(HttpListenerContext context)
        {
            int id = GetId(context);
            if (id == 0)
            {
                return;
            }

            lock (sync)
            {
                activePages[id] = System.DateTime.UtcNow;
            }
            sessions.MarkSeen(id);
        }

        private static bool IsPageActiveLocked(int id)
        {
            if (!activePages.TryGetValue(id, out System.DateTime lastSeen))
            {
                return false;
            }

            if ((System.DateTime.UtcNow - lastSeen).TotalSeconds <= 6)
            {
                return true;
            }

            activePages.Remove(id);
            return false;
        }

        private static bool IsLaunchRecentlyRequestedLocked(int id)
        {
            if (!recentLaunches.TryGetValue(id, out System.DateTime lastLaunch))
            {
                return false;
            }

            if ((System.DateTime.UtcNow - lastLaunch).TotalSeconds <= LaunchSuppressSeconds)
            {
                return true;
            }

            recentLaunches.Remove(id);
            return false;
        }

        private static StorageNetworkLogicDiy ResolveLogic(int id)
        {
            lock (sync)
            {
                if (registered.TryGetValue(id, out WeakReference<StorageNetworkLogicDiy> reference) &&
                    reference.TryGetTarget(out StorageNetworkLogicDiy logic) &&
                    logic != null)
                {
                    return logic;
                }
            }

            return null;
        }

        private static int GetId(HttpListenerContext context)
        {
            Dictionary<string, string> query = ParseQuery(context.Request.Url.Query);
            return query.TryGetValue("id", out string value) && int.TryParse(value, out int id) ? id : 0;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
            {
                return values;
            }

            string trimmed = query[0] == '?' ? query.Substring(1) : query;
            foreach (string part in trimmed.Split('&'))
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                int equalsIndex = part.IndexOf('=');
                string key = equalsIndex >= 0 ? part.Substring(0, equalsIndex) : part;
                string value = equalsIndex >= 0 ? part.Substring(equalsIndex + 1) : string.Empty;
                values[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace("+", " "));
            }

            return values;
        }

        private static void ServeEditor(HttpListenerContext context)
        {
            string html = File.ReadAllText(GetEditorPath());
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            AddNoCacheHeaders(context.Response);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static void ServeStaticFile(HttpListenerContext context, string relativePath)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string rootPath = Path.GetFullPath(Path.Combine(directory ?? string.Empty, "WebEditor"));
            string filePath = Path.GetFullPath(Path.Combine(rootPath, relativePath ?? string.Empty));
            if (!filePath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 403;
                context.Response.Close();
                return;
            }
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return;
            }

            string ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            string contentType = ext switch
            {
                ".js" => "application/javascript; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            byte[] bytes = File.ReadAllBytes(filePath);
            context.Response.StatusCode = 200;
            context.Response.ContentType = contentType;
            AddNoCacheHeaders(context.Response);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static void AddNoCacheHeaders(HttpListenerResponse response)
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
        }

        private static string GetEditorPath()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            return Path.Combine(directory ?? string.Empty, "WebEditor", "logic-diy-editor.html");
        }

        private static bool TryOpenTopmostBrowserWindow(string url, int id, string windowTitle)
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor)
            {
                return false;
            }

            string browserPath = FindBrowserPath();
            if (string.IsNullOrEmpty(browserPath))
            {
                return false;
            }

            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = browserPath,
                    Arguments = $"--app=\"{url}\" --new-window",
                    UseShellExecute = false
                };
                System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    ThreadPool.QueueUserWorkItem(_ => SetEditorWindowTopmost(process, id, windowTitle));
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"StorageNetwork LogicDiy web editor could not open topmost browser window: {ex.Message}");
                return false;
            }
        }

        private static string FindBrowserPath()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] candidates =
            {
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(localAppData, "Google", "Chrome", "Application", "chrome.exe")
            };

            foreach (string candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static void SetEditorWindowTopmost(System.Diagnostics.Process process, int id, string windowTitle)
        {
            try
            {
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    IntPtr handle = FindEditorWindow(id, windowTitle);
                    if (handle != IntPtr.Zero)
                    {
                        lock (sync)
                        {
                            editorWindowHandles[id] = handle;
                        }

                        ActivateAndTopmost(handle);
                        ThreadPool.QueueUserWorkItem(_ => KeepEditorWindowTopmost(id, windowTitle));
                        return;
                    }

                    if (process.HasExited)
                    {
                        // Browser launchers often exit after handing off to an existing process.
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"StorageNetwork LogicDiy web editor could not set browser window topmost: {ex.Message}");
            }
        }

        private static bool TryActivateExistingEditorWindow(int id, string windowTitle)
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer &&
                Application.platform != RuntimePlatform.WindowsEditor)
            {
                lock (sync)
                {
                    return IsPageActiveLocked(id);
                }
            }

            IntPtr handle = IntPtr.Zero;
            lock (sync)
            {
                if (editorWindowHandles.TryGetValue(id, out IntPtr cachedHandle) && cachedHandle != IntPtr.Zero && IsWindow(cachedHandle))
                {
                    handle = cachedHandle;
                }
            }

            if (handle == IntPtr.Zero)
            {
                handle = FindEditorWindow(id, windowTitle);
            }

            if (handle == IntPtr.Zero)
            {
                return false;
            }

            lock (sync)
            {
                editorWindowHandles[id] = handle;
                activePages[id] = System.DateTime.UtcNow;
            }

            ActivateAndTopmost(handle);
            ThreadPool.QueueUserWorkItem(_ => KeepEditorWindowTopmost(id, windowTitle));
            return true;
        }

        private static void TryActivateExistingEditorWindowRepeated(int id, string windowTitle)
        {
            for (int attempt = 0; attempt < 25; attempt++)
            {
                if (TryActivateExistingEditorWindow(id, windowTitle))
                {
                    return;
                }

                Thread.Sleep(120);
            }
        }

        private static void KeepEditorWindowTopmost(int id, string windowTitle)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                IntPtr handle = IntPtr.Zero;
                lock (sync)
                {
                    if (editorWindowHandles.TryGetValue(id, out IntPtr cachedHandle) && cachedHandle != IntPtr.Zero && IsWindow(cachedHandle))
                    {
                        handle = cachedHandle;
                    }
                }

                if (handle == IntPtr.Zero)
                {
                    handle = FindEditorWindow(id, windowTitle);
                }

                if (handle != IntPtr.Zero)
                {
                    ActivateAndTopmost(handle);
                }

                Thread.Sleep(250);
            }
        }

        private static void ActivateAndTopmost(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            ShowWindow(handle, SwShownormal);
            SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpShowWindow);
            SetForegroundWindow(handle);
        }

        private static IntPtr FindEditorWindow(int id, string windowTitle)
        {
            IntPtr found = IntPtr.Zero;
            string idNeedle = $"SNLD-{id}";
            string titleNeedle = windowTitle ?? string.Empty;
            string buildingNeedle = titleNeedle;
            int separatorIndex = buildingNeedle.IndexOf(" - ", StringComparison.Ordinal);
            if (separatorIndex >= 0)
            {
                buildingNeedle = buildingNeedle.Substring(0, separatorIndex);
            }

            EnumWindows((handle, _) =>
            {
                if (!IsWindowVisible(handle))
                {
                    return true;
                }

                string title = GetWindowTitle(handle);
                if (string.IsNullOrEmpty(title))
                {
                    return true;
                }

                bool titleMatchesEditorId = title.IndexOf(idNeedle, StringComparison.OrdinalIgnoreCase) >= 0;
                if (id != 0 && !titleMatchesEditorId)
                {
                    return true;
                }

                bool titleLooksLikeEditor = title.IndexOf("逻辑编辑器", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            title.IndexOf("Logic Editor", StringComparison.OrdinalIgnoreCase) >= 0;
                bool titleMatchesBuilding = string.IsNullOrEmpty(buildingNeedle) ||
                                            title.IndexOf(buildingNeedle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            (!string.IsNullOrEmpty(titleNeedle) && title.IndexOf(titleNeedle, StringComparison.OrdinalIgnoreCase) >= 0);
                titleLooksLikeEditor = titleMatchesEditorId || (titleLooksLikeEditor && titleMatchesBuilding);
                if (!titleLooksLikeEditor)
                {
                    return true;
                }

                GetWindowThreadProcessId(handle, out int processId);
                if (!IsBrowserProcess(processId))
                {
                    return true;
                }

                found = handle;
                return !titleMatchesEditorId;
            }, IntPtr.Zero);

            return found;
        }

        private static bool IsBrowserProcess(int processId)
        {
            try
            {
                string processName = System.Diagnostics.Process.GetProcessById(processId).ProcessName ?? string.Empty;
                return processName.IndexOf("msedge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       processName.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetWindowTitle(IntPtr handle)
        {
            int length = GetWindowTextLength(handle);
            if (length <= 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(handle, builder, builder.Capacity);
            return builder.ToString();
        }

        private static string GetExpectedWindowTitle(WebEditorState state)
        {
            string buildingName = StripWebEditorRichText(state?.BuildingName ?? string.Empty).Trim();
            return string.IsNullOrEmpty(buildingName)
                ? "逻辑编辑器"
                : buildingName + " - 逻辑编辑器";
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private static void WriteJson(HttpListenerContext context, object data, int statusCode = 200)
        {
            string json = JsonConvert.SerializeObject(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static string StripWebEditorRichText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string text = value;
            int guard = 0;
            while (guard++ < 12)
            {
                int open = text.IndexOf("<link=", StringComparison.OrdinalIgnoreCase);
                if (open < 0)
                {
                    break;
                }

                int close = text.IndexOf('>', open);
                if (close < 0)
                {
                    break;
                }

                text = text.Remove(open, close - open + 1);
            }

            return text.Replace("</link>", string.Empty);
        }

        private sealed class WebEditorState
        {
            public int Id { get; set; }
            public int CurrentBuildingInstanceId { get; set; }
            public string BuildingName { get; set; }
            public string RuntimeBlueprintJson { get; set; }
            public string RuntimeLayoutJson { get; set; }
            public int OutputModeValue { get; set; }
            public int SourceModeValue { get; set; }
            public float ConditionThresholdKg { get; set; }
            public string ConditionItemKey { get; set; }
            public float SelectedMaterialAmountKg { get; set; }
            public float TotalStoredKg { get; set; }
            public float TotalCapacityKg { get; set; }
            public float PowerStoredJoules { get; set; }
            public float PowerCapacityJoules { get; set; }
            public float PowerRemainingJoules { get; set; }
            public float PowerJoulesLostPerCycle { get; set; }
            public int OutputSignalValue { get; set; }
            public Dictionary<string, float> NodeOutputValues { get; set; } = new Dictionary<string, float>();
            public List<StorageNetworkLogicDiy.WebEditorMaterialOption> Materials { get; set; } = new List<StorageNetworkLogicDiy.WebEditorMaterialOption>();
            public List<StorageNetworkLogicDiy.WebEditorBuildingOption> Buildings { get; set; } = new List<StorageNetworkLogicDiy.WebEditorBuildingOption>();
        }

        private sealed class WebEditorSignalState
        {
            public int OutputSignalValue { get; set; }
            public Dictionary<string, float> NodeOutputValues { get; set; } = new Dictionary<string, float>();
        }

        private sealed class WebEditorSaveRequest
        {
            public string RuntimeBlueprintJson { get; set; }
            public string RuntimeLayoutJson { get; set; }
            public int OutputModeValue { get; set; }
            public int SourceModeValue { get; set; }
            public float ConditionThresholdKg { get; set; }
            public string ConditionItemKey { get; set; }
        }
    }
}
