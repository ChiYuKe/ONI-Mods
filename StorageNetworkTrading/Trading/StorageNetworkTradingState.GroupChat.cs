using NetworkInventory = StorageNetwork.API.StorageNetworkInventoryAccess;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        internal bool GroupChatBusy { get; private set; }
        internal string GroupChatStatus { get; private set; }
        private Coroutine groupChatCoroutine;
        private UnityWebRequest groupChatRequest;
        private int groupChatGeneration;

        private static string GroupText(string key, params string[] args) => TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING." + key, args);

        internal string SendGroupChat(string text, int worldId)
        {
            if (!TradingAiFeatureGate.Enabled) return GroupChatStatus = GroupText("GROUP_CONFIG_REQUIRED");
            if (GroupChatBusy) return GroupText("GROUP_BUSY");
            if (worldId < 0) worldId = ClusterManager.Instance?.activeWorldId ?? -1;
            if (!TradingAiSettingsStore.Local.Load(out var settings) || !settings.CanChat()) return GroupChatStatus = GroupText("GROUP_CONFIG_REQUIRED");
            text = text?.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > 2000) return GroupChatStatus = GroupText("GROUP_INPUT_LIMIT");
            Refresh(worldId);
            market.GroupChat = market.GroupChat ?? new TradingGroupChat();
            var responders = market.GroupChat.Route(text, market.Suppliers, Cycle, settings.ReplyProbability, () => UnityEngine.Random.value, catalog);
            market.GroupChat.ConversationState ??= new TradingConversationState();
            market.GroupChat.ConversationState.Observe(text, market.GroupChat.FocusSupplierId, catalog);
            market.GroupChat.Add(new TradingGroupMessage { Player = true, SupplierId = -1, Text = text, Cycle = Cycle });
            Persist();
            if (responders.Count == 0)
            {
                GroupChatStatus = GroupText(market.GroupChat.RoutingMode == "ambiguous_name" ? "GROUP_AMBIGUOUS_NAME" : text.Contains("@") ? "GROUP_UNKNOWN_MENTION" : "GROUP_QUIET");
                return null;
            }
            GroupChatBusy = true;
            GroupChatStatus = GroupText("GROUP_BUSY");
            int generation = ++groupChatGeneration;
            groupChatCoroutine = StartCoroutine(RunGroupChat(settings, responders.ToArray(), worldId, generation));
            return null;
        }

        private IEnumerator RunGroupChat(TradingAiSettings settings, int[] responders, int worldId, int generation)
        {
            yield return null;
            var routine = RunGroupChatCore(settings, responders, worldId, generation);
            try
            {
                while (generation == groupChatGeneration)
                {
                    bool next;
                    object current;
                    try { next = routine.MoveNext(); current = next ? routine.Current : null; }
                    catch (Exception ex)
                    {
                        GroupChatStatus = GroupText("GROUP_CLIENT_ERROR", ex.GetType().Name,
                            TradingAiChatProtocol.Redact(ex.Message, settings.ApiKey));
                        GroupChatBusy = false;
                        groupChatRequest = null;
                        groupChatCoroutine = null;
                        yield break;
                    }
                    if (!next) yield break;
                    yield return current;
                }
            }
            finally { (routine as IDisposable)?.Dispose(); }
        }

        private IEnumerator RunGroupChatCore(TradingAiSettings settings, int[] responders, int worldId, int generation)
        {
            string failure = null;
            try
            {
                foreach (int id in responders)
                {
                    if (generation != groupChatGeneration) yield break;
                    Refresh(worldId);
                    var supplier = market.Suppliers.Find(s => s.Id == id && s.Departure > Cycle);
                    if (supplier == null) continue;
                    var context = TradingDialogueContexts.Build(market, supplier, catalog, Cycle);
                    var roster = market.Suppliers.Select(s => new { s.Id, Name = TradingText.Render(s.Name), s.Specialty,
                        Temperament = TradingText.Render(s.Persona?.Public?.Temperament), RemainingCycles = Math.Max(0, s.Departure - Cycle) }).ToArray();
                    var live = new { WorldId = worldId, supplier.Arrival, supplier.Departure, RemainingCycles = supplier.Departure - Cycle,
                        supplier.BuyLimit, supplier.SellLimit, supplier.Bought, supplier.Sold,
                        Profile = TradingText.Render(ProfileName(supplier)), Preference = TradingText.Render(TradingMarket.PreferenceName(supplier)),
                        RelationshipBenefits = TradingText.Render(TradingRelationships.Card(supplier)),
                        MarketEvent = TradingText.Render(TradingMarket.EventName(supplier.Specialty, Cycle)),
                        WorldInventory = market.WorldInventorySignals.TryGetValue(worldId, out var signal) ? signal.Snapshot : null };
                    string requestBody = TradingAiChatProtocol.Request(settings.Model, context, live, roster, market.GroupChat);
                    using (var request = new UnityWebRequest(TradingAiChatProtocol.CompletionUrl(settings.Endpoint), "POST"))
                    {
                        groupChatRequest = request;
                        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        if (!string.IsNullOrEmpty(settings.ApiKey)) request.SetRequestHeader("Authorization", "Bearer " + settings.ApiKey);
                        request.timeout = 45;
                        request.redirectLimit = 0;
                        GroupChatStatus = GroupText("GROUP_SPEAKING", TradingText.Render(supplier.Name));
                        yield return request.SendWebRequest();
                        if (generation != groupChatGeneration) yield break;
                        // Ignore answers from visitors who have left during the request.
                        if (!market.Suppliers.Any(s => s.Id == id && s.Departure > Cycle)) continue;
                        string reply = request.result == UnityWebRequest.Result.Success ? TradingAiChatProtocol.Reply(request.downloadHandler.text) : null;
                        reply = TradingAiChatProtocol.CleanReply(reply, TradingText.Render(supplier.Name), supplier.Id);
                        if (reply == "[NO_REPLY]") continue;
                        if (reply == null)
                        {
                            failure = TradingAiChatProtocol.Failure(request.responseCode, request.error, request.downloadHandler.text, settings.ApiKey);
                            GroupChatStatus = failure;
                            if (request.responseCode == 401 || request.responseCode == 403 || request.responseCode == 429) break;
                            continue;
                        }
                        market.GroupChat.Add(new TradingGroupMessage { SupplierId = id, Name = TradingText.Render(supplier.Name), Specialty = supplier.Specialty, Appearance = supplier.Appearance, Text = reply, Cycle = Cycle });
                        Persist();
                    }
                    groupChatRequest = null;
                }
            }
            finally
            {
                if (generation == groupChatGeneration)
                {
                    GroupChatBusy = false;
                    groupChatRequest = null;
                    groupChatCoroutine = null;
                    GroupChatStatus = failure ?? GroupText("GROUP_READY");
                }
            }
        }

        internal void CancelGroupChat()
        {
            groupChatGeneration++;
            if (groupChatRequest != null) { groupChatRequest.Abort(); groupChatRequest.Dispose(); groupChatRequest = null; }
            if (groupChatCoroutine != null) { StopCoroutine(groupChatCoroutine); groupChatCoroutine = null; }
            if (GroupChatBusy) GroupChatStatus = GroupText("GROUP_CANCELLED");
            GroupChatBusy = false;
        }
    }
}
