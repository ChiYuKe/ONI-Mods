# StorageNetwork C# 文件功能索引

这份文档用于快速定位 `StorageNetwork` 模组代码。  
说明按当前目录结构整理；很多文件是 `partial class` 的功能拆分，文件名里的后缀通常就是它负责的 UI 或业务模块。

## 目录结构

| 目录 | 作用 |
| --- | --- |
| `API/` | 对外或跨模块使用的接口、Tag、世界浮动面板注册入口。 |
| `Buildings/` | 模组建筑配置和建筑运行时组件，包括核心、服务器、端口、冷库、电池、喷泉接入、火箭中继。 |
| `Components/` | 通用挂载组件：接入状态、过滤器状态、场景成员标记。 |
| `Core/` | 场景注册表、快照收集、分类规则、本地化、资源加载、生命周期和网络规则。 |
| `Game/` | 把建筑、接入能力、生产输出处理接入 ONI 原生系统。 |
| `ModConfig/` | 模组配置窗口、字段绑定、JSON 配置读写。 |
| `Patches/` | Harmony 补丁，连接 ONI 原生流程。 |
| `ProductionOrders/` | 生产订单系统：配方扫描、计划、提交、执行、持久化、库存保持。 |
| `Research/` | 科技树节点和建筑解锁注册。 |
| `Services/` | 网络搬运、目标选择、库存索引、过滤器、建造供料、性能计数等服务。 |
| `UI/` | 主面板、订单窗口、生产设置、可接入窗口、输入控件、世界浮动面板等 UI。 |

## 根目录

| 文件 | 功能 |
| --- | --- |
| `Config.cs` | 模组配置数据、默认值、配置规范化和配置路径。 |
| `ModEntry.cs` | 模组入口，初始化路径、资源、Harmony 和配置。 |
| `StorageNetworkOptions.cs` | 模组设置入口声明。 |
| `STRINGS.cs` | 建筑、UI、状态、提示、本地化文本定义。 |

## API

| 文件 | 功能 |
| --- | --- |
| `API/IStorageNetworkWorldPanelContentProvider.cs` | 世界浮动面板内容提供器接口。 |
| `API/StorageNetworkTags.cs` | 对外可引用的储存网络 Tag 定义。 |
| `API/StorageNetworkWorldPanelContent.cs` | 世界浮动面板的标题、行、图标等内容模型。 |
| `API/StorageNetworkWorldPanelRegistry.cs` | 世界浮动面板内容提供器注册表。 |

## Buildings

