using System.Linq;
using StorageNetwork.Components;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkPlanCategoryOrder
    {
        private static readonly string[] CategoryIds =
        {
            "Geyser",
            "Base",
            "Oxygen",
            "Power",
            "Food",
            "Plumbing",
            "HVAC",
            "Refining",
            "Medical",
            "Furniture",
            "Equipment",
            "Utilities",
            "Automation",
            "Conveyance",
            "Rocketry",
            "HEP"
        };

        public static string GetCategoryKey(StorageNetworkEnrollment enrollment)
        {
            if (enrollment != null && enrollment.IsGeyser())
            {
                return "Geyser";
            }

            KPrefabID prefabId = enrollment != null ? enrollment.GetComponent<KPrefabID>() : null;
            string buildingId = prefabId != null ? prefabId.PrefabID().ToString() : null;
            if (string.IsNullOrEmpty(buildingId))
            {
                return "Other";
            }

            foreach (PlanScreen.PlanInfo planInfo in global::TUNING.BUILDINGS.PLANORDER)
            {
                if (planInfo.buildingAndSubcategoryData != null &&
                    planInfo.buildingAndSubcategoryData.Any(entry => entry.Key == buildingId))
                {
                    return GetCategoryId(planInfo);
                }
            }

            return "Other";
        }

        public static int GetSortOrder(string categoryKey)
        {
            for (int i = 0; i < CategoryIds.Length; i++)
            {
                if (CategoryIds[i] == categoryKey)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private static string GetCategoryId(PlanScreen.PlanInfo planInfo)
        {
            foreach (string categoryId in CategoryIds)
            {
                if (planInfo.category == new HashedString(categoryId))
                {
                    return categoryId;
                }
            }

            return "Other";
        }
    }
}
