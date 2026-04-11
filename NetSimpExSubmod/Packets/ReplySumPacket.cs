using System.Text;
using NetSimplified;
using NetSimplified.Syncing;
using NetSimplifiedExample.CustomTypes;
using Terraria;

namespace NetSimpExSubmod.Packets;

[AutoSync]
public class ReplySumPacket : NetModule
{
    private int _l, _r;
    private FenwickTreeInt _fenwick;

    public static void Send(int target, int l, int r, FenwickTreeInt fenwick) {
        var packet = NetModuleLoader.Get<ReplySumPacket>();
        packet._l = l;
        packet._r = r;
        packet._fenwick = fenwick;
        packet.Send(target);
    }

    public override void Receive() {
        Main.NewText($"请求区间的区间和：{_fenwick.RangeSum(_l - 1, _r - 1)}");
    }
}