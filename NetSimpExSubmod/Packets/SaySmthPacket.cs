using NetSimplified;
using NetSimplified.Syncing;
using Terraria;

namespace NetSimpExSubmod.Packets;

[AutoSync]
public class SaySmthPacket : NetModule
{
    private string _something;

    public static SaySmthPacket Get(string something) {
        var packet = NetModuleLoader.Get<SaySmthPacket>();
        packet._something = something;
        return packet;
    }

    public override void Receive() {
        Main.NewText(_something, Main.DiscoColor);
    }
}