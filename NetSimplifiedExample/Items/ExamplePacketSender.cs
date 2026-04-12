using System.Collections.Generic;
using NetSimplified;
using NetSimplifiedExample.Packets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NetSimplifiedExample.Items;

public class ExamplePacketSender : ModItem
{
    public override void SetDefaults() {
        Item.damage = 50;
        Item.DamageType = DamageClass.Melee;
        Item.width = 40;
        Item.height = 40;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 6;
        Item.value = 10000;
        Item.rare = ItemRarityID.Green;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
    }

    public override bool CanUseItem(Player player) {
        if (Main.netMode is NetmodeID.Server) {
            InventoryPacket.Get(player.whoAmI).Send(toClient: player.whoAmI);

            if (CrossMod.TryGetExternalFlexibleModule("NetSimpExSubmod", "SaySmth", out var saySmthModule)) {
                saySmthModule.Set(["检测到加载了 NetSimpExSubmod"]);
                saySmthModule.SendAsExternalModule(toClient: player.whoAmI);
            }
        }

        return base.CanUseItem(player);
    }

    public override void AddRecipes() {
        CreateRecipe()
            .AddIngredient(ItemID.DirtBlock, 10)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}