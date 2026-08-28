using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NetSimplified.Syncing;
using Terraria;
using Terraria.GameInput;

namespace NetSimplified;

/// <summary>
///     显示最近收 / 发 NetModule 包列表的 UI，悬停某条记录可查看其详细数据（元信息 + Hex 转储 + AutoSync 字段解析）。<br/>
///     本类只负责绘制，开关键与绘制时机由使用库的 Mod 自行处理。
/// </summary>
public sealed class ModuleLogViewer
{
    private const int RowHeight = 16;
    private const int VisibleRows = 16;
    private const int HeaderHeight = 28;

    private readonly NetModuleDiagnostics _diagnostics;
    private int _scrollOffset;
    private PacketLogEntry _hovered;

    /// <summary>UI 是否可见，由宿主 Mod 控制</summary>
    public bool Visible { get; set; }

    /// <summary>UI 左上角坐标，由宿主 Mod 控制</summary>
    public Point Position { get; set; } = new Point(10, 10);

    /// <summary>创建收发包日志查看 UI，读取 <paramref name="diagnostics" /> 的数据</summary>
    public ModuleLogViewer(NetModuleDiagnostics diagnostics) {
        _diagnostics = diagnostics;
    }

    /// <summary>绘制日志查看 UI，应在 <see cref="Terraria.ModLoader.ModSystem.PostDrawInterface" /> 中调用</summary>
    public void Draw(SpriteBatch spriteBatch) {
        var entries = _diagnostics.GetRecentLog(NetModuleDiagnostics.MaxLogEntries);

        var panelWidth = 580;
        var panel = new Rectangle(Position.X, Position.Y, panelWidth, VisibleRows * RowHeight + HeaderHeight + 8);
        UIDrawing.DrawPanel(spriteBatch, panel, Color.White * 0.8f, new Color(0, 0, 0, 180));
        UIDrawing.DrawText(spriteBatch, "最近收发包日志 (滚轮滚动，悬停查看详情)", new Vector2(panel.X + 8, panel.Y + 6), Color.White);

        string whoAmIText = Main.myPlayer.ToString();
        int whoAmIWidth = (int) UIDrawing.MeasureText(whoAmIText).X;
        UIDrawing.DrawText(spriteBatch, whoAmIText, new Vector2(panel.Right - whoAmIWidth - 8, panel.Y + 6), Color.White);

        // 滚动（鼠标位于面板内时滚轮生效）
        if (panel.Contains(Main.MouseScreen.ToPoint())) {
            PlayerInput.LockVanillaMouseScroll("NetSimplified/Diagnostics");
            var delta = PlayerInput.ScrollWheelDelta;
            if (delta != 0) {
                var maxOffset = Math.Max(0, entries.Length - VisibleRows);
                _scrollOffset = Math.Clamp(_scrollOffset + (delta > 0 ? -3 : 3), 0, maxOffset);
            }
        }
        _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, entries.Length - VisibleRows));

        _hovered = null;
        var listX = panel.X + 6;
        var listY = panel.Y + HeaderHeight;
        for (var i = _scrollOffset; i < Math.Min(_scrollOffset + VisibleRows, entries.Length); i++) {
            var entry = entries[i];
            var rowRect = new Rectangle(listX, listY + (i - _scrollOffset) * RowHeight, panel.Width - 12, RowHeight);
            if (rowRect.Contains(Main.MouseScreen.ToPoint())) {
                _hovered = entry;
                UIDrawing.DrawPanel(spriteBatch, rowRect, Main.OurFavoriteColor * 0.8f, Color.Transparent);
            }

            var color = entry.Direction == PacketDirection.Sent ? new Color(130, 200, 255) : new Color(255, 180, 120);
            var arrow = entry.Direction == PacketDirection.Sent ? "-->" : "<--";
            var name = Truncate(_diagnostics.GetModule(entry.ModuleId)?.Name ?? "?", 22);
            var text = $"{entry.Sequence,5} {arrow} {name,-22} {entry.Length,5}B  {entry.Time:HH:mm:ss}";
            UIDrawing.DrawText(spriteBatch, text, new Vector2(rowRect.X, rowRect.Y), color);
        }

        if (_hovered != null)
            DrawDetailBox(spriteBatch, _hovered, panel);
    }

    private void DrawDetailBox(SpriteBatch spriteBatch, PacketLogEntry entry, Rectangle panel) {
        var lines = PacketDetailFormatter.BuildDetailLines(entry, _diagnostics);

        float maxWidth = 0;
        foreach (var line in lines)
            maxWidth = Math.Max(maxWidth, UIDrawing.MeasureText(line).X);
        var width = (int) maxWidth + 16;
        var height = lines.Length * 15 + 12;

        var rect = new Rectangle(panel.Right + 8, panel.Y, width, Math.Max(height, 40));
        if (rect.Right > Main.screenWidth) rect.X = Math.Max(0, panel.X - width - 8);
        rect.Y = Math.Clamp(rect.Y, 0, Math.Max(0, Main.screenHeight - rect.Height));

        UIDrawing.DrawPanel(spriteBatch, rect, Color.White * 0.8f, new Color(0, 0, 0, 230));

        var pos = new Vector2(rect.X + 8, rect.Y + 6);
        foreach (var line in lines) {
            UIDrawing.DrawText(spriteBatch, line, pos, Color.White);
            pos.Y += 15;
        }
    }

    private static string Truncate(string s, int maxLength) {
        if (s.Length <= maxLength) return s;
        return s.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
///     将一条收发包日志格式化为可读的详细文本（供 UI 悬停与 ModCommand 共用）。
/// </summary>
internal static class PacketDetailFormatter
{
    /// <summary>构建某条日志的详细文本行</summary>
    public static string[] BuildDetailLines(PacketLogEntry entry, NetModuleDiagnostics diagnostics) {
        var lines = new List<string>();
        var direction = entry.Direction == PacketDirection.Sent ? "发送 (Sent)" : "接收 (Received)";
        lines.Add($"方向: {direction}");
        lines.Add($"序号: {entry.Sequence}");
        lines.Add($"模块: {diagnostics.GetModule(entry.ModuleId)?.Name ?? "?"} (Type {entry.ModuleId})");
        lines.Add($"时间: {entry.Time:yyyy-MM-dd HH:mm:ss.fff}");
        lines.Add($"长度: {entry.Length} 字节");

        if (entry.Data == null) {
            lines.Add("负载字节数据不可用");
            return lines.ToArray();
        }

        if (entry.Data.Length >= 4) {
            var type = BitConverter.ToInt32(entry.Data, 0);
            lines.Add($"Type: {type}");
        }

        var fieldLines = ParseFields(entry.Data, diagnostics.GetModule(entry.ModuleId)?.Name);
        if (fieldLines.Count > 0) {
            lines.Add("-- AutoSync 字段 --");
            lines.AddRange(fieldLines);
        }

        lines.Add("-- Hex (前 256 字节) --");
        lines.AddRange(FormatHex(entry.Data, 256));

        return lines.ToArray();
    }

    private static List<string> ParseFields(byte[] data, string moduleName) {
        var result = new List<string>();
        if (moduleName == null || data.Length < 4) return result;
        if (!NetModuleLoader.FieldInfos.TryGetValue(moduleName, out var fields) || fields.Length == 0) return result;

        try {
            using var ms = new MemoryStream(data, 4, data.Length - 4, writable: false);
            using var reader = new BinaryReader(ms);
            foreach (var field in fields) {
                var value = AutoSyncHandler.ReadValue(reader, field.FieldType, field);
                result.Add($"{field.Name} = {value?.ToString() ?? "null"}");
            }
        }
        catch {
            // 字段解析失败，仅保留已解析的部分
        }
        return result;
    }

    private static List<string> FormatHex(byte[] data, int maxBytes) {
        var lines = new List<string>();
        var count = Math.Min(data.Length, maxBytes);
        for (var i = 0; i < count; i += 16) {
            var hex = new StringBuilder();
            var ascii = new StringBuilder();
            hex.Append(i.ToString("X4")).Append("  ");
            for (var j = 0; j < 16; j++) {
                if (i + j < count) {
                    hex.Append(data[i + j].ToString("X2")).Append(' ');
                    var c = (char) data[i + j];
                    ascii.Append(c >= 32 && c < 127 ? c : '.');
                }
                else {
                    hex.Append("   ");
                }
            }
            hex.Append(" | ").Append(ascii);
            lines.Add(hex.ToString());
        }
        if (count < data.Length)
            lines.Add($"... 共 {data.Length} 字节，仅显示前 {maxBytes} 字节");
        return lines;
    }
}
