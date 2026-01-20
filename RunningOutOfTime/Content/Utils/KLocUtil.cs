using System;
using System.IO;


namespace CykUtils
{
    public class Loc
    {
        /// <summary>
        /// 为指定类型注册并加载本地化字符串，并可选择生成翻译模板。
        /// </summary>
        /// <param name="root">要注册翻译的类型（通常是包含 LocString 字段的类）。</param>
        /// <param name="generateTemplate">是否生成翻译模板文件，默认 false。</param>
        public static void Translate(Type root, bool generateTemplate = false)
        {
            Localization.RegisterForTranslation(root);
            Loc.LoadStrings();
            LocString.CreateLocStringKeys(root, null);
            if (generateTemplate)
            {
                Localization.GenerateStringsTemplate(root, Path.Combine(KUtils.ModPath, "translations"));
            }
        }

        private static void LoadStrings()
        {
            Localization.Locale locale = Localization.GetLocale();
            string text = (locale != null) ? locale.Code : "en";
            if (text.IsNullOrWhiteSpace())
            {
                return;
            }
            string text2 = Path.Combine(KUtils.ModPath, "translations", text + ".po");
            if (File.Exists(text2))
            {
                Localization.OverloadStrings(Localization.LoadStringsFile(text2, false));
                LogUtil.Log("找到翻译文件: " + text + "." );
            }
        }
    }
}
