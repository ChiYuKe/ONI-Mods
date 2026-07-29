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

                foreach (string filePath in LocalCanvasConfig.EnumerateImageFiles(prefabId))
                {
                    Texture2D texture = LocalCanvasConfig.LoadTexture(filePath);
                    if (texture == null)
                    {
                        continue;
                    }

                    string key = MakeKey(prefabId, filePath);
                    string sourceSymbolName = SourceSymbolPrefix + MakeHash(key).ToString("X8");
                    string kanimName = sourceSymbolName + "_kanim";
                    KAnimFile.Mod mod = new KAnimFile.Mod
                    {
                        anim = CreateAnimBytes(sourceSymbolName, frame),
                        build = CreateBuildBytes(sourceSymbolName, frame)
                    };
                    mod.textures.Add(texture);

                    KAnimFile sourceFile = ModUtil.AddKAnimMod(kanimName, mod);
                    sourceFiles[key] = sourceFile;
                }
            }

            Debug.Log("[LocalCanvas] registered " + sourceFiles.Count + " local KAnim image sources");
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

        private static byte[] CreateBuildBytes(string sourceSymbolName, CanvasFrame frame)
        {
            int symbolHash = Hash.SDBMLower(sourceSymbolName);
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Encoding.ASCII.GetBytes("BILD"));
                writer.Write(10);
                writer.Write(1);
                writer.Write(0);
                WriteKleiString(writer, sourceSymbolName);
                writer.Write(symbolHash);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(1);
                writer.Write(0);
                writer.Write(1);
                writer.Write(0);
                writer.Write(frame.Center.x);
                writer.Write(frame.Center.y);
                writer.Write(frame.Size.x);
                writer.Write(frame.Size.y);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(1);
                writer.Write(symbolHash);
                WriteKleiString(writer, sourceSymbolName);
                return stream.ToArray();
            }
        }

        private static byte[] CreateAnimBytes(string sourceSymbolName, CanvasFrame frame)
        {
            int symbolHash = Hash.SDBMLower(sourceSymbolName);
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
                writer.Write(1);
                writer.Write(symbolHash);
                writer.Write(0);
                writer.Write(0); // head_anim hash
                writer.Write(0); // reserved element field
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);
                writer.Write(0f);
                writer.Write(0f);
                writer.Write(1f);
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
    }
}
