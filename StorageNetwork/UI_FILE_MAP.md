# StorageNetwork 文件职责清单

这份清单用于快速判断 `StorageNetwork` 下每个主要 `.cs` 文件大概负责什么。  
多数 `StorageNetworkPanel.*.cs` 和 `ProductionOrderService.*.cs` 文件都是 partial class 的拆分文件，只是按功能拆开，移动文件夹不会改变运行逻辑。

## 根目录

模组入口、配置和字符串定义。

| 文件 | 作用 |
| --- | --- |
| `Config.cs` | 模组运行配置：读取/保存配置、输出到网络开关、数值规范化。 |
| `ModEntry.cs` | ONI 模组入口，继承 `UserMod2`，负责启动时注册模组逻辑。 |
| `StorageNetworkOptions.cs` | 注册模组设置入口。 |
| `STRINGS.cs` | 建筑名、描述、UI 文本、状态文本等本地化字符串定义。 |

## API

给模组内部和外部扩展使用的公开接口/注册表。

| 文件 | 作用 |
| --- | --- |
| `API/IStorageNetworkWorldPanelContentProvider.cs` | 世界浮动面板内容提供器接口。 |
| `API/StorageNetworkTags.cs` | 本模组使用的公共 Tag 定义，比如网络建筑、输入端口、输出端口分类。 |
| `API/StorageNetworkWorldPanelContent.cs` | 世界浮动面板内容数据结构。 |
| `API/StorageNetworkWorldPanelRegistry.cs` | 世界浮动面板内容提供器注册表，按优先级尝试构建显示内容。 |

## Buildings

建筑 prefab/config 定义。

| 文件 | 作用 |
| --- | --- |
| `Buildings/StorageNetworkInfrastructureConfig.cs` | 仓储网络核心、服务器、场景储物箱等基础设施建筑规格。 |
| `Buildings/StorageNetworkPortConfig.cs` | 输入/输出端口建筑规格、方向、类型、过滤器、管道/固体端口配置。 |

## Components

挂在 GameObject 上的运行时组件，负责建筑行为、请求器、端口状态和登记开关。

| 文件 | 作用 |
| --- | --- |
| `Components/SceneStorageBoxMarker.cs` | 场景储物箱标记组件。 |
| `Components/StorageNetworkCore.cs` | 仓储网络核心建筑标记/核心组件。 |
| `Components/StorageNetworkDefaultFilterInitializer.cs` | 初始化默认过滤器，避免用户未配置时过滤条件为空或不合理。 |
| `Components/StorageNetworkEnergyGeneratorRequester.cs` | 发电机材料请求器：从网络取燃料、选择来源、限制请求量。 |
| `Components/StorageNetworkEnrollment.cs` | 建筑/间歇泉/储物箱加入网络的登记组件和用户菜单按钮。 |
| `Components/StorageNetworkFilterState.cs` | 记录过滤器是否默认初始化、是否被用户配置，支持临时抑制配置追踪。 |
| `Components/StorageNetworkGeyserOutput.cs` | 捕获间歇泉产物并导入网络。 |
| `Components/StorageNetworkMaterialRequester.cs` | 生产建筑材料请求器主文件：来源/输出模式、周期请求、显示请求量。 |
| `Components/StorageNetworkMaterialRequester.Outputs.cs` | 生产建筑产物输出到网络/指定仓库的逻辑。 |
| `Components/StorageNetworkMaterialRequester.Status.cs` | 生产建筑材料请求状态项刷新和移除。 |
| `Components/StorageNetworkMaterialRequester.Storage.cs` | 材料请求的储存匹配、排除列表、可用量计算、标签匹配。 |
| `Components/StorageNetworkPort.cs` | 端口主组件：端口类型推断、过滤器、储存、手动操作配置。 |
| `Components/StorageNetworkPortManualFetch.cs` | 端口手动搬运/Fetch 相关标记组件。 |
| `Components/StorageNetworkPortPickupBufferStorage.cs` | 输出端口取货缓冲仓库：缓存物品、回流网络、同步手动搬运状态。 |
| `Components/StorageNetworkPortRequester.cs` | 输入端口请求器：从网络拉取材料到端口，设置输出量和来源。 |
| `Components/StorageNetworkPortStatusReporter.cs` | 端口状态主刷新器，定期显示网络/缓存/自动化/手动状态。 |
| `Components/StorageNetworkPortStatusReporter.StatusItems.cs` | 端口状态项定义。 |
| `Components/StorageNetworkPortStatusReporter.Text.cs` | 端口状态名称和 tooltip 文本。 |
| `Components/StorageNetworkPortStatusSilencer.cs` | 屏蔽原版或不需要显示的端口状态项。 |
| `Components/StorageNetworkRelayCommandConditions.cs` | 火箭中继指令条件相关类型。 |
| `Components/StorageNetworkRelayModule.cs` | 火箭仓储网络中继模块，监听火箭状态并判断是否在太空。 |
| `Components/StorageNetworkStorageConnector.cs` | 普通仓库/服务器输出到网络的连接器，管理输出目标和自动输出开关。 |

