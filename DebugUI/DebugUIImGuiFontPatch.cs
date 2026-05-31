using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ImGuiNET;
using UnityEngine;

namespace DebugUI
{
    [HarmonyPatch]
    internal static class DebugUIImGuiFontPatch
    {
        private static readonly FieldInfo FontTextureIdField = AccessTools.Field("ImGuiRenderer:fontTextureID");
        private static readonly FieldInfo TextureBlahField = AccessTools.Field("ImGuiRenderer:texture_blah");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("ImGuiRenderer:BuildFontAtlas");
        }

        private static bool Prefix(object __instance)
        {
            string chineseFontPath = FindChineseFontPath();
            if (string.IsNullOrEmpty(chineseFontPath))
            {
                Debug.LogWarning("[DebugUI] No CJK font found. ImGui Chinese text may render as question marks.");
                return true;
            }

            try
            {
                BuildFontAtlasWithChineseGlyphs(__instance, chineseFontPath);
                Debug.Log("[DebugUI] ImGui Chinese font loaded: " + chineseFontPath);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DebugUI] Failed to rebuild ImGui font atlas with Chinese glyphs: " + e);
                return true;
            }
        }

        private static unsafe void BuildFontAtlasWithChineseGlyphs(object renderer, string chineseFontPath)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            int idealFontSize = GetIdealFontSize();
            int smallFontSize = Mathf.CeilToInt(idealFontSize * 0.75f);
            int largeFontSize = Mathf.CeilToInt(idealFontSize * 1.5f);
            string oniFontPath = Path.Combine(Application.streamingAssetsPath, "fonts", "NotoSansUI-Regular.ttf");
            IntPtr chineseRanges = io.Fonts.GetGlyphRangesChineseSimplifiedCommon();

            AddFontWithChineseFallback(io, oniFontPath, chineseFontPath, smallFontSize, chineseRanges);
            AddFontWithChineseFallback(io, oniFontPath, chineseFontPath, idealFontSize, chineseRanges);
            AddFontWithChineseFallback(io, oniFontPath, chineseFontPath, largeFontSize, chineseRanges);

            byte* pixels;
            int width;
            int height;
            int bytesPerPixel;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);

            Texture2D fontTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            fontTexture.LoadRawTextureData((IntPtr)pixels, width * height * bytesPerPixel);
            fontTexture.Apply();
            TextureBlahField?.SetValue(renderer, fontTexture);

            object oldTextureId = FontTextureIdField?.GetValue(renderer);
            if (oldTextureId is IntPtr oldPtr)
            {
                renderer.GetType().GetMethod("UnbindTexture")?.Invoke(renderer, new object[] { oldPtr });
            }

            IntPtr textureId = (IntPtr)renderer.GetType().GetMethod("BindTexture")?.Invoke(renderer, new object[] { fontTexture, false });
            FontTextureIdField?.SetValue(renderer, (IntPtr?)textureId);
            io.Fonts.SetTexID(textureId);
            io.Fonts.ClearTexData();
        }

        private static unsafe void AddFontWithChineseFallback(ImGuiIOPtr io, string oniFontPath, string chineseFontPath, int fontSize, IntPtr chineseRanges)
        {
            io.Fonts.AddFontFromFileTTF(oniFontPath, fontSize);

            ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
            config.MergeMode = true;
            config.PixelSnapH = true;
            config.GlyphRanges = chineseRanges;
            io.Fonts.AddFontFromFileTTF(chineseFontPath, fontSize, config, chineseRanges);
            config.Destroy();
        }

        private static int GetIdealFontSize()
        {
            MethodInfo method = AccessTools.Method("ImGuiRenderer:GetIdealFontSize");
            if (method == null)
            {
                return 16;
            }

            return (int)method.Invoke(null, null);
        }

        private static string FindChineseFontPath()
        {
            string[] candidates =
            {
                @"C:\Windows\Fonts\msyh.ttc",
                @"C:\Windows\Fonts\simhei.ttf",
                @"C:\Windows\Fonts\simsun.ttc",
                @"C:\Windows\Fonts\NotoSansCJK-Regular.ttc",
                @"C:\Windows\Fonts\方正粗黑宋简体.ttf"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            string fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (Directory.Exists(fontsDirectory))
            {
                string[] matches = Directory.GetFiles(fontsDirectory, "*.ttf");
                for (int i = 0; i < matches.Length; i++)
                {
                    string name = Path.GetFileName(matches[i]).ToLowerInvariant();
                    if (name.Contains("noto") || name.Contains("msyh") || name.Contains("simhei") || name.Contains("simsun"))
                    {
                        return matches[i];
                    }
                }
            }

            return null;
        }
    }
}
