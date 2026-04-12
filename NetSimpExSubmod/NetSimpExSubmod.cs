using System.IO;
using System.Reflection;
using NetSimpExSubmod.Packets;
using NetSimplified;
using Terraria;
using Terraria.ModLoader;

namespace NetSimpExSubmod;

public class NetSimpExSubmod : Mod
{
    internal static FlexibleModule SaySmthModule;

    public override void Load() {
        // 设置当前模组实例以供 NetModuleLoader 使用
        NetModuleLoader.CurrentMod = this;

        // 以下的注册中同时注册了引用模组的 AutoSyncType 实例，这样就可以在附属模组中直接使用了
        NetModuleLoader.LoadAutoSyncsFrom(typeof(NetModuleLoader).Assembly);
        NetModuleLoader.LoadAutoSyncsFrom(typeof(NetSimplifiedExample.NetSimplifiedExample).Assembly); // <- 注册主模组的 AutoSyncType
        NetModuleLoader.LoadAutoSyncsFrom(Assembly.GetExecutingAssembly());

        // 不需要注册引用模组的 NetModule 实例，引用模组的包由它自己处理，附属模组可以通过其公开的 Mod.Call API 获取并发送
        // NetModuleLoader.LoadNetModules();

        // 这里演示如何自行加载并注册 NetModule 实例，通常不需要这么做
        NetModuleLoader.Register(new ReplyListPacket());
        NetModuleLoader.Register(new ReplySumPacket());
        SaySmthModule = NetModuleLoader.Register(new FlexibleModule("SaySmth",
            () => Main.NewText(SaySmthModule.GetValue<string>(0), Main.DiscoColor),
            [typeof(string)]));
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