## Core

核心规则、场景快照、资源加载、本地化和跨世界判断。

| 文件 | 作用 |
| --- | --- |
| `Core/StorageCategories.cs` | 主列表分类规则：普通网络仓储、输入端口、输出端口等分类名和排序。 |
| `Core/StorageNetworkAssetBundles.cs` | 加载 AssetBundle prefab。 |
| `Core/StorageNetworkLifecycle.cs` | 模组运行时状态统一重置入口。 |
| `Core/StorageNetworkLocalization.cs` | 加载翻译、注册建筑菜单字符串、生成/读取本地化文本。 |
| `Core/StorageNetworkMembership.cs` | 判断对象是否属于网络、仓库是否可收集。 |
| `Core/StorageNetworkModInfoResolver.cs` | 解析仓库/建筑来源模组名称。 |
| `Core/StorageNetworkNotifications.cs` | 自定义通知，比如异常订单提醒。 |
| `Core/StorageNetworkSpriteLoader.cs` | 从文件加载 sprite 并注册。 |
| `Core/StorageNetworkSprites.cs` | 模组图标入口。 |
| `Core/StorageNetworkStorageRules.cs` | 仓储网络核心规则：是否网络仓库、是否在线、是否端口、是否可配置材料端口。 |
| `Core/StorageNetworkWorldUtility.cs` | 获取 GameObject 所在世界 ID。 |
| `Core/StorageSceneCollector.cs` | 收集场景仓储快照，带缓存、按世界收集、跨星球中继判断。 |
| `Core/StorageSceneRegistry.cs` | 运行时注册表：仓库、间歇泉、登记对象、输出端口缓冲等。 |
| `Core/StorageSceneSnapshot.cs` | 场景快照和 StorageInfo 数据结构/内容仓库解析。 |
| `Core/StorageSceneTags.cs` | 场景内部使用的 Tag 定义。 |

## Game

把建筑、登记能力、生产输出处理接入游戏系统。

| 文件 | 作用 |
| --- | --- |
| `Game/StorageNetworkBuildingPlanInstaller.cs` | 把仓储网络建筑加入建筑菜单和火箭模块排序。 |
| `Game/StorageNetworkEnrollmentInstaller.cs` | 给原版/兼容建筑安装登记组件：储物箱、间歇泉、制造站、发电机等。 |
| `Game/StorageNetworkProductionOutputHandler.cs` | 生产完成后强制把产物交给材料请求器处理。 |
| `Game/StorageNetworkStorageConnectorResolver.cs` | 为设置面板目标仓库获取或创建 `StorageNetworkStorageConnector`。 |

## ModConfig

模组设置窗口、配置绑定和 JSON 存储。

