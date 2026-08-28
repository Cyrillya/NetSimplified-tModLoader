using System;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     查看网络诊断数据的指令（主要用于服务器端，客户端亦可使用）。
///     需由模组在 Load 中通过 <c>AddContent&lt;NetSimplifiedDiagnosticsCommand&gt;()</c> 注册。
///     <para>
///         子命令：
///         <c>nsdiag traffic</c> 各模块收发流量总表；
///         <c>nsdiag log [count]</c> 最近收发包记录（默认 10，上限 256）；
///         <c>nsdiag detail &lt;seq&gt;</c> 某条记录的详细信息；
///         <c>nsdiag reset</c> 重置所有数据。
///     </para>
/// </summary>
public sealed class NetSimplifiedDiagnosticsCommand : ModCommand
{
    /// <inheritdoc />
    public override string Command => "nsdiag";

    /// <inheritdoc />
    public override CommandType Type => CommandType.World | CommandType.Console;

    /// <inheritdoc />
    public override string Usage => "/nsdiag traffic | log [count] | detail <seq> | reset";

    /// <inheritdoc />
    public override string Description => "查看 NetSimplified 网络诊断数据：各模块收发流量、最近收发包日志及某条记录的详细信息";

    /// <inheritdoc />
    public override void Action(CommandCaller caller, string input, string[] args) {
        var diagnostics = NetModuleLoader.Diagnostics;
        if (diagnostics == null) {
            caller.Reply("NetModuleLoader 未注册，无法获取诊断数据。请在模组 Load 中调用 AddContent<NetModuleLoader>()。", Color.Red);
            return;
        }

        var sub = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
        switch (sub) {
            case "traffic":
                ReplyTraffic(caller, diagnostics);
                break;
            case "log":
                ReplyLog(caller, diagnostics, args);
                break;
            case "detail":
                ReplyDetail(caller, diagnostics, args);
                break;
            case "reset":
                diagnostics.Reset();
                caller.Reply("诊断数据已重置。", Color.Green);
                break;
            default:
                caller.Reply($"用法: {Usage}");
                break;
        }
    }

    private static void ReplyTraffic(CommandCaller caller, NetModuleDiagnostics diagnostics) {
        var sb = new StringBuilder();
        sb.AppendLine($"NetModule 收发流量 (共 {diagnostics.ModuleCount} 个模块):");
        sb.AppendLine("模块名                        收次数   收字节   发次数   发字节");
        for (var i = 0; i < diagnostics.ModuleCount; i++) {
            var name = diagnostics.GetModule(i)?.Name ?? "?";
            var c = diagnostics.GetCounters(i);
            sb.AppendLine($"{name,-30} {c.TimesReceived,6} {c.BytesReceived,8} {c.TimesSent,6} {c.BytesSent,8}");
        }
        caller.Reply(sb.ToString().TrimEnd());
    }

    private static void ReplyLog(CommandCaller caller, NetModuleDiagnostics diagnostics, string[] args) {
        var count = 10;
        if (args.Length > 1 && int.TryParse(args[1], out var parsed))
            count = Math.Clamp(parsed, 1, NetModuleDiagnostics.MaxLogEntries);

        var entries = diagnostics.GetRecentLog(count);
        if (entries.Length == 0) {
            caller.Reply("暂无收发包记录。");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"最近 {entries.Length} 条收发包记录 (最新在前):");
        foreach (var e in entries) {
            var dir = e.Direction == PacketDirection.Sent ? "发送" : "接收";
            var name = diagnostics.GetModule(e.ModuleId)?.Name ?? "?";
            sb.AppendLine($"[{e.Sequence}] {dir} {name} {e.Length}B {e.Time:HH:mm:ss}");
        }
        sb.AppendLine("使用 /nsdiag detail <seq> 查看某条记录的详细数据。");
        caller.Reply(sb.ToString().TrimEnd());
    }

    private static void ReplyDetail(CommandCaller caller, NetModuleDiagnostics diagnostics, string[] args) {
        if (args.Length < 2 || !long.TryParse(args[1], out var seq)) {
            caller.Reply("用法: /nsdiag detail <seq>");
            return;
        }

        var entry = diagnostics.FindBySequence(seq);
        if (entry == null) {
            caller.Reply($"未找到序号 {seq} 的记录。");
            return;
        }

        var lines = PacketDetailFormatter.BuildDetailLines(entry, diagnostics);
        caller.Reply(string.Join("\n", lines));
    }
}
