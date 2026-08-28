# 跨模组调用

要使用跨模组调用功能，请确保被调用的模组在 `Mod.Call` 中调用了 [CrossMod 接口](../src/CrossMod.cs#L17)，具体实现可以参考[示例](../NetSimplifiedExample/NetSimplifiedExample.cs#L30)

跨模组调用功能允许你获取并发送其他模组的 `NetModule`，可参考该[例子](../NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L41)以及 [CrossMod.cs](../src/CrossMod.cs)

该功能依赖于 `Mod.Call` 实现，会识别 `args[0]` 为以下操作名称的调用，详见 [CrossMod.cs](../src/CrossMod.cs)：

| 操作名称 | 说明 |
|---|---|
| `NetSimplified_GetModule` | 按名称获取该模组中的 `NetModule` 实例（返回 `object`） |
| `NetSimplified_SendModule` | 对传入的 `NetModule` 实例调用 `Send`（需先通过 `GetModule` 获取） |
| `NetSimplified_SetAndSendFlexibleModule` | 在目标模组的程序集上下文中完成 `FlexibleModule` 的 `Set` 与 `Send`（用于跨模组调用 `FlexibleModule`） |

这意味着不使用该库的模组也可以通过 `Mod.Call` 来调用实现了接口的模组的 `NetModule`

该功能实际上是“委托”被调用模组发包，发送和接收全程均由被调用模组处理

## 跨模组调用 FlexibleModule

由于 tModLoader 会为每个模组各自加载一份独立的 `NetSimplified.dll` 副本，不同模组中的 `FlexibleModule` 类型在运行时并非同一个类型，无法直接跨程序集强制转换。因此，**不能**尝试获取其他模组的 `FlexibleModule` 实例后直接使用，而应使用 `CrossMod.TrySendExternalFlexibleModule`，让目标模组在自己的上下文中完成 Set 与 Send：

```csharp
// ✅ 正确做法：通过 TrySendExternalFlexibleModule 委托目标模组执行
CrossMod.TrySendExternalFlexibleModule("TargetMod", "MyFlexPacket",
    [value1, value2], toClient: player.whoAmI);
```

若目标模组未加载，该方法静默返回 `false`，不会产生副作用。可参考[主示例模组的例子](../NetSimplifiedExample/Items/ExamplePacketSender.cs)

[返回 README](../README.md)
