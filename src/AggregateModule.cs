using System.Collections.Generic;
using System.IO;
using NetSimplified.Syncing;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     以一个 <see cref="ModPacket" /> 包的形式发送多个 <see cref="NetModule" /> 包, 能有效避免分散性地多次发包。<br />
///     与普通包一样, 发包时调用 <see cref="NetModule.Send(int, int, bool)" /> 即可
/// </summary>
public sealed class AggregateModule : NetModule
{
    /// <summary>所有要发的包</summary>
    public List<NetModule> Modules { get; set; }

    /// <summary>
    ///     创建一个 <see cref="AggregateModule" /> 包实例
    /// </summary>
    /// <param name="modules">所有要发的 <see cref="NetModule" /> 包</param>
    public static AggregateModule Get(List<NetModule> modules) {
        var module = NetModuleLoader.Get<AggregateModule>();
        module.Modules = modules;
        return module;
    }

    /// <inheritdoc />
    public override void Read(BinaryReader r) {
        var count = r.ReadInt32();
        Modules = new List<NetModule>();
        for (var i = 0; i < count; i++) {
            var type = r.ReadInt32();
            var module = NetModuleLoader.Get(type);
            AutoSyncHandler.HandleAutoRead(module, r);
            module.Read(r);
            Modules.Add(module);
        }
    }

    /// <inheritdoc />
    public override void Send(ModPacket p) {
        p.Write(Modules.Count);
        foreach (var module in Modules) {
            p.Write(module.Type);
            AutoSyncHandler.HandleAutoSend(module, p);
            module.Send(p);
        }
    }

    /// <inheritdoc />
    public override void Receive() {
        foreach (var module in Modules) module.Receive();
    }
}