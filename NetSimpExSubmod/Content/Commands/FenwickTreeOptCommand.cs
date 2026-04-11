using System.Collections.Generic;
using NetSimpExSubmod.Packets;
using NetSimplifiedExample.CustomTypes;
using Terraria.ModLoader;

namespace NetSimpExSubmod.Content.Commands;

// 从客户端向服务器发送 FenwickTreeInt 的查询和修改请求，用于演示：
// - 附属模组中直接使用主模组中定义的 AutoSyncType 和 NetModule 来进行自定义类型的网络通信
// - 实现了 AutoSyncType 的自定义类型对 IEnumerable<T> 的自动支持
// 用法：
// /ftree build <n> <m>             - 构建 n 个 FenwickTreeInt，每个包含 m 个元素，初始值为 0
// /ftree add <idx> <l> <r> <val>   - 对第 idx 个 FenwickTreeInt 的 [l, r] 区间添加 val
// /ftree sum <idx> <l> <r>         - 查询第 idx 个 FenwickTreeInt 的 [l, r] 区间和
// /ftree list                      - 列出当前所有 FenwickTreeInt 的所有元素值
// 另外，使用 caller.Reply 实际上可以不需要专门定义 ReplySumPacket 和 ReplyListPacket 这两个 NetModule 来进行回复，这里只是为了演示
public class FenwickTreeOptCommand : ModCommand
{
    public static List<FenwickTreeInt> Fenwicks = new List<FenwickTreeInt>();

    public override void Action(CommandCaller caller, string input, string[] args) {
        if (args.Length < 1) {
            caller.Reply("至少要包含一个参数");
            return;
        }

        string opt = args[0];
        if (opt == "build") {
            int n = int.Parse(args[1]);
            int m = int.Parse(args[2]);
            Fenwicks.Clear();
            for (int i = 0; i < n; i++) {
                Fenwicks.Add(new FenwickTreeInt(m));
            }

            caller.Reply($"成功构建了 {n} 个 FenwickTreeInt，每个包含 {m} 个元素");
        }
        else if (opt == "add") {
            int idx = int.Parse(args[1]);
            int l = int.Parse(args[2]);
            int r = int.Parse(args[3]);
            int val = int.Parse(args[4]);
            if (idx < 0 || idx >= Fenwicks.Count) {
                caller.Reply("索引越界");
                return;
            }

            Fenwicks[idx - 1].RangeAdd(l - 1, r - 1, val);
            caller.Reply($"成功对第 {idx} 个 FenwickTreeInt 的 [{l}, {r}] 区间添加了 {val}");
        }
        else if (opt == "sum") {
            int idx = int.Parse(args[1]);
            int l = int.Parse(args[2]);
            int r = int.Parse(args[3]);
            if (idx < 0 || idx >= Fenwicks.Count) {
                caller.Reply("索引越界");
                return;
            }

            ReplySumPacket.Send(caller.Player.whoAmI, l, r, Fenwicks[idx - 1]);
        }
        else if (opt == "list") {
            ReplyListPacket.Send(caller.Player.whoAmI, Fenwicks);
        }
        else {
            caller.Reply("未知的操作");
        }
    }

    public override string Command => "ftree";

    // 这个命令由客户端发送，但是在服务器执行，我们只需要实现服务器端对客户端的回复即可
    public override CommandType Type => CommandType.World;
}