| 文件 | 功能 |
| --- | --- |
| `Buildings/Core/Components/StorageNetworkCore.cs` | 储存网络核心运行时组件，注册核心并维护在线状态。 |
| `Buildings/Core/UI/StorageNetworkCoreSideScreen.cs` | 储存网络核心右侧信息面板。 |
| `Buildings/Core/UI/StorageNetworkCoreSideScreenInstaller.cs` | 安装核心侧屏到 `DetailsScreen`。 |
| `Buildings/EnergyGenerator/Components/StorageNetworkEnergyGeneratorRequester.cs` | 发电机从网络请求燃料的组件。 |
| `Buildings/Geyser/Components/StorageNetworkGeyserOutput.cs` | 喷泉自动入网逻辑，只在可存入网络时截流产物。 |
| `Buildings/Infrastructure/Components/StorageNetworkColdStorageController.cs` | 冷库服务器降温状态机。 |
| `Buildings/Infrastructure/Components/StorageNetworkColdStorageCooling.cs` | 冷库服务器对内容物降温、耗电和产热逻辑。 |
| `Buildings/Infrastructure/Components/StorageNetworkPowerOverlayBattery.cs` | 电池服务器用于电力概览显示的伪原生电池。 |
| `Buildings/Infrastructure/Components/StorageNetworkPowerStorage.cs` | 电池服务器虚拟电力容量、充放电、漏电和状态栏。 |
| `Buildings/Infrastructure/Components/StorageNetworkServerStatus.cs` | 服务器通用状态显示。 |
| `Buildings/Infrastructure/StorageNetworkInfrastructureConfig.cs` | 储存网络核心、固体/液体/气体/电池/冷库服务器建筑配置。 |
| `Buildings/Infrastructure/UI/StorageNetworkPanel.ColdStorageSettings.cs` | 冷库服务器设置面板，目标温度和滑条同步。 |
| `Buildings/Ports/GasInput/Components/StorageNetworkGasInputPortIngress.Conduit.cs` | 气体入网端口管道读取和消耗。 |
| `Buildings/Ports/GasInput/Components/StorageNetworkGasInputPortIngress.cs` | 气体入网端口缓存、过滤、自动入网和状态。 |
| `Buildings/Ports/GasOutput/Components/StorageNetworkGasOutputPortEgress.Conduit.cs` | 气体出网端口向管道输出。 |
| `Buildings/Ports/GasOutput/Components/StorageNetworkGasOutputPortEgress.cs` | 气体出网端口从网络取气、缓存、限额和状态。 |
| `Buildings/Ports/LiquidInput/Components/StorageNetworkLiquidInputPortIngress.Conduit.cs` | 液体入网端口管道读取和消耗。 |
| `Buildings/Ports/LiquidInput/Components/StorageNetworkLiquidInputPortIngress.cs` | 液体入网端口缓存、过滤、自动入网和状态。 |
| `Buildings/Ports/LiquidOutput/Components/StorageNetworkLiquidOutputPortEgress.Conduit.cs` | 液体出网端口向管道输出。 |
| `Buildings/Ports/LiquidOutput/Components/StorageNetworkLiquidOutputPortEgress.cs` | 液体出网端口从网络取液、缓存、限额和状态。 |
| `Buildings/Ports/LiquidOutput/UI/StorageNetworkLiquidOutputPortSideScreen.cs` | 液体出网端口侧屏。 |
| `Buildings/Ports/Power/Components/StorageNetworkPowerInputPortConsumer.cs` | 电力入网端口，从电路/电池取电并存入网络。 |
| `Buildings/Ports/Power/Components/StorageNetworkPowerOutputPortGenerator.cs` | 电力出网端口，把网络电力输出到电路。 |
| `Buildings/Ports/Power/Components/StorageNetworkPowerService.cs` | 电力服务器聚合、平摊充电、平摊放电和容量查询。 |
| `Buildings/Ports/SolidInput/Components/StorageNetworkSolidInputPortIngress.cs` | 材料入网端口，接收运输轨道/复制人投放并存入网络。 |
| `Buildings/Ports/SolidOutput/Components/StorageNetworkSolidOutputPortEgress.cs` | 材料出网端口，从网络取货、缓存并供轨道/复制人取用。 |
| `Buildings/Ports/SolidOutput/Components/StorageNetworkSolidOutputPortManualOperationButton.cs` | 材料出网端口手动取货按钮逻辑。 |
| `Buildings/Ports/StorageNetworkPortConfig.cs` | 材料/液体/气体/电力端口建筑配置和规格表。 |
| `Buildings/Ports/UI/StorageNetworkPanel.ProductionSettings.Port.cs` | 端口在生产设置面板中的配置 UI。 |
| `Buildings/Production/Components/StorageNetworkMaterialRequester.cs` | 生产建筑材料请求主组件。 |
| `Buildings/Production/Components/StorageNetworkMaterialRequester.Outputs.cs` | 生产建筑产物回存网络逻辑。 |
| `Buildings/Production/Components/StorageNetworkMaterialRequester.Status.cs` | 生产建筑材料请求状态栏。 |
| `Buildings/Production/Components/StorageNetworkMaterialRequester.Storage.cs` | 生产建筑输入/输出仓库解析和材料来源选择。 |
| `Buildings/RocketRelay/Components/StorageNetworkRelayCommandConditions.cs` | 火箭中继模块发射条件描述。 |
| `Buildings/RocketRelay/Components/StorageNetworkRelayModule.cs` | 火箭中继模块运行时状态，判断是否在太空。 |
| `Buildings/StorageConnector/Components/StorageNetworkStorageConnector.cs` | 原版储物建筑自动输出到网络的连接器。 |

## Components

