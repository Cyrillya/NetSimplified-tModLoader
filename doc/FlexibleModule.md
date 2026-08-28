# [FlexibleModule 类](../src/FlexibleModule.cs)

`FlexibleModule` 是一种特殊的 `NetModule`，允许在**不继承 `NetModule` 的情况下**，通过构造函数直接声明包内容与收包行为，适合用在不值得为其单独新建文件的简单包场景。

## 注册

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

## 构造函数参数

| 参数 | 类型 | 说明 |
|---|---|---|
| `name` | `string` | 模块唯一名称，不能与其他已注册的 `FlexibleModule` 重复 |
| `receiveAction` | `Action<FlexibleModule>` | 收包时执行的回调，参数 `self` 为模块自身实例，可通过它调用 `GetValue` 读取字段值 |
| `args` | `Type[]` | 包含的字段类型数组，所有类型须已注册对应的 `AutoSyncType` |
| `attributes` | `Attribute[]`（可选） | 与 `args` 一一对应的特性数组，用于控制字段传输行为（如 `ItemSyncAttribute`、`ColorSyncAttribute`），可为 `null` |

## Set(object[] args)

发包前需调用 `Set` 为包中的字段赋值，赋值顺序须与构造时声明的 `args` 类型顺序一致：

```csharp
_myModule.Set([42, "hello"]);
_myModule.Send(toClient: player.whoAmI);
```

## GetValue\<T\>(int index)

在收包回调中通过索引读取字段值，索引从 0 开始，与 `args` 类型顺序对应：

```csharp
self => {
    int number   = self.GetValue<int>(0);
    string text  = self.GetValue<string>(1);
}
```

## 例子

来自配套的附属示例Mod，[文件在这](../NetSimpExSubmod/Content/Items/ExamplePacketSender.cs)

```csharp
// 在任意模组加载时运行一次的重写函数中注册
_saySmthModule = NetModuleLoader.Register(new FlexibleModule("SaySmth",
    self => Main.NewText(self.GetValue<string>(0), Main.DiscoColor),
    [typeof(string)]));

// 发包
_saySmthModule.Set(["Hello, World!"]);
_saySmthModule.Send(toClient: player.whoAmI);
```

[返回 README](../README.md)
