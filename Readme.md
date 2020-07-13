# DelayedEventManager
---

## 简介

本项目用于解决在线实时游戏中，多种延时事件与单线程游戏逻辑之间的冲突，采用线程安全的优先级队列将延时事件根据预计到期的时间排序，将到期事件送入输出队列中供实际处理程序完成处理，提供了可靠的、资源占用低的延时事件处理机制。

- 支持多线程并发加入延时事件
- 支持可调节、动态的性能平衡（CPU占用与事件预期/实际时间差）
- 线程池、定时器等资源占用低
- 锁结构简单

## 实现

#### ConcurrentPriorityQueue.cs

线程安全的优先级队列（最小堆），提供如下接口：

|          接口          |                     备注                      |
| :--------------------: | :-------------------------------------------: |
|        `Count`         |                 队列元素数量                  |
|    `Peek` `TryPeek`    |        获取队列中优先级`Key`最小的元素        |
|       `Enqueue`        |         加入新的数据对`(Key, Value)`          |
| `Dequeue` `TryDequeue` | 获取队列中优先级`Key`最小的元素并将其移出队列 |

提供如下参数用于性能调优：

|      参数      |       备注       |
| :------------: | :--------------: |
| `InitCapacity` | 队列初始分配大小 |

#### TaskManager.cs

将多种延时事件按预期发生事件转为待处理事件的组件，提供如下接口：

|   接口   |                         备注                         |
| :------: | :--------------------------------------------------: |
| `Output` |      达到时限的待处理事件队列（供外部程序处理）      |
| `Closed` |                   组件是否停止工作                   |
| `Start`  |                       启动组件                       |
| `Close`  | 关闭组件（**其中对外告知`Output`终止的机制不完善**） |
|  `Add`   |                   加入新的延时事件                   |

提供如下参数用于性能调优：

|   参数（单位ms）    |                     备注                     |
| :-----------------: | :------------------------------------------: |
|     `InitBusy`      |                忙等待初始时限                |
|  `ToleratedDelta`   |          容忍的事件预期/实际时间差           |
|     `MinusStep`     | 产生容忍的时间差内的处理时，忙等待时限减少量 |
|   `MultipleRatio`   |  产生容忍的时间差外的处理时，忙等待时限倍率  |
| `MaxBusy` `MinBusy` |                忙等待时限范围                |

本组件以延时事件的预期发生时间建立优先级队列，在每次循环时检测队列头部事件是否到期，进行以下处理：

- 事件到期：从优先级队列中移除转为待处理事件，进入`Output`，再次检测队列头部事件；
- 事件到期剩余事件小于忙等待时限：继续循环检测队列头部事件（忙等待）；
- 事件到期剩余事件大于忙等待时限：等待条件变量，通过定时器（定时时长为到当前队列头部事件预期时间前一小段时间（忙等待时限））或`Add`唤醒，唤醒后检测队列头部事件。

忙等待时限通过一定规则自适应调整，用于平衡CPU占用与反应速度（事件预期/实际时间差）：

- 当到期事件的实际事件与预期时间小于`ToleratedDelta`时，将忙等待时限减少`MinusStep`；反之乘`MultipleRatio`。同时规则设定忙等待时限的初始值为`InitBusy`，上限为`MaxBusy`，下限为`MinBusy`。