| 文件 | 功能 |
| --- | --- |
| `Components/SceneStorageBoxMarker.cs` | 旧存档场景储物箱标记。 |
| `Components/StorageNetworkDefaultFilterInitializer.cs` | 初始化储存过滤器默认状态。 |
| `Components/StorageNetworkEnrollment.cs` | 建筑接入/移出网络、喷泉自动入网开关、用户菜单按钮。 |
| `Components/StorageNetworkFilterState.cs` | 记录过滤器是否默认、是否用户修改。 |
| `Components/StorageNetworkSceneMember.cs` | 场景成员注册/注销标记组件。 |

## Core

| 文件 | 功能 |
| --- | --- |
| `Core/StorageCategories.cs` | 面板分类 key、显示名和排序。 |
| `Core/StorageNetworkAssetBundles.cs` | 加载模组 AssetBundle。 |
| `Core/StorageNetworkLifecycle.cs` | 游戏生命周期中重置运行时缓存。 |
| `Core/StorageNetworkLocalization.cs` | 注册本地化、加载 `.po` 翻译。 |
| `Core/StorageNetworkMembership.cs` | 判断储存是否属于网络、是否可被快照收集。 |
| `Core/StorageNetworkModInfoResolver.cs` | 解析建筑或物品来源模组信息。 |
| `Core/StorageNetworkNotifications.cs` | 储存网络通知封装。 |
| `Core/StorageNetworkSpriteLoader.cs` | 从资源文件加载并缓存 Sprite。 |
| `Core/StorageNetworkSprites.cs` | 模组通用图标入口。 |
| `Core/StorageNetworkStorageRules.cs` | 储存网络规则：服务器、端口、在线、过滤和分类判断。 |
| `Core/StorageNetworkWorldUtility.cs` | 世界 ID 和跨世界辅助。 |
| `Core/StorageSceneCollector.cs` | 构建储存网络快照，含按世界缓存和轻量快照。 |
| `Core/StorageSceneRegistry.cs` | 运行时注册表，维护服务器、端口、喷泉、核心、中继、电池服务器集合。 |
| `Core/StorageSceneSnapshot.cs` | 快照模型、`StorageInfo`、轻量快照模型。 |
| `Core/StorageSceneTags.cs` | 模组内部使用的场景 Tag。 |

## Game

| 文件 | 功能 |
| --- | --- |
| `Game/StorageNetworkBuildingPlanInstaller.cs` | 把模组建筑加入对应建筑菜单。 |
| `Game/StorageNetworkEnrollmentInstaller.cs` | 给原版储物箱、制造站、喷泉、发电机安装接入组件。 |
| `Game/StorageNetworkProductionOutputHandler.cs` | 生产完成后把产物交给网络输出逻辑处理。 |
| `Game/StorageNetworkStorageConnectorResolver.cs` | 获取或创建储物建筑的网络连接器。 |

## ModConfig

| 文件 | 功能 |
| --- | --- |
| `ModConfig/JsonConfigStore.cs` | JSON 配置文件读写。 |
| `ModConfig/ModConfigController.cs` | 配置加载、保存和设置窗口控制。 |
| `ModConfig/ModConfigDialog.cs` | 设置窗口主类。 |
| `ModConfig/ModConfigDialog.Fields.cs` | 设置字段创建和应用。 |
| `ModConfig/ModConfigDialog.InputBinding.cs` | 设置输入框和滑条绑定。 |
| `ModConfig/ModConfigDialog.Layout.cs` | 设置窗口布局。 |
| `ModConfig/ModConfigInputBuilder.cs` | 设置窗口输入控件工厂。 |
| `ModConfig/ModConfigOptionAttribute.cs` | 配置项元数据特性。 |
| `ModConfig/ModsScreenOptionsButton.cs` | Mods 页面设置按钮。 |

## Patches

