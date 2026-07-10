# Logic DIY 模块清单

共 56 个模块。新增模块时必须同步更新 HTML `nodeMeta`、JS `evaluatePreviewNode`、C# `EvaluateRuntimeNumber` switch、以及本文件。

## 数据源 (sources)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `material` | `MaterialCondition` | 0 | 1 | 否 | 所选材料在仓储网络中的数量 (kg) |
| `constant` | `Constant` | 0 | 1 | 否 | 固定数值，常做阈值 |

## 时序 (timing)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `timer` | `TimerPulse` | 0 | 1 | 是 | 按设定秒数周期性输出短暂脉冲 |
| `cycle4` | `Cycle4` | 0 | 4 | 是 | 4 个输出端口轮流输出 1 |

## 信号变换 (signal)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `split4` | `Split4` | 1 | 4 | 否 | 0-15 合成信号拆为 4 路布尔 |
| `merge4` | `Merge4` | 4 | 1 | 否 | 4 路布尔合并为 0-15 合成信号 |
| `output` | `Output` | 4 | 0 | 否 | 输出到游戏自动化端口 |
| `map-range` | `MapRange` | 1 | 1 | 否 | 线性映射 InMin-Max → OutMin-Max |
| `select` | `Select` | 1+N | 1 | 否 | 根据 Sel 值选一路数据输出（默认 N=6，2-12 可调） |

## 调试 (debug)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `test-signal` | `TestSignal` | 1 | 0 | 否 | 显示信号值，不参与输出 |

## 条件判定 (conditions)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `logic-gt` | `GreaterThan` | 2 | 1 | 否 | A > B → 1 |
| `logic-lt` | `LessThan` | 2 | 1 | 否 | A < B → 1 |
| `logic-eq` | `Equal` | 2 | 1 | 否 | A = B → 1 |
| `logic-range` | `Range` | 3 | 1 | 否 | A 在 [B,C] 内 → 1 |
| `material-low` | `MaterialLow` | 0 | 1 | 否 | 材料量 < 阈值 → 1 |
| `material-high` | `MaterialHigh` | 0 | 1 | 否 | 材料量 ≥ 阈值 → 1 |

## 数学运算 (math)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `math-add` | `Add` | 2 | 1 | 否 | A + B |
| `math-subtract` | `Subtract` | 2 | 1 | 否 | A - B |
| `math-multiply` | `Multiply` | 2 | 1 | 否 | A × B |
| `math-divide` | `Divide` | 2 | 1 | 否 | A ÷ B |
| `math-mod` | `Modulo` | 2 | 1 | 否 | A % B |
| `math-clamp` | `Clamp` | 3 | 1 | 否 | Clamp(A, B, C) |
| `math-negate` | `Negate` | 1 | 1 | 否 | -A |
| `math-min` | `Min` | 2 | 1 | 否 | Min(A, B) |
| `math-max` | `Max` | 2 | 1 | 否 | Max(A, B) |

## 布尔逻辑 (bool)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `bool-and` | `BoolAnd` | 2 | 1 | 否 | A ∧ B |
| `bool-nand` | `BoolNand` | 2 | 1 | 否 | ¬(A ∧ B) |
| `bool-or` | `BoolOr` | 2 | 1 | 否 | A ∨ B |
| `bool-nor` | `BoolNor` | 2 | 1 | 否 | ¬(A ∨ B) |
| `bool-xor` | `BoolXor` | 2 | 1 | 否 | A ⊕ B |
| `bool-not` | `BoolNot` | 1 | 1 | 否 | ¬A |
| `bool-selector` | `Selector` | 3 | 1 | 否 | A ? B : C |
| `bool-true` | `BoolTrue` | 0 | 1 | 否 | 恒 1 |
| `bool-false` | `BoolFalse` | 0 | 1 | 否 | 恒 0 |

## 状态控制 (state)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `delay` | `Delay` | 1 | 1 | 是 | 输入持续真达设定秒数后输出 1，变假立即复位 |
| `number-changed` | `NumberChanged` | 1 | 3 | 是 | 检测数值变大/变小/任意变化，分别输出脉冲 |
| `latch` | `Latch` | 2 | 1 | 是 | Set 置 1，Reset 清 0 |
| `counter` | `Counter` | 2 | 1 | 是 | 上升沿计数，Reset 清零 |
| `random` | `RandomChance` | 0 | 1 | 是 | 按概率随机输出 1 |
| `edge-pulse` | `EdgePulse` | 1 | 1 | 是 | 上升沿输出一帧脉冲 |
| `hysteresis` | `Hysteresis` | 1 | 1 | 是 | 迟滞比较器（上下限） |
| `toggle` | `Toggle` | 1 | 1 | 是 | 上升沿翻转输出 |
| `pulse-shaper` | `PulseShaper` | 1 | 1 | 是 | 脉冲延伸设定秒数 |
| `material-change` | `MaterialChanged` | 0 | 1 | 是 | 材料量变化时输出脉冲 |
| `sequence` | `Sequence` | 2 | 2 | 是 | 步进序列器 |

## 网络状态 (network)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `inventory-percent` | `InventoryPercent` | 0 | 1 | 否 | 仓储占用率 0-100 |
| `inventory-stored` | `InventoryStored` | 0 | 1 | 否 | 已用容量 kg |
| `inventory-remaining` | `InventoryRemaining` | 0 | 1 | 否 | 剩余容量 kg |
| `inventory-capacity` | `InventoryCapacity` | 0 | 1 | 否 | 总容量 kg |
| `power-percent` | `PowerPercent` | 0 | 1 | 否 | 电量百分比 0-100 |
| `power-stored` | `PowerStored` | 0 | 1 | 否 | 当前电量 J |
| `power-remaining` | `PowerRemaining` | 0 | 1 | 否 | 剩余可存电量 J |
| `power-capacity` | `PowerCapacity` | 0 | 1 | 否 | 电池总容量 J |
| `building-status` | `BuildingStatus` | 0 | 1 | 否 | 目标建筑可用性 |
| `building-signal` | `BuildingSignal` | 0 | 1 | 否 | 目标建筑输出信号值 |

## 整理 (organize)
| HTML type | C# module | 输入 | 输出 | 有状态 | 说明 |
|-----------|-----------|------|------|--------|------|
| `group` | `Group` | 0 | 0 | 否 | 视觉分组框 |

## 新增模块检查清单
- [ ] HTML `nodeMeta` 添加条目
- [ ] HTML `sidebarGroups` 加入分类
- [ ] JS `evaluatePreviewNode` 实现预览逻辑
- [ ] C# `EvaluateRuntimeNumber` switch 添加 case
- [ ] `docGroups` 添加文档条目
- [ ] 中英文 `sidebarText` 添加描述
- [ ] 本文件更新
- [ ] 同步到 `bin/Release` 和 `Documents/.../Dev/StorageNetwork`
