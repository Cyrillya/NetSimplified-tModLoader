using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
    public ushort Type { get; internal set; }

    /// <summary>关联的 Mod 实例（由 NetModuleLoader 在注册时设置）</summary>
    public Mod Mod { get; internal set; }

    /// <summary>模块显示名（默认为类型名，可重写）</summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    ///     使用这个函数来自行发送字段
    /// </summary>
    /// <param name="writer">用于发包的 <see cref="BinaryWriter" /> 实例</param>
    public virtual void Send(BinaryWriter writer) {
    }

    /// <summary>
    ///     通过 <see cref="ModPacket" /> 发包
    /// </summary>
    /// <param name="toClient">如果不是 -1, 则包<b>只会</b>发送给对应的客户端</param>
    /// <param name="ignoreClient">如果不是 -1, 则包<b>不会</b>发送给对应的客户端</param>
    /// <param name="runLocally">如果为 <see langword="true" /> 则在发包时会调用 <see cref="Receive()" /> 方法</param>
    /// <param name="compress">
    ///     如果为 <see langword="true" />，则对包的数据部分进行 Deflate 压缩后再发送，可降低传输数据量。<br />
    ///     接收方会自动检测并解压，无需额外处理。
    /// </param>
    public void Send(int toClient = -1, int ignoreClient = -1, bool runLocally = false, bool compress = false) {
        if (PreSend(toClient, ignoreClient)) {
            if (Main.netMode != NetmodeID.SinglePlayer) {
                if (Mod == null) throw new InvalidOperationException("NetModule.Mod 未被设置，请通过 NetModuleLoader.Register 或 LoadNetModulesFrom 加载模块");
                var mp = Mod.GetPacket();
                mp.Write(Type); // 包类型 ID
                mp.Write(compress); // 压缩标志

                if (compress) {
                    // 将包数据写入临时缓冲区，再压缩后写入 mp
                    using var ms = new MemoryStream();
                    using (var tempWriter = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true)) {
                        AutoSyncHandler.HandleAutoSend(this, tempWriter);
                        Send(tempWriter);
                    }

                    var uncompressed = ms.ToArray();
                    using var compressedMs = new MemoryStream();
                    using (var deflate = new DeflateStream(compressedMs, CompressionMode.Compress, leaveOpen: true)) {
                        deflate.Write(uncompressed, 0, uncompressed.Length);
                    }

                    var compressedBytes = compressedMs.ToArray();
                    mp.Write(compressedBytes.Length);
                    mp.Write(compressedBytes);
                }
                else {
                    AutoSyncHandler.HandleAutoSend(this, mp);
                    Send(mp);
                }

                // 发送
                mp.Send(toClient, ignoreClient);

                var len = (ushort) mp.GetType().GetField("len", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(mp)!;
                if (Main.netMode is NetmodeID.MultiplayerClient)
                    NetModuleLoader.NetModuleDiagnosticsUI?.CountSentMessage(Type, len - 3); // 2 bytes for ushort id + 1 byte for compress flag
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

        var id = reader.ReadUInt16();
        var compressed = reader.ReadBoolean();

        var module = NetModuleLoader.Get(id);
        module.Sender = whoAmI;

        if (compressed) {
            var compressedLength = reader.ReadInt32();
            var compressedBytes = reader.ReadBytes(compressedLength);

            using var decompressedMs = new MemoryStream();
            using (var deflate = new DeflateStream(new MemoryStream(compressedBytes), CompressionMode.Decompress)) {
                deflate.CopyTo(decompressedMs);
            }

            decompressedMs.Position = 0;
            using var dataReader = new BinaryReader(decompressedMs);
            AutoSyncHandler.HandleAutoRead(module, dataReader);
            module.Read(dataReader);
        }
        else {
            AutoSyncHandler.HandleAutoRead(module, reader);
            module.Read(reader);
        }

        module.Receive();

        var length = (int) reader.BaseStream.Position - start;
        if (Main.netMode is NetmodeID.MultiplayerClient)
            NetModuleLoader.NetModuleDiagnosticsUI?.CountReadMessage(id, length - 3); // 2 bytes for ushort id + 1 byte for compress flag
    }
}