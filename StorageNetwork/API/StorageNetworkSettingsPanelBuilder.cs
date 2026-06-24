using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络设置面板构建器。附属模组通过这个对象添加统一风格的设置面板内容。
    /// </summary>
    public sealed class StorageNetworkSettingsPanelBuilder
    {
        private readonly System.Action<string> setTitle;
        private readonly System.Func<string, string, float, GameObject> addCard;
        private readonly System.Func<Transform, string, string, float, GameObject> addCardToParent;
        private readonly System.Func<string, float, float, Transform> addRootHorizontalGroup;
        private readonly System.Func<Transform, string, float, float, Transform> addHorizontalGroup;
        private readonly System.Action<Transform, string, string, Color> addMetricTile;
        private readonly System.Action<Transform, string, Color> addStatusStrip;
        private readonly System.Action<Transform, string, string> addReadOnlyRow;
        private readonly System.Action<Transform, string> addHeading;
        private readonly System.Action<Transform, string> addText;
        private readonly System.Action<Transform, string, string, System.Action> addButton;
        private readonly System.Action<Transform, string, string, System.Action, bool> addToggleButton;

        internal StorageNetworkSettingsPanelBuilder(
            System.Action<string> setTitle,
            System.Func<string, string, float, GameObject> addCard,
            System.Func<Transform, string, string, float, GameObject> addCardToParent,
            System.Func<string, float, float, Transform> addRootHorizontalGroup,
            System.Func<Transform, string, float, float, Transform> addHorizontalGroup,
            System.Action<Transform, string, string, Color> addMetricTile,
            System.Action<Transform, string, Color> addStatusStrip,
            System.Action<Transform, string, string> addReadOnlyRow,
            System.Action<Transform, string> addHeading,
            System.Action<Transform, string> addText,
            System.Action<Transform, string, string, System.Action> addButton,
            System.Action<Transform, string, string, System.Action, bool> addToggleButton)
        {
            this.setTitle = setTitle;
            this.addCard = addCard;
            this.addCardToParent = addCardToParent;
            this.addRootHorizontalGroup = addRootHorizontalGroup;
            this.addHorizontalGroup = addHorizontalGroup;
            this.addMetricTile = addMetricTile;
            this.addStatusStrip = addStatusStrip;
            this.addReadOnlyRow = addReadOnlyRow;
            this.addHeading = addHeading;
            this.addText = addText;
            this.addButton = addButton;
            this.addToggleButton = addToggleButton;
        }

        /// <summary>
        /// 设置窗口标题栏文本。
        /// </summary>
        public void SetTitle(string title)
        {
            setTitle?.Invoke(title);
        }

        /// <summary>
        /// 添加一张设置卡片，并返回卡片 Transform，后续行可以添加到这个卡片中。
        /// </summary>
        public Transform AddCard(string title, float preferredHeight = 0f)
        {
            GameObject card = addCard?.Invoke("AddonSettingsCard", title, preferredHeight);
            return card != null ? card.transform : null;
        }

        /// <summary>
        /// 在指定父级下添加一张设置卡片。常用于横向组中创建并排的设置卡片。
        /// </summary>
        public Transform AddCard(Transform parent, string title, float preferredHeight = 0f)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject card = addCardToParent?.Invoke(parent, "AddonSettingsCard", title, preferredHeight);
            return card != null ? card.transform : null;
        }

        /// <summary>
        /// 在面板根内容区添加一个横向布局组。可用于放置两张并排的设置卡片。
        /// </summary>
        public Transform AddHorizontalGroup(float preferredHeight = 0f, float spacing = 8f)
        {
            return addRootHorizontalGroup?.Invoke("AddonHorizontalGroup", preferredHeight, spacing);
        }

        /// <summary>
        /// 添加一个横向布局组。可用于放置指标块，或放置两张并排的设置卡片。
        /// </summary>
        public Transform AddHorizontalGroup(Transform parent, float preferredHeight = 0f, float spacing = 8f)
        {
            if (parent == null)
            {
                return null;
            }

            return addHorizontalGroup?.Invoke(parent, "AddonHorizontalGroup", preferredHeight, spacing);
        }

        /// <summary>
        /// 添加一个指标块。
        /// </summary>
        public void AddMetricTile(Transform parent, string label, string value)
        {
            AddMetricTile(parent, label, value, StorageNetworkPanelPalette.MetricAccent);
        }

        /// <summary>
        /// 添加一个指标块，并指定指标值的强调色。
        /// </summary>
        public void AddMetricTile(Transform parent, string label, string value, Color accent)
        {
            if (parent == null)
            {
                return;
            }

            addMetricTile?.Invoke(parent, label, value, accent);
        }

        /// <summary>
        /// 添加一条状态横幅。适合显示“启用 / 停用”、“自动 / 手动”等状态。
        /// </summary>
        public void AddStatusStrip(Transform parent, string text, Color color)
        {
            addStatusStrip?.Invoke(parent, text, color);
        }

        /// <summary>
        /// 添加一行只读键值信息。
        /// </summary>
        public void AddReadOnlyRow(Transform parent, string label, string value)
        {
            addReadOnlyRow?.Invoke(parent, label, value);
        }

        /// <summary>
        /// 添加醒目的标题文本。适合在卡片内显示建筑名、设备名或当前目标。
        /// </summary>
        public void AddHeading(Transform parent, string text)
        {
            addHeading?.Invoke(parent, text);
        }

        /// <summary>
        /// 添加说明文本。
        /// </summary>
        public void AddText(Transform parent, string text)
        {
            addText?.Invoke(parent, text);
        }

        /// <summary>
        /// 添加一个操作按钮。
        /// </summary>
        public void AddButton(Transform parent, string label, string buttonText, System.Action onClick)
        {
            addButton?.Invoke(parent, label, buttonText, onClick);
        }

        /// <summary>
        /// 添加一个开关按钮行。按钮会根据当前启用状态使用统一的开启/关闭样式。
        /// </summary>
        public void AddToggleButton(Transform parent, string label, string buttonText, System.Action onClick, bool currentlyEnabled)
        {
            addToggleButton?.Invoke(parent, label, buttonText, onClick, currentlyEnabled);
        }
    }
}