| 文件 | 作用 |
| --- | --- |
| `ModConfig/JsonConfigStore.cs` | JSON 配置读写工具。 |
| `ModConfig/ModConfigController.cs` | 配置控制器：加载/保存、注册设置按钮、构建选项绑定。 |
| `ModConfig/ModConfigDialog.cs` | 设置弹窗主入口：显示、关闭、重置运行时状态、重启提示。 |
| `ModConfig/ModConfigDialog.Fields.cs` | 设置字段 UI：数字输入、滑条、布尔开关、字段应用。 |
| `ModConfig/ModConfigDialog.InputBinding.cs` | 设置数字输入和滑条的双向绑定。 |
| `ModConfig/ModConfigDialog.Layout.cs` | 设置窗口布局、按钮、滚动条、Klei 样式。 |
| `ModConfig/ModConfigInputBuilder.cs` | 设置窗口输入控件创建工具。 |
| `ModConfig/ModConfigOptionAttribute.cs` | 配置项特性，用于声明标题、范围、默认值等元数据。 |
| `ModConfig/ModsScreenOptionsButton.cs` | Mods 页面设置按钮接入。 |

## Patches

Harmony 补丁，把仓储网络接进 ONI 原生流程。

| 文件 | 作用 |
| --- | --- |
| `Patches/BuildingRegistrationPatch.cs` | 建筑注册/初始化相关补丁。 |
| `Patches/ComplexFabricatorOutputStorePatch.cs` | 生产建筑产物输出补丁，接入网络输出处理。 |
| `Patches/ComplexRecipeBuildingEnrollmentPatch.cs` | 制造站类建筑登记接入补丁。 |
| `Patches/EnergyGeneratorEnrollmentPatch.cs` | 发电机登记接入补丁。 |
| `Patches/GeyserElementEmitterPatch.cs` | 间歇泉元素喷发补丁，用于捕获产物。 |
| `Patches/GeyserEnrollmentPatch.cs` | 间歇泉登记接入补丁。 |
| `Patches/LifecyclePatch.cs` | 游戏生命周期补丁，触发运行时状态重置。 |
| `Patches/NotificationScreenPatch.cs` | 通知系统补丁，注册仓储网络通知。 |
| `Patches/ProductionOrderPersistencePatch.cs` | 生产订单存档/读档补丁。 |
| `Patches/RocketRelayLaunchConditionPatch.cs` | 火箭中继发射条件补丁。 |
| `Patches/SelectToolPatch.cs` | 选择工具补丁，处理 UI/世界面板点击交互。 |
| `Patches/SideScreenPatch.cs` | 详情侧边栏补丁，安装仓储网络侧屏。 |
| `Patches/StorageLockerEnrollmentPatch.cs` | 储物箱登记接入补丁。 |
| `Patches/StorageNetworkFetchBridgePatch.cs` | FetchChore 桥接补丁，让复制人可从网络输出端口取货。 |
| `Patches/StorageNetworkLargeStorageMassPatch.cs` | 大容量仓储质量显示/逻辑相关补丁。 |
| `Patches/StorageNetworkPanelInputPatch.cs` | 仓储网络面板输入框相关补丁。 |
| `Patches/StorageNetworkPortPlacementPatch.cs` | 端口放置/建造相关补丁。 |
| `Patches/StorageNetworkWorldInventoryMirrorPatch.cs` | WorldInventory 镜像补丁，让网络库存参与世界库存查询。 |
| `Patches/TreeFilterableNetworkBypassPatch.cs` | 过滤器旁路补丁，处理网络自动搬运时的过滤行为。 |

## ProductionOrders

生产订单系统：配方目录、计划、提交、状态维护、持久化和自动保留规则。

