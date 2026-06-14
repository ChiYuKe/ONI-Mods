using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    internal sealed class ProductionOverviewCardView
    {
        public TextMeshProUGUI BuildingName { get; set; }

        public TextMeshProUGUI StorageValue { get; set; }

        public TextMeshProUGUI StateValue { get; set; }

        public TextMeshProUGUI RecipeValue { get; set; }

        public TextMeshProUGUI NetworkValue { get; set; }
    }

    internal sealed class ProductionInventoryCardView
    {
        public Dictionary<string, ProductionInventoryRowView> Rows { get; } = new Dictionary<string, ProductionInventoryRowView>();
    }

    internal sealed class ProductionAutomationCardsView
    {
        public TextMeshProUGUI MaterialStatus { get; set; }

        public TextMeshProUGUI OutputDescription { get; set; }

        public TextMeshProUGUI OutputStatus { get; set; }
    }

    internal sealed class ProductionInventoryRowView
    {
        public TextMeshProUGUI Name { get; set; }

        public TextMeshProUGUI Mass { get; set; }

        public Image Icon { get; set; }
    }

    internal sealed class ProductionPickerOption
    {
        public ProductionPickerOption(string title, string details, bool selected, System.Action onClick)
            : this(title, details, selected, onClick, null)
        {
        }

        public ProductionPickerOption(string title, string details, bool selected, System.Action onClick, Tag? iconTag)
        {
            Title = title;
            Details = details;
            Selected = selected;
            OnClick = onClick;
            IconTag = iconTag;
        }

        public string Title { get; }

        public string Details { get; }

        public bool Selected { get; }

        public System.Action OnClick { get; }

        public Tag? IconTag { get; }
    }
}
