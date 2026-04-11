using System;
using System.Linq;
using NetSimplifiedExample.Packets;
using Terraria;
using Terraria.ModLoader;

namespace NetSimplifiedExample.Commands;

// 发送请求：/rtp [玩家名字]
// 接受请求：/rtp accept
// 拒绝请求：/rtp reject [拒绝理由]
// 作范例用，没有考虑玩家就叫accept/reject的情况，没有考虑玩家同时收到多个请求的情况
public class TpCommand : ModCommand
{
    public static int PendingRequestTarget = -1; // 这个变量用来存储当前正在等待接受/拒绝的请求的目标玩家的whoAmI
    
    public override void Action(CommandCaller caller, string input, string[] args) {
        if (args.Length < 1)
        {
            caller.Reply("应该有至少一个参数");
            return;
        }

        if (args[0] == "accept") {
            if (PendingRequestTarget is -1) {
                caller.Reply("没有待处理的传送请求");
                return;
            }

            TpReplyPacket.Get(PendingRequestTarget, true).Send();
            PendingRequestTarget = -1;
            return;
        }

        if (args[0] == "reject") {
            if (PendingRequestTarget is -1) {
                caller.Reply("没有待处理的传送请求");
                return;
            }
            
            string? reason = args.Length >= 2 ? args[1] : null;
            TpReplyPacket.Get(PendingRequestTarget, false, reason).Send();
            PendingRequestTarget = -1;
            return;
        }

        Player plr = null;
        foreach (var player in Main.ActivePlayers) {
            if (!player.name.Equals(args[0], StringComparison.OrdinalIgnoreCase)) continue;
            plr = player;
            break;
        }
        if (plr == null) {
            caller.Reply($"没有找到这个玩家：{args[0]}");
            return;
        }
        if (plr.whoAmI == Main.myPlayer) {
            caller.Reply("你不能传送到自己");
            return;
        }
        
        TpRequestPacket.Get(plr.whoAmI).Send();
    }

    public override string Command => "rtp"; // request_teleport
    public override CommandType Type => CommandType.Chat;
}