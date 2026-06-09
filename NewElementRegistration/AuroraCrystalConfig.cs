using UnityEngine;

namespace NewElementRegistration
{
    public sealed class AuroraCrystalConfig : IOreConfig
    {
        public SimHashes ElementID => ModElements.AuroraCrystal;

        public GameObject CreatePrefab()
        {
            return EntityTemplates.CreateSolidOreEntity(ElementID, null);
        }
    }
}
