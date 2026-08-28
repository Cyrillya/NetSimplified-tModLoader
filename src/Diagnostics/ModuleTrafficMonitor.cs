using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace NetSimplified;

/// <summary>
///     监视所有已注册 NetModule 在当前端已接收 / 已发送总流量的 UI。<br/>
///     本类只负责绘制，开关键与绘制时机由使用库的 Mod 自行处理。
/// </summary>
public sealed class ModuleTrafficMonitor
{
    private const float TextScale = 0.7f;
    private const string NetModuleString = "NetModule";
    private const string RxTxString = "Received(#, Bytes)       Sent(#, Bytes)";
    private const int MaxLinesPerColumn = 50;

    private readonly NetModuleDiagnostics _diagnostics;
    private float _firstColumnWidth;

    /// <summary>UI 是否可见，由宿主 Mod 控制</summary>
    public bool Visible { get; set; }

    /// <summary>UI 左上角坐标，由宿主 Mod 控制</summary>
    public Point Position { get; set; } = new Point(10, 10);

    /// <summary>创建流量监视 UI，读取 <paramref name="diagnostics" /> 的数据</summary>
    public ModuleTrafficMonitor(NetModuleDiagnostics diagnostics) {
        _diagnostics = diagnostics;
        RecalculateColumnWidth();
    }

    private void RecalculateColumnWidth() {
        var font = FontAssets.MouseText.Value;
        _firstColumnWidth = font.MeasureString(NetModuleString).X;

        for (var i = 0; i < _diagnostics.ModuleCount; i++) {
            var name = _diagnostics.GetModule(i)?.Name;
            if (name != null) {
                var length = font.MeasureString(name).X;
                if (_firstColumnWidth < length) _firstColumnWidth = length;
            }
        }

        _firstColumnWidth += font.MeasureString(": ").X + 2;
        _firstColumnWidth *= TextScale;
    }

    /// <summary>绘制流量监视 UI，应在 <see cref="Terraria.ModLoader.ModSystem.PostDrawInterface" /> 中调用</summary>
    public void Draw(SpriteBatch spriteBatch) {
        var count = _diagnostics.ModuleCount;
        var numCols = count == 0 ? 0 : (count - 1) / MaxLinesPerColumn;
        int x = Position.X;
        var xBuf = x + 10;
        int y = Position.Y;
        var yBuf = y + 10;

        var width = 232;
        width += (int) (_firstColumnWidth + FontAssets.MouseText.Value.MeasureString("888888888").X * TextScale);
        var widthBuf = width + 10;
        const int lineHeight = 13;

        // 计算当前最高流量，用于热力色
        long maxTraffic = 1;
        for (var j = 0; j < count; j++) {
            var c = _diagnostics.GetCounters(j);
            var traffic = (long) c.BytesReceived + c.BytesSent;
            if (traffic > maxTraffic) maxTraffic = traffic;
        }

        for (var i = 0; i <= numCols; i++) {
            var lineCountInCol = i == numCols ? 1 + (count - 1) % MaxLinesPerColumn : MaxLinesPerColumn;
            if (count == 0) lineCountInCol = 0;
            var height = lineHeight * (lineCountInCol + 2);
            var heightBuf = height + 10;
            var rect = new Rectangle(x + widthBuf * i, y, width, heightBuf);
            UIDrawing.DrawPanel(spriteBatch, rect, Color.White * 0.8f, new Color(0, 0, 0, 180));

            var modPos = new Vector2(xBuf + widthBuf * i, yBuf);
            var headerPos = modPos + new Vector2(_firstColumnWidth, 0);
            UIDrawing.DrawText(spriteBatch, RxTxString, headerPos, Color.White);
            UIDrawing.DrawText(spriteBatch, NetModuleString, modPos, Color.White);
        }

        Vector2 position = default;
        for (var j = 0; j < count; j++) {
            var colNum = j / MaxLinesPerColumn;
            var lineNum = j - colNum * MaxLinesPerColumn;
            position.X = xBuf + colNum * widthBuf;
            position.Y = yBuf + lineHeight + lineNum * lineHeight;

            DrawCounter(spriteBatch, _diagnostics.GetCounters(j), _diagnostics.GetModule(j)?.Name ?? j.ToString(), maxTraffic, position);
        }
    }

    private void DrawCounter(SpriteBatch spriteBatch, NetModuleCounters counter, string title, long maxTraffic, Vector2 position) {
        var traffic = (long) counter.BytesReceived + counter.BytesSent;
        var heat = MathHelper.Clamp((float) (traffic / (double) maxTraffic), 0f, 1f);
        var color = Main.hslToRgb(0.3f * (1f - heat), 1f, 0.5f);

        var pos = position;
        UIDrawing.DrawText(spriteBatch, title + ": ", pos, color);
        pos.X += _firstColumnWidth;
        UIDrawing.DrawText(spriteBatch, "rx:" + counter.TimesReceived, pos, color);
        pos.X += 70f;
        UIDrawing.DrawText(spriteBatch, counter.BytesReceived.ToString(), pos, color);
        pos.X += 70f;
        UIDrawing.DrawText(spriteBatch, "tx:" + counter.TimesSent, pos, color);
        pos.X += 70f;
        UIDrawing.DrawText(spriteBatch, counter.BytesSent.ToString(), pos, color);
    }
}
