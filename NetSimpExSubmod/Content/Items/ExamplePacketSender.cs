using System;
using System.Collections.Generic;
using System.Globalization;
using NetSimplified;
using NetSimplifiedExample.Packets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NetSimpExSubmod.Content.Items;

public class ExamplePacketSender : ModItem
{
    private static FlexibleModule _saySmthModule;

    public override void SetStaticDefaults() {
        _saySmthModule = NetModuleLoader.Register(new FlexibleModule("SaySmth",
            self => Main.NewText(self.GetValue<string>(0), Main.DiscoColor),
            [typeof(string)]));
    }

    public override void SetDefaults() {
        Item.damage = 50;
        Item.DamageType = DamageClass.Melee;
        Item.width = 40;
        Item.height = 40;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 6;
        Item.value = Item.buyPrice(silver: 1);
        Item.rare = ItemRarityID.Blue;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
    }

    public override bool CanUseItem(Player player) {
        if (Main.netMode is NetmodeID.Server) {
            _saySmthModule.Set(["下面的包是由附属模组发送的！"]);
            _saySmthModule.Send(player.whoAmI);
            CrossMod.GetExternalModule<InventoryPacket>("NetSimplifiedExample")
                .Set(player.whoAmI)
                .SendAsExternalModule(player.whoAmI);
        }

        return base.CanUseItem(player);
    }

    public override void AddRecipes() {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.DirtBlock, 10);
        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }
}