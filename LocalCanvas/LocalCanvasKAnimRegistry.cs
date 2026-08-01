using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LocalCanvas
{
    internal static class LocalCanvasKAnimRegistry
    {
        private const string CanvasSymbolName = "canvas";
        private const string SourceSymbolPrefix = "local_canvas_image_";

        private static readonly Dictionary<string, KAnimFile> sourceFiles = new Dictionary<string, KAnimFile>(StringComparer.OrdinalIgnoreCase);
        private static bool registered;

        public static void Register()
        {
            if (registered)
            {
                return;
            }

            registered = true;
            foreach (string prefabId in new[] { "Canvas", "CanvasTall", "CanvasWide" })
            {
                if (!TryReadCanvasFrame(prefabId, out CanvasFrame frame))
                {
                    Debug.LogWarning("[LocalCanvas] could not read the official canvas symbol bounds for " + prefabId);
                    continue;
                }

                RegisterPrefabImages(prefabId, frame);
            }

        }

        public static bool RegisterImage(string prefabId, string filePath)
        {
            string key = MakeKey(prefabId, filePath);
            return sourceFiles.ContainsKey(key);
        }

        private static void RegisterPrefabImages(string prefabId, CanvasFrame frame)
        {
            List<CanvasSource> sources = new List<CanvasSource>();
            foreach (string filePath in LocalCanvasConfig.EnumerateImageFiles(prefabId))
            {
                string key = MakeKey(prefabId, filePath);
                Texture2D texture = LocalCanvasConfig.LoadTexture(filePath);
                if (texture == null)
                {
                    continue;
                }

                sources.Add(new CanvasSource
                {
                    Key = key,
                    SymbolName = SourceSymbolPrefix + MakeHash(key).ToString("X8"),
                    Texture = texture
                });
            }

            if (sources.Count == 0)
            {
                return;
            }

            Texture2D[] textures = new Texture2D[sources.Count];
            for (int i = 0; i < sources.Count; i++)
            {
                textures[i] = sources[i].Texture;
            }

            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            Rect[] atlasRects;
            try
            {
                int maximumAtlasSize = Mathf.Min(8192, SystemInfo.maxTextureSize);
                atlasRects = atlas.PackTextures(textures, 4, maximumAtlasSize, false);
            }
            catch (Exception ex)
            {
                UnityEngine.Object.Destroy(atlas);
                Debug.LogError("[LocalCanvas] failed to pack " + prefabId + " image atlas: " + ex);
                return;
            }

            if (atlasRects == null || atlasRects.Length != sources.Count)
            {
                UnityEngine.Object.Destroy(atlas);
                Debug.LogError("[LocalCanvas] failed to pack every " + prefabId + " image into one atlas");
                return;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                sources[i].Uv = atlasRects[i];
            }

            atlas.name = "LocalCanvasAtlas_" + prefabId;
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Bilinear;
            atlas.Apply(false, true);

            string kanimName = "local_canvas_" + prefabId.ToLowerInvariant() + "_atlas_kanim";
            KAnimFile.Mod mod = new KAnimFile.Mod
            {
                anim = CreateAnimBytes(frame),
                build = CreateBuildBytes(prefabId, sources, frame)
            };
            mod.textures.Add(atlas);
            KAnimFile sourceFile = ModUtil.AddKAnimMod(kanimName, mod);
            foreach (CanvasSource source in sources)
            {
                sourceFiles[source.Key] = sourceFile;
            }

        }

        public static bool TryGetSourceSymbol(string prefabId, string filePath, out KAnim.Build.Symbol symbol)
        {
            symbol = null;
            if (!sourceFiles.TryGetValue(MakeKey(prefabId, filePath), out KAnimFile sourceFile) || sourceFile == null)
            {
                return false;
            }

            symbol = sourceFile.GetData()?.build?.GetSymbol(new HashedString(GetSourceSymbolName(prefabId, filePath)));
            return symbol != null;
        }

        private static string GetSourceSymbolName(string prefabId, string filePath)
        {
            return SourceSymbolPrefix + MakeHash(MakeKey(prefabId, filePath)).ToString("X8");
        }

        private static string MakeKey(string prefabId, string filePath)
        {
            return prefabId + "|" + Path.GetFullPath(filePath);
        }

        private static int MakeHash(string value)
        {
            return Hash.SDBMLower(value.ToLowerInvariant());
        }

        private static bool TryReadCanvasFrame(string prefabId, out CanvasFrame frame)
        {
            frame = default(CanvasFrame);
            KAnimFile anim = Assets.GetAnim(GetAnimFile(prefabId));
            byte[] bytes = anim?.buildBytes;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(bytes, false))
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "BILD")
                    {
                        return false;
                    }

                    int version = reader.ReadInt32();
                    int symbolCount = reader.ReadInt32();
                    reader.ReadInt32();
                    ReadKleiString(reader);
                    int canvasHash = Hash.SDBMLower(CanvasSymbolName);

                    for (int i = 0; i < symbolCount; i++)
                    {
                        int symbolHash = reader.ReadInt32();
                        if (version > 9)
                        {
                            reader.ReadInt32();
                        }
                        reader.ReadInt32();
                        reader.ReadInt32();
                        int frameCount = reader.ReadInt32();
                        for (int j = 0; j < frameCount; j++)
                        {
                            reader.ReadInt32();
                            reader.ReadInt32();
                            reader.ReadInt32();
                            float centerX = reader.ReadSingle();
                            float centerY = reader.ReadSingle();
                            float width = reader.ReadSingle();
                            float height = reader.ReadSingle();
                            reader.ReadSingle();
                            reader.ReadSingle();
                            reader.ReadSingle();
                            reader.ReadSingle();

                            if (symbolHash == canvasHash)
                            {
                                frame = new CanvasFrame
                                {
                                    Center = new Vector2(centerX, centerY),
                                    Size = new Vector2(width, height)
                                };
                                return width > 0f && height > 0f;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LocalCanvas] failed to read canvas build data for " + prefabId + ": " + ex.Message);
            }

            return false;
        }

        private static byte[] CreateBuildBytes(string prefabId, IList<CanvasSource> sources, CanvasFrame frame)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Encoding.ASCII.GetBytes("BILD"));
                writer.Write(10);
                writer.Write(sources.Count);
                writer.Write(sources.Count);
                WriteKleiString(writer, "local_canvas_" + prefabId.ToLowerInvariant() + "_atlas");

                foreach (CanvasSource source in sources)
                {
                    int symbolHash = Hash.SDBMLower(source.SymbolName);
                    Rect uv = source.Uv;
                    writer.Write(symbolHash);
                    writer.Write(0); // path hash
                    writer.Write(0); // colour channel
                    writer.Write(0); // flags
                    writer.Write(1); // one source frame
                    writer.Write(0); // source frame number
                    writer.Write(1); // duration
                    writer.Write(0); // every symbol uses the shared atlas texture
                    writer.Write(frame.Center.x);
                    writer.Write(frame.Center.y);
                    writer.Write(frame.Size.x);
                    writer.Write(frame.Size.y);
                    writer.Write(uv.xMin);
                    writer.Write(1f - uv.yMax);
                    writer.Write(uv.xMax);
                    writer.Write(1f - uv.yMin);
                }

                writer.Write(sources.Count);
                foreach (CanvasSource source in sources)
                {
                    writer.Write(Hash.SDBMLower(source.SymbolName));
                    WriteKleiString(writer, source.SymbolName);
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateAnimBytes(CanvasFrame frame)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Encoding.ASCII.GetBytes("ANIM"));
                writer.Write(5U);
                writer.Write(0);
                writer.Write(0);
                writer.Write(1);
                WriteKleiString(writer, "off");
                writer.Write(0);
                writer.Write(30f);
                writer.Write(1);
                writer.Write(frame.Center.x);
                writer.Write(frame.Center.y);
                writer.Write(frame.Size.x);
                writer.Write(frame.Size.y);
                writer.Write(0); // source animation has no visible elements
                writer.Write(0); // maxVisSymbolFrames
                writer.Write(0); // animation hash table count
                return stream.ToArray();
            }
        }

        private static string ReadKleiString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            return length < 0 ? null : Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        private static void WriteKleiString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
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

        private struct CanvasFrame
        {
            public Vector2 Center;
            public Vector2 Size;
        }

        private sealed class CanvasSource
        {
            public string Key;
            public string SymbolName;
            public Texture2D Texture;
            public Rect Uv;
        }

    }
}
