namespace TestMod
{
    public sealed class BaseScreenOptions : UiPrefabWindowOptions
    {
        public BaseScreenOptions(string title = "AB UI 工具包")
            : base(
                title,
                new[] { "cyk_ab" },
                new[]
                {
                    "Base_Screen_820x664.prefab"
                })
        {
            Id = "cyk_base_screen";
        }

        public new BaseScreenOptions SetId(string id)
        {
            base.SetId(id);
            return this;
        }

        public new BaseScreenOptions SetTitle(string title)
        {
            base.SetTitle(title);
            return this;
        }

        public new BaseScreenOptions SetSize(float width, float height)
        {
            base.SetSize(width, height);
            return this;
        }

        public new BaseScreenOptions SetCentered(bool centered = true)
        {
            base.SetCentered(centered);
            return this;
        }

        public new BaseScreenOptions SetAutoBindClose(bool autoBindClose = true)
        {
            base.SetAutoBindClose(autoBindClose);
            return this;
        }
    }
}
