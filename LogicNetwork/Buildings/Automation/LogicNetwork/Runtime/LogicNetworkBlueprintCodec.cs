using Newtonsoft.Json;
using LogicNetwork.Components;
using System;

namespace LogicNetwork.Runtime
{
    /// <summary>Owns blueprint parsing and keeps malformed editor payloads from destroying saved state.</summary>
    internal sealed class LogicNetworkBlueprintCodec
    {
        private string lastJson;
        private LogicNetworkEmitter.RuntimeBlueprint lastBlueprint;

        public LogicNetworkEmitter.RuntimeBlueprint Parse(string json)
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
                lastBlueprint = JsonConvert.DeserializeObject<LogicNetworkEmitter.RuntimeBlueprint>(json);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"LogicNetwork blueprint parse failed: {ex.Message}");
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
