using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace ONIResourceBridge
{
    internal static class ResourceBridgeServer
    {
        private const string ModName = "ONIResourceBridge";
        private const string Version = "0.2.0";
        private const int DefaultPort = 17871;
        private const int MaxPort = 17890;

        private static readonly object SyncRoot = new object();
        private static readonly ConcurrentQueue<MainThreadRequest> MainThreadQueue = new ConcurrentQueue<MainThreadRequest>();
        private static readonly object OfflineCacheSync = new object();
        private static readonly string OfflineCachePath = Path.Combine(Path.GetTempPath(), "KAnimGui.ONIResourceBridge.OfflineCache.json");
        private static readonly string OfflineSpriteCachePath = Path.Combine(Path.GetTempPath(), "KAnimGui.ONIResourceBridge.OfflineSprites.json");
        private static TcpListener listener;
        private static Thread worker;
        private static int port;
        private static List<OfflineAnimEntry> offlineAnimCache;
        private static List<OfflineSpriteEntry> offlineSpriteCache;

        public static void Start()
        {
            lock (SyncRoot)
            {
                if (listener != null)
                {
                    return;
                }

                for (int candidate = DefaultPort; candidate <= MaxPort; candidate++)
                {
                    if (TryStart(candidate))
                    {
                        port = candidate;
                        WriteStatusFile(candidate);
                        Debug.Log($"[{ModName}] Listening on http://127.0.0.1:{candidate}/");
                        return;
                    }
                }

                Debug.LogError($"[{ModName}] Could not bind localhost port {DefaultPort}-{MaxPort}.");
            }
        }

        private static bool TryStart(int candidatePort)
        {
            try
            {
                listener = new TcpListener(IPAddress.Loopback, candidatePort);
                listener.Start();
                worker = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "ONIResourceBridge"
                };
                worker.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ModName}] Port {candidatePort} unavailable: {ex.Message}");
                listener = null;
                return false;
            }
        }

        private static void ListenLoop()
        {
            while (listener != null)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(_ => Handle(client));
                }
                catch (SocketException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{ModName}] Listen error: {ex}");
                }
            }
        }

        private static void Handle(object state)
        {
            var client = (TcpClient)state;
            try
            {
                using (client)
                {
                    var request = ReadRequest(client.GetStream());
                    var path = request.Path.Trim('/').ToLowerInvariant();
                    switch (path)
                    {
                        case "":
                        case "status":
                            WriteJson(client, BuildStatusJson());
                            break;
                        case "assets/anims":
                            WriteJson(client, BuildAnimListJson());
                            break;
                        case "assets/sprites":
                            WriteJson(client, BuildSpriteListJson());
                            break;
                        case "assets/offline-anims":
                            WriteJson(client, BuildOfflineAnimListJson());
                            break;
                        case "assets/offline-sprites":
                            WriteJson(client, BuildOfflineSpriteListJson());
                            break;
                        case "assets/offline-refresh":
                            WriteJson(client, RefreshOfflineAnimListJson());
                            break;
                        case "assets/offline-sprite-refresh":
                            WriteJson(client, RefreshOfflineSpriteListJson());
                            break;
                        case "assets/kanim":
                            WriteJson(client, BuildKAnimJson(request.GetQuery("name")));
                            break;
                        case "assets/sprite":
                            WriteJson(client, BuildSpriteJson(request.GetQuery("id")));
                            break;
                        case "assets/offline-kanim":
                            WriteJson(client, BuildOfflineKAnimJson(request.GetQuery("id")));
                            break;
                        case "assets/offline-sprite":
                            WriteJson(client, BuildOfflineSpriteJson(request.GetQuery("id")));
                            break;
                        case "assets/preview":
                            WriteJson(client, BuildPreviewJson(request.GetQuery("name")));
                            break;
                        case "assets/offline-preview":
                            WriteJson(client, BuildOfflinePreviewJson(request.GetQuery("id")));
                            break;
                        default:
                            WriteJson(client, "{\"ok\":false,\"error\":\"not_found\"}", 404);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ModName}] Request error: {ex}");
                TryWriteError(client);
            }
        }

        private static string BuildStatusJson()
        {
            var anims = Assets.Anims;
            int count = anims == null ? 0 : anims.Count;
            bool ready = anims != null && count > 0;
            int resourcePackageCount = CountResourcePackages();
            return "{"
                + "\"ok\":true,"
                + "\"mod\":\"" + Json(ModName) + "\","
                + "\"version\":\"" + Json(Version) + "\","
                + "\"port\":" + port + ","
                + "\"assetsReady\":" + Bool(ready) + ","
                + "\"animCount\":" + count + ","
                + "\"resourcePackageCount\":" + resourcePackageCount
                + "}";
        }

        private static string BuildAnimListJson()
        {
            var items = GetAnimFiles()
                .Select(file => file.GetData())
                .Where(data => data != null)
                .OrderBy(data => data.name, StringComparer.OrdinalIgnoreCase)
                .Select(data => "{"
                    + "\"id\":\"loaded|" + Json(data.name) + "\","
                    + "\"name\":\"" + Json(data.name) + "\","
                    + "\"source\":\"loaded\","
                    + "\"animCount\":" + data.animCount + ","
                    + "\"frameCount\":" + data.frameCount + ","
                    + "\"elementCount\":" + data.elementCount
                    + "}");

            return "{\"ok\":true,\"items\":[" + string.Join(",", items.ToArray()) + "]}";
        }

        private static string BuildSpriteListJson()
        {
            var items = GetLoadedSprites()
                .OrderBy(sprite => sprite.name, StringComparer.OrdinalIgnoreCase)
                .Select(sprite => "{"
                    + "\"id\":\"loaded|" + sprite.GetInstanceID() + "\","
                    + "\"name\":\"" + Json(sprite.name ?? "sprite") + "\","
                    + "\"source\":\"loaded\","
                    + "\"bundle\":\"" + Json(sprite.texture == null ? string.Empty : sprite.texture.name ?? string.Empty) + "\","
                    + "\"width\":" + Mathf.RoundToInt(sprite.textureRect.width) + ","
                    + "\"height\":" + Mathf.RoundToInt(sprite.textureRect.height)
                    + "}");

            return "{\"ok\":true,\"items\":[" + string.Join(",", items.ToArray()) + "]}";
        }

        private static string BuildOfflineAnimListJson()
        {
            var items = GetOfflineAnimEntries()
                .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(entry => "{"
                    + "\"id\":\"" + Json(entry.Id) + "\","
                    + "\"name\":\"" + Json(entry.DisplayName) + "\","
                    + "\"source\":\"offline\","
                    + "\"bundle\":\"" + Json(entry.BundleName) + "\","
                    + "\"animCount\":" + entry.AnimCount + ","
                    + "\"frameCount\":" + entry.FrameCount + ","
                    + "\"elementCount\":" + entry.ElementCount
                    + "}");

            return "{\"ok\":true,\"items\":[" + string.Join(",", items.ToArray()) + "]}";
        }

        private static string BuildOfflineSpriteListJson()
        {
            var items = GetOfflineSprites()
                .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(entry => "{"
                    + "\"id\":\"" + Json(entry.Id) + "\","
                    + "\"name\":\"" + Json(entry.DisplayName) + "\","
                    + "\"source\":\"offline\","
                    + "\"bundle\":\"" + Json(entry.BundleName) + "\","
                    + "\"width\":" + entry.Width + ","
                    + "\"height\":" + entry.Height
                    + "}");

            return "{\"ok\":true,\"items\":[" + string.Join(",", items.ToArray()) + "]}";
        }

        private static string RefreshOfflineAnimListJson()
        {
            lock (OfflineCacheSync)
            {
                offlineAnimCache = null;
                try
                {
                    if (File.Exists(OfflineCachePath))
                    {
                        File.Delete(OfflineCachePath);
                    }
                }
                catch
                {
                }
            }

            var items = GetOfflineAnimEntries();
            return "{\"ok\":true,\"count\":" + items.Count + "}";
        }

        private static string RefreshOfflineSpriteListJson()
        {
            lock (OfflineCacheSync)
            {
                offlineSpriteCache = null;
                try
                {
                    if (File.Exists(OfflineSpriteCachePath))
                    {
                        File.Delete(OfflineSpriteCachePath);
                    }
                }
                catch
                {
                }
            }

            var items = GetOfflineSprites();
            return "{\"ok\":true,\"count\":" + items.Count + "}";
        }

        private static string BuildKAnimJson(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "{\"ok\":false,\"error\":\"missing_name\"}";
            }

            var file = FindAnimFile(name);
            if (file == null)
            {
                return "{\"ok\":false,\"error\":\"anim_not_found\"}";
            }

            var data = file.GetData();
            byte[] animBytes = file.animBytes;
            byte[] buildBytes = file.buildBytes;
            string source = "raw";

            if ((animBytes == null || animBytes.Length == 0) || (buildBytes == null || buildBytes.Length == 0))
            {
                try
                {
                    if (data == null)
                    {
                        return "{\"ok\":false,\"error\":\"runtime_data_not_available\"}";
                    }

                    animBytes = RuntimeKAnimBytes.BuildAnimBytes(data);
                    buildBytes = RuntimeKAnimBytes.BuildBuildBytes(data);
                    source = "runtime";
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{ModName}] Runtime rebuild failed for {name}: {ex}");
                    return "{\"ok\":false,\"error\":\"runtime_rebuild_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
                }
            }

            var textures = data == null ? (file.textureList ?? new List<Texture2D>()) : RuntimeKAnimBytes.GetTextures(file, data);
            var textureJson = textures.Select((texture, index) => TextureToJson(texture, index)).ToArray();

            return "{"
                + "\"ok\":true,"
                + "\"name\":\"" + Json(data != null ? data.name : name) + "\","
                + "\"source\":\"" + Json(source) + "\","
                + "\"animBytes\":\"" + Convert.ToBase64String(animBytes ?? new byte[0]) + "\","
                + "\"buildBytes\":\"" + Convert.ToBase64String(buildBytes ?? new byte[0]) + "\","
                + "\"textures\":[" + string.Join(",", textureJson) + "]"
                + "}";
        }

        private static string BuildPreviewJson(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "{\"ok\":false,\"error\":\"missing_name\"}";
            }

            var file = FindAnimFile(name);
            if (file == null)
            {
                return "{\"ok\":false,\"error\":\"anim_not_found\"}";
            }

            try
            {
                var data = file.GetData();
                var textures = data == null ? (file.textureList ?? new List<Texture2D>()) : RuntimeKAnimBytes.GetTextures(file, data);
                var texture = textures.FirstOrDefault(item => item != null);
                if (texture == null)
                {
                    return "{\"ok\":false,\"error\":\"preview_not_available\"}";
                }

                string png = Convert.ToBase64String(EncodeTextureToPngOnMainThread(texture));
                return "{"
                    + "\"ok\":true,"
                    + "\"name\":\"" + Json(data != null ? data.name : name) + "\","
                    + "\"width\":" + texture.width + ","
                    + "\"height\":" + texture.height + ","
                    + "\"pngBytes\":\"" + png + "\""
                    + "}";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ModName}] Preview failed for {name}: {ex.Message}");
                return "{\"ok\":false,\"error\":\"preview_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
            }
        }

        private static string BuildSpriteJson(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "{\"ok\":false,\"error\":\"missing_id\"}";
            }

            var sprite = GetLoadedSprites().FirstOrDefault(item => string.Equals("loaded|" + item.GetInstanceID(), id, StringComparison.OrdinalIgnoreCase));
            if (sprite == null)
            {
                return "{\"ok\":false,\"error\":\"sprite_not_found\"}";
            }

            try
            {
                return RunOnMainThread(() => BuildSpriteJsonOnMainThread(sprite, "loaded"));
            }
            catch (Exception ex)
            {
                return "{\"ok\":false,\"error\":\"sprite_export_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
            }
        }

        private static string BuildOfflineKAnimJson(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "{\"ok\":false,\"error\":\"missing_id\"}";
            }

            var entry = GetOfflineAnimEntries().FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return "{\"ok\":false,\"error\":\"offline_anim_not_found\"}";
            }

            try
            {
                return RunOnMainThread(() => BuildOfflineKAnimJsonOnMainThread(entry));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ModName}] Offline export failed for {id}: {ex}");
                return "{\"ok\":false,\"error\":\"offline_export_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
            }
        }

        private static string BuildOfflineSpriteJson(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "{\"ok\":false,\"error\":\"missing_id\"}";
            }

            var entry = GetOfflineSprites().FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return "{\"ok\":false,\"error\":\"offline_sprite_not_found\"}";
            }

            try
            {
                return RunOnMainThread(() => BuildOfflineSpriteJsonOnMainThread(entry));
            }
            catch (Exception ex)
            {
                return "{\"ok\":false,\"error\":\"offline_sprite_export_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
            }
        }

        private static string BuildOfflinePreviewJson(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "{\"ok\":false,\"error\":\"missing_id\"}";
            }

            var entry = GetOfflineAnimEntries().FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                return "{\"ok\":false,\"error\":\"offline_anim_not_found\"}";
            }

            try
            {
                return RunOnMainThread(() => BuildOfflinePreviewJsonOnMainThread(entry));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ModName}] Offline preview failed for {id}: {ex.Message}");
                return "{\"ok\":false,\"error\":\"offline_preview_failed\",\"detail\":\"" + Json(ex.Message) + "\"}";
            }
        }

        private static IEnumerable<Sprite> GetLoadedSprites()
        {
            return Resources.FindObjectsOfTypeAll<Sprite>()
                .Where(sprite =>
                    sprite != null &&
                    !string.IsNullOrWhiteSpace(sprite.name) &&
                    sprite.texture != null &&
                    sprite.textureRect.width > 0f &&
                    sprite.textureRect.height > 0f)
                .GroupBy(sprite => sprite.GetInstanceID())
                .Select(group => group.First());
        }

        private static IReadOnlyList<OfflineSpriteEntry> GetOfflineSprites()
        {
            lock (OfflineCacheSync)
            {
                if (offlineSpriteCache != null)
                {
                    return offlineSpriteCache;
                }

                offlineSpriteCache = TryReadOfflineSpriteCache();
                if (offlineSpriteCache != null)
                {
                    return offlineSpriteCache;
                }

                offlineSpriteCache = RunOnMainThread(ScanOfflineSpritesOnMainThread);
                TryWriteOfflineSpriteCache(offlineSpriteCache);
                return offlineSpriteCache;
            }
        }

        private static List<OfflineSpriteEntry> ScanOfflineSpritesOnMainThread()
        {
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingAssetsPath))
            {
                return new List<OfflineSpriteEntry>();
            }

            var bundlePaths = Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    string name = Path.GetFileName(path);
                    return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                        && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            var result = new List<OfflineSpriteEntry>();
            foreach (string bundlePath in bundlePaths)
            {
                AssetBundle bundle = null;
                bool ownsBundle = false;
                try
                {
                    bundle = OpenBundleForRead(bundlePath, out ownsBundle);
                    if (bundle == null)
                    {
                        continue;
                    }

                    foreach (var sprite in bundle.LoadAllAssets<Sprite>())
                    {
                        if (sprite == null || string.IsNullOrWhiteSpace(sprite.name) || sprite.texture == null)
                        {
                            continue;
                        }

                        string bundleName = Path.GetFileName(bundlePath);
                        result.Add(new OfflineSpriteEntry(
                            "offline_sprite|" + bundleName + "|" + sprite.name,
                            sprite.name,
                            sprite.name,
                            bundlePath,
                            bundleName,
                            Mathf.RoundToInt(sprite.textureRect.width),
                            Mathf.RoundToInt(sprite.textureRect.height)));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{ModName}] Offline sprite scan skipped {bundlePath}: {ex.Message}");
                }
                finally
                {
                    if (bundle != null && ownsBundle)
                    {
                        bundle.Unload(true);
                    }
                }
            }

            return result
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static string TextureToJson(Texture2D texture, int index)
        {
            if (texture == null)
            {
                return "{\"index\":" + index + ",\"name\":\"\",\"width\":0,\"height\":0,\"pngBytes\":\"\"}";
            }

            string png = string.Empty;
            try
            {
                png = Convert.ToBase64String(EncodeTextureToPngOnMainThread(texture));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ModName}] Could not encode texture {texture.name}: {ex.Message}");
            }

            return "{"
                + "\"index\":" + index + ","
                + "\"name\":\"" + Json(texture.name ?? string.Empty) + "\","
                + "\"width\":" + texture.width + ","
                + "\"height\":" + texture.height + ","
                + "\"pngBytes\":\"" + png + "\""
                + "}";
        }

        private static IEnumerable<KAnimFile> GetAnimFiles()
        {
            return Assets.Anims == null
                ? Enumerable.Empty<KAnimFile>()
                : Assets.Anims.Where(file => file != null);
        }

        private static KAnimFile FindAnimFile(string name)
        {
            return GetAnimFiles().FirstOrDefault(file =>
            {
                var data = file.GetData();
                return data != null && string.Equals(data.name, name, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static BridgeRequest ReadRequest(NetworkStream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                string requestLine = reader.ReadLine() ?? string.Empty;
                while (!string.IsNullOrEmpty(reader.ReadLine()))
                {
                }

                string target = "/";
                var parts = requestLine.Split(' ');
                if (parts.Length >= 2)
                {
                    target = parts[1];
                }

                return BridgeRequest.Parse(target);
            }
        }

        private static void WriteJson(TcpClient client, string json, int statusCode = 200)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            string statusText = statusCode == 200 ? "OK" : statusCode == 404 ? "Not Found" : "Internal Server Error";
            string headers = "HTTP/1.1 " + statusCode + " " + statusText + "\r\n"
                + "Content-Type: application/json; charset=utf-8\r\n"
                + "Content-Length: " + buffer.Length + "\r\n"
                + "Access-Control-Allow-Origin: http://127.0.0.1\r\n"
                + "Connection: close\r\n"
                + "\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            var stream = client.GetStream();
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        private static void TryWriteError(TcpClient client)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    WriteJson(client, "{\"ok\":false,\"error\":\"internal_error\"}", 500);
                }
            }
            catch
            {
            }
        }

        private static void WriteStatusFile(int selectedPort)
        {
            try
            {
                string path = GetStatusFilePath();
                string json = "{"
                    + "\"ok\":true,"
                    + "\"mod\":\"" + Json(ModName) + "\","
                    + "\"version\":\"" + Json(Version) + "\","
                    + "\"host\":\"127.0.0.1\","
                    + "\"port\":" + selectedPort + ","
                    + "\"url\":\"http://127.0.0.1:" + selectedPort + "/\""
                    + "}";
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ModName}] Could not write status file: {ex.Message}");
            }
        }

        private static string GetStatusFilePath()
        {
            return Path.Combine(Path.GetTempPath(), "KAnimGui.ONIResourceBridge.json");
        }

        private static IReadOnlyList<OfflineAnimEntry> GetOfflineAnimEntries()
        {
            lock (OfflineCacheSync)
            {
                if (offlineAnimCache != null)
                {
                    return offlineAnimCache;
                }

                offlineAnimCache = TryReadOfflineAnimCache();
                if (offlineAnimCache != null)
                {
                    return offlineAnimCache;
                }

                offlineAnimCache = RunOnMainThread(ScanOfflineAnimEntriesOnMainThread);
                TryWriteOfflineAnimCache(offlineAnimCache);
                return offlineAnimCache;
            }
        }

        private static List<OfflineAnimEntry> ScanOfflineAnimEntriesOnMainThread()
        {
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingAssetsPath))
            {
                return new List<OfflineAnimEntry>();
            }

            var bundlePaths = Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                {
                    string name = Path.GetFileName(path);
                    return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                        && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            var result = new List<OfflineAnimEntry>();
            foreach (string bundlePath in bundlePaths)
            {
                AssetBundle bundle = null;
                bool ownsBundle = false;
                try
                {
                    bundle = OpenBundleForRead(bundlePath, out ownsBundle);
                    if (bundle == null)
                    {
                        continue;
                    }

                    foreach (var file in bundle.LoadAllAssets<KAnimFile>())
                    {
                        if (file == null)
                        {
                            continue;
                        }

                        var data = file.GetData();
                        if (data == null || string.IsNullOrWhiteSpace(data.name))
                        {
                            continue;
                        }

                        string bundleName = Path.GetFileName(bundlePath);
                        string assetName = string.IsNullOrWhiteSpace(file.name) ? data.name : file.name;
                        result.Add(new OfflineAnimEntry(
                            "offline|" + bundleName + "|" + assetName,
                            data.name,
                            assetName,
                            bundlePath,
                            bundleName,
                            data.animCount,
                            data.frameCount,
                            data.elementCount));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{ModName}] Offline scan skipped {bundlePath}: {ex.Message}");
                }
                finally
                {
                    if (bundle != null && ownsBundle)
                    {
                        bundle.Unload(true);
                    }
                }
            }

            return result;
        }

        private static List<OfflineAnimEntry> TryReadOfflineAnimCache()
        {
            try
            {
                if (!File.Exists(OfflineCachePath))
                {
                    return null;
                }

                string[] lines = File.ReadAllLines(OfflineCachePath, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    return null;
                }

                var header = lines[0].Split('\t');
                if (header.Length < 2 || !string.Equals(header[0], "version", StringComparison.OrdinalIgnoreCase) || header[1] != Version)
                {
                    return null;
                }

                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                var bundlePaths = Directory.Exists(streamingAssetsPath)
                    ? Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                        .Where(path =>
                        {
                            string name = Path.GetFileName(path);
                            return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                                && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                        })
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : new List<string>();

                string expectedSignature = BuildBundleSignature(bundlePaths);
                if (!string.Equals(lines[1], expectedSignature, StringComparison.Ordinal))
                {
                    return null;
                }

                var result = new List<OfflineAnimEntry>();
                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');
                    if (parts.Length < 8)
                    {
                        continue;
                    }

                    result.Add(new OfflineAnimEntry(
                        parts[0],
                        parts[1],
                        parts[2],
                        parts[3],
                        parts[4],
                        ParseInt(parts[5]),
                        ParseInt(parts[6]),
                        ParseInt(parts[7])));
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        private static List<OfflineSpriteEntry> TryReadOfflineSpriteCache()
        {
            try
            {
                if (!File.Exists(OfflineSpriteCachePath))
                {
                    return null;
                }

                string[] lines = File.ReadAllLines(OfflineSpriteCachePath, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    return null;
                }

                var header = lines[0].Split('\t');
                if (header.Length < 2 || !string.Equals(header[0], "version", StringComparison.OrdinalIgnoreCase) || header[1] != Version)
                {
                    return null;
                }

                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                var bundlePaths = Directory.Exists(streamingAssetsPath)
                    ? Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                        .Where(path =>
                        {
                            string name = Path.GetFileName(path);
                            return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                                && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                        })
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : new List<string>();

                string expectedSignature = BuildBundleSignature(bundlePaths);
                if (!string.Equals(lines[1], expectedSignature, StringComparison.Ordinal))
                {
                    return null;
                }

                var result = new List<OfflineSpriteEntry>();
                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');
                    if (parts.Length < 7)
                    {
                        continue;
                    }

                    result.Add(new OfflineSpriteEntry(
                        parts[0],
                        parts[1],
                        parts[2],
                        parts[3],
                        parts[4],
                        ParseInt(parts[5]),
                        ParseInt(parts[6])));
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        private static void TryWriteOfflineAnimCache(IReadOnlyList<OfflineAnimEntry> entries)
        {
            try
            {
                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                var bundlePaths = Directory.Exists(streamingAssetsPath)
                    ? Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                        .Where(path =>
                        {
                            string name = Path.GetFileName(path);
                            return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                                && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                        })
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : new List<string>();

                var lines = new List<string>
                {
                    "version\t" + Version,
                    BuildBundleSignature(bundlePaths)
                };

                lines.AddRange(entries.Select(entry => string.Join("\t", new[]
                {
                    entry.Id,
                    entry.DisplayName,
                    entry.AssetName,
                    entry.BundlePath,
                    entry.BundleName,
                    entry.AnimCount.ToString(),
                    entry.FrameCount.ToString(),
                    entry.ElementCount.ToString()
                })));

                File.WriteAllLines(OfflineCachePath, lines, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static void TryWriteOfflineSpriteCache(IReadOnlyList<OfflineSpriteEntry> entries)
        {
            try
            {
                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                var bundlePaths = Directory.Exists(streamingAssetsPath)
                    ? Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                        .Where(path =>
                        {
                            string name = Path.GetFileName(path);
                            return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                                && !name.StartsWith("hires_", StringComparison.OrdinalIgnoreCase);
                        })
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : new List<string>();

                var lines = new List<string>
                {
                    "version\t" + Version,
                    BuildBundleSignature(bundlePaths)
                };

                lines.AddRange(entries.Select(entry => string.Join("\t", new[]
                {
                    entry.Id,
                    entry.DisplayName,
                    entry.AssetName,
                    entry.BundlePath,
                    entry.BundleName,
                    entry.Width.ToString(),
                    entry.Height.ToString()
                })));

                File.WriteAllLines(OfflineSpriteCachePath, lines, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string BuildBundleSignature(IReadOnlyList<string> bundlePaths)
        {
            return string.Join("|", bundlePaths.Select(path =>
            {
                var info = new FileInfo(path);
                return Path.GetFileName(path) + ":" + info.Length + ":" + info.LastWriteTimeUtc.Ticks;
            }));
        }

        private static int ParseInt(string value)
        {
            int.TryParse(value, out int result);
            return result;
        }

        private static string BuildOfflineKAnimJsonOnMainThread(OfflineAnimEntry entry)
        {
            AssetBundle bundle = null;
            bool ownsBundle = false;
            try
            {
                bundle = OpenBundleForRead(entry.BundlePath, out ownsBundle);
                if (bundle == null)
                {
                    return "{\"ok\":false,\"error\":\"bundle_load_failed\"}";
                }

                var file = bundle.LoadAllAssets<KAnimFile>().FirstOrDefault(candidate =>
                {
                    if (candidate == null)
                    {
                        return false;
                    }

                    string candidateName = string.IsNullOrWhiteSpace(candidate.name) ? string.Empty : candidate.name;
                    var data = candidate.GetData();
                    return string.Equals(candidateName, entry.AssetName, StringComparison.OrdinalIgnoreCase)
                        || (data != null && string.Equals(data.name, entry.DisplayName, StringComparison.OrdinalIgnoreCase));
                });

                if (file == null)
                {
                    return "{\"ok\":false,\"error\":\"offline_asset_missing\"}";
                }

                var data = file.GetData();
                if (data == null)
                {
                    return "{\"ok\":false,\"error\":\"offline_runtime_data_not_available\"}";
                }

                byte[] animBytes = file.animBytes;
                byte[] buildBytes = file.buildBytes;
                string source = "offline_raw";

                if ((animBytes == null || animBytes.Length == 0) || (buildBytes == null || buildBytes.Length == 0))
                {
                    animBytes = RuntimeKAnimBytes.BuildAnimBytes(data);
                    buildBytes = RuntimeKAnimBytes.BuildBuildBytes(data);
                    source = "offline_runtime";
                }

                var textures = RuntimeKAnimBytes.GetTextures(file, data);
                var textureJson = textures.Select((texture, index) => TextureToJson(texture, index)).ToArray();

                return "{"
                    + "\"ok\":true,"
                    + "\"name\":\"" + Json(data.name) + "\","
                    + "\"source\":\"" + Json(source) + "\","
                    + "\"animBytes\":\"" + Convert.ToBase64String(animBytes ?? new byte[0]) + "\","
                    + "\"buildBytes\":\"" + Convert.ToBase64String(buildBytes ?? new byte[0]) + "\","
                    + "\"textures\":[" + string.Join(",", textureJson) + "]"
                    + "}";
            }
            finally
            {
                if (bundle != null && ownsBundle)
                {
                    bundle.Unload(true);
                }
            }
        }

        private static string BuildSpriteJsonOnMainThread(Sprite sprite, string source)
        {
            if (sprite == null || sprite.texture == null)
            {
                return "{\"ok\":false,\"error\":\"sprite_not_available\"}";
            }

            string png = Convert.ToBase64String(EncodeSpriteToPng(sprite));
            return "{"
                + "\"ok\":true,"
                + "\"name\":\"" + Json(sprite.name ?? "sprite") + "\","
                + "\"source\":\"" + Json(source) + "\","
                + "\"pngBytes\":\"" + png + "\","
                + "\"width\":" + Mathf.RoundToInt(sprite.textureRect.width) + ","
                + "\"height\":" + Mathf.RoundToInt(sprite.textureRect.height)
                + "}";
        }

        private static string BuildOfflinePreviewJsonOnMainThread(OfflineAnimEntry entry)
        {
            AssetBundle bundle = null;
            bool ownsBundle = false;
            try
            {
                bundle = OpenBundleForRead(entry.BundlePath, out ownsBundle);
                if (bundle == null)
                {
                    return "{\"ok\":false,\"error\":\"bundle_load_failed\"}";
                }

                var file = bundle.LoadAllAssets<KAnimFile>().FirstOrDefault(candidate =>
                {
                    if (candidate == null)
                    {
                        return false;
                    }

                    string candidateName = string.IsNullOrWhiteSpace(candidate.name) ? string.Empty : candidate.name;
                    var data = candidate.GetData();
                    return string.Equals(candidateName, entry.AssetName, StringComparison.OrdinalIgnoreCase)
                        || (data != null && string.Equals(data.name, entry.DisplayName, StringComparison.OrdinalIgnoreCase));
                });

                if (file == null)
                {
                    return "{\"ok\":false,\"error\":\"offline_asset_missing\"}";
                }

                var data = file.GetData();
                var textures = data == null ? (file.textureList ?? new List<Texture2D>()) : RuntimeKAnimBytes.GetTextures(file, data);
                var texture = textures.FirstOrDefault(item => item != null);
                if (texture == null)
                {
                    return "{\"ok\":false,\"error\":\"preview_not_available\"}";
                }

                string png = Convert.ToBase64String(EncodeTextureToPng(texture));
                return "{"
                    + "\"ok\":true,"
                    + "\"name\":\"" + Json(data != null ? data.name : entry.DisplayName) + "\","
                    + "\"width\":" + texture.width + ","
                    + "\"height\":" + texture.height + ","
                    + "\"pngBytes\":\"" + png + "\""
                    + "}";
            }
            finally
            {
                if (bundle != null && ownsBundle)
                {
                    bundle.Unload(true);
                }
            }
        }

        private static string BuildOfflineSpriteJsonOnMainThread(OfflineSpriteEntry entry)
        {
            AssetBundle bundle = null;
            bool ownsBundle = false;
            try
            {
                bundle = OpenBundleForRead(entry.BundlePath, out ownsBundle);
                if (bundle == null)
                {
                    return "{\"ok\":false,\"error\":\"bundle_load_failed\"}";
                }

                var sprite = bundle.LoadAllAssets<Sprite>().FirstOrDefault(candidate =>
                    candidate != null &&
                    string.Equals(candidate.name, entry.AssetName, StringComparison.OrdinalIgnoreCase));

                if (sprite == null)
                {
                    return "{\"ok\":false,\"error\":\"offline_sprite_missing\"}";
                }

                return BuildSpriteJsonOnMainThread(sprite, "offline");
            }
            finally
            {
                if (bundle != null && ownsBundle)
                {
                    bundle.Unload(true);
                }
            }
        }

        private static AssetBundle OpenBundleForRead(string bundlePath, out bool ownsBundle)
        {
            ownsBundle = false;

            string bundleName = Path.GetFileName(bundlePath);
            foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (loadedBundle == null)
                {
                    continue;
                }

                if (string.Equals(loadedBundle.name, bundleName, StringComparison.OrdinalIgnoreCase))
                {
                    return loadedBundle;
                }
            }

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            ownsBundle = bundle != null;
            return bundle;
        }

        private static byte[] EncodeSpriteToPng(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;
            int x = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, texture.height - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 1, texture.width - x);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 1, texture.height - y);

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                output.ReadPixels(new Rect(x, y, width, height), 0, 0);
                output.Apply();
                return output.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.Destroy(output);
            }
        }

        private static int CountResourcePackages()
        {
            try
            {
                string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
                if (!Directory.Exists(streamingAssetsPath))
                {
                    return 0;
                }

                return Directory.EnumerateFiles(streamingAssetsPath, "*", SearchOption.TopDirectoryOnly)
                    .Count(path =>
                    {
                        string name = Path.GetFileName(path);
                        return name.EndsWith("_bundle", StringComparison.OrdinalIgnoreCase)
                            || name.EndsWith(".bank", StringComparison.OrdinalIgnoreCase);
                    });
            }
            catch
            {
                return 0;
            }
        }

        private static string Json(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string Bool(bool value)
        {
            return value ? "true" : "false";
        }

        private static byte[] EncodeTextureToPng(Texture2D texture)
        {
            try
            {
                return texture.EncodeToPNG();
            }
            catch
            {
                return CopyTextureToReadable(texture).EncodeToPNG();
            }
        }

        private static byte[] EncodeTextureToPngOnMainThread(Texture2D texture)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                return EncodeTextureToPng(texture);
            }

            return RunOnMainThread(() => EncodeTextureToPng(texture));
        }

        internal static void PumpMainThreadQueue()
        {
            while (MainThreadQueue.TryDequeue(out var request))
            {
                try
                {
                    request.TrySetResult(request.Callback());
                }
                catch (Exception ex)
                {
                    request.TrySetException(ex);
                }
            }
        }

        private static T RunOnMainThread<T>(Func<T> callback)
        {
            var request = new MainThreadRequest(() => (object)callback());
            MainThreadQueue.Enqueue(request);

            if (!request.WaitHandle.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("main thread did not process resource bridge request in time");
            }

            if (request.Error != null)
            {
                throw request.Error;
            }

            return (T)request.Result;
        }

        private static Texture2D CopyTextureToReadable(Texture2D source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);

                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                var readable = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }

        private sealed class BridgeRequest
        {
            private readonly Dictionary<string, string> query;

            private BridgeRequest(string path, Dictionary<string, string> query)
            {
                Path = path;
                this.query = query;
            }

            public string Path { get; }

            public string GetQuery(string key)
            {
                return query.TryGetValue(key, out var value) ? value : null;
            }

            public static BridgeRequest Parse(string target)
            {
                string path = target;
                string queryText = string.Empty;
                int queryIndex = target.IndexOf('?');
                if (queryIndex >= 0)
                {
                    path = target.Substring(0, queryIndex);
                    queryText = target.Substring(queryIndex + 1);
                }

                var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var part in queryText.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int equals = part.IndexOf('=');
                    if (equals < 0)
                    {
                        query[Uri.UnescapeDataString(part)] = string.Empty;
                    }
                    else
                    {
                        query[Uri.UnescapeDataString(part.Substring(0, equals))] = Uri.UnescapeDataString(part.Substring(equals + 1));
                    }
                }

                return new BridgeRequest(Uri.UnescapeDataString(path), query);
            }
        }

        private sealed class MainThreadRequest
        {
            public MainThreadRequest(Func<object> callback)
            {
                Callback = callback;
            }

            public Func<object> Callback { get; }
            public ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim(false);
            public object Result { get; private set; }
            public Exception Error { get; private set; }

            public void TrySetResult(object result)
            {
                Result = result;
                WaitHandle.Set();
            }

            public void TrySetException(Exception error)
            {
                Error = error;
                WaitHandle.Set();
            }
        }

        private sealed class OfflineAnimEntry
        {
            public OfflineAnimEntry(string id, string displayName, string assetName, string bundlePath, string bundleName, int animCount, int frameCount, int elementCount)
            {
                Id = id;
                DisplayName = displayName;
                AssetName = assetName;
                BundlePath = bundlePath;
                BundleName = bundleName;
                AnimCount = animCount;
                FrameCount = frameCount;
                ElementCount = elementCount;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string AssetName { get; }
            public string BundlePath { get; }
            public string BundleName { get; }
            public int AnimCount { get; }
            public int FrameCount { get; }
            public int ElementCount { get; }
        }

        private sealed class OfflineSpriteEntry
        {
            public OfflineSpriteEntry(string id, string displayName, string assetName, string bundlePath, string bundleName, int width, int height)
            {
                Id = id;
                DisplayName = displayName;
                AssetName = assetName;
                BundlePath = bundlePath;
                BundleName = bundleName;
                Width = width;
                Height = height;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string AssetName { get; }
            public string BundlePath { get; }
            public string BundleName { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
