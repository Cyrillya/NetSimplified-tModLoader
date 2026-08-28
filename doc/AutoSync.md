# AutoSync 自动传输特性

此库提供了一个自动通过 `Write` 与 `Read` 系列方法传输数据的功能，可以通过对类或字段标记 `[AutoSync]` 特性以使其自动同步，[这个例子](../NetSimplifiedExample/Packets/InventoryPacket.cs)可以展示了它的使用方法。

如果你要使用这个特性，请确保你在模组加载时，`NetModule` 注册前，注册了所有的 `AutoSyncType`，可参考[该例子](../NetSimplifiedExample/NetSimplifiedExample.cs)

内置了自动传输支持的变量为：`byte, byte[], bool, short, int, long, sbyte, ushort, uint, ulong, float, double, char, string, Vector2, Color, Point, Point16, Item`

可以通过继承 [`AutoSyncType<T>`](../src/AutoSyncType.cs) 来实现其他字段的自动传输，具体参见[附属示例模组中的例子](../NetSimpExSubmod/NetSimpExSubmod.cs)

若 `Array`, `KeyValuePair<TKey, TValue>`, `IEnumerable<T>` 及其嵌套中包含的所有变量类型均已支持自动传输，则它们本身支持自动传输，不需要另外注册。比如我实现了结构体 `MyStruct` 的自动传输，则 `MyStruct[,]`, `List<List<MyStruct>>` 等变量类型均支持自动传输

**注意：对于没有注册对应 `AutoSyncType` 的变量类型，无法使用自动传输，你仍需要自行编写传输代码，或为其注册自动传输**

## 对类使用特性

对类使用 `AutoSync` 特性可以让此类中所有支持自动传输的字段传输，用法示例如下:

```csharp
// 使用 AutoSync 特性以使变量自动传输，免去自己写 Send 和 Receive 的功夫
[AutoSync]
public class ExamplePacket : NetModule {
    private byte _exampleByte;
    [ItemSync(syncStack: true, syncFavorite: true)] private Item _exampleItem;
}
```

在这个例子中，`_exampleByte` 与 `_exampleItem` 变量均会自动传输，不需要手动写 `packet.Write(_exampleByte)` 与 `_exampleByte = reader.ReadByte()` 这类麻烦的代码。

其中，`_exampleItem` 使用了特性 `[ItemSync(syncStack: true, syncFavorite: true)]`，这意味着它会同时传输堆叠与是否被标记为收藏的信息，若 `Item` 类型传输不使用特性，则默认只传输堆叠，而不传输收藏信息。对于 `Color` 类型，可以使用 `ColorSync` 特性来决定是否传输 `Alpha` 信息（透明度）

## 对字段使用特性

对字段使用 `AutoSync` 特性可以选择性地自动传输需要的变量，用法示例如下:

```csharp
public class ExamplePacket : NetModule {
    // 使用 AutoSync 特性以使变量自动传输，免去自己写 Send 和 Receive 的功夫
    [AutoSync] private byte _exampleByte;
    private Item _exampleItem;

    public override void Send(ModPacket p) {
        p.Write(_exmapleItem, writeStack: true, writeFavorite: true);
    }

    public override void Read(BinaryReader r) {
        _exmapleItem = r.ReadItem(readStack: true, readFavorite: true);
    }
}
```

在这个例子中，只有 `_exampleByte` 会自动传输，而 `_exmapleItem` 则通过手动编写代码传输。这种方法可以使字段有选择性地自动传输，而在不同的情况下传输不同的变量，便于对数据包的特定操作。

[返回 README](../README.md)
