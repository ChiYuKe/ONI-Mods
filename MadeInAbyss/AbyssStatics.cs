using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 深渊层阶与笛级的静态定义。
    /// </summary>
    public static class AbyssStatics
    {
        public class LayerDef
        {
            /// <summary>层阶序号（0 基，对应配置数组下标）。</summary>
            public readonly int Index;

            /// <summary>层阶名称（动漫中的六层，中文）。</summary>
            public readonly string Name;

            /// <summary>层阶英文名。</summary>
            public readonly string EnglishName;

            /// <summary>该层的诅咒效果 ID。</summary>
            public readonly string CurseEffectId;

            public LayerDef(int index, string name, string englishName, string curseEffectId)
            {
                Index = index;
                Name = name;
                EnglishName = englishName;
                CurseEffectId = curseEffectId;
            }
        }

        /// <summary>六层深渊定义，深度阈值取自配置 LayerDepthM。</summary>
        public static readonly LayerDef[] Layers =
        {
            new LayerDef(0, "阿比斯之渊", "Edge of the Abyss", "AbyssCurse1"),
            new LayerDef(1, "诱惑之森", "Forest of Temptation", "AbyssCurse2"),
            new LayerDef(2, "大断层", "Great Fault", "AbyssCurse3"),
            new LayerDef(3, "巨人之杯", "Goblets of Giants", "AbyssCurse4"),
            new LayerDef(4, "亡骸之海", "Sea of Corpses", "AbyssCurse5"),
            new LayerDef(5, "最终地", "Capital of the Unreturned", "AbyssCurse6"),
        };

        /// <summary>所有诅咒效果 ID（用于生骸免疫）。</summary>
        public static readonly string[] CurseEffectIds =
        {
            "AbyssCurse1", "AbyssCurse2", "AbyssCurse3", "AbyssCurse4", "AbyssCurse5", "AbyssCurse6",
        };

        /// <summary>各层诅咒施放时造成的一次性伤害（HP）；前三层不扣血。</summary>
        public static readonly float[] CurseInstantDamageHp = { 0f, 0f, 0f, 20f, 30f, 80f };

        /// <summary>最终地诅咒被弹药包挡下时受到的伤害（HP）。</summary>
        public const float FinalLayerPouchBlockedDamage = 30f;

        /// <summary>
        /// 生骸化时随机变成的小动物；当前游戏版本不存在的 prefab 会被自动跳过（兼容 DLC 差异）。
        /// </summary>
        public static readonly string[] NarehateCritterIds =
        {
            "Hatch", "HatchHard", "HatchVeggie", "HatchMeat", "HatchMetal",
            "Puft", "PuftOxylite", "PuftBleachstone", "ShoveVole", "Morb",
            "Pip",
        };

        public class WhistleDef
        {
            /// <summary>笛级序号（0 = 无笛级，1 = 赤笛 … 5 = 白笛）：笛级 i 需要抵达第 i 层，白笛为最高笛级（含最终地）。</summary>
            public readonly int Rank;

            public readonly string Name;

            /// <summary>晋升到该笛级时施加的永久效果 ID；赤笛为荣誉头衔，无效果。</summary>
            public readonly string EffectId;

            public WhistleDef(int rank, string name, string effectId)
            {
                Rank = rank;
                Name = name;
                EffectId = effectId;
            }
        }

        /// <summary>笛级定义（下标 = 笛级序号）。</summary>
        public static readonly WhistleDef[] Whistles =
        {
            new WhistleDef(0, "无", null),
            new WhistleDef(1, "赤笛", null),
            new WhistleDef(2, "苍笛", "AbyssWhistleBlue"),
            new WhistleDef(3, "月笛", "AbyssWhistleMoon"),
            new WhistleDef(4, "黑笛", "AbyssWhistleBlack"),
            new WhistleDef(5, "白笛", "AbyssWhistleWhite"),
        };

        /// <summary>生骸化的永久效果 ID。</summary>
        public const string NarehateEffectId = "AbyssNarehate";

        /// <summary>
        /// 计算指定世界六层深渊的深度阈值（米）：
        /// 以营地表锚点为地表、世界底部（y=0）为基准，按配置百分比等比例划分。
        /// 无打印舱的星球（扫描锚点）额外留出地表 10% 的浅层缓冲——
        /// 百分比表压缩到深度区间的 10%~100% 段内，避免一落地就是深渊。
        /// 该世界没有锚点时返回 null。
        /// </summary>
        public static float[] GetLayerThresholds(int worldId)
        {
            float surfaceY = AbyssAnchors.GetSurfaceY(worldId);
            if (float.IsNaN(surfaceY))
                return null;

            // 群星星球在世界网格中垂直堆叠，地表锚点的全局 Y 可能远大于星球高度；
            // 世界纵深必须按“锚点到本世界网格底边”的距离计算，否则阈值会落到世界之外。
            WorldContainer world = ClusterManager.Instance.GetWorld(worldId);
            if (world == null)
                return null;

            float maxDepth = Mathf.Max(1f, surfaceY - world.WorldOffset.y);
            float offset = AbyssAnchors.IsScannedAnchor(worldId) ? 0.10f : 0f;
            float[] percents = AbyssConfig.Instance.LayerDepthPercents;
            if (percents == null || percents.Length == 0)
                percents = new float[] { 20f, 35f, 50f, 65f, 80f, 95f };

            float[] thresholds = new float[Mathf.Min(percents.Length, Layers.Length)];
            for (int i = 0; i < thresholds.Length; i++)
                thresholds[i] = maxDepth * Mathf.Clamp(offset + (1f - offset) * Mathf.Clamp(percents[i], 0f, 100f) / 100f, 0f, 1f);
            return thresholds;
        }

        /// <summary>
        /// 计算给定深度（米）在一组阈值下所抵达的最深层阶；未进入第 1 层时返回 -1。
        /// </summary>
        public static int GetLayerIndex(float depthM, float[] thresholds)
        {
            if (thresholds == null)
                return -1;
            int deepest = -1;
            for (int i = 0; i < thresholds.Length && i < Layers.Length; i++)
            {
                if (depthM >= thresholds[i])
                    deepest = i;
            }
            return deepest;
        }

        /// <summary>
        /// 计算给定世界、给定深度（米）所抵达的最深层阶；未进入第 1 层时返回 -1。
        /// </summary>
        public static int GetLayerIndex(int worldId, float depthM)
        {
            return GetLayerIndex(depthM, GetLayerThresholds(worldId));
        }

        /// <summary>
        /// 计算给定世界、给定历史最深深度（米）对应的笛级序号；未进入第 1 层时返回 0。
        /// 赤笛对应第 1 层、白笛对应第 5 层；抵达第 6 层「最终地」不再晋升更高笛级。
        /// </summary>
        public static int GetWhistleRank(int worldId, float deepestDepthM)
        {
            int layer = GetLayerIndex(worldId, deepestDepthM);
            if (layer < 0)
                return 0;
            return Mathf.Min(layer + 1, Whistles.Length - 1);
        }
    }
}
