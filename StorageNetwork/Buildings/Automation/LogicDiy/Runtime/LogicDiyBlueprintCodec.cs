using Newtonsoft.Json;
using StorageNetwork.Components;
using System;

namespace StorageNetwork.LogicDiy.Runtime
{
    /// <summary>Owns blueprint parsing and keeps malformed editor payloads from destroying saved state.</summary>
    internal sealed class LogicDiyBlueprintCodec
    {
        private string lastJson;
        private StorageNetworkLogicDiy.RuntimeBlueprint lastBlueprint;

        public StorageNetworkLogicDiy.RuntimeBlueprint Parse(string json)
        {
            if (string.Equals(lastJson, json, StringComparison.Ordinal))
            {
                return lastBlueprint;
            }

            lastJson = json;
            lastBlueprint = null;
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                lastBlueprint = JsonConvert.DeserializeObject<StorageNetworkLogicDiy.RuntimeBlueprint>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"StorageNetwork LogicDiy blueprint parse failed: {ex.Message}");
            }

            return lastBlueprint;
        }

        public void Invalidate()
        {
            lastJson = null;
            lastBlueprint = null;
        }
    }
}
