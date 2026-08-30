using HarmonyLib;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 自定义元素：祈愿培养基（粉红液体）与祈愿雾气（沸腾产生的粉红气体）。
    /// 物理参数定义在 elements/custom_elements.yaml（游戏自动加载 mod 目录下的 elements 文件夹），
    /// 这里只负责挂上视觉：分别以水与蒸汽的 Substance 为底，把贴图染成粉红色。
    /// </summary>
    public static class AbyssElements
    {
        public const string TalismanCondensateId = "TalismanCondensate";
        public const string WishingVaporId = "WishingVapor";

        public static readonly SimHashes TalismanCondensate =
            (SimHashes)Hash.SDBMLower(TalismanCondensateId);

        public static readonly SimHashes WishingVapor =
            (SimHashes)Hash.SDBMLower(WishingVaporId);

        [HarmonyPatch(typeof(Assets), "SubstanceListHookup")]
        public static class SubstanceListHookup_Patch
        {
            public static void Postfix()
            {
                Color32 pink = new Color32(255, 160, 195, byte.MaxValue);
                // 液体保留一点水纹理细节；雾气用纯粉，保证一眼就是粉红色。
                HookupSubstance(TalismanCondensate, SimHashes.Water, pink, 0.82f);
                HookupSubstance(WishingVapor, SimHashes.Steam, pink, 1f);
            }

            private static void HookupSubstance(SimHashes targetHash, SimHashes sourceHash, Color32 tint, float tintStrength)
            {
                Element element = ElementLoader.FindElementByHash(targetHash);
                if (element == null || element.substance == null)
                {
                    Debug.LogWarning($"[MadeInAbyss] {targetHash} 未从 elements/custom_elements.yaml 加载");
                    return;
                }

                // 源解析多级回退：元素已挂接的 substance → 物质表 → 水。
                Substance water = ResolveSubstance(SimHashes.Water);
                Substance source = ResolveSubstance(sourceHash);
                if (source == null || source.material == null || source.anim == null)
                    source = water;
                if (source == null || source.material == null || source.anim == null)
                {
                    Debug.LogWarning($"[MadeInAbyss] {sourceHash}/{targetHash} 找不到可用 substance 视觉，回退为默认视觉");
                    return;
                }

                Material material = new Material(source.material) { name = "mat" + targetHash };
                Texture2D tinted = TintedTexture(source.material.mainTexture as Texture2D, tint, tintStrength);
                if (tinted != null)
                    material.mainTexture = tinted;

                element.substance.anim = source.anim;
                element.substance.material = material;
                element.substance.colour = tint;
                element.substance.uiColour = tint;
                element.substance.conduitColour = tint;

                KAnimFile[] anims = Traverse.Create(source).Field("anims").GetValue<KAnimFile[]>();
                if (anims != null && anims.Length > 0)
                    Traverse.Create(element.substance).Field("anims").SetValue(anims);
            }

            /// <summary>解析某元素的视觉 substance：优先元素已挂接的，其次物质表。</summary>
            private static Substance ResolveSubstance(SimHashes hash)
            {
                Element element = ElementLoader.FindElementByHash(hash);
                if (element != null && element.substance != null && element.substance.material != null && element.substance.anim != null)
                    return element.substance;
                if (Assets.instance != null && Assets.instance.substanceTable != null)
                    return Assets.instance.substanceTable.GetSubstance(hash);
                return null;
            }

            /// <summary>把源贴图向目标色偏移（1.0 = 纯色）；源贴图不可读时生成一张程序化纯色贴图兜底。</summary>
            private static Texture2D TintedTexture(Texture2D source, Color32 tint, float strength)
            {
                Texture2D result;
                try
                {
                    result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                    Color32[] pixels = source.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = (Color32)Color.Lerp(pixels[i], (Color)tint, strength);
                    result.SetPixels32(pixels);
                }
                catch (System.Exception)
                {
                    int size = 64;
                    result = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    Color32[] pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float shade = 1f - strength * 0.1f + strength * 0.1f * (((x * 7 + y * 13) % 16) / 16f);
                            pixels[y * size + x] = (Color32)((Color)tint * shade);
                        }
                    }
                    result.SetPixels32(pixels);
                }
                result.Apply();
                result.name = "texAbyssMist";
                return result;
            }
        }
    }
}
