using System;
using System.Linq;
using NetSimplified;
using NetSimplified.Syncing;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.UI;

namespace NetSimplifiedExample.Packets;

// 使用 AutoSync 特性以使变量自动传输，免去自己写 Send 和 Receive 的功夫
[AutoSync]
public class InventoryPacket : NetModule
{
    // 如果你不想对全部变量进行自动传输，AutoSync 也可以对于某一个单独的变量使用，例如下面这个例子:
    // [AutoSync] private byte _whoAmI;
    // 注意: 这种情况下类不应该使用 AutoSync 特性，否则还是会全部变量自动传输

    private byte _whoAmI;
    private Item _holdItem; // 这个物品会传输 stack 变量，但不会传输 favorite 变量。这是物品传输的默认行为
    [ItemSync(syncStack: false)] private Item[] _items; // 这一数组的物品不会传输 stack 和 favorite 变量

    public static InventoryPacket Get(int plr) {
        var module = NetModuleLoader.Get<InventoryPacket>();
        module._whoAmI = (byte) plr;
        module._holdItem = Main.player[plr].HeldItem;
        module._items = Main.player[plr].inventory[10..20];
        return module;
    }

    public InventoryPacket Set(int plr) {
        _whoAmI = (byte) plr;
        _holdItem = Main.player[plr].HeldItem;
        _items = Main.player[plr].inventory[10..20];
        return this;
    }

    public override void Receive() {
        if (Main.netMode is not NetmodeID.MultiplayerClient) return;

        // 以接收到的物品数组生成聊天框Tag文本 [i:xxx]
        var output = _items.Where(item => !item.IsAir)
            .Aggregate("", (current, item) => current + ItemTagHandler.GenerateTag(item));

        Main.NewText($"你的whoAmI: {_whoAmI}");
        Main.NewText($"你手持的物品: {ItemTagHandler.GenerateTag(_holdItem)}");
        Main.NewText($"你物品栏第二行的物品: {output}");
    }
}