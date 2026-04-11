using System.Collections.Generic;
using System.Text;
using NetSimplified;
using NetSimplified.Syncing;
using NetSimplifiedExample.CustomTypes;
using Terraria;

namespace NetSimpExSubmod.Packets;

[AutoSync]
public class ReplyListPacket : NetModule
{
    private List<FenwickTreeInt> _fenwicks;

    public static void Send(int target, List<FenwickTreeInt> fenwicks) {
        var packet = NetModuleLoader.Get<ReplyListPacket>();
        packet._fenwicks = fenwicks;
        packet.Send(target);
    }

    public override void Receive() {
        Main.NewText("当前所有 FenwickTreeInt 的元素值：");
        foreach (var fenwick in _fenwicks) {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (int i in fenwick.ToArray()) {
                stringBuilder.Append($"{i} ");
            }

            Main.NewText(stringBuilder.ToString());
        }
    }
}