| 文件 | 作用 |
| --- | --- |
| `ProductionOrders/ProductionKeepRule.cs` | 自动保持库存规则。 |
| `ProductionOrders/ProductionNetworkInventoryCache.cs` | 生产订单用的网络库存缓存。 |
| `ProductionOrders/ProductionOrderAssignments.cs` | 订单队列分配、材料租约、输出租约等分配模型。 |
| `ProductionOrders/ProductionOrderDraft.cs` | 订单草稿和计划预览数据。 |
| `ProductionOrders/ProductionOrderFormatting.cs` | 订单显示格式化工具。 |
| `ProductionOrders/ProductionOrderModels.cs` | 产品、配方、计划节点、需求、风险等核心模型。 |
| `ProductionOrders/ProductionOrderPersistence.cs` | 生产订单持久化数据结构和读写。 |
| `ProductionOrders/ProductionOrderRecord.cs` | 已提交订单记录。 |
| `ProductionOrders/ProductionOrderService.cs` | 生产订单服务主类入口和公共状态。 |
| `ProductionOrders/ProductionOrderService.Execution.cs` | 执行订单：推进队列、消耗材料、处理完成/异常。 |
| `ProductionOrders/ProductionOrderService.KeepRules.cs` | 自动保持库存规则的查询、设置、运行。 |
| `ProductionOrders/ProductionOrderService.OrderCancellation.cs` | 取消订单和清理制造站队列。 |
| `ProductionOrders/ProductionOrderService.OrderMaintenance.cs` | 维护活跃订单计划，缺料/计划变化时重排或修复。 |
| `ProductionOrders/ProductionOrderService.Persistence.cs` | 订单加载、保存、运行时重置。 |
| `ProductionOrders/ProductionOrderService.PlanLeases.cs` | 根据计划生成材料租约、输出租约和队列分配。 |
| `ProductionOrders/ProductionOrderService.PlanMetrics.cs` | 计划耗时、缺料量、队列负载等指标估算。 |
| `ProductionOrders/ProductionOrderService.Planning.cs` | 构建生产计划树，递归选择可生产材料和分配制造站。 |
| `ProductionOrders/ProductionOrderService.Queries.cs` | 查询可制作配方、产品组、网络库存、近期订单、重复订单。 |
| `ProductionOrders/ProductionOrderService.State.cs` | 更新订单状态、产量、队列负载、清理过期完成订单。 |
| `ProductionOrders/ProductionOrderService.Submission.cs` | 构建订单草稿、提交订单、生成订单 key。 |
| `ProductionOrders/ProductionOrderService.Types.cs` | 订单提交结果等服务内部类型。 |
| `ProductionOrders/ProductionRecipeCatalog.cs` | 扫描制造站配方，构建产品列表，查找可连接生产链。 |

## Research

科技树和解锁项接入。

| 文件 | 作用 |
| --- | --- |
| `Research/StorageNetworkResearchInstaller.cs` | 安装仓储网络科技节点、同步解锁项、布局科技树节点和连线。 |

## Services

无状态或缓存型服务，负责搬运、索引、过滤器、端口取货、世界库存镜像等核心业务。

