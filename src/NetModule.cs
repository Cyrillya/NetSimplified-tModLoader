using System;
using System.IO;
using NetSimplified.Syncing;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     用于写入、读取 <see cref="ModPacket" /> 的基类
///     注：NetModule 不再继承 ModType，需由 NetModuleLoader 手动加载/注册。
/// </summary>
public abstract class NetModule
{
    /// <summary>包的发送者</summary>
    protected int Sender { get; private set; } = Main.myPlayer;

    /// <summary>该 <see cref="NetModule" /> 被分配到的ID</summary>
    public int Type { get; internal set; }

    /// <summary>关联的 Mod 实例（由 NetModuleLoader 在注册时设置）</summary>
    public Mod Mod { get; internal set; }

    /// <summary>模块显示名（默认为类型名，可重写）</summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    ///     使用这个函数来自行发送字段
    /// </summary>
    /// <param name="p">用于发包的 <see cref="ModPacket" /> 实例</param>
    public virtual void Send(ModPacket p) {
    }

    /// <summary>
    ///     通过 <see cref="ModPacket" /> 发包
    /// </summary>
    /// <param name="toClient">如果不是 -1, 则包<b>只会</b>发送给对应的客户端</param>
    /// <param name="ignoreClient">如果不是 -1, 则包<b>不会</b>发送给对应的客户端</param>
    /// <param name="runLocally">如果为 <see langword="true" /> 则在发包时会调用 <see cref="Receive()" /> 方法</param>
    /// <param name="handler">自定义的自动传输处理器</param>
    public void Send(int toClient = -1, int ignoreClient = -1, bool runLocally = false) {
        if (PreSend(toClient, ignoreClient)) {
            if (Main.netMode != NetmodeID.SinglePlayer) {
                if (Mod == null) throw new InvalidOperationException("NetModule.Mod 未被设置，请通过 NetModuleLoader.Register 或 LoadNetModulesFrom 加载模块");
                var mp = Mod.GetPacket();

                var diagnostics = NetModuleLoader.Diagnostics;
                var payloadStart = diagnostics != null ? NetModuleDiagnostics.GetPacketPayloadStart(mp) : 0;

                mp.Write(Type); // 包类型 ID
                AutoSyncHandler.HandleAutoSend(this, mp);
                Send(mp);
                // 发送
                mp.Send(toClient, ignoreClient);

                if (diagnostics != null && Type >= 0)
                    diagnostics.CountSentMessage(Type, NetModuleDiagnostics.CaptureSentPayload(mp, payloadStart));
            }

            if (runLocally) Receive();
        }
    }

    /// <summary>
    ///     使用这个函数来自行读取字段
    /// </summary>
    /// <param name="r">用于读取的 <see cref="BinaryReader" /> 实例</param>
    public virtual void Read(BinaryReader r) {
    }

    /// <summary>
    ///     使用这个函数来进行接收后的操作 (与 <see cref="Read(BinaryReader)" /> 分开以适配 runLocally)
    /// </summary>
    public abstract void Receive();

    /// <summary>发包前调用, 返回 <see langword="false" /> 则不会发包, 也不会调用 <see cref="Receive()" />。 默认为 <see langword="true" />.</summary>
    protected virtual bool PreSend(int toClient = -1, int ignoreClient = -1) {
        return true;
    }

    /// <summary>接收来自你的Mod的发包, 请在 <see cref="Mod.HandlePacket(BinaryReader, int)" /> 调用</summary>
    public static void ReceiveModule(BinaryReader reader, int whoAmI) {
        var start = (int) reader.BaseStream.Position;

        var id = reader.ReadInt32();
        var module = NetModuleLoader.Get(id);
        module.Sender = whoAmI;
        AutoSyncHandler.HandleAutoRead(module, reader);
        module.Read(reader);
        module.Receive();

        if (id >= 0) {
            var end = (int) reader.BaseStream.Position;
            var diagnostics = NetModuleLoader.Diagnostics;
            if (diagnostics != null)
                diagnostics.CountReadMessage(id, NetModuleDiagnostics.CaptureReceivedPayload(reader, start, end));
        }
    }
}