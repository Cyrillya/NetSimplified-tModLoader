# Net Simplified
这是一个用于tModLoader联机代码的类库  
不需要Mod强引用(因为根本没有Mod)

[最新版本在这里](https://github.com/Crapsky233/NetSimplified-tModLoader/releases/latest)
***
## 添加到你的Mod中
在Mod根目录创建一个 `lib/` 文件夹，将 `dll` 与 `xml` 文件放入（在[Releases](https://github.com/Crapsky233/NetSimplified-tModLoader/releases/latest)界面下载最新版的这两个文件）  
在build.txt添加: `dllReferences = NetSimplified`  
最后在VS添加引用即可

要是你还不懂，[更好的体验](https://gitee.com/MyGoold/improve-game)欢迎您

*p.s. `dll` 文件为类库，即本体，`xml` 文件为注释*
***
## [NetModule 类](NetModule.cs)
这是一个基本的网络传输类，用于控制 `ModPacket` 包的发送/接收  
如果你要自定义发包，直接新建一个类并继承 `NetModule` 类即可（注意引用命名空间 `using NetSimplified`）

一个 `NetModule` 类应包含以下函数

### Send(ModPacket p)
该重写函数用于发包时向 `ModPacket` 写入数据，直接使用 `p.Write(xx)` 即可

本模组对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `ModPacket.Write()` 扩展方法，可参考[该文件](Extensions.cs)

### Read(BinaryReader r)
*不要与 `Receive()` 相混淆*

该重写函数用于读取数据，需要按照在 `Send(ModPacket p)` 中写入的顺序使用 `r.ReadXX()` 依次读取

本模组对于数据类型 `Point`, `Point16`, `Item`, `Item[]` 添加了 `BinaryReader.ReadXX()` 扩展方法，可参考[该文件](Extensions.cs)

### Receive()
该重写函数用于对接收到的数据进行操作，将其与 `Read(BinaryReader r)` 分开以规范程序并且实现发包时的 `runLocally` 功能，在下面会讲到

### 实例化
`NetModule` 类继承了类 `ModType`，即会在模组加载时自动读取并创建一个实例类，因此你不能重写构造函数，而应该使用 [`NetModuleLoader`](NetModuleLoader.cs) 内的方法来获取你想要的 `NetModule` 实例  
建议使用 `NetModuleLoader.Get<T>()` 方法，使用方法就和 `ModContent.ItemType<T>()` 什么的差不多，这里不多赘述了

### 例子
这个我暂时建议你看看[更好的体验](https://gitee.com/MyGoold/improve-game/tree/master/Common/Packets)里的用法，因为我懒得写ExampleMod了（

更好的体验里的包结构: `Send`, `Read`, `Receive` 肯定必不可少的。每个包类都有一个静态名为 `Get` 的方法，传入参数为这个包应该有的数据，使用 `NetModuleLoader.Get<T>()` 获取到包实例，然后将数据逐一赋值

这里是一个例子
```CSharp
// 第一张创建包的方案，逐一传入
public static ItemUsePacket Get(int whoAmI, float itemRotation, int direction, short itemAnimation)
{
    // 获取实例
    var module = NetModuleLoader.Get<ItemUsePacket>();
    // 逐一传入
    module.whoAmI = (byte)whoAmI;
    module.itemRotation = itemRotation;
    module.direction = direction;
    module.itemAnimation = itemAnimation;
    // 返回实例
    return module;
}

// 第二种创建包的方案，传入Player实例然后调用上面那个
public static ItemUsePacket Get(Player player) => Get(player.whoAmI, player.itemRotation, player.direction, (short)player.itemAnimation);
```
*（注意，直接复制粘贴是不行的，要复制建议你去下载整个文件）*
***
## [AggregateModule 类](AggregateModule.cs)
以一个 `ModPacket` 包的形式发送多个 `NetModule` 包, 能有效避免分散性地多次发包

与普通包一样, 发包时要调用 `AggregateModule.Send(Mod, int, int, bool)`, 注意有一个 `Mod` 参数在前面的，而不是一般的 `NetModule.Send(int, int, bool)`

正常情况下, 其 `NetModule.Type` 应为0, 获取时应调用 `AggregateModule.Get(List<NetModule>)` 而不是 `NetModuleLoader.Get<T>`, 否则会获取到 `null` 值

就是一次性把好几个包一起发出去，避免延迟上多包不同步导致的问题，非常的好用  
### 例子
依然是来自更好的体验，[文件在这](https://gitee.com/MyGoold/improve-game/blob/master/Common/Packets/NetAutofisher/OpenFisherPackets.cs#L46)
```CSharp
// 获取包含了多个 NetModule 包的 AggregateModule 包实例
var packets = AggregateModule.Get(
    // 传参
    new() {
        // 第一个包
        FisherAllFiltersPacket.Get(
            position,
            autofisher.CatchCrates,
            autofisher.CatchAccessories,
            autofisher.CatchTools,
            autofisher.CatchWhiteRarityCatches,
            autofisher.CatchNormalCatches
        ), // 留意逗号
        // 第二个包
        LocatePointPacket.Get(
            position,
            autofisher.locatePoint
        ), // 留意逗号
        // 第二个包
        ItemsSyncAllPacket.Get(
            position,
            true,
            autofisher.fishingPole,
            autofisher.bait,
            autofisher.accessory,
            autofisher.fish
        ) // 这里最后一个没有逗号了
    }
);
// 发包
packets.Send(Mod, -1, -1, false);
```

***
## 发包
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
直接看 [Extensions](Extensions.cs) 吧（摆烂