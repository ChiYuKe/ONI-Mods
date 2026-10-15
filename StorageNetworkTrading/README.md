# 存储网络交易 / Storage Network Trading

独立交易附属模组，前置为 **StorageNetwork（储存网络）**。请同时启用本仓库生成的两个模组并重启游戏。只启用储存网络时不会加载交易、行情或 AI 聊天；缺少前置或前置版本过旧时，交易模组会停止初始化并记录原因。

包括随机供应商、库存和交易额度、周期行情、角色头像和身份、好感度、本地对话、AI 群聊。前置要求公开 API 版本 1 或更高。入口、库存查询、交付路由、仓库规则、生产预留量、图标与界面焦点均通过 `StorageNetwork.API` 调用；交易模组不引用前置的 Core、Services、ProductionOrders 或 UI 类型。储存网络不引用交易程序集。接口说明见 [公开 API](../StorageNetwork/API/README.md)。

中文为源文本，英文来自 [translations/en.po](translations/en.po)。AI 配置与前置订单数据一样，保存在当前 `.sav` 文件旁，文件名为 `<存档文件名>.sav.StorageNetworkTradingAi.json`，不嵌入 `.sav` 内部。每个存档独立读取，另存和自动保存时会写出对应的配置文件；API Key 仍使用当前 Windows 账户加密。当前存档尚无配置时，优先迁移旧 `mods/config/StorageNetworkTrading/trading-ai.json`，其次迁移旧 AppData 配置；已有存档配置优先，旧文件保留作为备份。交易状态及聊天记录由本模组的存档组件管理。

构建 `dotnet build StorageNetworkTrading/StorageNetworkTrading.csproj -c Release` 会同时构建前置，两个程序集分别位于各自的 `bin/Release`。发布时分别打包两个模组目录，勿将 `StorageNetwork.dll` 放入交易模组。

Debug 构建会分别部署到游戏的 `mods/Dev/StorageNetwork` 和 `mods/Dev/StorageNetworkTrading`。新模组仍需在游戏模组列表启用。功能细节见 [交易说明](Trading/README.md) 和 [AI 接入说明](Trading/AI_DIALOGUE.md)。

## AI 功能文件开关

聊天室与 AI 设置（齿轮按钮）共用 `TradingAi.enabled` 文件开关，默认关闭并隐藏两个入口；已有 AI 配置不会自动启用它们。关闭时不能打开设置、聊天室或发送 AI 群聊请求，普通交易和本地对话照常使用。

启用时，在以下任一位置创建名为 `TradingAi.enabled` 的空文件，然后重启游戏：

- 游戏用户目录下的 `mods/StorageNetworkTrading/TradingAi.enabled`（与储存网络性能监控的文件开关目录规则一致）。
- 当前交易模组安装目录下的 `TradingAi.enabled`，即 `StorageNetworkTrading.dll` 所在目录。

删除这两个位置的启用文件并重启游戏即可关闭。文件仅控制功能可用性，API 配置仍通过齿轮按钮按存档保存；不要将启用文件加入默认发布包。
