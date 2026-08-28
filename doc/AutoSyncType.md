# [AutoSyncType 类](../src/AutoSyncType.cs)

用于为某个变量类型注册自动传输特性

一个 `AutoSyncType` 类应实现以下方法

## Send(BinaryWriter bw, object value, MemberInfo fieldInfo)

决定此 AutoSyncType 应该如何将对应类型的值写入网络包

## Read(BinaryReader r, MemberInfo fieldInfo)

决定此 AutoSyncType 应该如何从网络包中读取对应类型的值，需要按照在 `Send(...)` 中写入的顺序使用 `r.ReadXX()` 依次读取

## 实例化

无需也不应该实例化，在模组加载时调用 `NetModuleLoader.LoadAutoSyncsFrom(Assembly asm)` 从程序集中自动读取并注册所有 `AutoSyncType` 即可，详见[示例](../NetSimplifiedExample/NetSimplifiedExample.cs)

## 例子

- [AutoSyncPrimitives.cs](../src/AutoSyncTypes/AutoSyncPrimitives.cs)
- [AutoSyncColor.cs](../src/AutoSyncTypes/AutoSyncColor.cs)
- [AutoSyncItem.cs](../src/AutoSyncTypes/AutoSyncItem.cs)

[返回 README](../README.md)
