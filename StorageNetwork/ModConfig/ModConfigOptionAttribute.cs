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

        public string Label { get; }
        public string Description { get; }
        public float Min { get; }
        public float Max { get; }
        public bool Integer { get; set; }
    }
}