| 文件 | 作用 |
| --- | --- |
| `Services/NetworkStorageTransferService.cs` | 网络仓储搬运服务：物品进网络、从网络出到仓库、输出目标选择和状态格式。 |
| `Services/StorageItemUtility.cs` | 物品工具：标签匹配、物品 key、质量、储存质量、可匹配标签集合。 |
| `Services/StorageNetworkFetchBridgeCache.cs` | Fetch 桥接缓存：失败冷却、端口查找缓存、清理缓存。 |
| `Services/StorageNetworkFetchBridgeRequest.cs` | 从 `FetchChore` 构建网络取货请求，包含目标、标签、数量和防循环判断。 |
| `Services/StorageNetworkFetchBridgeService.cs` | 给 FetchChore 提供网络输出端口里的可取物。 |
| `Services/StorageNetworkFilterBypass.cs` | 判断并应用过滤器旁路。 |
| `Services/StorageNetworkFilterChangeTransferService.cs` | 过滤器变化后，把不再接受的物品移回网络。 |
| `Services/StorageNetworkFilterConfigurator.cs` | 配置 `TreeFilterable`。 |
| `Services/StorageNetworkFilterSelectionNormalizer.cs` | 规范化过滤器选择，把大类展开成具体 tag。 |
| `Services/StorageNetworkInventoryIndexService.cs` | 网络库存索引缓存，按世界/跨世界查询物品数量。 |
| `Services/StorageNetworkPerformanceCounters.cs` | 性能计数器：记录索引重建、扫描、端口请求等次数。 |
| `Services/StorageNetworkPortPickupBuffer.cs` | 输出端口取货缓冲：把网络物品提供成复制人可捡的 `Pickupable`。 |
| `Services/StorageNetworkPortPickupSelector.cs` | 为 Fetch 请求选择可达的输出端口缓冲。 |
| `Services/StorageNetworkPortPickupState.cs` | 同步端口缓冲仓库和其中物品的可捡取状态。 |
| `Services/StorageNetworkProductionStorageCollector.cs` | 收集制造站相关的输入/输出仓库。 |
| `Services/StorageNetworkRocketRelayService.cs` | 判断火箭模块是否具备仓储网络中继。 |
| `Services/StorageNetworkSourceIndexService.cs` | 网络来源仓库索引，按标签、世界、排除列表查找可用来源。 |
| `Services/StorageNetworkWorldInventoryMirrorService.cs` | 把网络库存镜像给 `WorldInventory` 查询。 |
| `Services/StorageTargetSelector.cs` | 目标选择主逻辑：寻找输出目标、网络来源、排除集、世界 ID。 |
| `Services/StorageTargetSelector.Filters.cs` | 目标选择过滤细节：可用目标、过滤器接受、世界可达、排序。 |

## Properties

| 文件 | 作用 |
| --- | --- |
| `Properties/AssemblyInfo.cs` | 程序集元数据。 |

## UI

UI 窗口、侧边栏、输入框、浮动面板和各类显示辅助。

## Common

通用 UI 工具、格式化和可复用行为。

| 文件 | 作用 |
| --- | --- |
| `UI/Common/ScrollWheelBlocker.cs` | 阻止滚轮事件继续冒泡，避免内层滚动区域影响外层滚动。 |
| `UI/Common/SmoothScrollEdgeBounce.cs` | 给滚动区域加边缘缓冲/回弹处理，让滚动手感更平滑。 |
| `UI/Common/StorageNetworkCycleTime.cs` | 获取当前周期时间，用于 UI 上显示时间、趋势或速率。 |
| `UI/Common/StorageNetworkGeyserText.cs` | 间歇泉相关显示文本：元素名、平均产量、储存列表详情、登记详情。 |
| `UI/Common/StorageNetworkKeyedRowCache.cs` | 按 key 复用 UI 行对象，降低列表刷新时的销毁/重建成本。 |
| `UI/Common/StorageNetworkStorageDisplay.cs` | 储存对象显示辅助：分类 key、类型名、物品名、图标等。 |
| `UI/Common/StorageNetworkTextFormatting.cs` | 文本搜索和格式处理：规范化搜索文本、去掉 Klei 链接/标签格式。 |
| `UI/Common/StorageNetworkWindowDrag.cs` | 可拖动窗口逻辑：拖拽、屏幕夹取、布局保存和恢复。 |

## HeaderWindow

顶部/订单窗口相关 UI。这里集中处理产品列表、订单编辑、计划预览、执行跟踪和世界筛选。

