<h1 align="center">Net Simplified</h1>

<div align="center">

[最新版本](https://github.com/Cyrillya/NetSimplified-tModLoader/releases/latest)

一个用于tModLoader联机代码的类库，不需要Mod强引用

</div>

## 编译

如果你想编译这个库，请将源码clone到本地或下载zip并解压，然后将tModLoader Mod源码文件夹（即ModSources）下的 `tModLoader.targets` 文件复制并替换到该项目根目录

用 IDE 打开 [NetSimplified.sln](NetSimplified.sln)，然后右键选择“编译解决方案”即可

## 添加到你的Mod中

在Mod根目录创建一个 `lib/` 文件夹，将 `dll` 与 `xml` 文件放入（在[Releases](https://github.com/Cyrillya/NetSimplified-tModLoader/releases/latest)界面下载最新版的这两个文件）  
在 `build.txt` 添加：`dllReferences = NetSimplified`  
最后在VS添加引用即可

要是你还不懂，建议查看[配套示例](NetSimplifiedExample)

*p.s. `dll` 文件为类库，即本体，`xml` 文件为注释*

### 建立框架

对于你的模组，你只需要在编写一些激活性质的代码即可正常使用此库的全部内容:

在 `Mod` [主类](NetSimplifiedExample/NetSimplifiedExample.cs)中:

- 在 `Load` 注册需要用到的 `NetModule` 以激活模组功能（重要）
- 在 `Load` 注册 `AutoSyncType` 以激活字段[自动传输功能](# AutoSync 自动传输特性)
- 在 `HandlePacket` 调用 `NetModule.ReceiveModule`，以让库接收并处理二进制数据包

此外，还有一些可选功能:

- 在 `Call` 调用 `CrossMod.HandleModCalls` 以支持 `NetModule` 的跨模组调用（可选，常用于允许附属模组调用主模组的包）
- 在 `Load` 调用 `AddContent<NetModuleLoader>()`，以激活发包数据统计（可选，调试用）

以下是激活该库全部功能的最小代码:

```csharp

public class YourMod : Mod {
    public override void Load() {
        NetModuleLoader.CurrentMod = this;
        NetModuleLoader.LoadAutoSyncsFrom(typeof(NetModuleLoader).Assembly);
        NetModuleLoader.LoadAutoSyncsFrom(Assembly.GetExecutingAssembly());
        NetModuleLoader.LoadNetModules();
    }

    public override object Call(params object[] args) {
        object netReply = CrossMod.HandleModCalls(args);
        if (netReply is not false) {
            return netReply;
        }

        return base.Call(args);
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI) {
        NetModule.ReceiveModule(reader, whoAmI);
    }
}
```

如果你想要知道每一行都是干什么用的，你可以参考[示例模组](NetSimplifiedExample/NetSimplifiedExample.cs)中的注释

***

## 配套示例模组

这个库包含两个示例模组：[主示例模组](NetSimplifiedExample)与[附属示例模组](NetSimpExSubmod)，以下是其包含的内容:

### 主示例模组

- `build.txt` 中添加了：`dllReferences = NetSimplified`
- `Mod` [主类](NetSimplifiedExample/NetSimplifiedExample.cs)中激活库功能相关代码
- 四个 `NetModule` 类的[例子](NetSimplifiedExample/Packets)
- 一个发单独包的[例子](NetSimplifiedExample/Items/ExamplePacketSender.cs)
- 一个发 AggregateModule 合并包的[例子](NetSimplifiedExample/Items/ExampleAggregateSender.cs)
- 一个请求传送功能的完整实现[例子](NetSimplifiedExample/Commands/TpCommand.cs)，用于演示自定义传输字段以及[如何为其绑定自动传输](NetSimplifiedExample/Packets/TpPackets.cs#L45)
- 一个[树状数组及其自动传输的实现](NetSimplifiedExample/CustomTypes)，在主模组中没有作用，用于在附属模组中演示跨模组调用

### 附属示例模组

- `build.txt` 中添加了：`dllReferences = NetSimplified` 与 `modReferences = NetSimplifiedExample`
- `Mod` [主类](NetSimpExSubmod/NetSimpExSubmod.cs)中演示了如何自动读取并注册其他模组（程序集）的 `AutoSyncType`，以及通过 `NetModuleLoader.Register` 手动注册 `NetModule` 的方法
- 三个 `NetModule` 类的[例子](NetSimpExSubmod/Packets)，涉及对主模组中的 [FenwickTreeInt](NetSimplifiedExample/CustomTypes/FenwickTreeInt.cs) 类的传输
- 通过指令对存储在服务器的 `List<FenwickTreeInt>` 变量进行查询和修改的[例子](NetSimpExSubmod/Content/Commands/FenwickTreeOptCommand.cs)
- 获取并传输来自其他模组的 `NetModule` 的[例子](NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L41)
- 一个 `FlexibleModule` 注册与使用的[例子](NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L14)

***

## AutoSync 自动传输特性

此库提供了一个自动通过 `Write` 与 `Read` 系列方法传输数据的功能，可以通过对类或字段标记 `[AutoSync]` 特性以使其自动同步，[这个例子](NetSimplifiedExample/Packets/InventoryPacket.cs)可以展示了它的使用方法。

如果你要使用这个特性，请确保你在模组加载时，`NetModule` 注册前，注册了所有的 `AutoSyncType`，可参考[该例子](NetSimplifiedExample/NetSimplifiedExample.cs)

内置了自动传输支持的变量为：`byte, byte[], bool, short, int, long, sbyte, ushort, uint, ulong, float, double, char, string, Vector2, Color, Point, Point16, Item`

可以通过继承 [`AutoSyncType<T>`](src/AutoSyncType.cs) 来实现其他字段的自动传输，具体参见[附属示例模组中的例子](NetSimpExSubmod/NetSimpExSubmod.cs)

若 `Array`, `KeyValuePair<TKey, TValue>`, `IEnumerable<T>` 及其嵌套中包含的所有变量类型均已支持自动传输，则它们本身支持自动传输，不需要另外注册。比如我实现了结构体 `MyStruct` 的自动传输，则 `MyStruct[,]`, `List<List<MyStruct>>` 等变量类型均支持自动传输

**注意：对于没有注册对应 `AutoSyncType` 的变量类型，无法使用自动传输，你仍需要自行编写传输代码，或为其注册自动传输**

### 对类使用特性

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

### 对字段使用特性

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

***

## 跨模组调用

要使用跨模组调用功能，请确保被调用的模组在 `Mod.Call` 中调用了 [CrossMod 接口](src/CrossMod.cs#L17)，具体实现可以参考[示例](NetSimplifiedExample/NetSimplifiedExample.cs#L30)

跨模组调用功能允许你获取并发送其他模组的 `NetModule`，可参考该[例子](NetSimpExSubmod/Content/Items/ExamplePacketSender.cs#L33)以及 [CrossMod.cs](src/CrossMod.cs)

该功能依赖于 `Mod.Call` 实现，会识别 `args[0]` 为 `NetSimplified_GetModule` 与 `NetSimplified_SendModule` 的调用，详见 [CrossMod.cs](src/CrossMod.cs)。这意味着不使用该库的模组也可以通过 `Mod.Call` 来调用实现了接口的模组的 `NetModule`

该功能实际上是“委托”被调用模组发包，发送和接收全程均由被调用模组处理

***

## [NetModule 类](src/NetModule.cs)

这是一个基本的网络传输类，用于控制 `ModPacket` 包的发送/接收  
如果你要自定义发包，直接新建一个类并继承 `NetModule` 类即可（注意引用命名空间 `using NetSimplified`）

一个 `NetModule` 类应实现以下方法

### Send(ModPacket p)

该重写函数用于发包时向 `ModPacket` 写入数据，直接使用 `p.Write(xx)` 即可

本库对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `ModPacket.Write()` 扩展方法，可参考[该文件](src/Extensions.cs)

### Read(BinaryReader r)

*不要与 `Receive()` 相混淆*

该重写函数用于读取数据，需要按照在 `Send(ModPacket p)` 中写入的顺序使用 `r.ReadXX()` 依次读取

本库对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `BinaryReader.ReadXX()` 扩展方法，可参考[该文件](src/Extensions.cs)

### Receive()

该重写函数用于对接收到的数据进行操作，将其与 `Read(BinaryReader r)` 分开以规范程序并且实现发包时的 `runLocally` 功能，在下面会讲到

### 实例化

仅在注册时需要实例化 `NetModule`，然而实际上注册只需在模组加载时调用 `NetModuleLoader.LoadNetModules()` 即可，此方法会对程序集中的所有 `NetModule` 自动使用无参构造函数实例化，并添加到已注册的 `NetModule` 列表中，之后可以使用 [`NetModuleLoader`](src/NetModuleLoader.cs) 内的方法来获取你想要的 `NetModule` 实例

建议使用 `NetModuleLoader.Get<T>()` 方法，使用方法就和 `ModContent.ItemType<T>()` 什么的差不多，这里不多赘述了

要获取其他模组的 `NetModule`，可参考上文[跨模组调用](#跨模组调用)部分

### 例子

`NetModule` 结构可参考：[InventoryPacket](NetSimplifiedExample/Packets/InventoryPacket.cs)

发包可参考：[ExamplePacketSender](NetSimplifiedExample/Items/ExamplePacketSender.cs)

## [AutoSyncType 类](src/AutoSyncType.cs)

用于为某个变量类型注册自动传输特性

一个 `AutoSyncType` 类应实现以下方法

### Send(BinaryWriter bw, object value, MemberInfo fieldInfo)

决定此 AutoSyncType 应该如何将对应类型的值写入网络包

### Read(BinaryReader r, MemberInfo fieldInfo)

决定此 AutoSyncType 应该如何从网络包中读取对应类型的值，需要按照在 `Send(...)` 中写入的顺序使用 `r.ReadXX()` 依次读取

### 实例化

无需也不应该实例化，在模组加载时调用 `NetModuleLoader.LoadAutoSyncsFrom(Assembly asm)` 从程序集中自动读取并注册所有 `AutoSyncType` 即可，详见[示例](NetSimplifiedExample/NetSimplifiedExample.cs)

### 例子

- [AutoSyncPrimitives.cs](src/AutoSyncTypes/AutoSyncPrimitives.cs)
- [AutoSyncColor.cs](src/AutoSyncTypes/AutoSyncColor.cs)
- [AutoSyncItem.cs](src/AutoSyncTypes/AutoSyncItem.cs)


## [AggregateModule 类](src/AggregateModule.cs)

以一个 `ModPacket` 包的形式发送多个 `NetModule` 包, 能有效避免分散性地多次发包

与普通包一样, 发包时要调用 `AggregateModule.Send(Mod, int, int, bool)`, 注意有一个 `Mod` 参数在前面的，而不是一般的 `NetModule.Send(int, int, bool)`

正常情况下, 其 `NetModule.Type` 应为0, 获取时应调用 `AggregateModule.Get(List<NetModule>)` 而不是 `NetModuleLoader.Get<T>`, 否则会获取到 `null` 值

就是一次性把好几个包一起发出去，避免延迟上多包不同步导致的问题

### 例子

来自配套的示例Mod，[文件在这](NetSimplifiedExample/Items/ExampleAggregateSender.cs)

```CSharp
// 获取包含了多个 NetModule 包的 AggregateModule 包实例
AggregateModule.Get(new List<NetModule> {
    // 第一个 NetModule 包
    SystemTimePacket.Get(DateTime.Now.ToString(CultureInfo.InvariantCulture)), // 注意逗号
    // 第二个 NetModule 包
    RandomStringPacket.Get()
}).Send(toClient: player.whoAmI); // 发送
```

***

## [FlexibleModule 类](src/FlexibleModule.cs)

`FlexibleModule` 是一种特殊的 `NetModule`，允许在**不继承 `NetModule` 的情况下**，通过构造函数直接声明包内容与收包行为，适合用在不值得为其单独新建文件的简单包场景。

### 注册

`FlexibleModule` 无法通过 `LoadNetModules()` 自动注册，需要使用 `NetModuleLoader.Register` 手动注册，并将返回的实例保存为静态字段以供后续使用。

注册代码只需在**服务器和客户端各执行一次**即可，放在任何模组加载时运行一次的重写函数中均可（如 `ModItem.SetStaticDefaults`、`Mod.Load` 等）：

```csharp
public class MyItem : ModItem {
    private static FlexibleModule _myModule;

    public override void SetStaticDefaults() {
        _myModule = NetModuleLoader.Register(new FlexibleModule(
            "MyPacket",                                      // 唯一名称
            self => {                                        // 收包回调，self 为模块自身实例
                var number = self.GetValue<int>(0);
                var text   = self.GetValue<string>(1);
                Main.NewText($"{number}: {text}");
            },
            [typeof(int), typeof(string)]                   // 字段类型列表
        ));
    }
}
```

**注意：** 在调用 `Register` 前，需要确保所有字段类型对应的 `AutoSyncType` 已通过 `NetModuleLoader.LoadAutoSyncsFrom` 注册，否则会抛出 `InvalidOperationException`。

### 构造函数参数

| 参数 | 类型 | 说明 |
|---|---|---|
| `name` | `string` | 模块唯一名称，不能与其他已注册的 `FlexibleModule` 重复 |
| `receiveAction` | `Action<FlexibleModule>` | 收包时执行的回调，参数 `self` 为模块自身实例，可通过它调用 `GetValue` 读取字段值 |
| `args` | `Type[]` | 包含的字段类型数组，所有类型须已注册对应的 `AutoSyncType` |
| `attributes` | `Attribute[]`（可选） | 与 `args` 一一对应的特性数组，用于控制字段传输行为（如 `ItemSyncAttribute`、`ColorSyncAttribute`），可为 `null` |

### Set(object[] args)

发包前需调用 `Set` 为包中的字段赋值，赋值顺序须与构造时声明的 `args` 类型顺序一致：

```csharp
_myModule.Set([42, "hello"]);
_myModule.Send(toClient: player.whoAmI);
```

### GetValue\<T\>(int index)

在收包回调中通过索引读取字段值，索引从 0 开始，与 `args` 类型顺序对应：

```csharp
self => {
    int number   = self.GetValue<int>(0);
    string text  = self.GetValue<string>(1);
}
```

### 例子

来自配套的附属示例Mod，[文件在这](NetSimpExSubmod/Content/Items/ExamplePacketSender.cs)

```csharp
// 在任意模组加载时运行一次的重写函数中注册，此处以 SetStaticDefaults 为例
_saySmthModule = NetModuleLoader.Register(new FlexibleModule("SaySmth",
    self => Main.NewText(self.GetValue<string>(0), Main.DiscoColor),
    [typeof(string)]));

// 发包
_saySmthModule.Set(["Hello, World!"]);
_saySmthModule.Send(toClient: player.whoAmI);
```



发包调用 `NetModule.Send` 即可，获取包实例看上面的

### 传参

- `toClient` -> 如果不是 `-1`, 则包<b>只会</b>发送给对应的客户端
- `ignoreClient` -> 如果不是 `-1`, 则包<b>不会</b>发送给对应的客户端
- `runLocally` -> 如果为 `true` 则在发包时会调用相应 `NetModule` 包的 `NetModule.Receive()` 方法，<b>默认为 `false`</b>

若 `toClient` 和 `ignoreClient` 皆为 `-1` 时

- 在服务器调用 `Send` -> 发给所有客户端
- 在客户端调用 `Send` -> 发给服务器
  
***

## 扩展方法
直接看 [Extensions](src/Extensions.cs) 吧（摆烂
