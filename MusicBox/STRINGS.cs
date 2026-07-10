namespace MusicBox
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class MUSICBOX
                {
                    public static LocString NAME = "音乐盒";
                    public static LocString DESC = "一个根据信号播放钢琴音符的小型音乐盒。";
                    public static LocString EFFECT = "信号值 1-12 对应 12 个半音:\n"
                        + "• 0: 休止符\n"
                        + "• 1: C   • 2: C#\n"
                        + "• 3: D   • 4: D#\n"
                        + "• 5: E   • 6: F\n"
                        + "• 7: F#  • 8: G\n"
                        + "• 9: G#  • 10: A\n"
                        + "• 11: A# • 12: B";

                    public static LocString LOGIC_PORT = "音符信号输入";
                    public static LocString LOGIC_PORT_ACTIVE = "接收音符信号：" + STRINGS.UI.FormatAsHotkey("绿色") + " = 播放音符";
                    public static LocString LOGIC_PORT_INACTIVE = "接收音符信号：" + STRINGS.UI.FormatAsHotkey("红色") + " = 休止／不播放";
                }
            }
        }

        public class UI
        {
            public class UISIDESCREENS
            {
                public class MUSICBOX
                {
                    public static LocString VOLUME_TITLE = "音乐盒音量";
                    public static LocString VOLUME_TOOLTIP = "调整音乐盒播放音量";
                }
            }

            public static string FormatAsHotkey(string text)
            {
                return "<b><color=#F44A4A>" + text + "</b></color>";
            }
        }
    }
}
