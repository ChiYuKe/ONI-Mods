namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRelayCommandConditions : CommandConditions
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            RocketModule module = GetComponent<RocketModule>();
            RocketModuleCluster clusterModule = GetComponent<RocketModuleCluster>();
            LaunchableRocketCluster launchable = GetComponent<LaunchableRocketCluster>();

            reachable = (ConditionDestinationReachable)module.AddModuleCondition(
                ProcessCondition.ProcessConditionType.RocketPrep,
                new ConditionDestinationReachable(module));
            allModulesComplete = (ConditionAllModulesComplete)module.AddModuleCondition(
                ProcessCondition.ProcessConditionType.RocketPrep,
                new ConditionAllModulesComplete(launchable));
            hasEngine = (ConditionHasEngine)module.AddModuleCondition(
                ProcessCondition.ProcessConditionType.RocketPrep,
                new ConditionHasEngine(launchable));
            onLaunchPad = (ConditionOnLaunchPad)module.AddModuleCondition(
                ProcessCondition.ProcessConditionType.RocketPrep,
                new ConditionOnLaunchPad(clusterModule.CraftInterface));

            int horizontalClearance = DlcManager.FeatureClusterSpaceEnabled() ? 0 : 1;
            flightPathIsClear = (ConditionFlightPathIsClear)module.AddModuleCondition(
                ProcessCondition.ProcessConditionType.RocketFlight,
                new ConditionFlightPathIsClear(gameObject, horizontalClearance));
        }
    }
}
