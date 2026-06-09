using System.Collections.Generic;
using PeterHan.PLib.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RunningOutOfTime.Content.UI
{
    public class InheritanceReportScreen : KScreen
    {
        public static InheritanceReportScreen ScreenInstance;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ConsumeMouseScroll = true;
        }

        public static GameObject Createpanel(
            List<(string attrName, int oldLevel, int newLevel)> attrList,
            List<(string attrName, int oldLevel, int newLevel)> skillList,
            List<(string attrName, int oldLevel, int newLevel)> traitList)
        {
            CustomStyles.Init();

            // 外层容器
            PPanel root = new PPanel("InheritanceInfo")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                BackImage = PUITuning.Images.GetSpriteByName("web_box"),
                BackColor = new Color(0.7882f, 0.7882f, 0.7882f, 1f),
                ImageMode = Image.Type.Sliced,
                FlexSize = Vector2.one
            };

            // 顶部
            var top = new PPanel("Top")
            {
                Direction = PanelDirection.Horizontal,
                Alignment = TextAnchor.MiddleCenter,
                FlexSize = new Vector2(1, 0),
                BackImage = PUITuning.Images.GetSpriteByName("web_box"),
                BackColor = PUITuning.Colors.ButtonPinkStyle.inactiveColor,
                ImageMode = Image.Type.Sliced,
            }.AddChild(new PLabel("TopLabel")
            {
                Text = Config.STRINGS.UI.INHERITANCEINFORMATION.TOPTEXT,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                Margin = new RectOffset(5, 5, 5, 5)
            });

            // 中间内容
            var middleContent = new PPanel("MiddleContent")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 10,
                FlexSize = new Vector2(1, 0)
            };

            middleContent.AddChild(CreateAttributeBlock(attrList));
            middleContent.AddChild(CreateSkillBlock(skillList));
            middleContent.AddChild(CreateTraitBlock(traitList));

            var middle = new PScrollPane("MiddleScroll")
            {
                Child = middleContent,
                ScrollVertical = true,
                ScrollHorizontal = false,
                AlwaysShowVertical = true,
                FlexSize = new Vector2(1, 1),
                TrackSize = 8f,
                BackColor = Color.white
            };

            // 底部确认按钮
            var bottom = new PPanel("Bottom")
            {
                Direction = PanelDirection.Horizontal,
                Alignment = TextAnchor.MiddleCenter,
                FlexSize = new Vector2(1, 0),
            }.AddChild(new PButton("OkButton")
            {
                Text = Config.STRINGS.UI.INHERITANCEINFORMATION.BUTTONTEXT,
                FlexSize = new Vector2(1, 0),
                OnClick = (go) => ScreenInstance?.Close() // 使用实例方法关闭
            });

            root.AddChild(top).AddChild(middle).AddChild(bottom);

            // 构建对象
            GameObject go = root.Build();

            // 关联实例并赋值给 ScreenInstance
            ScreenInstance = go.AddComponent<InheritanceReportScreen>();
            ScreenInstance.Activate();

            return go;
        }

        private static PPanel CreateAttributeBlock(List<(string attrName, int oldLevel, int newLevel)> attrList)
        {
            var wrapper = new PPanel("AttrWrapper")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                FlexSize = new Vector2(1, 0),
                Margin = new RectOffset(5, 10, 5, 5),
                Spacing = 4,
                BackImage = PUITuning.Images.GetSpriteByName("web_box"),
                ImageMode = Image.Type.Sliced,
                BackColor = new Color(0.24f, 0.3f, 0.4072f, 1f)
            };

            wrapper.AddChild(new PLabel("AttrTitle")
            {
                Text = Config.STRINGS.UI.INHERITANCEINFORMATION.ATTRIBUTESTITLE.NAME,
                TextStyle = CustomStyles.BigLightStyle,
                ToolTip = Config.STRINGS.UI.INHERITANCEINFORMATION.ATTRIBUTESTITLE.DESC,
                TextAlignment = TextAnchor.UpperCenter,
                Margin = new RectOffset(2, 2, 2, 4)
            });

            var content = new PPanel("AttrContent")
            {
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperLeft,
                FlexSize = new Vector2(1, 0),
                Spacing = 2
            };

            int index = 0;
            foreach (var (attrName, oldLevel, newLevel) in attrList)
            {
                var card = new PPanel($"AttrCard_{index}")
                {
                    Direction = PanelDirection.Horizontal,
                    Alignment = TextAnchor.MiddleCenter,
                    Spacing = 10,
                    FlexSize = new Vector2(1, 0),
                    BackImage = PUITuning.Images.GetSpriteByName("web_rounded"),
                    ImageMode = Image.Type.Sliced,
                    BackColor = new Color(0.75f, 0.75f, 0.75f, 1f),
                    Margin = new RectOffset(5, 5, 2, 2)
                }
                .AddChild(new PLabel($"Name_{index}") { Text = attrName, TextStyle = PUITuning.Fonts.TextDarkStyle, FlexSize = new Vector2(1, 0), TextAlignment = TextAnchor.MiddleLeft })
                .AddChild(new PLabel($"Old_{index}") { Text = $"Lv.{oldLevel}", TextStyle = PUITuning.Fonts.TextLightStyle })
                .AddChild(new PLabel($"Arrow_{index}") { Text = "→", TextStyle = newLevel > oldLevel ? CustomStyles.GreenText : PUITuning.Fonts.TextDarkStyle })
                .AddChild(new PLabel($"New_{index}") { Text = $"Lv.{newLevel}", TextStyle = newLevel > oldLevel ? CustomStyles.GreenText : PUITuning.Fonts.TextDarkStyle });

                content.AddChild(card);
                index++;
            }

            wrapper.AddChild(content);
            return wrapper;
        }

        private static PPanel CreateSkillBlock(List<(string attrName, int oldLevel, int newLevel)> skillList)
        {
            return new PPanel("SkillWrapper");
        }

        private static PPanel CreateTraitBlock(List<(string attrName, int oldLevel, int newLevel)> traitList)
        {
            return new PPanel("TraitWrapper");
        }

        public void Close()
        {
            ScreenInstance = null;
            Object.Destroy(this.gameObject);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.TryConsume(global::Action.Escape)) Close();
            base.OnKeyDown(e);
        }
    }

    public static class CustomStyles
    {
        public static TextStyleSetting GreenText;
        public static TextStyleSetting BigLightStyle;

        public static void Init()
        {
            if (GreenText != null) return;
            GreenText = PUITuning.Fonts.TextDarkStyle.DeriveStyle(0, Color.yellow);
            BigLightStyle = PUITuning.Fonts.TextLightStyle.DeriveStyle(size: 20);
        }
    }
}