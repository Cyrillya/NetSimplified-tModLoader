# [AggregateModule 类](../src/AggregateModule.cs)

以一个 `ModPacket` 包的形式发送多个 `NetModule` 包, 能有效避免分散性地多次发包

与普通包一样, 发包时要调用 `AggregateModule.Send(Mod, int, int, bool)`, 注意有一个 `Mod` 参数在前面的，而不是一般的 `NetModule.Send(int, int, bool)`

正常情况下, 其 `NetModule.Type` 应为0, 获取时应调用 `AggregateModule.Get(List<NetModule>)` 而不是 `NetModuleLoader.Get<T>`, 否则会获取到 `null` 值

就是一次性把好几个包一起发出去，避免延迟上多包不同步导致的问题

## 例子

来自配套的示例Mod，[文件在这](../NetSimplifiedExample/Items/ExampleAggregateSender.cs)

```CSharp
// 获取包含了多个 NetModule 包的 AggregateModule 包实例
AggregateModule.Get(new List<NetModule> {
    // 第一个 NetModule 包
    SystemTimePacket.Get(DateTime.Now.ToString(CultureInfo.InvariantCulture)), // 注意逗号
    // 第二个 NetModule 包
    RandomStringPacket.Get()
}).Send(toClient: player.whoAmI); // 发送
```

[返回 README](../README.md)
