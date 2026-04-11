using System.IO;
using System.Reflection;
using NetSimplified;
using NetSimplified.Syncing;
using NetSimplifiedExample.Commands;
using Terraria;
using Terraria.ID;

namespace NetSimplifiedExample.Packets;

[AutoSync]
public class TpRequestPacket : NetModule
{
    public static TpRequestPacket Get(int plr) {
        if (Main.dedServ) return null;
        var module = NetModuleLoader.Get<TpRequestPacket>();
        module._sender = (byte)Main.LocalPlayer.whoAmI;
        module._receiver = (byte)plr;
        return module;
    }
    
    public byte _sender;
    public byte _receiver;
    
    public override void Receive() {
        if (Main.netMode is NetmodeID.Server) {
            Send(_receiver); // 服务器将请求发送给目标玩家
            return;
        }
        
        if (Main.netMode is NetmodeID.MultiplayerClient) {
            Main.NewText($"{Main.player[_sender].name} 请求传送你");
            Main.NewText($"[c/00FF00:接受] > /rtp accept");
            Main.NewText($"[c/FF0000:拒绝] > /rtp reject [拒绝理由]");
            TpCommand.PendingRequestTarget = _sender;
        }
    }
}

// 回复Tp请求包，使用结构体TpReplyData来储存回复数据，使用AutoSyncType来实现自动传输
// 正常实现时完全没必要这样写，直接在TpReplyPacket里写字段就行了，这里只是为了演示AutoSyncType的用法
[AutoSync]
public class TpReplyPacket : NetModule
{
    public class AutoSyncReplyData : AutoSyncType<TpReplyData>
    {
        public override object Read(BinaryReader r, MemberInfo fieldInfo)
        {
            return new TpReplyData
            {
                Accepted = r.ReadBoolean(),
                RejectReason = r.ReadBoolean() ? r.ReadString() : null
            };
        }

        public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo)
        {
            var data = (TpReplyData) value;
            bw.Write(data.Accepted);
            bw.Write(data.RejectReason != null);
            if (data.RejectReason != null)
                bw.Write(data.RejectReason);
        }
    }
    
    public struct TpReplyData
    {
        public bool Accepted;
        public string? RejectReason; // 如果被拒绝了，这个字段可以用来存储拒绝理由
    }
    
    public static TpReplyPacket Get(int plr, bool accepted, string? rejectReason = null) {
        if (Main.dedServ) return null;
        var module = NetModuleLoader.Get<TpReplyPacket>();
        module._sender = (byte) Main.LocalPlayer.whoAmI;
        module._receiver = (byte) plr;
        module._replyData = new TpReplyData {
            Accepted = accepted,
            RejectReason = rejectReason
        };
        return module;
    }

    public byte _sender;
    public byte _receiver;
    private TpReplyData _replyData;
    
    public override void Receive() {
        if (Main.netMode is NetmodeID.Server) {
            Send(_receiver); // 服务器将请求发送给目标玩家
            return;
        }
        
        if (Main.netMode is NetmodeID.MultiplayerClient) {
            if (_replyData.Accepted) {
                Main.NewText($"{Main.player[_sender].name} [c/00FF00:接受]了你的传送请求");
                Main.LocalPlayer.UnityTeleport(Main.player[_sender].position);
            }
            else {
                if (_replyData.RejectReason is null) {
                    Main.NewText($"{Main.player[_sender].name} [c/FF0000:拒绝]了你的传送请求，但没有给出理由");
                }
                else {
                    Main.NewText($"{Main.player[_sender].name} [c/FF0000:拒绝]了你的传送请求，ta说：");
                    Main.NewText("> " + _replyData.RejectReason);
                }
            }
        }
    }
}