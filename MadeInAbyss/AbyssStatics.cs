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

            /// <summary>层阶名称（动漫中的六层）。</summary>
            public readonly string Name;

            /// <summary>该层的诅咒效果 ID。</summary>
            public readonly string CurseEffectId;

            public LayerDef(int index, string name, string curseEffectId)
            {
                Index = index;
                Name = name;
                CurseEffectId = curseEffectId;
            }
        }

        /// <summary>六层深渊定义，深度阈值取自配置 LayerDepthM。</summary>
        public static readonly LayerDef[] Layers =
        {
            new LayerDef(0, "阿比斯之渊", "AbyssCurse1"),
            new LayerDef(1, "诱惑之森", "AbyssCurse2"),
            new LayerDef(2, "大断层", "AbyssCurse3"),
            new LayerDef(3, "巨人之杯", "AbyssCurse4"),
            new LayerDef(4, "亡骸之海", "AbyssCurse5"),
            new LayerDef(5, "最终地", "AbyssCurse6"),
        };

        /// <summary>所有诅咒效果 ID（用于生骸免疫）。</summary>
        public static readonly string[] CurseEffectIds =
        {
            "AbyssCurse1", "AbyssCurse2", "AbyssCurse3", "AbyssCurse4", "AbyssCurse5", "AbyssCurse6",
        };

        /// <summary>各层诅咒施放时造成的一次性伤害（HP）；前三层不扣血。</summary>
        public static readonly float[] CurseInstantDamageHp = { 0f, 0f, 0f, 20f, 30f, 50f };

        public class WhistleDef
        {
            /// <summary>笛级序号（0 = 无笛级，1 = 红笛 … 5 = 白笛）：笛级 i 需要抵达第 i 层，白笛为最高笛级（含最终地）。</summary>
            public readonly int Rank;

            public readonly string Name;

            /// <summary>晋升到该笛级时施加的永久效果 ID；红笛为荣誉头衔，无效果。</summary>
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
            new WhistleDef(1, "红笛", null),
            new WhistleDef(2, "蓝笛", "AbyssWhistleBlue"),
            new WhistleDef(3, "苍笛", "AbyssWhistleMoon"),
            new WhistleDef(4, "黑笛", "AbyssWhistleBlack"),
            new WhistleDef(5, "白笛", "AbyssWhistleWhite"),
        };

        /// <summary>生骸化的永久效果 ID。</summary>
        public const string NarehateEffectId = "AbyssNarehate";

        /// <summary>
        /// 计算给定深度（米）所抵达的最深层阶；未进入第 1 层时返回 -1。
        /// </summary>
        public static int GetLayerIndex(float depthM)
        {
            float[] thresholds = AbyssConfig.Instance.LayerDepthM;
            int deepest = -1;
            for (int i = 0; i < thresholds.Length && i < Layers.Length; i++)
            {
                if (depthM >= thresholds[i])
                    deepest = i;
            }
            return deepest;
        }

        /// <summary>
        /// 计算给定历史最深深度（米）对应的笛级序号；未进入第 1 层时返回 0。
        /// 红笛对应第 1 层、白笛对应第 5 层；抵达第 6 层「最终地」不再晋升更高笛级。
        /// </summary>
        public static int GetWhistleRank(float deepestDepthM)
        {
            int layer = GetLayerIndex(deepestDepthM);
            if (layer < 0)
                return 0;
            return Mathf.Min(layer + 1, Whistles.Length - 1);
        }
    }
}
