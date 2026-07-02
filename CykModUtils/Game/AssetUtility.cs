using System.Collections.Generic;
using UnityEngine;

namespace CykModUtils.Game
{
    /// <summary>
    /// 对 ONI Assets 常用查询入口的安全包装。
    /// </summary>
    public static class AssetUtility
    {
        /// <summary>
        /// 尝试根据建筑 prefab ID 获取 BuildingDef。
        /// </summary>
        /// <param name="prefabId">建筑 prefab ID，例如 OxygenDiffuser。</param>
        /// <param name="def">找到的建筑定义。</param>
        /// <returns>找到定义时返回 true。</returns>
        public static bool TryGetBuildingDef(string prefabId, out BuildingDef def)
        {
            def = string.IsNullOrWhiteSpace(prefabId) ? null : Assets.GetBuildingDef(prefabId);
            return def != null;
        }

        /// <summary>
        /// 根据 Tag 尝试获取 prefab，不存在时不会输出游戏原生 warning。
        /// </summary>
        /// <param name="tag">目标 prefab tag。</param>
        /// <returns>找到的 prefab；不存在时返回 null。</returns>
        public static GameObject TryGetPrefab(Tag tag)
        {
            return tag.IsValid ? Assets.TryGetPrefab(tag) : null;
        }

        /// <summary>
        /// 根据字符串 ID 尝试获取 prefab。
        /// </summary>
        /// <param name="prefabId">prefab ID。</param>
        /// <returns>找到的 prefab；不存在时返回 null。</returns>
        public static GameObject TryGetPrefab(string prefabId)
        {
            return string.IsNullOrWhiteSpace(prefabId) ? null : TryGetPrefab(new Tag(prefabId));
        }

        /// <summary>
        /// 尝试获取动画文件。
        /// </summary>
        /// <param name="animName">动画资源名，例如 "oxygen_generator_kanim"。</param>
        /// <param name="anim">找到的 KAnimFile。</param>
        /// <returns>找到动画时返回 true。</returns>
        public static bool TryGetAnim(string animName, out KAnimFile anim)
        {
            anim = null;
            return !string.IsNullOrWhiteSpace(animName) && Assets.TryGetAnim(animName, out anim);
        }

        /// <summary>
        /// 尝试获取 Sprite。
        /// </summary>
        /// <param name="spriteName">Sprite 名称。</param>
        /// <returns>找到的 Sprite；不存在时返回 null。</returns>
        public static Sprite TryGetSprite(string spriteName)
        {
            return string.IsNullOrWhiteSpace(spriteName) ? null : Assets.GetSprite(spriteName);
        }

        /// <summary>
        /// 获取所有带有指定组件的 prefab。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>prefab 列表；Assets 尚未初始化时返回空列表。</returns>
        public static List<GameObject> GetPrefabsWithComponent<T>()
        {
            return Assets.Prefabs == null ? new List<GameObject>() : Assets.GetPrefabsWithComponent<T>();
        }

        /// <summary>
        /// 获取所有带有指定 Tag 的 prefab。
        /// </summary>
        /// <param name="tag">目标 Tag。</param>
        /// <returns>prefab 列表。</returns>
        public static List<GameObject> GetPrefabsWithTag(Tag tag)
        {
            return tag.IsValid ? Assets.GetPrefabsWithTag(tag) : new List<GameObject>();
        }
    }
}
