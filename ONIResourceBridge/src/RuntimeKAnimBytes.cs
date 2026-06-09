using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ONIResourceBridge
{
    internal static class RuntimeKAnimBytes
    {
        public static byte[] BuildBuildBytes(KAnimFileData file)
        {
            var build = file.build;
            if (build == null)
            {
                throw new InvalidOperationException("runtime build is not available");
            }

            var hashes = new Dictionary<int, string>();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("BILD"));
                writer.Write(10);
                writer.Write(build.symbols.Length);
                writer.Write(build.symbols.Sum(symbol => symbol == null ? 0 : symbol.numFrames));
                writer.WriteKleiString(build.name ?? file.name);
                AddHash(hashes, build.fileHash.HashValue);

                var group = KAnimBatchManager.Instance().GetBatchGroupData(build.batchTag);
                if (group == null)
                {
                    throw new InvalidOperationException("runtime build batch group is not available");
                }

                foreach (var symbol in build.symbols)
                {
                    if (symbol == null)
                    {
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                        continue;
                    }

                    writer.Write(symbol.hash.HashValue);
                    writer.Write(symbol.path.HashValue);
                    writer.Write(symbol.colourChannel.HashValue);
                    writer.Write(symbol.flags);
                    writer.Write(symbol.numFrames);
                    AddHash(hashes, symbol.hash.HashValue);
                    AddHash(hashes, symbol.path.HashValue);
                    AddHash(hashes, symbol.folder.HashValue);
                    AddHash(hashes, symbol.colourChannel.HashValue);

                    for (int i = 0; i < symbol.numFrames; i++)
                    {
                        var frame = group.GetSymbolFrameInstance(symbol.firstFrameIdx + i);
                        float width = frame.bboxMax.x - frame.bboxMin.x;
                        float height = frame.bboxMax.y - frame.bboxMin.y;
                        float centerX = frame.bboxMin.x + width * 0.5f;
                        float centerY = frame.bboxMin.y + height * 0.5f;

                        writer.Write(frame.sourceFrameNum);
                        writer.Write(frame.duration);
                        writer.Write(Math.Max(0, frame.buildImageIdx - build.textureStartIdx));
                        writer.Write(centerX);
                        writer.Write(centerY);
                        writer.Write(width);
                        writer.Write(height);
                        writer.Write(frame.uvMin.x);
                        writer.Write(1f - frame.uvMin.y);
                        writer.Write(frame.uvMax.x);
                        writer.Write(1f - frame.uvMax.y);
                    }
                }

                WriteHashes(writer, hashes);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static byte[] BuildAnimBytes(KAnimFileData file)
        {
            var group = ResolveAnimGroup(file);
            if (group == null)
            {
                throw new InvalidOperationException("runtime anim batch group is not available");
            }

            var hashes = new Dictionary<int, string>();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("ANIM"));
                writer.Write(5);
                writer.Write(file.elementCount);
                writer.Write(file.frameCount);
                writer.Write(file.animCount);

                for (int i = 0; i < file.animCount; i++)
                {
                    var anim = GetRuntimeAnim(group, file, i);
                    writer.WriteKleiString(anim.name ?? string.Empty);
                    writer.Write(anim.rootSymbol.HashValue);
                    writer.Write(anim.frameRate);
                    writer.Write(anim.numFrames);
                    AddHash(hashes, anim.hash.HashValue);
                    AddHash(hashes, anim.rootSymbol.HashValue);

                    for (int frameIndex = 0; frameIndex < anim.numFrames; frameIndex++)
                    {
                        var frame = group.animFrames[anim.firstFrameIdx + frameIndex];
                        var bounds = EstimateFrameBounds(group, frame);

                        writer.Write(bounds.X);
                        writer.Write(bounds.Y);
                        writer.Write(bounds.Width);
                        writer.Write(bounds.Height);
                        writer.Write(frame.numElements);

                        for (int elementIndex = 0; elementIndex < frame.numElements; elementIndex++)
                        {
                            var element = group.frameElements[frame.firstElementIdx + elementIndex];
                            var symbol = group.GetSymbol(element.symbol);

                            writer.Write(element.symbol.HashValue);
                            writer.Write(element.frame);
                            writer.Write(symbol != null ? symbol.folder.HashValue : 0);
                            writer.Write(0);
                            writer.Write(Mathf.Clamp01(element.multAlpha));
                            writer.Write(1f);
                            writer.Write(1f);
                            writer.Write(1f);
                            writer.Write(element.transform.m00);
                            writer.Write(element.transform.m10);
                            writer.Write(element.transform.m01);
                            writer.Write(element.transform.m11);
                            writer.Write(element.transform.m02);
                            writer.Write(element.transform.m12);
                            writer.Write(0f);

                            AddHash(hashes, element.symbol.HashValue);
                            if (symbol != null)
                            {
                                AddHash(hashes, symbol.folder.HashValue);
                                AddHash(hashes, symbol.path.HashValue);
                                AddHash(hashes, symbol.colourChannel.HashValue);
                            }
                        }
                    }
                }

                writer.Write(file.maxVisSymbolFrames);
                WriteHashes(writer, hashes);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static List<Texture2D> GetTextures(KAnimFile file, KAnimFileData data)
        {
            var textures = file.textureList;
            if (textures != null && textures.Count > 0)
            {
                return textures;
            }

            var build = data.build;
            if (build == null)
            {
                return new List<Texture2D>();
            }

            var group = KAnimBatchManager.Instance().GetBatchGroupData(build.batchTag);
            if (group == null)
            {
                return new List<Texture2D>();
            }

            var result = new List<Texture2D>();
            for (int i = 0; i < build.textureCount; i++)
            {
                result.Add(build.GetTexture(i, group));
            }

            return result;
        }

        private static KBatchGroupData ResolveAnimGroup(KAnimFileData file)
        {
            var manager = KAnimBatchManager.Instance();
            var candidates = new List<HashedString>();
            AddCandidate(candidates, file.animBatchTag);
            AddCandidate(candidates, file.batchTag);

            try
            {
                var group = KAnimGroupFile.GetGroup(file.batchTag);
                if (group != null)
                {
                    AddCandidate(candidates, group.animTarget);
                    AddCandidate(candidates, group.id);
                    AddCandidate(candidates, group.swapTarget);
                    AddCandidate(candidates, group.target);
                }
            }
            catch
            {
            }

            foreach (var candidate in candidates)
            {
                var group = manager.GetBatchGroupData(candidate);
                if (ContainsFileAnim(group, file))
                {
                    return group;
                }
            }

            foreach (var candidate in candidates)
            {
                var group = manager.GetBatchGroupData(candidate);
                if (group != null && group.anims != null && group.anims.Count >= file.firstAnimIndex + file.animCount)
                {
                    return group;
                }
            }

            return null;
        }

        private static KAnim.Anim GetRuntimeAnim(KBatchGroupData group, KAnimFileData file, int relativeIndex)
        {
            int absoluteIndex = file.firstAnimIndex + relativeIndex;
            if (absoluteIndex >= 0 && absoluteIndex < group.anims.Count)
            {
                var anim = group.anims[absoluteIndex];
                if (anim != null && anim.animFile == file)
                {
                    return anim;
                }
            }

            int seen = 0;
            for (int i = 0; i < group.anims.Count; i++)
            {
                var anim = group.anims[i];
                if (anim != null && anim.animFile == file)
                {
                    if (seen == relativeIndex)
                    {
                        return anim;
                    }

                    seen++;
                }
            }

            if (absoluteIndex >= 0 && absoluteIndex < group.anims.Count)
            {
                return group.anims[absoluteIndex];
            }

            throw new InvalidOperationException("runtime anim index is out of range");
        }

        private static bool ContainsFileAnim(KBatchGroupData group, KAnimFileData file)
        {
            if (group == null || group.anims == null)
            {
                return false;
            }

            return group.anims.Any(anim => anim != null && anim.animFile == file);
        }

        private static void AddCandidate(List<HashedString> candidates, HashedString value)
        {
            if (!value.IsValid || candidates.Contains(value))
            {
                return;
            }

            candidates.Add(value);
        }

        private static RuntimeBounds EstimateFrameBounds(KBatchGroupData group, KAnim.Anim.Frame frame)
        {
            if (frame.numElements <= 0)
            {
                return new RuntimeBounds(0f, 0f, 1f, 1f);
            }

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < frame.numElements; i++)
            {
                var element = group.frameElements[frame.firstElementIdx + i];
                var symbol = group.GetSymbol(element.symbol);
                if (symbol == null)
                {
                    continue;
                }

                int symbolFrameIndex = symbol.GetFrameIdx(element.frame);
                if (symbolFrameIndex < 0)
                {
                    continue;
                }

                var symbolFrame = group.GetSymbolFrameInstance(symbolFrameIndex);
                Vector2 size = symbolFrame.bboxMax - symbolFrame.bboxMin;
                float halfW = Math.Max(1f, size.x) * 0.5f;
                float halfH = Math.Max(1f, size.y) * 0.5f;
                AddPoint(element.transform, -halfW, -halfH, ref minX, ref minY, ref maxX, ref maxY);
                AddPoint(element.transform, halfW, -halfH, ref minX, ref minY, ref maxX, ref maxY);
                AddPoint(element.transform, -halfW, halfH, ref minX, ref minY, ref maxX, ref maxY);
                AddPoint(element.transform, halfW, halfH, ref minX, ref minY, ref maxX, ref maxY);
            }

            if (float.IsInfinity(minX) || float.IsInfinity(minY) || float.IsInfinity(maxX) || float.IsInfinity(maxY))
            {
                return new RuntimeBounds(0f, 0f, 1f, 1f);
            }

            float width = Math.Max(1f, maxX - minX);
            float height = Math.Max(1f, maxY - minY);
            return new RuntimeBounds(minX + width * 0.5f, minY + height * 0.5f, width, height);
        }

        private static void AddPoint(Matrix2x3 transform, float x, float y, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float tx = transform.m00 * x + transform.m01 * y + transform.m02;
            float ty = transform.m10 * x + transform.m11 * y + transform.m12;
            minX = Math.Min(minX, tx);
            minY = Math.Min(minY, ty);
            maxX = Math.Max(maxX, tx);
            maxY = Math.Max(maxY, ty);
        }

        private static void AddHash(Dictionary<int, string> hashes, int hash)
        {
            if (hash == 0 || hashes.ContainsKey(hash))
            {
                return;
            }

            string value = HashCache.Get().Get(hash);
            if (!string.IsNullOrEmpty(value))
            {
                hashes[hash] = value;
            }
        }

        private static void WriteHashes(BinaryWriter writer, Dictionary<int, string> hashes)
        {
            writer.Write(hashes.Count);
            foreach (var pair in hashes)
            {
                writer.Write(pair.Key);
                writer.WriteKleiString(pair.Value);
            }
        }

        private static void WriteKleiString(this BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private struct RuntimeBounds
        {
            public RuntimeBounds(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
        }
    }
}