| 文件 | 功能 |
| --- | --- |
| `Patches/BuildingRegistrationPatch.cs` | 建筑、研究和本地化注册补丁。 |
| `Patches/ColdStorageSliderSetPatch.cs` | 冷库温度滑条接入原生 SliderSet。 |
| `Patches/ComplexFabricatorOutputStorePatch.cs` | 制造站成品生成时转交网络输出处理。 |
| `Patches/ComplexRecipeBuildingEnrollmentPatch.cs` | 给制造站类建筑安装网络接入组件。 |
| `Patches/ConstructableSupplyPatch.cs` | 建造材料从储存网络供料的补丁。 |
| `Patches/EnergyGeneratorEnrollmentPatch.cs` | 给发电机安装燃料请求组件。 |
| `Patches/GeyserElementEmitterPatch.cs` | 喷泉 ElementEmitter 激活后刷新自动入网状态。 |
| `Patches/GeyserEnrollmentPatch.cs` | 喷泉创建/游戏生成后安装接入组件。 |
| `Patches/LifecyclePatch.cs` | 游戏加载、清理、销毁时重置模组运行时状态。 |
| `Patches/NotificationScreenPatch.cs` | 注册异常订单等通知类型。 |
| `Patches/ProductionOrderPersistencePatch.cs` | 游戏保存时写入生产订单数据。 |
| `Patches/RocketRelayLaunchConditionPatch.cs` | 火箭中继模块发射条件补丁。 |
| `Patches/SelectToolPatch.cs` | 选择工具交互补丁。 |
| `Patches/SideScreenPatch.cs` | 安装储存网络相关侧屏和详情按钮。 |
| `Patches/SolidOutputConstructionReservePatch.cs` | 材料出网端口供建造取货时防止轨道/其它逻辑抢走材料。 |
| `Patches/StorageLockerEnrollmentPatch.cs` | 给储物箱、冰箱、液库、气库安装接入组件。 |
| `Patches/StorageNetworkBatteryDescriptorPatch.cs` | 隐藏电池服务器伪原生电池的原版效果描述。 |
| `Patches/StorageNetworkCodexPatch.cs` | 给数据库/索引添加储存网络分类和建筑条目。 |
| `Patches/StorageNetworkLargeStorageMassPatch.cs` | 大容量服务器质量显示相关补丁。 |
| `Patches/StorageNetworkPanelInputPatch.cs` | 面板输入框、右键关闭、拖动和快捷键处理补丁。 |
| `Patches/StorageNetworkPortPlacementPreviewPatch.cs` | 端口建造预览/放置相关补丁。 |
| `Patches/StorageNetworkPowerOverlayBatterySyncPatch.cs` | 电池服务器电力概览 UI 同步和颜色处理。 |
| `Patches/StorageNetworkWorldInventoryMirrorPatch.cs` | 把网络库存镜像到 `WorldInventory` 查询。 |
| `Patches/TreeFilterableNetworkBypassPatch.cs` | 过滤器更新时旁路网络搬运造成的误判。 |

## ProductionOrders

| 文件 | 功能 |
| --- | --- |
| `ProductionOrders/ProductionKeepRule.cs` | 自动保持库存规则模型。 |
| `ProductionOrders/ProductionNetworkInventoryCache.cs` | 订单系统使用的网络库存缓存。 |
| `ProductionOrders/ProductionOrderAssignments.cs` | 订单分配、队列分配、材料租约模型。 |
| `ProductionOrders/ProductionOrderDraft.cs` | 订单草稿和预览结果。 |
| `ProductionOrders/ProductionOrderFormatting.cs` | 订单数量、状态、名称格式化。 |
| `ProductionOrders/ProductionOrderModels.cs` | 产品、配方、材料需求、计划节点等核心模型。 |
| `ProductionOrders/ProductionOrderPersistence.cs` | 生产订单存档数据。 |
| `ProductionOrders/ProductionOrderRecord.cs` | 已提交订单记录。 |
| `ProductionOrders/ProductionOrderService.cs` | 生产订单服务主类和共享状态。 |
| `ProductionOrders/ProductionOrderService.Execution.cs` | 执行订单、推进队列、完成或异常处理。 |
| `ProductionOrders/ProductionOrderService.KeepRules.cs` | 自动保持库存规则查询、保存和执行。 |
| `ProductionOrders/ProductionOrderService.OrderCancellation.cs` | 取消订单、清理队列和租约。 |
| `ProductionOrders/ProductionOrderService.OrderMaintenance.cs` | 订单维护、无进度检测和异常修复。 |
| `ProductionOrders/ProductionOrderService.Persistence.cs` | 订单服务读档、存档和运行时重置。 |
| `ProductionOrders/ProductionOrderService.PlanLeases.cs` | 为计划生成材料租约、输出租约和机器分配。 |
| `ProductionOrders/ProductionOrderService.PlanMetrics.cs` | 计划耗时、缺口、负载等指标估算。 |
| `ProductionOrders/ProductionOrderService.Planning.cs` | 构建生产计划树，处理递归补产。 |
| `ProductionOrders/ProductionOrderService.Queries.cs` | 配方、产品、订单、库存查询。 |
| `ProductionOrders/ProductionOrderService.State.cs` | 订单状态刷新、完成记录和清理。 |
| `ProductionOrders/ProductionOrderService.Submission.cs` | 草稿构建和订单提交。 |
| `ProductionOrders/ProductionOrderService.Types.cs` | 订单服务内部结果类型。 |
| `ProductionOrders/ProductionRecipeCatalog.cs` | 扫描游戏配方并构建可生产产品目录。 |