| 文件 | 作用 |
| --- | --- |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.cs` | Header Window 总入口：打开/关闭窗口，创建产品列表、订单编辑、订单跟踪区域。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Layout.cs` | Header Window 布局：宽度、高度、pane、viewport、scroll rect 等。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Controls.cs` | Header Window 里复用的小控件：选择按钮、数量按钮、输入框、步进器。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Products.cs` | 产品列表：重建产品按钮、选择产品、产品行显示。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Shared.cs` | Header Window 共用 UI 小组件：section、sub panel、指标块、文本行、图标获取。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.WorldFilter.cs` | 订单窗口里的世界筛选下拉框。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.cs` | 订单编辑主区域：根据选中产品/路线/draft 重建编辑界面。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.Controls.cs` | 订单编辑控件：数量、保留规则、路线、配方选择。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.OrderEditor.Submission.cs` | 订单提交区域：校验提示、页脚按钮、风险颜色、提交状态文本。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.cs` | 计划预览主区域：材料计划、指标、生产链、派工概览。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.DispatchDiagram.cs` | 材料派工图：根节点、材料堆、连线、绝对坐标线条。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Flow.cs` | 生产流程/链路列：流程标题、行、列标签、材料需求关系。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.cs` | 计划台账主表：机器分配表、材料表、分配卡片。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.Materials.cs` | 材料台账行/卡片：材料需求、子路线、缩进层级。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Ledger.Widgets.cs` | 台账使用的小控件：缩进线、固定间距、数值列、状态 badge。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.ResearchCanvas.cs` | 研究树画布底层绘制：节点位置、图标槽、进度线、连接线。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.ResearchTree.cs` | 材料研究树 viewport 和节点结构。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.PlanPreview.Tree.cs` | 计划树节点：配方节点、材料节点、需求分支、连接线。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.cs` | 订单执行跟踪主区域：重建跟踪列表、卡片、标题行、信息行。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.Detail.cs` | 单个订单跟踪详情：订单/机器/材料节点图。 |
| `UI/HeaderWindow/StorageNetworkPanel.HeaderWindow.Tracking.Widgets.cs` | 跟踪界面小控件：进度条、状态 badge、分隔线、圆角图片。 |

## Input

输入框、数字输入和选择工具相关修补。

| 文件 | 作用 |
| --- | --- |
| `UI/Input/StorageNetworkInputBuilder.cs` | 创建 TMP/Klei 数字输入框的工厂方法。 |
| `UI/Input/StorageNetworkInputFieldEvents.cs` | 输入框焦点、值变化、结束编辑、延迟停止编辑事件处理。 |
| `UI/Input/StorageNetworkInputPatchSupport.cs` | 输入框 Harmony/补丁辅助：识别本模组输入框、修正光标和 RectMask 问题。 |
| `UI/Input/StorageNetworkNumberInputField.cs` | 数字输入行为：范围限制、整数限制、显示值同步。 |
| `UI/Input/StorageNetworkSelectionInputHandler.cs` | 选择工具交互辅助，处理特定点击/快捷选择输入。 |
| `UI/Input/StorageNetworkTextInputGuard.cs` | 文本输入保护：点击、选中、失焦、输入样式和编辑状态维护。 |

## Installers

把 UI 挂到游戏原生界面上的安装入口。

| 文件 | 作用 |
| --- | --- |
| `UI/Installers/StorageNetworkCoreSideScreenInstaller.cs` | 把核心建筑侧边栏加入 `DetailsScreen`。 |
| `UI/Installers/StorageNetworkManagementMenuInstaller.cs` | 把仓储网络管理入口按钮加入管理菜单。 |

## Order

订单编辑和跟踪的文本/签名/规则辅助，不直接负责创建大块 UI。

| 文件 | 作用 |
| --- | --- |
| `UI/Order/StorageNetworkOrderEditorSignatureBuilder.cs` | 构建订单编辑界面的签名，用于判断是否需要重建。 |
| `UI/Order/StorageNetworkOrderEditorText.cs` | 订单编辑界面显示文本。 |
| `UI/Order/StorageNetworkOrderTrackingRules.cs` | 订单跟踪筛选、状态或展示规则。 |

## Panel

主仓储网络面板。这里是打开后看到的主要列表、分类、搜索、状态、弹窗和基础控件。

