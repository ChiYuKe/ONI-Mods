using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StorageNetwork.API;
using StorageNetwork.Trading;
using UnityEngine;

namespace StorageNetworkTrading.UI
{
    // Runtime projection of the saved voyage: registered in the real ClusterGrid.
    // No UI cards, custom mesh, movement interpolation, or meteor gameplay events.
    internal sealed class TradingMerchantClusterEntity : ClusterGridEntity, ISidescreenButtonControl
    {
        internal TradingVoyage Voyage;
        // AxialI's constructor takes (R, Q), unlike the saved voyage's Q/R fields.
        internal static AxialI FromCoordinates(int q, int r) => new AxialI { Q = q, R = r };
        private ClusterTraveler traveler;
        private ClusterDestinationSelector selector;
        private double legStart = double.NaN;
        private AxialI legDestination;
        private bool registered;
        private readonly List<AnimConfig> animations = new List<AnimConfig>();

        public override string Name => TradingText.Render(Voyage?.Visitor?.Name ?? "");
        public override EntityLayer Layer => EntityLayer.Payload;
        public override List<AnimConfig> AnimConfigs => animations;
        public override bool IsVisible => Voyage != null &&
            ClusterManager.Instance?.GetWorld(Voyage.WorldId)?.IsDiscovered == true;
        public override ClusterRevealLevel IsVisibleInFOW => ClusterRevealLevel.Visible;
        public override bool ShowName() => true;
        public override bool SpaceOutInSameHex() => true;
        public override bool KeepRotationWhenSpacingOutInHex() => true;
        public override bool ShowPath() => m_selectable != null && m_selectable.IsSelected;
        public override bool ShowProgressBar() => ShowPath() && traveler != null && traveler.IsTraveling();
        public override float GetProgress() => traveler != null ? traveler.GetMoveProgress() : 0;

        internal static TradingMerchantClusterEntity Create(Transform parent, TradingVoyage voyage)
        {
            var template = EntityTemplates.CreateEntity("StorageNetworkTradingMerchant_" + voyage.Visitor.Id,
                TradingText.Render(voyage.Visitor.Name));
            // CreateEntity builds a template; KInstantiate supplies the runtime ID,
            // tracker registration, tags and instantiate callbacks expected by native menus.
            var go = Util.KInstantiate(template, parent.gameObject);
            UnityEngine.Object.Destroy(template);
            go.transform.SetParent(parent, false);
            go.AddOrGet<StateMachineController>();
            go.AddOrGet<InfoDescription>();
            var destination = go.AddOrGet<ClusterDestinationSelector>();
            destination.assignable = false;
            destination.canNavigateFogOfWar = true;
            // Native validation only recognizes revealed asteroids. ApplyLeg validates
            // against the actual destination WorldContainer, including unexplored worlds.
            destination.requireAsteroidDestination = false;
            destination.requireLaunchPadOnAsteroidDestination = false;
            destination.shouldPointTowardsPath = true;
            var travel = go.AddOrGet<ClusterTraveler>();
            travel.revealsFogOfWarAsItTravels = false;
            travel.quickTravelToAsteroidIfInOrbit = false;
            var entity = go.AddOrGet<TradingMerchantClusterEntity>();
            entity.Voyage = voyage;
            entity.traveler = travel;
            entity.selector = destination;
            entity.animations.Add(new AnimConfig { animFile = Assets.GetAnim("rocket01_kanim"),
                initialAnim = "idle_loop", playMode = KAnim.PlayMode.Loop });
            travel.getSpeedCB = entity.Speed;
            travel.getCanTravelCB = _ => entity.Voyage != null && StorageNetworkTradingState.Cycle < entity.Voyage.Arrival;
            entity.ApplyLeg(true);
            go.SetActive(true);
            return entity;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            registered = true;
            // ClusterMapSelectTool rejects targets that resolve to a colony grid cell.
            // Use world coordinates so the SaveGame parent's position cannot affect this.
            transform.position = new Vector3(-10000f, -10000f, 0f);
            RefreshDescription();
        }

        internal void Synchronize(TradingVoyage voyage)
        {
            Voyage = voyage;
            ApplyLeg(false);
            RefreshDescription();
        }

