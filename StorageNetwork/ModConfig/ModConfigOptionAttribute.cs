using System;

namespace StorageNetwork.ModConfig
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ModConfigOptionAttribute : Attribute
    {
        public ModConfigOptionAttribute(string label, string description, float min, float max)
        {
            Label = label;
            Description = description;
            Min = min;
            Max = max;
        }

        public ModConfigOptionAttribute(string labelKey, string descriptionKey, string label, string description, float min, float max)
            : this(label, description, min, max)
        {
            LabelKey = labelKey;
            DescriptionKey = descriptionKey;
        }

        public string Label { get; }
        public string Description { get; }
        public string LabelKey { get; }
        public string DescriptionKey { get; }
        public float Min { get; }
        public float Max { get; }
        public bool Integer { get; set; }
    }
}