## Research

| 文件 | 功能 |
| --- | --- |
| `Research/StorageNetworkResearchInstaller.cs` | 注册储存网络科技节点、解锁项和科技树位置。 |

## Services

| 文件 | 功能 |
| --- | --- |
| `Services/NetworkStorageTransferService.cs` | 网络物品转移、输入输出目标选择和移动结果。 |
| `Services/StorageItemUtility.cs` | 物品 key、标签、温度、质量和匹配工具。 |
| `Services/StorageNetworkConstructionSupplyService.cs` | 建造材料从网络调拨到材料出网端口。 |
| `Services/StorageNetworkFetchTargetResolver.cs` | 解析 FetchChore 可用目标和网络供料目标。 |
| `Services/StorageNetworkFilterBypass.cs` | 网络搬运时的过滤器旁路判断。 |
| `Services/StorageNetworkFilterChangeTransferService.cs` | 过滤器变化后迁出不再接受的物品。 |
| `Services/StorageNetworkFilterConfigurator.cs` | 配置 `TreeFilterable` 和过滤器默认项。 |
| `Services/StorageNetworkFilterSelectionNormalizer.cs` | 把过滤器大类展开成具体 tag。 |
| `Services/StorageNetworkInventoryIndexService.cs` | 网络库存索引，按世界和 tag 查询数量。 |
| `Services/StorageNetworkPerformanceCounters.cs` | 性能计数器和调试统计。 |
| `Services/StorageNetworkProductionStorageCollector.cs` | 收集生产建筑输入/输出仓库。 |
| `Services/StorageNetworkRocketRelayService.cs` | 判断跨星球中继状态。 |
| `Services/StorageNetworkSourceIndexService.cs` | 来源仓库索引，快速查找可取材料。 |
| `Services/StorageNetworkWorldInventoryMirrorService.cs` | 网络库存参与世界库存查询。 |
| `Services/StorageTargetSelector.cs` | 选择存入目标、取出来源和喷泉输出目标。 |
| `Services/StorageTargetSelector.Filters.cs` | 目标选择过滤条件、排序和世界可达性。 |

## UI/Common

| 文件 | 功能 |
| --- | --- |
| `UI/Common/ScrollWheelBlocker.cs` | 阻断滚轮冒泡，避免滚动 UI 时控制镜头。 |
| `UI/Common/SmoothScrollEdgeBounce.cs` | 滚动边缘缓冲和手感优化。 |
| `UI/Common/StorageNetworkCycleTime.cs` | 周期时间和趋势采样时间辅助。 |
| `UI/Common/StorageNetworkGeyserText.cs` | 喷泉名称、产量、接入详情文本。 |
| `UI/Common/StorageNetworkKeyedRowCache.cs` | 按 key 复用 UI 行，减少频繁销毁。 |
| `UI/Common/StorageNetworkStorageDisplay.cs` | 储存对象分类、名称、图标、显示文本。 |
| `UI/Common/StorageNetworkTextFormatting.cs` | 搜索文本规范化和 Klei 链接格式清理。 |
| `UI/Common/StorageNetworkWindowDrag.cs` | 可拖动窗口行为、位置保存和屏幕夹取。 |

## UI/HeaderWindow

