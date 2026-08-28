# [NetModule 类](../src/NetModule.cs)

这是一个基本的网络传输类，用于控制 `ModPacket` 包的发送/接收  
如果你要自定义发包，直接新建一个类并继承 `NetModule` 类即可（注意引用命名空间 `using NetSimplified`）

一个 `NetModule` 类应实现以下方法

## Send(ModPacket p)

该重写函数用于发包时向 `ModPacket` 写入数据，直接使用 `p.Write(xx)` 即可

本库对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `ModPacket.Write()` 扩展方法，可参考[该文件](../src/Extensions.cs)

## Read(BinaryReader r)

*不要与 `Receive()` 相混淆*

该重写函数用于读取数据，需要按照在 `Send(ModPacket p)` 中写入的顺序使用 `r.ReadXX()` 依次读取

本库对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `BinaryReader.ReadXX()` 扩展方法，可参考[该文件](../src/Extensions.cs)

## Receive()

该重写函数用于对接收到的数据进行操作，将其与 `Read(BinaryReader r)` 分开以规范程序并且实现发包时的 `runLocally` 功能，在下面会讲到

## 实例化

仅在注册时需要实例化 `NetModule`，然而实际上注册只需在模组加载时调用 `NetModuleLoader.LoadNetModules()` 即可，此方法会对程序集中的所有 `NetModule` 自动使用无参构造函数实例化，并添加到已注册的 `NetModule` 列表中，之后可以使用 [`NetModuleLoader`](../src/NetModuleLoader.cs) 内的方法来获取你想要的 `NetModule` 实例

建议使用 `NetModuleLoader.Get<T>()` 方法，使用方法就和 `ModContent.ItemType<T>()` 什么的差不多，这里不多赘述了

要获取其他模组的 `NetModule`，可参考上文[跨模组调用](CrossMod.md#跨模组调用)部分

## 例子

`NetModule` 结构可参考：[InventoryPacket](../NetSimplifiedExample/Packets/InventoryPacket.cs)

发包可参考：[ExamplePacketSender](../NetSimplifiedExample/Items/ExamplePacketSender.cs)

[返回 README](../README.md)
