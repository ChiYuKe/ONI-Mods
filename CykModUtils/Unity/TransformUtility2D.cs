using UnityEngine;

namespace CykModUtils.Unity
{
    /// <summary>
    /// 面向 2D 场景的 Transform 读写辅助方法，只改 x/y 或 z 旋转，保留另一维度现有值。
    /// </summary>
    public static class TransformUtility2D
    {
        /// <summary>
        /// 获取对象世界坐标的 x/y。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="x">输出的 X 坐标。</param>
        /// <param name="y">输出的 Y 坐标。</param>
        /// <returns>对象和 Transform 有效时返回 true。</returns>
        public static bool TryGetPosition(GameObject target, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            x = transform.position.x;
            y = transform.position.y;
            return true;
        }

        /// <summary>
        /// 设置对象世界坐标的 x/y，并保留原 z 坐标。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="x">新的 X 坐标。</param>
        /// <param name="y">新的 Y 坐标。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetPosition(GameObject target, float x, float y)
        {
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            transform.position = new Vector3(x, y, transform.position.z);
            return true;
        }

        /// <summary>
        /// 获取对象的 z 轴欧拉角。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="rotationZ">输出的 z 轴角度。</param>
        /// <returns>对象和 Transform 有效时返回 true。</returns>
        public static bool TryGetRotationZ(GameObject target, out float rotationZ)
        {
            rotationZ = 0f;
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            rotationZ = transform.eulerAngles.z;
            return true;
        }

        /// <summary>
        /// 设置对象的 z 轴欧拉角，并保留 x/y 轴角度。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="rotationZ">新的 z 轴角度。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetRotationZ(GameObject target, float rotationZ)
        {
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            Vector3 current = transform.eulerAngles;
            transform.eulerAngles = new Vector3(current.x, current.y, rotationZ);
            return true;
        }

        /// <summary>
        /// 获取对象本地缩放的 x/y。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="x">输出的 X 缩放。</param>
        /// <param name="y">输出的 Y 缩放。</param>
        /// <returns>对象和 Transform 有效时返回 true。</returns>
        public static bool TryGetScale(GameObject target, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            x = transform.localScale.x;
            y = transform.localScale.y;
            return true;
        }

        /// <summary>
        /// 设置对象本地缩放的 x/y，并保留原 z 缩放。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="x">新的 X 缩放。</param>
        /// <param name="y">新的 Y 缩放。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetScale(GameObject target, float x, float y)
        {
            if (!TryGetTransform(target, out Transform transform))
            {
                return false;
            }

            transform.localScale = new Vector3(x, y, transform.localScale.z);
            return true;
        }

        private static bool TryGetTransform(GameObject target, out Transform transform)
        {
            transform = target != null ? target.transform : null;
            return transform != null;
        }
    }
}
