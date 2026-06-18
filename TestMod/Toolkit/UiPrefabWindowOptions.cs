using System.Collections.Generic;
using UnityEngine;

namespace TestMod
{
    public class UiPrefabWindowOptions
    {
        public UiPrefabWindowOptions(string title, IReadOnlyList<string> bundleNames, IReadOnlyList<string> prefabNames)
        {
            Title = title;
            BundleNames = bundleNames;
            PrefabNames = prefabNames;
        }

        public string Id { get; set; }
        public string Title { get; protected set; }
        public IReadOnlyList<string> BundleNames { get; protected set; }
        public IReadOnlyList<string> PrefabNames { get; protected set; }
        public bool CenterOnScreen { get; protected set; } = true;
        public bool AutoBindClose { get; protected set; } = true;
        public Vector2? Size { get; protected set; }

        public UiPrefabWindowOptions SetId(string id)
        {
            Id = id;
            return this;
        }

        public UiPrefabWindowOptions SetTitle(string title)
        {
            Title = title;
            return this;
        }

        public UiPrefabWindowOptions SetSize(float width, float height)
        {
            Size = new Vector2(width, height);
            return this;
        }

        public UiPrefabWindowOptions SetCentered(bool centered = true)
        {
            CenterOnScreen = centered;
            return this;
        }

        public UiPrefabWindowOptions SetAutoBindClose(bool autoBindClose = true)
        {
            AutoBindClose = autoBindClose;
            return this;
        }
    }
}
