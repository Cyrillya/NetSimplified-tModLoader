using System;
using System.IO;
using System.Linq;
using NetSimplified;
using NetSimplified.Syncing;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.ModLoader;

namespace NetSimplifiedExample.Packets;

// 这个例子演示了如何自己写发包
public class RandomStringPacket : NetModule
{
    private char _rdChar;
    
    public static RandomStringPacket Get() {
        var module = NetModuleLoader.Get<RandomStringPacket>();
        module._rdChar = Main.rand.Next(new[] { 'a', 'b', 'c', 'd'});
        return module;
    }

    public override void Send(BinaryWriter writer) {
        writer.Write(_rdChar);
    }

    public override void Read(BinaryReader r) {
        _rdChar = r.ReadChar();
    }

    public override void Receive() {
        if (Main.netMode is not NetmodeID.MultiplayerClient) return;

        Main.NewText($"服务器随机发的字母: {_rdChar}");
    }
}