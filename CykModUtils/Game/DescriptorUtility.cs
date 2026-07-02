using System.Collections.Generic;

namespace CykModUtils.Game
{
    /// <summary>
    /// 创建和整理 ONI Descriptor 的辅助方法。
    /// </summary>
    public static class DescriptorUtility
    {
        /// <summary>
        /// 创建一个 Descriptor。
        /// </summary>
        /// <param name="text">显示文本。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <param name="type">描述类型。</param>
        /// <param name="indent">缩进层级。</param>
        /// <param name="onlyForSimpleInfoScreen">是否只显示在 SimpleInfoScreen。</param>
        /// <returns>创建好的 Descriptor。</returns>
        public static Descriptor Create(
            string text,
            string tooltip,
            Descriptor.DescriptorType type = Descriptor.DescriptorType.Effect,
            int indent = 0,
            bool onlyForSimpleInfoScreen = false)
        {
            var descriptor = new Descriptor(text ?? string.Empty, tooltip ?? string.Empty, type, onlyForSimpleInfoScreen);
            for (int i = 0; i < indent; i++)
            {
                descriptor.IncreaseIndent();
            }

            return descriptor;
        }

        /// <summary>
        /// 创建 Effect 类型 Descriptor。
        /// </summary>
        /// <param name="text">显示文本。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <param name="indent">缩进层级。</param>
        /// <returns>Effect Descriptor。</returns>
        public static Descriptor Effect(string text, string tooltip, int indent = 0)
        {
            return Create(text, tooltip, Descriptor.DescriptorType.Effect, indent);
        }

        /// <summary>
        /// 创建 Requirement 类型 Descriptor。
        /// </summary>
        /// <param name="text">显示文本。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <param name="indent">缩进层级。</param>
        /// <returns>Requirement Descriptor。</returns>
        public static Descriptor Requirement(string text, string tooltip, int indent = 0)
        {
            return Create(text, tooltip, Descriptor.DescriptorType.Requirement, indent);
        }

        /// <summary>
        /// 创建 Detail 类型 Descriptor。
        /// </summary>
        /// <param name="text">显示文本。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <param name="indent">缩进层级。</param>
        /// <returns>Detail Descriptor。</returns>
        public static Descriptor Detail(string text, string tooltip, int indent = 0)
        {
            return Create(text, tooltip, Descriptor.DescriptorType.Detail, indent);
        }

        /// <summary>
        /// 向列表追加 Descriptor，并在列表为空时自动创建。
        /// </summary>
        /// <param name="descriptors">目标列表。</param>
        /// <param name="descriptor">要追加的 Descriptor。</param>
        /// <returns>包含新项的列表。</returns>
        public static List<Descriptor> Add(List<Descriptor> descriptors, Descriptor descriptor)
        {
            descriptors = descriptors ?? new List<Descriptor>();
            descriptors.Add(descriptor);
            return descriptors;
        }
    }
}
