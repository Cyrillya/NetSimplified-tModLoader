using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace NetSimplified;

/// <summary>
///     基本上是抄的 tModLoader 源码的 UIModNetDiagnostics<br />
///     用于记录各 NetModule 的传输量
/// </summary>
#pragma warning disable CS1591
public class NetModuleDiagnostics : INetDiagnosticsUI
{
    private const float TextScale = 0.7f;
    private const string Suffix = ": ";
    private const string NetModuleString = "NetModule";
    private const string RxTxString = "Received(#, Bytes)       Sent(#, Bytes)";

    private readonly NetModule[] _netModules;
    private CounterForMessage[] _counterByModuleId;
    private float _firstColumnWidth;
    private int _highestFoundReadBytes = 1;
    private int _highestFoundSentBytes = 1;

    internal NetModuleDiagnostics(IEnumerable<NetModule> netModules) {
        _netModules = netModules.ToArray();
        Reset();
    }

    private static Asset<DynamicSpriteFont> FontAsset => FontAssets.MouseText;

    /// <summary>
    ///     重置计数器
    /// </summary>
    public void Reset() {
        _counterByModuleId = new CounterForMessage[_netModules.Length];

        var font = FontAsset.Value;
        _firstColumnWidth = font.MeasureString(NetModuleString).X; // Default in case no modules are loaded

        for (var i = 0; i < _netModules.Length; i++) {
            var length = font.MeasureString(_netModules[i].Name).X;
            if (_firstColumnWidth < length)
                _firstColumnWidth = length;
        }

        _firstColumnWidth += font.MeasureString(Suffix).X + 2;
        _firstColumnWidth *= TextScale;
    }

    public void CountReadMessage(int messageId, int messageLength) {
        var index = Array.FindIndex(_netModules, module => module.Type == messageId);
        if (index > -1)
            _counterByModuleId[index].CountReadMessage(messageLength);
    }

    public void CountSentMessage(int messageId, int messageLength) {
        var index = Array.FindIndex(_netModules, mod => mod.Type == messageId);
        if (index > -1)
            _counterByModuleId[index].CountSentMessage(messageLength);
    }

    [Obsolete("Use Draw(SpriteBatch, int, int) instead", true)]
    public void Draw(SpriteBatch spriteBatch) {
        throw new ConstraintException();
    }

    // Not needed
    public void CountReadModuleMessage(int moduleMessageId, int messageLength) {
        throw new ConstraintException();
    }

    public void CountSentModuleMessage(int moduleMessageId, int messageLength) {
        throw new ConstraintException();
    }

    public void Draw(SpriteBatch spriteBatch, int x, int y) {
        var count = _counterByModuleId.Length;
        const int maxLinesPerCol = 50;
        var numCols = (count - 1) / maxLinesPerCol;
        var xBuf = x + 10;
        var yBuf = y + 10;

        var width = 232;
        // Adjust based on left column width and right column width
        width += (int) (_firstColumnWidth +
                        FontAsset.Value.MeasureString(_highestFoundSentBytes.ToString()).X * TextScale);
        var widthBuf = width + 10;
        var lineHeight = 13;

        for (var i = 0; i <= numCols; i++) {
            var lineCountInCol = i == numCols ? 1 + (count - 1) % maxLinesPerCol : maxLinesPerCol;
            var height = lineHeight * (lineCountInCol + 2);
            var heightBuf = height + 10;
            Utils.DrawInvBG(spriteBatch, x + widthBuf * i, y, width, heightBuf);

            var modPos = new Vector2(xBuf + widthBuf * i, yBuf);
            var headerPos = modPos + new Vector2(_firstColumnWidth, 0);
            DrawText(spriteBatch, RxTxString, headerPos, Color.White);
            DrawText(spriteBatch, NetModuleString, modPos, Color.White);
        }

        Vector2 position = default;
        for (var j = 0; j < count; j++) {
            var colNum = j / maxLinesPerCol;
            var lineNum = j - colNum * maxLinesPerCol;
            position.X = xBuf + colNum * widthBuf;
            position.Y = yBuf + lineHeight + lineNum * lineHeight;

            DrawCounter(spriteBatch, _counterByModuleId[j], _netModules[j].Name, position);
        }
    }

    // Copied from vanilla, adjusted
    private void DrawCounter(SpriteBatch spriteBatch, CounterForMessage counter, string title, Vector2 position) {
        if (_highestFoundSentBytes < counter.BytesSent)
            _highestFoundSentBytes = counter.BytesSent;

        if (_highestFoundReadBytes < counter.BytesReceived)
            _highestFoundReadBytes = counter.BytesReceived;

        var pos = position;
        var lineName = title + Suffix;
        var num = Utils.Remap(counter.BytesReceived, 0f, _highestFoundReadBytes, 0f, 1f);
        var color = Main.hslToRgb(0.3f * (1f - num), 1f, 0.5f);

        var drawText = lineName;
        DrawText(spriteBatch, drawText, pos, color);
        pos.X += _firstColumnWidth;
        drawText = "rx:" + string.Format("{0,0}", counter.TimesReceived);
        DrawText(spriteBatch, drawText, pos, color);
        pos.X += 70f;
        drawText = string.Format("{0,0}", counter.BytesReceived);
        DrawText(spriteBatch, drawText, pos, color);
        pos.X += 70f;
        drawText = "tx:" + string.Format("{0,0}", counter.TimesSent);
        DrawText(spriteBatch, drawText, pos, color);
        pos.X += 70f;
        drawText = string.Format("{0,0}", counter.BytesSent);
        DrawText(spriteBatch, drawText, pos, color);
    }

    private void DrawText(SpriteBatch spriteBatch, string text, Vector2 pos, Color color) {
        spriteBatch.DrawString(FontAsset.Value, text, pos, color, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
    }

    // Copied from vanilla, adjusted
    private struct CounterForMessage
    {
        public int TimesReceived;
        public int TimesSent;
        public int BytesReceived;
        public int BytesSent;

        public void CountReadMessage(int messageLength) {
            TimesReceived++;
            BytesReceived += messageLength;
        }

        public void CountSentMessage(int messageLength) {
            TimesSent++;
            BytesSent += messageLength;
        }
    }
}
#pragma warning restore CS1591