| 文件 | 作用 |
| --- | --- |
| `UI/Panel/StorageNetworkPanel.cs` | 主面板核心字段、状态和基础结构。 |
| `UI/Panel/StorageNetworkPanel.Lifecycle.cs` | 主面板生命周期：显示、关闭、创建、运行时状态重置、右键关闭。 |
| `UI/Panel/StorageNetworkPanel.Window.cs` | 主窗口搭建入口。 |
| `UI/Panel/StorageNetworkPanel.Layout.cs` | 主面板布局辅助：stretch、top stretch、RectTransform 设置。 |
| `UI/Panel/StorageNetworkPanel.ListRefresh.cs` | 列表刷新：保留滚动位置、重建 storage rows、清理列表和分类。 |
| `UI/Panel/StorageNetworkPanel.Items.cs` | 主列表物品数据展开/折叠、物品聚合和显示相关逻辑。 |
| `UI/Panel/StorageNetworkPanel.Rows.cs` | 主列表行：储存类型行、单个储存行、信息行、分类按钮。 |
| `UI/Panel/StorageNetworkPanel.Rows.Items.cs` | 已储存物品行、温度格式、目标选择行。 |
| `UI/Panel/StorageNetworkPanel.Rows.Geysers.cs` | 间歇泉行：喷发状态、间歇泉描述详情。 |
| `UI/Panel/StorageNetworkPanel.Rows.Widgets.cs` | 行内小控件：折叠图标、按钮图标、图标加文本。 |
| `UI/Panel/StorageNetworkPanel.UI.cs` | 主面板通用 UI 创建：折叠头、容器、图片、输入槽、滑条样式。 |
| `UI/Panel/StorageNetworkPanel.UI.Buttons.cs` | 主面板按钮创建：普通按钮、样式按钮、图标按钮、关闭按钮。 |
| `UI/Panel/StorageNetworkPanel.UI.Scrollbars.cs` | 滚动条创建、平滑滚动、滚动条贴图样式。 |
| `UI/Panel/StorageNetworkPanel.UI.SettingsWindow.cs` | 可拖动设置窗口：标题、viewport、屏幕内保持。 |
| `UI/Panel/StorageNetworkPanel.Status.cs` | 主面板状态区：储存概览、网络健康条、健康指标 tile、搜索 tile。 |
| `UI/Panel/StorageNetworkPanel.Style.cs` | 主面板颜色和贴图样式：按钮、box、Klei 蓝/粉样式。 |
| `UI/Panel/StorageNetworkPanel.WorldFilter.cs` | 主面板世界筛选：快照收集、下拉框、选项、筛选文本。 |
| `UI/Panel/StorageNetworkPanel.DragDrop.cs` | 主面板拖放相关交互。 |
| `UI/Panel/StorageNetworkPanel.Modal.cs` | 通用弹窗：丢弃、转移、目标选择、储存设置、间歇泉设置、消息框。 |
| `UI/Panel/StorageNetworkPanel.Modal.Amount.cs` | 数量弹窗：滑条、输入框、数量格式化和解析。 |
| `UI/Panel/StorageNetworkPanel.GeyserSettings.cs` | 间歇泉设置界面逻辑。 |
| `UI/Panel/StorageNetworkPanelHealthMetrics.cs` | 主面板健康指标计算。 |
| `UI/Panel/StorageNetworkPanelListSignature.cs` | 构建主列表签名，用于判断列表是否变化。 |

## Panel/CategorySummary

主面板分类汇总区域，比如每个分类的总量、趋势和采样。

