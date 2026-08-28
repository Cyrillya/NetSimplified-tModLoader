# 配套示例模组

这个库包含两个示例模组：[主示例模组](../NetSimplifiedExample)与[附属示例模组](../NetSimpExSubmod)，以下是其包含的内容:

## 主示例模组

- `build.txt` 中添加了：`dllReferences = NetSimplified`
- `Mod` [主类](../NetSimplifiedExample/NetSimplifiedExample.cs)中激活库功能相关代码
- 四个 `NetModule` 类的[例子](../NetSimplifiedExample/Packets)
- 一个发单独包的[例子](../NetSimplifiedExample/Items/ExamplePacketSender.cs)，其中也包含了调用并发送附属模组的 `FlexibleModule` 的[例子](../NetSimplifiedExample/Items/ExamplePacketSender.cs#L31)
- 一个发 AggregateModule 合并包的[例子](../NetSimplifiedExample/Items/ExampleAggregateSender.cs)
- 一个请求传送功能的完整实现[例子](../NetSimplifiedExample/Commands/TpCommand.cs)，用于演示自定义传输字段以及[如何为其绑定自动传输](../NetSimplifiedExample/Packets/TpPackets.cs#L45)
- 一个[树状数组及其自动传输的实现](../NetSimplifiedExample/CustomTypes)，在主模组中没有作用，用于在附属模组中演示跨模组调用
- [激活调试功能](../NetSimplifiedExample/UI/ExampleDiagnosticsUISystem.cs)的相关代码

## 附属示例模组

- `build.txt` 中添加了：`dllReferences = NetSimplified` 与 `modReferences = NetSimplifiedExample`
- `Mod` [主类](../NetSimpExSubmod/NetSimpExSubmod.cs)中演示了如何自动读取并注册其他模组（程序集）的 `AutoSyncType`，以及通过 `NetModuleLoader.Register` 手动注册 `NetModule` 的方法
- 三个 `NetModule` 类的[例子](../NetSimpExSubmod/Packets)，涉及对主模组中的 [FenwickTreeInt](../NetSimplifiedExample/CustomTypes/FenwickTreeInt.cs) 类的传输
- 通过指令对存储在服务器的 `List<FenwickTreeInt>` 变量进行查询和修改的[例子](../NetSimpExSubmod/Content/Commands/FenwickTreeOptCommand.cs)
- 获取并传输来自其他模组的 `NetModule` 的[例子](../NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L41)
- 一个 `FlexibleModule` 注册与使用的[例子](../NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L14)

[返回 README](../README.md)
