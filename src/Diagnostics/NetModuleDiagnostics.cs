using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>包的传输方向</summary>
public enum PacketDirection : byte
{
    /// <summary>发送</summary>
    Sent,

    /// <summary>接收</summary>
    Received
}

/// <summary>一条最近收发包的日志记录</summary>
public sealed class PacketLogEntry
{
    /// <summary>递增序号，用于指令定位某条记录</summary>
    public long Sequence;

    /// <summary>传输方向</summary>
    public PacketDirection Direction;

    /// <summary>包对应的 NetModule Type</summary>
    public int ModuleId;

    /// <summary>负载字节数（含 4 字节 Type id）</summary>
    public int Length;

    /// <summary>负载字节（含 4 字节 Type id）；若采集失败则为 null</summary>
    public byte[] Data;

    /// <summary>记录时间</summary>
    public DateTime Time;
}

/// <summary>单个 NetModule 的收发计数</summary>
public struct NetModuleCounters
{
    /// <summary>接收次数</summary>
    public int TimesReceived;

    /// <summary>发送次数</summary>
    public int TimesSent;

    /// <summary>接收字节数</summary>
    public int BytesReceived;

    /// <summary>发送字节数</summary>
    public int BytesSent;
}

/// <summary>
///     网络数据监视器：记录所有已注册 NetModule 的收发流量计数，以及最近收发包的负载字节日志。
///     由 <see cref="NetModuleLoader" /> 创建，客户端 UI 与服务器 ModCommand 均读取此数据。
/// </summary>
public sealed class NetModuleDiagnostics
{
    /// <summary>最近收发包日志的环形缓冲上限</summary>
    public const int MaxLogEntries = 256;

    private readonly NetModule[] _modules;
    private readonly NetModuleCounters[] _counters;
    private readonly PacketLogEntry[] _log;
    private int _logHead;
    private int _logCount;
    private long _sequence;

    internal NetModuleDiagnostics(IEnumerable<NetModule> modules) {
        _modules = modules as NetModule[] ?? new List<NetModule>(modules).ToArray();
        _counters = new NetModuleCounters[_modules.Length];
        _log = new PacketLogEntry[MaxLogEntries];
    }

    /// <summary>已注册的 NetModule 数量</summary>
    public int ModuleCount => _modules.Length;

    /// <summary>当前日志条数</summary>
    public int LogCount => _logCount;

    /// <summary>按 Type 获取模块实例（越界返回 null）</summary>
    public NetModule GetModule(int type) {
        return type >= 0 && type < _modules.Length ? _modules[type] : null;
    }

    /// <summary>按 Type 获取模块的收发计数（越界返回默认值）</summary>
    public NetModuleCounters GetCounters(int type) {
        return type >= 0 && type < _counters.Length ? _counters[type] : default;
    }

    /// <summary>重置所有计数与日志</summary>
    public void Reset() {
        Array.Clear(_counters, 0, _counters.Length);
        Array.Clear(_log, 0, _log.Length);
        _logHead = 0;
        _logCount = 0;
        _sequence = 0;
    }

    /// <summary>记录一次发送</summary>
    public void CountSentMessage(int moduleId, byte[] payload) {
        Record(moduleId, PacketDirection.Sent, payload);
    }

    /// <summary>记录一次接收</summary>
    public void CountReadMessage(int moduleId, byte[] payload) {
        Record(moduleId, PacketDirection.Received, payload);
    }

    private void Record(int moduleId, PacketDirection direction, byte[] payload) {
        if (moduleId < 0 || moduleId >= _counters.Length) return;

        var length = payload?.Length ?? 0;
        if (direction == PacketDirection.Sent) {
            _counters[moduleId].TimesSent++;
            _counters[moduleId].BytesSent += length;
        }
        else {
            _counters[moduleId].TimesReceived++;
            _counters[moduleId].BytesReceived += length;
        }

        _log[_logHead] = new PacketLogEntry {
            Sequence = _sequence++,
            Direction = direction,
            ModuleId = moduleId,
            Length = length,
            Data = payload,
            Time = DateTime.Now
        };
        _logHead = (_logHead + 1) % MaxLogEntries;
        if (_logCount < MaxLogEntries) _logCount++;
    }

    /// <summary>获取最近 <paramref name="count" /> 条日志（最新在前）</summary>
    public PacketLogEntry[] GetRecentLog(int count) {
        var take = Math.Min(count, _logCount);
        var result = new PacketLogEntry[take];
        for (var i = 0; i < take; i++) {
            var idx = (_logHead - 1 - i + MaxLogEntries) % MaxLogEntries;
            result[i] = _log[idx];
        }
        return result;
    }

    /// <summary>按序号查找日志记录（未找到返回 null）</summary>
    public PacketLogEntry FindBySequence(long sequence) {
        for (var i = 0; i < _logCount; i++) {
            var idx = (_logHead - 1 - i + MaxLogEntries) % MaxLogEntries;
            if (_log[idx].Sequence == sequence) return _log[idx];
        }
        return null;
    }

    // ---------- 负载字节采集（仅供 NetModule 内部调用） ----------

    /// <summary>获取 ModPacket 负载起点（写 Type 之前的流位置，即 ModPacket 头部长度）</summary>
    internal static int GetPacketPayloadStart(ModPacket packet) {
        try {
            var outStream = typeof(BinaryWriter)
                .GetProperty("OutStream", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(packet) as Stream;
            return outStream == null ? 0 : (int) outStream.Position;
        }
        catch {
            return 0;
        }
    }

    /// <summary>从已 Send 的 ModPacket 中提取负载字节（反射读取 buf/len 私有字段）</summary>
    internal static byte[] CaptureSentPayload(ModPacket packet, int payloadStart) {
        try {
            var type = packet.GetType();
            var bufField = type.GetField("buf", BindingFlags.NonPublic | BindingFlags.Instance);
            var lenField = type.GetField("len", BindingFlags.NonPublic | BindingFlags.Instance);
            if (bufField == null || lenField == null) return null;

            var buf = bufField.GetValue(packet) as byte[];
            var len = (ushort) lenField.GetValue(packet);
            if (buf == null || len < payloadStart) return null;

            var slice = new byte[len - payloadStart];
            Array.Copy(buf, payloadStart, slice, 0, slice.Length);
            return slice;
        }
        catch {
            return null;
        }
    }

    /// <summary>从收包 reader 的底层 MemoryStream 中截取 [start, end) 负载字节</summary>
    internal static byte[] CaptureReceivedPayload(BinaryReader reader, int start, int end) {
        try {
            if (end < start) return null;
            if (reader.BaseStream is not MemoryStream ms) return null;

            var buf = ms.GetBuffer();
            var slice = new byte[end - start];
            Array.Copy(buf, start, slice, 0, slice.Length);
            return slice;
        }
        catch {
            return null;
        }
    }
}