| 文件 | 功能 |
| --- | --- |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Controls.cs` | 订单窗口通用按钮、输入框和控件。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.cs` | 订单/产品窗口主入口。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Layout.cs` | 订单窗口布局。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.Controls.cs` | 订单编辑控件。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.cs` | 订单编辑主界面。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.Submission.cs` | 订单提交区域和校验提示。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.cs` | 生产计划预览主界面。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.DispatchDiagram.cs` | 材料派工图。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Flow.cs` | 生产流程图。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.cs` | 计划台账主表。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.Materials.cs` | 材料台账行。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.Widgets.cs` | 台账小控件。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.ResearchCanvas.cs` | 计划研究树画布。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.ResearchTree.cs` | 计划研究树结构。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Tree.cs` | 计划树节点和连线。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Products.cs` | 产品列表和产品选择。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Shared.cs` | 订单窗口共用 UI 小组件。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.cs` | 订单跟踪列表。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.Detail.cs` | 单个订单跟踪详情。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.Widgets.cs` | 订单跟踪小控件。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.WorldFilter.cs` | 订单窗口世界筛选。 |

## UI/Input

| 文件 | 功能 |
| --- | --- |
| `UI/Input/StorageNetworkInputBuilder.cs` | 创建输入框控件。 |
| `UI/Input/StorageNetworkInputFieldEvents.cs` | 输入框事件和焦点处理。 |
| `UI/Input/StorageNetworkInputPatchSupport.cs` | 输入框补丁辅助。 |
| `UI/Input/StorageNetworkNumberInputField.cs` | 数字输入字段，限制范围和格式。 |
| `UI/Input/StorageNetworkSelectionInputHandler.cs` | 选择工具输入处理。 |
| `UI/Input/StorageNetworkTextInputGuard.cs` | 文本输入保护，避免快捷键穿透。 |

## UI/Installers

| 文件 | 功能 |
| --- | --- |
| `UI/Installers/StorageNetworkManagementMenuInstaller.cs` | 把储存网络按钮加入管理菜单。 |

## UI/Order

| 文件 | 功能 |
| --- | --- |
| `UI/Order/StorageNetworkOrderEditorSignatureBuilder.cs` | 订单编辑界面刷新签名。 |
| `UI/Order/StorageNetworkOrderEditorText.cs` | 订单编辑文本。 |
| `UI/Order/StorageNetworkOrderTrackingRules.cs` | 订单跟踪状态和展示规则。 |

## UI/Panel

| 文件 | 功能 |
| --- | --- |
| `UI/Panel/StorageNetworkPanel.cs` | 主面板字段、刷新节奏和快照入口。 |
| `UI/Panel/StorageNetworkPanel.DragDrop.cs` | 主面板拖动物品转移。 |
| `UI/Panel/StorageNetworkPanel.GeyserSettings.cs` | 喷泉设置窗口。 |
| `UI/Panel/StorageNetworkPanel.Items.cs` | 内容物聚合、搜索、展开状态。 |
| `UI/Panel/StorageNetworkPanel.Layout.cs` | 主面板 RectTransform 布局工具。 |
| `UI/Panel/StorageNetworkPanel.Lifecycle.cs` | 主面板显示、关闭、生命周期、右键关闭。 |
| `UI/Panel/StorageNetworkPanel.ListRefresh.cs` | 列表重建、签名、滚动位置保持。 |
| `UI/Panel/StorageNetworkPanel.Modal.Amount.cs` | 数量选择弹窗。 |
| `UI/Panel/StorageNetworkPanel.Modal.cs` | 通用弹窗、选择器、设置入口。 |
| `UI/Panel/StorageNetworkPanel.Rows.cs` | 分类按钮、服务器行、类型行。 |
| `UI/Panel/StorageNetworkPanel.Rows.Geysers.cs` | 喷泉列表行和喷泉详情。 |
| `UI/Panel/StorageNetworkPanel.Rows.Items.cs` | 物品行、虚拟电力行、内容物详情。 |
| `UI/Panel/StorageNetworkPanel.Rows.Widgets.cs` | 行内折叠按钮、图标和小控件。 |
| `UI/Panel/StorageNetworkPanel.Status.cs` | 面板顶部状态、容量、健康指标。 |
| `UI/Panel/StorageNetworkPanel.Style.cs` | 主面板颜色、背景、按钮样式。 |
| `UI/Panel/StorageNetworkPanel.UI.Buttons.cs` | 通用按钮创建。 |
| `UI/Panel/StorageNetworkPanel.UI.cs` | 通用 UI 创建方法。 |
| `UI/Panel/StorageNetworkPanel.UI.Scrollbars.cs` | 滚动条和滚动区域创建。 |
| `UI/Panel/StorageNetworkPanel.UI.SettingsWindow.cs` | 设置窗口框架。 |
| `UI/Panel/StorageNetworkPanel.Window.cs` | 主窗口创建。 |
| `UI/Panel/StorageNetworkPanel.WorldFilter.cs` | 主面板世界筛选和快照过滤。 |
| `UI/Panel/StorageNetworkPanelHealthMetrics.cs` | 网络健康指标计算。 |
| `UI/Panel/StorageNetworkPanelListSignature.cs` | 列表结构签名。 |

## UI/Panel/CategorySummary

| 文件 | 功能 |
| --- | --- |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryItemTotal.cs` | 分类汇总单项总量模型。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummarySignature.cs` | 分类汇总刷新签名。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryTrend.cs` | 分类趋势数据。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryTrendSampler.cs` | 分类趋势采样器。 |
| `UI/Panel/CategorySummary/StorageNetworkPanel.CategorySummary.cs` | 分类汇总 UI。 |

## UI/Panel/Enrollable

| 文件 | 功能 |
| --- | --- |
| `UI/Panel/Enrollable/StorageNetworkEnrollableWindowSignature.cs` | 可接入窗口刷新签名。 |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.cs` | 可接入建筑窗口主逻辑。 |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.Rows.cs` | 可接入建筑行。 |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.WorldFilter.cs` | 可接入窗口世界筛选。 |

