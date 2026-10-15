# 附属模组公开 API

附属模组只引用 `StorageNetwork.API` 中的前置类型，不直接引用 `Core`、`Services`、`ProductionOrders` 或 `UI` 实现类型。原版游戏和 Unity 的公开类型可正常使用。

`StorageNetworkApi.ApiVersion` 当前为 **1**。兼容的接口增补保持版本；破坏兼容性的修改递增版本。附属模组应在所有模组加载完成后检查版本，再注册入口与补丁。依赖检测应识别程序集及 API，避免依赖 Dev/创意工坊不同的默认 staticID。

## 库存与交付路由

`StorageNetworkInventoryAccess` 提供：

- 仓库查询：`GetStorages`、`FillSourceStorages`、`HasOnlineCoreInWorld`、`GetWorldName`。
- 仓库规则：`IsServerStorage`、`IsConnectedNetworkStorage`、`IsSolidStorageServer`、`IsColdStorageServer`、`MatchesElementState`、`IsStorageReachableFromWorld`。
- 生产预留量：`GetReservedAmount`，出售时应从库存中扣除；不得把生产订单预留材料当作可出售数量。
- 物品接收：`FindItemReceiver`、`FindFoodReceiver`；元素接收：`FindElementReceivers`、`HasElementReceiverIgnoringCapacity`。这些接口由前置处理过滤器、网络和容量规则，不需要构建内部场景快照。
- 物品标识：`GetStoredItemKey`、`GetStorageInstanceId`、`GetStorageMatchTagsNonAlloc`。标签视图取得后立即用于查询，不跨帧保存。
- 缓存失效：`Invalidate(storage)` 或双仓库重载，在实际增减货物后调用。无参数重载用于场景/网络状态变化。

旧版带场景快照参数的接收查询接口暂保留，确保已有调用不失效；新附属模组应使用上述 Receiver 接口，避免依赖内部快照类型。查询结果不是库存或容量预留，实际交付前仍需复核，并按实际转移量结算。

## 界面集成

`StorageNetworkPanelHeaderButtonRegistry.RegisterFactory` 注册标题栏按钮；前置负责按钮布局，附属模组负责按钮回调打开的窗口。

`StorageNetworkUiAccess.CurrentWorldId` 提供与主面板一致的世界上下文。`GetSprite` 提供游戏/前置图标，找不到时返回 null。界面颜色从 `StorageNetworkPanelPalette` 获取。

`RegisterTextInputFocus(modId, query)` 注册焦点查询；同 ID 替换，不覆盖其他模组。前置统一判断任一模组输入框是否正在编辑。查询应轻量、无副作用；卸载界面扩展时可用 `UnregisterTextInputFocus` 撤销。关闭普通窗口不必撤销，查询返回 false 即可。

`RegisterRightClickClose(modId, isOpen, closeTop, priority)` 注册右键关闭回调。优先级高的已打开窗口先处理；回调只关闭当前最上层窗口。附属界面在 `Update` 中调用 `TrackRightClickClose(out closed)`，这样主面板隐藏时仍然有效。前置主面板使用同一分发器：同帧重复调用不会重复关闭，右键移动超过8像素后不会关闭，底层主面板也不会同时响应。卸载时使用 `UnregisterRightClickClose` 撤销注册。

交易模组的验证会检查编译 DLL 中所有指向 StorageNetwork 的类型引用，拒绝 API 命名空间以外的依赖，同时锁定公开接口签名，防止误删旧接口。