        private void ApplyLeg(bool initial)
        {
            var target = ClusterManager.Instance.GetWorld(Voyage.WorldId)?.GetComponent<AsteroidGridEntity>();
            var exit = StorageNetworkTradingState.Instance?.Market?.ExitPort;
            if (Voyage.Outbound ? exit == null : target == null) return;
            AxialI to = Voyage.Outbound ? FromCoordinates(exit.Q, exit.R) : target.Location;
            Voyage.ToQ = to.Q; Voyage.ToR = to.R;
            if (!initial && legStart == Voyage.Start && legDestination == to)
            {
                // Market arrival and native movement share a deadline, including large time jumps.
                if (StorageNetworkTradingState.Cycle >= Voyage.Arrival)
                {
                    var end = to;
                    if (Location != end) Location = end;
                    if (selector.GetDestination() != end) selector.SetDestination(end);
                }
                return;
            }
            legStart = Voyage.Start;
            legDestination = to;
            var grid = ClusterGrid.Instance;
            AxialI from = ClosestValid(FromCoordinates(Voyage.FromQ, Voyage.FromR));
            Voyage.FromQ = from.Q; Voyage.FromR = from.R;
            Voyage.ToQ = to.Q; Voyage.ToR = to.R;
            var path = grid.GetPath(from, to, selector);
            AxialI current = from;
            if (path != null && path.Count > 0)
            {
                int completed = Mathf.Clamp((int)Math.Floor(Voyage.Progress(StorageNetworkTradingState.Cycle) * path.Count), 0, path.Count);
                if (completed > 0) current = path[completed - 1];
            }
            if (initial) m_location = current;
            else Location = current;
            selector.SetDestination(to);
        }

        private static AxialI ClosestValid(AxialI requested)
        {
            var grid = ClusterGrid.Instance;
            if (grid.IsValidCell(requested)) return requested;
            return grid.cellContents.Keys.OrderBy(c => (c.ToWorld() - requested.ToWorld()).sqrMagnitude)
                .ThenBy(c => c.Q).ThenBy(c => c.R).First();
        }

        private float Speed()
        {
            double remaining = Voyage.Arrival - StorageNetworkTradingState.Cycle;
            if (remaining <= 0 || traveler.CurrentPath == null) return 0;
            // Native speed is hexes per cycle. Account for a partially completed hex.
            return (float)(Math.Max(0, traveler.CurrentPath.Count - traveler.GetMoveProgress()) / remaining);
        }

        private void RefreshDescription()
        {
            double cycle = StorageNetworkTradingState.Cycle;
            string key = Voyage.WaitingForDestination ? "WAITING_ROUTE" : Voyage.Outbound ? "OUTBOUND" : cycle < Voyage.Arrival ? "INBOUND" : "DOCKED";
            double remaining = Voyage.Outbound || cycle < Voyage.Arrival ? Voyage.Arrival - cycle : Voyage.Visitor.Departure - cycle;
            GetComponent<InfoDescription>().description = TradingText.Render(TradingText.Ref(
                "STRINGS.UI.STORAGENETWORK_TRADING.STARMAP_" + key, Voyage.Visitor.Name,
                StorageNetworkInventoryAccess.GetWorldName(Voyage.WorldId), TradingText.Argument(Math.Max(0, remaining), "0.0")));
            if (m_selectable != null && m_selectable.IsSelected && DetailsScreen.Instance != null)
                DetailsScreen.Instance.Refresh(gameObject);
        }

        protected override void OnCleanUp()
        {
            if (registered && ClusterGrid.Instance != null) base.OnCleanUp();
            registered = false;
        }

        public string SidescreenButtonText => TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.STARMAP_TRADE"));
        public string SidescreenButtonTooltip => SidescreenButtonInteractable() ? SidescreenButtonText :
            TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.STARMAP_NOT_DOCKED"));
        public bool SidescreenEnabled() => Voyage != null;
        public bool SidescreenButtonInteractable() => Voyage != null && !Voyage.WaitingForDestination && StorageNetworkTradingState.Instance != null &&
            StorageNetworkTradingState.Instance.Market.IsVisiting(Voyage.Visitor,
                StorageNetworkTradingState.TradeWorldId(StorageNetworkUiAccess.CurrentWorldId), StorageNetworkTradingState.Cycle);
        public void OnSidescreenButtonPressed()
        {
            if (SidescreenButtonInteractable()) TradingPanel.ShowNativeTradingSupplier(Voyage.Visitor.Id);
        }
        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride) { }
        public int HorizontalGroupID() => -1;
        public int ButtonSideScreenSortOrder() => 20;
    }

    [HarmonyPatch(typeof(ClusterMapSelectTool), nameof(ClusterMapSelectTool.Select))]
    internal static class TradingMerchantDetailsSelectionPatch
    {
        private static void Postfix(KSelectable new_selected)
        {
            if (new_selected == null || !new_selected.IsSelected ||
                new_selected.GetComponent<TradingMerchantClusterEntity>() == null) return;
            var details = DetailsScreen.Instance;
            if (details == null) return;
            // Refresh alone only changes content. Show the native details screen
            // after the select tool and RootMenu have completed their normal selection events.
            details.Refresh(new_selected.gameObject);
            details.Show();
        }
    }
}