| 文件 | 作用 |
| --- | --- |
| `UI/Panel/CategorySummary/StorageNetworkPanel.CategorySummary.cs` | 主面板分类汇总 UI。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryItemTotal.cs` | 分类汇总中的单项总量模型/计算。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummarySignature.cs` | 分类汇总签名，用于判断是否需要刷新。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryTrend.cs` | 分类汇总趋势数据。 |
| `UI/Panel/CategorySummary/StorageNetworkCategorySummaryTrendSampler.cs` | 分类趋势采样器。 |

## Panel/Enrollable

可登记建筑/对象窗口，把可加入仓储网络的对象列出来。

| 文件 | 作用 |
| --- | --- |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.cs` | 可登记窗口主逻辑。 |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.Rows.cs` | 可登记对象列表行。 |
| `UI/Panel/Enrollable/StorageNetworkPanel.Enrollable.WorldFilter.cs` | 可登记窗口的世界筛选。 |
| `UI/Panel/Enrollable/StorageNetworkEnrollableWindowSignature.cs` | 可登记窗口签名，用于判断刷新。 |

## PlanPreview

计划预览通用计算和文本辅助。HeaderWindow 里的计划预览 UI 会调用这些。

| 文件 | 作用 |
| --- | --- |
| `UI/PlanPreview/StorageNetworkPanZoom.cs` | 计划/树状视图的拖拽和平移缩放。 |
| `UI/PlanPreview/StorageNetworkPlanCategoryOrder.cs` | 计划分类排序和显示名。 |
| `UI/PlanPreview/StorageNetworkPlanPreviewMetrics.cs` | 计划预览尺寸估算：树高度、深度、宽度等。 |
| `UI/PlanPreview/StorageNetworkPlanPreviewText.cs` | 计划预览文本：缺口数量、库存覆盖、生产动作、分配摘要。 |

## ProductionSettings

生产建筑/端口/请求器的设置面板，包括自动化、材料来源、输出目标、库存、限制弹窗。

| 文件 | 作用 |
| --- | --- |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.cs` | 生产设置面板主入口：打开、关闭、创建、刷新、标题、实时更新。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Automation.cs` | 生产建筑自动化卡片：材料请求、输出存储、生产概览。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.ExternalAutomation.cs` | 外部自动化卡片：存储输出、端口请求、发电机材料请求。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Inventory.cs` | 生产设置里的库存卡片和物品行。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.LimitDialog.cs` | 材料请求上限弹窗。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Picker.cs` | 通用选择器窗口：选项行、页脚、打开/关闭。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Picker.Options.cs` | 具体选择器选项：材料来源、端口来源、输出仓库等。 |
| `UI/ProductionSettings/StorageNetworkPanel.ProductionSettings.Widgets.cs` | 生产设置卡片和小控件：布局、指标、状态条。 |
| `UI/ProductionSettings/StorageNetworkMaterialLimitRules.cs` | 材料限制规则：解析输入、格式化最大值、启用时保证限制值。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsSignatureBuilder.cs` | 构建生产设置签名，用于判断是否需要重建。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsStyle.cs` | 生产设置样式和颜色：卡片高度、启用状态、端口状态颜色。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsText.cs` | 生产设置文本：请求模式、端口状态、输出模式、生产状态。 |
| `UI/ProductionSettings/StorageNetworkProductionSettingsViews.cs` | 生产设置用的 view/data holder 类型。 |

## SideScreens

游戏右侧详情栏/侧边栏 UI。

| 文件 | 作用 |
| --- | --- |
| `UI/SideScreens/StorageNetworkCoreSideScreen.cs` | 仓储网络核心建筑的 DetailsScreen 侧边栏内容。 |

## World

游戏世界内浮动文本和世界信息显示。

| 文件 | 作用 |
| --- | --- |
| `UI/World/StorageNetworkWorldDisplay.cs` | 世界名、世界图标、对象所在世界显示辅助。 |
| `UI/World/StorageNetworkWorldTextPanel.cs` | 选中对象时的世界内浮动文本面板控制。 |

## WorldPanel

世界内文本面板的内容提供和视图渲染。

| 文件 | 作用 |
| --- | --- |
| `UI/WorldPanel/DefaultStorageNetworkWorldPanelContentProvider.cs` | 默认内容提供器：根据目标对象构建生产建筑/储存相关内容。 |
| `UI/WorldPanel/StorageNetworkWorldTextPanelView.cs` | 浮动文本面板视图：创建、更新位置、显示隐藏、销毁、诊断日志。 |
