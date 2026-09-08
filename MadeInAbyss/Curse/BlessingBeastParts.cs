using Klei.AI;
using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 深渊祝福的兽化外观（跟随体方案）：祝福存在期间，在复制人身上吸附 1~3 个
    /// "动物部件"跟随体——独立的 KBatchedAnimController 播放小动物幼崽动画，
    /// 隐藏幼崽其余部位只留目标部件（眼睛/小腿），每帧吸附到复制人对应的 symbol 上。
    ///
    /// 不走 SymbolOverrideController 覆盖：覆盖只把目标 symbol 的帧映射到动物部件
    /// 的帧区间，而复制人动画仍按原时间线取帧，帧号超出动物部件帧数时原生端直接
    /// 丢帧不渲染，导致"切换动画时部件时有时无"。跟随体用自己的动画和帧池，稳定
    /// 可见，还能保留动物部件自带的动画（眨眼、摆动）。
    ///
    /// 部件选择由随机种子驱动并随存档序列化，读档后按种子重建同样组合。
    /// </summary>
    public class BlessingBeastParts : KMonoBehaviour, ISim1000ms
    {
        /// <summary>兽化随机种子：决定挂哪几个部件。</summary>
        [Serialize]
        private int beastSeed;

        /// <summary>跟随体是否已生成（应用/未应用状态机，延迟到模拟节拍再动）。</summary>
        private bool applied;

        private readonly List<Follower> followers = new List<Follower>();

        /// <summary>祝福效果 ID（与 AbyssEffects 注册的一致）。</summary>
        private const string BlessingEffectId = "AbyssBlessing";

        private class BeastPart
        {
            /// <summary>部件显示名（日志用）。</summary>
            public string Label;

            /// <summary>来源动物 kanim（跟随体播放的动画）。</summary>
            public string Kanim;

            /// <summary>只显示该前缀的 symbol，幼崽其余部位全部隐藏。</summary>
            public string KeepPrefix;

            /// <summary>吸附到复制人身上的 symbol。</summary>
            public string AnchorSymbol;

            /// <summary>相对锚点的偏移（米，x 随复制人朝向翻转）。</summary>
            public Vector2 Offset;

            /// <summary>整体缩放。</summary>
            public float Scale;
        }

        // 可兽化的部件表：锚点 symbol 均已在复制人 build 中核实
        // （head_master_swap 的 head、body_comp_default 的 hand_paint/foot）。
        // 来源为动物幼崽的小部件；偏移与缩放是首版估值，进游戏看效果后在表里调。
        private static readonly BeastPart[] Parts =
        {
            new BeastPart
            {
                Label = "兽瞳",
                Kanim = "baby_puft_kanim",
                KeepPrefix = "alp_baby_eyes",
                AnchorSymbol = "head",
                Offset = new Vector2(0f, 0.02f),
                Scale = 1.15f,
            },
            new BeastPart
            {
                Label = "兽爪",
                Kanim = "baby_puft_kanim",
                KeepPrefix = "alp_baby_leg",
                AnchorSymbol = "hand_paint",
                Offset = new Vector2(0.02f, 0f),
                Scale = 0.9f,
            },
            new BeastPart
            {
                Label = "兽足",
                Kanim = "baby_hatch_kanim",
                KeepPrefix = "front_leg",
                AnchorSymbol = "foot",
                Offset = new Vector2(0f, 0f),
                Scale = 0.9f,
            },
        };

        private class Follower
        {
            public GameObject GameObject;
            public BeastPart Part;
        }

        /// <summary>授予祝福时调用：掷一个新的兽化种子；跟随体在下个模拟节拍生成。</summary>
        public void RandomizeAndApply()
        {
            beastSeed = UnityEngine.Random.Range(1, int.MaxValue);
            applied = false;
        }

        public void Sim1000ms(float dt)
        {
            Effects effects = GetComponent<Effects>();
            bool blessingActive = effects != null && effects.Get(BlessingEffectId) != null;

            if (blessingActive && !applied)
                Apply();
            else if (!blessingActive && applied)
                RemoveAndDie();
            else if (!blessingActive && beastSeed == 0)
                Destroy(this); // 从未兽化过（异常路径），自清理
        }

        private void Apply()
        {
            applied = true;
            // 种子决定启用 1~3 个部件；System.Random(种子) 保证读档后组合一致。
            System.Random rng = new System.Random(beastSeed);
            List<BeastPart> pool = new List<BeastPart>(Parts);
            int count = Mathf.Min(pool.Count, 1 + rng.Next(3));

            while (count-- > 0 && pool.Count > 0)
            {
                int idx = rng.Next(pool.Count);
                BeastPart part = pool[idx];
                pool.RemoveAt(idx);
                SpawnFollower(part);
            }
        }

        private void SpawnFollower(BeastPart part)
        {
            KAnimFile file = Assets.GetAnim(part.Kanim);
            KAnimFileData data = file != null ? file.GetData() : null;
            if (data == null || data.build == null)
            {
                Debug.LogWarning($"[MadeInAbyss] 兽化部件「{part.Label}」跳过：{part.Kanim} 加载失败");
                return;
            }

            // 用原版特效模板生成跟随体（FXHelpers 负责挂 KBatchedAnimController 并指定动画文件）。
            KBatchedAnimController kac = FXHelpers.CreateEffect(
                part.Kanim, transform.GetPosition(), null, false, Grid.SceneLayer.Front, true);
            kac.name = "MadeInAbyss_Beast_" + part.Label;
            kac.initialAnim = ResolveAnimName(data);
            kac.gameObject.SetActive(true);

            // 隐藏幼崽其余部位，只留目标部件。
            foreach (KAnim.Build.Symbol symbol in data.build.symbols)
            {
                if (!symbol.hash.IsValid())
                    continue;
                string name = symbol.hash.ToString();
                if (string.IsNullOrEmpty(name) || !name.StartsWith(part.KeepPrefix))
                    kac.SetSymbolVisiblity(symbol.hash, false);
            }

            followers.Add(new Follower { GameObject = kac.gameObject, Part = part });
            Debug.Log($"[MadeInAbyss] 深渊祝福兽化：{part.Label}（{part.Kanim} → 吸附 {part.AnchorSymbol}，动画 {kac.initialAnim}）");
        }

        protected override void OnCleanUp()
        {
            foreach (Follower f in followers)
                if (f.GameObject != null)
                    Destroy(f.GameObject);
            followers.Clear();
            base.OnCleanUp();
        }

        /// <summary>每帧把跟随体吸附到复制人对应 symbol 的世界位置上。</summary>
        private void LateUpdate()
        {
            if (followers.Count == 0)
                return;
            KBatchedAnimController dupeAnim = GetComponent<KBatchedAnimController>();
            if (dupeAnim == null)
                return;

            for (int i = 0; i < followers.Count; i++)
            {
                Follower f = followers[i];
                if (f.GameObject == null)
                    continue;

                bool visible;
                Matrix4x4 m = dupeAnim.GetSymbolTransform(f.Part.AnchorSymbol, out visible);
                if (!visible)
                {
                    if (f.GameObject.activeSelf)
                        f.GameObject.SetActive(false);
                    continue;
                }
                if (!f.GameObject.activeSelf)
                    f.GameObject.SetActive(true);

                Vector3 pos = m.GetColumn(3);
                bool flip = m.GetColumn(0).x < 0f; // 复制人朝向（X 轴基向量翻转）
                f.GameObject.transform.SetPositionAndRotation(
                    pos + new Vector3(flip ? -f.Part.Offset.x : f.Part.Offset.x, f.Part.Offset.y, 0f),
                    Quaternion.identity);
                float sx = f.Part.Scale * (flip ? -1f : 1f);
                f.GameObject.transform.localScale = new Vector3(sx, f.Part.Scale, 1f);
            }
        }

        private void RemoveAndDie()
        {
            foreach (Follower f in followers)
                if (f.GameObject != null)
                    Destroy(f.GameObject);
            followers.Clear();
            Destroy(this);
        }

        /// <summary>优先用 "idle"，没有就退回 "ui"，再不行用 "idle" 兜底。</summary>
        private static string ResolveAnimName(KAnimFileData data)
        {
            if (data.GetAnim("idle") != null)
                return "idle";
            if (data.GetAnim("ui") != null)
                return "ui";
            return "idle";
        }
    }
}