## UI/PlanPreview

| 文件 | 功能 |
| --- | --- |
| `UI/PlanPreview/StorageNetworkPanZoom.cs` | 计划预览拖拽和平移缩放。 |
| `UI/PlanPreview/StorageNetworkPlanCategoryOrder.cs` | 计划分类排序。 |
| `UI/PlanPreview/StorageNetworkPlanPreviewMetrics.cs` | 计划预览尺寸和指标估算。 |
| `UI/PlanPreview/StorageNetworkPlanPreviewText.cs` | 计划预览文本。 |

## UI/ProductionSettings

| 文件 | 功能 |
| --- | --- |
| `UI/ProductionSettings/StorageNetworkMaterialLimitRules.cs` | 材料上限规则。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Automation.cs` | 生产建筑自动化设置卡片。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.cs` | 生产设置面板主逻辑。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.ExternalAutomation.cs` | 外部建筑、端口和发电机自动化设置。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Inventory.cs` | 设置面板库存卡片。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.LimitDialog.cs` | 材料上限弹窗。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Picker.cs` | 通用选择器弹窗。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Picker.Options.cs` | 选择器选项构建。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Widgets.cs` | 生产设置卡片和小控件。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsSignatureBuilder.cs` | 生产设置刷新签名。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsStyle.cs` | 生产设置样式。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsText.cs` | 生产设置显示文本。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsViews.cs` | 生产设置 View 数据结构。 |

## UI/World

| 文件 | 功能 |
| --- | --- |
| `UI/World/StorageNetworkWorldDisplay.cs` | 世界名称、图标和显示辅助。 |
| `UI/World/StorageNetworkWorldTextPanel.cs` | 世界内浮动文本面板控制。 |

## UI/WorldPanel

| 文件 | 功能 |
| --- | --- |
| `UI/WorldPanel/DefaultStorageNetworkWorldPanelContentProvider.cs` | 默认世界浮动面板内容提供器。 |
| `UI/WorldPanel/StorageNetworkWorldTextPanelView.cs` | 世界浮动面板视图创建、定位和销毁。 |

## Properties

| 文件 | 功能 |
| --- | --- |
| `Properties/AssemblyInfo.cs` | 程序集元数据。 |
