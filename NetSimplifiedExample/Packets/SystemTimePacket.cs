using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using NetSimplified;
using NetSimplified.Syncing;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;

namespace NetSimplifiedExample.Packets;

// 使用 AutoSync 特性以使变量自动传输，免去自己写 Send 和 Receive 的功夫
[AutoSync]
public class SystemTimePacket : NetModule
{
    private string _time;
    [ColorSync(syncAlpha: false)] private Color[,] _colors;
    
    public static SystemTimePacket Get(string time) {
        var module = NetModuleLoader.Get<SystemTimePacket>();
        module._time = time;
        int colorRowsCount = Main.rand.Next(3, 6);
        int colorColsCount = Main.rand.Next(3, 6);
        module._colors = new Color[colorRowsCount, colorColsCount];
        for (int i = 0; i < colorRowsCount; i++) {
            for (int j = 0; j < colorColsCount; j++) {
                module._colors[i, j] = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
            }
        }
        return module;
    }

    public override void Receive() {
        if (Main.netMode is not NetmodeID.MultiplayerClient) return;

        Main.NewText($"服务器系统时间: {_time}");
        Main.NewText($"随机生成的颜色矩阵:");
        for (int i = 0; i < _colors.GetLength(0); i++) {
            StringBuilder stringBuilder = new();
            for (int j = 0; j < _colors.GetLength(1); j++) {
                var color = _colors[i, j];
                stringBuilder.Append($"[c/{color.R:X2}{color.G:X2}{color.B:X2}:█]");
            }
            Main.NewText(stringBuilder.ToString());
        }
    }
}