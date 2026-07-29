using Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace LocalCanvas
{
    internal sealed class LocalCanvasImageInfo
    {
        public string StageId { get; }
        public string PrefabId { get; }
        public string FilePath { get; }
        public string DisplayName { get; }

        public LocalCanvasImageInfo(string stageId, string prefabId, string filePath)
        {
            StageId = stageId;
            PrefabId = prefabId;
            FilePath = filePath;
            DisplayName = Path.GetFileNameWithoutExtension(filePath);
        }

        public Sprite GetSprite() => LocalCanvasConfig.LoadSprite(FilePath);

        public bool TryGetSourceSymbol(out KAnim.Build.Symbol symbol)
        {
            return LocalCanvasKAnimRegistry.TryGetSourceSymbol(PrefabId, FilePath, out symbol);
        }
    }

    internal static class LocalCanvasRegistry
    {
        private static readonly Dictionary<string, LocalCanvasImageInfo> imagesByStage = new Dictionary<string, LocalCanvasImageInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] StatusIds = { "LookingUgly", "LookingOkay", "LookingGreat" };
        private static bool stagesRegistered;

        public static void RegisterStages()
        {
            if (stagesRegistered)
            {
                return;
            }

            ArtableStages stages = Db.GetArtableStages();
            if (stages == null)
            {
                Debug.LogWarning("[LocalCanvas] ArtableStages is not available");
                return;
            }

            stagesRegistered = true;

            foreach (string prefabId in new[] { "Canvas", "CanvasTall", "CanvasWide" })
            {
                foreach (string filePath in LocalCanvasConfig.EnumerateImageFiles(prefabId))
                {
                    string baseStageId = MakeStageId(prefabId, filePath);
                    LocalCanvasImageInfo image = new LocalCanvasImageInfo(baseStageId, prefabId, filePath);

                    foreach (string statusId in StatusIds)
                    {
                        string stageId = baseStageId + "_" + statusId;
                        if (stages.TryGet(stageId) != null)
                        {
                            imagesByStage[stageId] = image;
                            continue;
                        }

                        stages.Add(
                            stageId,
                            image.DisplayName,
                            "本地图片：" + image.DisplayName,
                            PermitRarity.Universal,
                            GetAnimFile(prefabId),
                            "off",
                            10,
                            false,
                            statusId,
                            prefabId,
                            "",
                            Array.Empty<string>(),
                            Array.Empty<string>());
                        imagesByStage[stageId] = image;
                    }
                }
            }

            Debug.Log($"[LocalCanvas] registered {imagesByStage.Count} local canvas choices");
        }

        public static bool TryGetImage(string stageId, out LocalCanvasImageInfo image)
        {
            return imagesByStage.TryGetValue(stageId, out image);
        }

        private static string MakeStageId(string prefabId, string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath).TrimStart('.');
            string safeName = new string((fileName + "_" + extension)
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray());
            using (MD5 md5 = MD5.Create())
            {
                string hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant())))
                    .Replace("-", string.Empty)
                    .Substring(0, 8);
                return "LocalCanvas_" + prefabId + "_" + safeName + "_" + hash;
            }
        }

        private static string GetAnimFile(string prefabId)
        {
            return prefabId switch
            {
                "CanvasTall" => "painting_tall_off_kanim",
                "CanvasWide" => "painting_wide_off_kanim",
                _ => "painting_off_kanim"
            };
        }
    }
}
