using System;
using System.IO;
using System.Reflection;
using NetSimplified;
using NetSimplified.Syncing;
using Terraria.ModLoader;

namespace NetSimplifiedExample;

public class NetSimplifiedExample : Mod
{
    // 在这里注册 NetModuleLoader 并显式加载 AutoSyncType 与 NetModule
    public override void Load() {
        // 注册 NetModuleLoader 以启用网络诊断（数据监视器），并注册服务器诊断指令
        AddContent<NetModuleLoader>();
        AddContent<NetSimplifiedDiagnosticsCommand>();
        // 设置当前模组实例以供 NetModuleLoader 使用
        NetModuleLoader.CurrentMod = this;

        // 先注册基础库中的 AutoSyncType 与示例模组内的 AutoSyncType
        NetModuleLoader.LoadAutoSyncsFrom(typeof(NetModuleLoader).Assembly);
        NetModuleLoader.LoadAutoSyncsFrom(Assembly.GetExecutingAssembly());

        // 加载并注册 NetModule 实例
        NetModuleLoader.LoadNetModules();
    }

    // 这里的 Call 用于处理附属模组的调用请求，附属模组通过 Mod.Call 进行调用，参数 args 的第一个元素为方法名，之后为参数列表
    // 如果你希望别的模组能够通过 Call 获取并发送你的 NetModule，可以参考以下代码
    // 一般来说，建议所有使用了 NetSimplified 的模组都实现一个类似的 Call 来处理 NetSimplified 的调用请求，这样就可以互相调用了
    public override object Call(params object[] args) {
        // 调用 CrossMod.HandleModCalls 对调用进行处理
        // 返回 false 代表并没有在调用 NetSimplified 的接口
        // 返回 null 表示调用不成功
        // 其他返回表示成功调用，返回了对应的处理结果
        object netReply = CrossMod.HandleModCalls(args);
        if (netReply is not false) {
            // 如果 netReply 不是 false，说明调用了 NetSimplified 的接口（无论成功与否），直接返回 netReply
            return netReply;
        }

        // 此处可以正常处理其他非 NetSimplified 的调用请求
        return base.Call(args);
    }

    // 调用 NetModule.ReceiveModule 以进行收包处理，必不可少
    public override void HandlePacket(BinaryReader reader, int whoAmI) {
        NetModule.ReceiveModule(reader, whoAmI);
    }
}