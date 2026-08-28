# 添加到你的Mod中

在Mod根目录创建一个 `lib/` 文件夹，将 `dll` 与 `xml` 文件放入（在[Releases](https://github.com/Cyrillya/NetSimplified-tModLoader/releases/latest)界面下载最新版的这两个文件）  
在 `build.txt` 添加：`dllReferences = NetSimplified`  
最后在VS添加引用即可

要是你还不懂，建议查看[配套示例](../NetSimplifiedExample)

*p.s. `dll` 文件为类库，即本体，`xml` 文件为注释*

## 建立框架

对于你的模组，你只需要在编写一些激活性质的代码即可正常使用此库的全部内容:

在 `Mod` [主类](../NetSimplifiedExample/NetSimplifiedExample.cs)中:

- 在 `Load` 注册需要用到的 `NetModule` 以激活模组功能（重要）
- 在 `Load` 注册 `AutoSyncType` 以激活字段[自动传输功能](AutoSync.md#autosync-自动传输特性)
- 在 `HandlePacket` 调用 `NetModule.ReceiveModule`，以让库接收并处理二进制数据包

此外，还有一些可选功能:

- 在 `Call` 调用 `CrossMod.HandleModCalls` 以支持 `NetModule` 的跨模组调用（常用于允许附属模组调用主模组的包）
- 在 `Load` 调用 `AddContent<NetModuleLoader>()` 与 `AddContent<NetSimplifiedDiagnosticsCommand>()`，以激活调试模块，详见本文文末部分

以下是激活该库除调试模块外全部功能的最小代码:

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

如果你想要知道每一行都是干什么用的，你可以参考[示例模组](../NetSimplifiedExample/NetSimplifiedExample.cs)中的注释

> **注意**：`NetModuleLoader.LoadNetModules()` 会基于 `NetModuleLoader.CurrentMod`（即上方的 `this`）自动获取当前模组的程序集来注册其中的 `NetModule`，因此务必在调用前先设置 `NetModuleLoader.CurrentMod = this;`。若你的 `NetModule` 与 `Mod` 主类不在同一程序集，请改用 `NetModuleLoader.LoadNetModules(Assembly)` 显式传入程序集。

[返回 README](../README.md)
